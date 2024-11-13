using System;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Web;

namespace ShareLib5
{
    public class HtmlTag
    {
        public string Tag;
        public int Index;
        public int Length;
        private bool _good;
        public List<HtmlTag> _children;
        private HtmlTree _tree;

        public HtmlTag(HtmlTree Tree, string Tag, int Index)
        {
            _tree = Tree;
            this.Tag = Tag;
            this.Index = Index;
            _children = new List<HtmlTag>();
        }

        public List<HtmlTag> Children
        {
            get { return _children; }
        }

        public bool Good
        {
            set
            {
                _good = value;
                if (value)
                    return;
                int i = ToString().LastIndexOf("<");
                if (i != -1)
                    Length = i;
            }
            get { return _good; }
        }

        public void AddChild(HtmlTag Child)
        {
            _children.Add(Child);
        }

        //complete <tag...>....</tag>
        public override string ToString()
        {
            return _tree.Html.Substring(Index, Length);
        }

        /*
        public HtmlTag LastChild
        {
            get
            {
                int i=_children.Count;
                if(i==0)
                    return null;
                return _children[i-1];
            }
        }*/

        //<tag ...>...</tag>, result is string between > and <
        public string InnerText()
        {
            int i = ToString().IndexOf('>');
            if (i < 1)
                return "";
            string s = ToString().Substring(i + 1);
            i = s.LastIndexOf('<');
            if (i == -1)
                return s;
            return s.Remove(i);
        }

        /// <summary>
        /// remove each of child's html
        /// </summary>
        /// <returns></returns>
        public string SelfString()
        {
            string result = HtmlTree.RemoveTagAndText(InnerText(), Tag);
            return FullTagBegin() + result + FullTagEnd();
        }
        /// <summary>
        /// if Text is in SelfString()
        /// </summary>
        /// <param name="Text"></param>
        /// <returns></returns>
        private bool SelfContains(string Text)
        {
            return SelfString().IndexOf(Text, StringComparison.OrdinalIgnoreCase) != -1;
        }

        /// <summary>
        /// if Text is in FullTagBegin(), e.g. <tag ...>
        /// </summary>
        /// <param name="Text"></param>
        /// <returns></returns>
        private bool TagContains(string Text)
        {
            return FullTagBegin().IndexOf(Text, StringComparison.OrdinalIgnoreCase) != -1;
        }

        public bool Contains(string Text, bool InTagOnly)
        {
            if (InTagOnly)
                return TagContains(Text);
            else
                return SelfContains(Text);
        }

        //<tag ...>
        public string FullTagBegin()
        {
            string s = ToString();
            int i = s.IndexOf('>');
            if (i < 1)
                return "";
            if (i < s.Length - 1)
                s = s.Remove(i + 1);
            return s;
        }

        //</tag ...>
        public string FullTagEnd()
        {
            int i = ToString().LastIndexOf('<');
            if (i < 1)
                return "";
            string result = ToString().Substring(i);
            if (result.EndsWith(">"))
                return result;
            i = ToString().LastIndexOf('>');
            if (i < 1)
                return "";
            return result.Remove(++i);
        }

        /// <summary>
        /// extract image from <img src="image" ...>
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Src"></param>
        /// <returns></returns>
        public bool ImgSrc(out string Src)
        {
            Src = "";
            if (Tag.ToLower() != "img")
                return false;
            Regex rgx = new Regex("src=[\"']{1}((?!\")(?!').)*[\"']{1}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match m = rgx.Match(ToString());
            if (!m.Success)
                return false;
            rgx = new Regex("src=", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Src = rgx.Replace(m.Value, "");
            Src = Src.Replace("\"", "").Replace("'", "").Trim();
            return true;
        }
    }

    public class TagData
    {
        public string Text;
        public bool Single;

        public TagData(string Item)
        {
            Item = Item.ToLower().Trim();
            Single = Item.EndsWith("/");
            if (Single)
                Item = Item.Remove(Item.Length - 1);
            Text = Item;
        }
    }

    public class TagList
    {
        private List<TagData> _items;

        public TagList(string[] List)
        {
            _items = new List<TagData>();
            foreach (string s in List)
            {
                string t = s.Trim();
                if (t != "")
                    _items.Add(new TagData(t));
            }
        }

        private TagData Find(string Text)
        {
            Text = Text.ToLower().Trim();
            if (Text.StartsWith("/"))
                Text = Text.Remove(0, 1);
            foreach (TagData t in _items)
                if (t.Text == Text)
                    return t;
            return null;
        }

        public bool Exists(string Text)
        {
            return Find(Text) != null;
        }

        public bool IsSingle(string Text)
        {
            TagData t = Find(Text);
            if (t == null)
                return false;
            return t.Single;
        }
    }

    public class HtmlTree
    {
        private string _html;
        private HtmlTag _root;
        private int _pos;
        private Stack _tags;
        private string _begin;
        private TagList _includes;

        public HtmlTree(string Html, string Tags)
        {
            _html = Html;
            if (string.IsNullOrEmpty(Tags))
                Tags = MyFunc.GetWebconfigValue("HtmlTreeTags", "");
            Tags = Tags.ToLower();
            string[] tags = Tags.Split(new char[] { ',' });
            _includes = new TagList(tags);
            _tags = new Stack();
            _root = new HtmlTag(this, "", 0);
            _tags.Push(_root);
            BuildTree();
            _tags.Pop();
        }

        public HtmlTree(string Html)
            : this(Html, "")
        {
        }

        public string Html
        {
            get { return _html; }
        }

        private void BuildTree()
        {
            if (string.IsNullOrEmpty(_html))
                return;
            for (int i = 0; i < _html.Length; i++)
            {
                _pos = i;
                char c = _html[_pos];
                switch (c)
                {
                    case '<':
                        CreateTag();
                        break;
                    case '>':
                        EndTag();
                        break;
                }
            }
            while (_tags.Count > 1)
                CompleteTag(false);
        }

        private void GetBeginTag()
        {
            if (_pos > 0)
                if (_html[_pos - 1] == '\\')
                {
                    _begin = "";
                    return;
                }
            _begin = _pos + 25 < _html.Length ? _html.Substring(_pos + 1, 20) : _html.Substring(_pos + 1);
            _begin = _begin.Trim();
            _begin = _begin.Replace("\t", "").Replace("\n", "").Replace("\r", "");
            for (int i = 0; i < _begin.Length; i++)
            {
                if (_begin[i] == ' ' || _begin[i] == '>' || Convert.ToInt32(_begin[i]) > 127)
                {
                    _begin = _begin.Substring(0, i).Trim().ToLower();
                    string s = _begin;
                    if (s.StartsWith("/"))
                        s = s.Substring(1);
                    if (!_includes.Exists(s))
                    {
                        _begin = "";
                        return;
                    }
                    return;
                }
            }
            _begin = "";
        }

        private void CreateTag()
        {
            GetBeginTag();
            if (_begin == "")
                return;
            if (_begin.StartsWith("/"))
                return;
            HtmlTag tag = new HtmlTag(this, _begin, _pos);
            _tags.Push(tag);
        }

        private string GetEndStr()
        {
            int n = _pos - 10;
            if (n < 0)
                n = 0;
            string result = _html.Substring(n, _pos - n);
            result = result.Replace("\t", "").Replace("\n", "").Replace("\r", "").Trim();
            return result;
        }

        private void CompleteTag(bool Good)
        {
            if (_tags.Count < 2)
                return;
            HtmlTag tag = _tags.Pop() as HtmlTag;
            tag.Length = _pos - tag.Index + 1;
            tag.Good = Good;
            HtmlTag parent = _tags.Peek() as HtmlTag;
            if (parent != null)
                parent.AddChild(tag);
        }

        private void EndTag()
        {
            if (_begin == string.Empty)
                return;
            string begin = _begin;
            _begin = "";
            bool wrong_tag;
            while (true)
            {
                if (!TagCanEnd(begin, out wrong_tag))
                    return;
                if (!wrong_tag)
                {
                    CompleteTag(true);
                    return;
                }
                CompleteTag(false);
            }
        }

        private bool TagCanEnd(string BeginTag, out bool WrongTag)
        {
            WrongTag = false;
            if (_pos == 0)
                return false;
            if (BeginTag == "")
                return false;
            if (_tags.Count < 2)
                return false;
            HtmlTag current = _tags.Peek() as HtmlTag;
            if (current == null)
                return false;
            //Single tag, as <img src="...">
            if (_includes.IsSingle(BeginTag))
                return true;
            string end = GetEndStr();
            //Single tag, as <meta .../>
            if (end.EndsWith("/"))
                return true;
            //Get to the end
            if (_pos == _html.LastIndexOf('>'))
                return true;
            //Comment, <!--...-->
            if (end.EndsWith("--") && BeginTag == "!--")
                return true;
            if (!BeginTag.StartsWith("/"))
                return false;
            //Block tag, as <body></body>
            WrongTag = BeginTag.ToLower() != "/" + current.Tag.ToLower();
            return true;
        }

        private void ItemToList(string Tag, HtmlTag Item, List<HtmlTag> Items)
        {
            if (Item.Good)
                if (string.Compare(Tag, Item.Tag, true) == 0)
                    Items.Add(Item);
            foreach (HtmlTag tag in Item.Children)
                ItemToList(Tag, tag, Items);
        }

        public List<HtmlTag> FindTags(string Tag)
        {
            if (Tag.Contains("/"))
                Tag = Tag.Replace("/", "");
            List<HtmlTag> result = new List<HtmlTag>();
            if (_root == null)
                return result;
            Tag = Tag.Trim();
            ItemToList(Tag, _root, result);
            return result;
        }

        public static List<HtmlTag> FindTags(string Text, string Tag)
        {
            HtmlTree tree = new HtmlTree(Text, Tag);
            return tree.FindTags(Tag);
        }

        /// <summary>
        /// Find all <tag ... Words, Words,...>...</tag>
        /// </summary>
        /// <param name="Tag"></param>
        /// NotUse, seperator
        /// <param name="Words"></param>
        /// <returns>object list</returns>
        public List<HtmlTag> FindTags(string Tag, object NotUse, params string[] Words)
        {
            return FindTags(Tag, false, Words);
        }

        public List<HtmlTag> FindTags(string Tag, bool InTag, params string[] Words)
        {
            List<HtmlTag> result = new List<HtmlTag>();
            List<HtmlTag> items = FindTags(Tag);
            foreach (HtmlTag tag in items)
            {
                bool ok = true;
                foreach (string w in Words)
                    if (!tag.Contains(w, InTag))
                    {
                        ok = false;
                        break;
                    }
                if (ok)
                    result.Add(tag);
            }
            return result;
        }

        public static List<HtmlTag> FindTags(string Text, string Tag, object sep, params string[] Words)
        {
            HtmlTree tree = new HtmlTree(Text, Tag);
            return tree.FindTags(Tag, sep, Words);
        }

        public static List<HtmlTag> FindTags(string Text, string Tag, bool InTag, params string[] Words)
        {
            HtmlTree tree = new HtmlTree(Text, Tag);
            return tree.FindTags(Tag, InTag, Words);
        }

        public static HtmlTag FindFirstTag(string Text, string Tag, bool InTag, params string[] Words)
        {
            List<HtmlTag> tags = FindTags(Text, Tag, InTag, Words);
            if (tags.Count == 0)
                return null;
            return tags[0];
        }

        public static HtmlTag FindFirstTag(string Text, string Tag)
        {
            List<HtmlTag> tags = FindTags(Text, Tag);
            if (tags.Count == 0)
                return null;
            return tags[0];
        }

        /// <summary>
        ///  <tag ... Words ...> Text </tag> with words in self html(not in children's)
        /// </summary>
        /// <param name="Tag"></param>
        /// NotUse, seperator
        /// <param name="Words"></param>
        /// Default, value returned if not successful
        /// <returns>result or Default</returns>
        public string TextOfTag(string Tag, string Default, object NotUse, params string[] Words)
        {
            List<HtmlTag> items = FindTags(Tag, NotUse, Words);
            if (items.Count == 0)
                return Default;
            return items[0].ToString();
        }

        /// <summary>
        /// same as others
        /// </summary>
        /// <param name="Tag"></param>
        /// <param name="Default"></param>
        /// <param name="InTag">Words in <tag...></param>
        /// <param name="Words"></param>
        /// <returns></returns>
        public string TextOfTag(string Tag, string Default, bool InTag, params string[] Words)
        {
            List<HtmlTag> items = FindTags(Tag, InTag, Words);
            if (items.Count == 0)
                return Default;
            return items[0].ToString();
        }

        /// <summary>
        ///  get text between <tag>...</tag> with words in self html(not in children's)
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Tags"></param>
        /// <param name="Tag"></param>
        /// <param name="Default"></param>
        /// <param name="NotUse"></param>
        /// <param name="Words"></param>
        /// <returns></returns>
        public static string TextOfTag(string Text, string Tag, string Default, object NotUse, params string[] Words)
        {
            HtmlTree tree = new HtmlTree(Text, Tag);
            return tree.TextOfTag(Tag, Default, NotUse, Words);
        }

        /// <summary>
        /// Same as others
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Tag"></param>
        /// <param name="Default"></param>
        /// <param name="InTag">Words in <tag...></param>
        /// <param name="Words"></param>
        /// <returns></returns>
        public static string TextOfTag(string Text, string Tag, string Default, bool InTag, params string[] Words)
        {
            HtmlTree tree = new HtmlTree(Text, Tag);
            return tree.TextOfTag(Tag, Default, InTag, Words);
        }

        public string TextOfTag(string Tag, string Default)
        {
            List<HtmlTag> items = FindTags(Tag);
            if (items.Count == 0)
                return Default;
            return items[0].ToString();
        }

        /// <summary>
        /// get text between <tag>...</tag>
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Tags"></param>
        /// <param name="Tag"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        public static string TextOfTag(string Text, string Tag, string Default)
        {
            HtmlTree tree = new HtmlTree(Text, Tag);
            return tree.TextOfTag(Tag, Default);
        }

        /// <summary>
        /// get text of <tag...>, all text 
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Tag"></param>
        /// <returns></returns>
        public static string TextOfBeginTag(string Text, string Tag)
        {
            List<HtmlTag> tags = FindTags(Text, Tag);
            if (tags.Count == 0)
                return "";
            return tags[0].FullTagBegin();
        }

        /// <summary>
        /// remove all <tag ...>  </tag>, nothing or all blanks
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Tag"></param>
        /// <returns></returns>
        private string RemoveBlankTagPair(string Text, string Tag)
        {
            string result = Text;
            List<HtmlTag> list = FindTags(Tag);
            foreach (HtmlTag tag in list)
                if (MyFunc.RemoveCtrlChars(tag.InnerText().Trim()) == "")
                    result = result.Replace(tag.ToString(), "");
            return result;
        }

        public string RemoveBlankHtml(string Tags)
        {
            string result = _html;
            string[] strs = Tags.Split(new char[] { ',' });
            foreach (string tag in strs)
                result = RemoveBlankTagPair(result, tag);
            return result;
        }

        /// <summary>
        /// remove tags if blank is in between, such as <tag>  </tag>
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Tags"></param>
        /// <returns></returns>
        public static string RemoveBlankHtml(string Text, string Tags)
        {
            string result = Text;
            string[] strs = Tags.Split(new char[] { ',' });
            foreach (string tag in strs)
            {
                HtmlTree tree = new HtmlTree(result, tag);
                result = tree.RemoveBlankHtml(tag);
            }
            return result;
        }

        /// <summary>
        /// remove all <tag>, <tag ...>,</tag>, leave text not in <>
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Tag">Tag="" remove all tags</param>
        /// <returns></returns>
        public static string RemoveTag(string Text, string Tag)
        {
            Regex rgx = new Regex("</?" + Tag + "((?!>).)*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return rgx.Replace(Text, "");
        }

        public static string RemoveAllTags(string Text)
        {
            return RemoveTag(Text, "");
        }

        /// <summary>
        /// Remove <tag>...</tag>, all text 
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Tag"></param>
        /// <returns></returns>
        public static string RemoveTagAndText(string Text, string Tags)
        {
            return RemoveTagAndText(Text, Tags, null, null);
        }

        public static string RemoveTagAndText(string Text, string Tags, object sep, params string[] Words)
        {
            return RemoveTagAndText(Text, Tags, false, Words);
        }

        public static string RemoveTagAndText(string Text, string Tags, bool InTag, params string[] Words)
        {
            string result = Text;
            string[] strs = Tags.Split(new char[] { ',' });
            foreach (string s in strs)
            {
                List<HtmlTag> tags = null;
                if (Words == null)
                    tags = FindTags(Text, s);
                else
                    tags = FindTags(Text, s, InTag, Words);
                foreach (HtmlTag tag in tags)
                    result = result.Replace(tag.ToString(), "");
            }
            return result;
        }

        /// <summary>
        /// convert [url=...]...[/url] to <a href="...">...</a>
        /// </summary>
        /// <param name="Text"></param>
        /// <returns></returns>
        public static string UserUrlToRealUrl(string Text)
        {
            Text = Regex.Replace(Text, "\\[/url\\]", "</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            MatchCollection mm = Regex.Matches(Text, "\\[url=((?!\\]).)*\\]", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match m in mm)
            {
                string orginal = m.Value;
                string rep = Regex.Replace(m.Value, "\\[url=", "<a href=", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                rep = Regex.Replace(rep, "\\]", ">", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                Text = Text.Replace(orginal, rep);
            }
            return Text;
        }

        /// <summary>
        /// convert <a href="...">...</a> to [url=...]...[/url] 
        /// </summary>
        /// <param name="Text"></param>
        /// <returns></returns>
        public static string RealUrlToUserUrl(string Text)
        {
            Text = Regex.Replace(Text, "</a>", "[/url]", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            MatchCollection mm = Regex.Matches(Text, "<a[ ]*\\bhref=\"((?!>).)*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match m in mm)
            {
                string orginal = m.Value;
                string rep = Regex.Replace(m.Value, "<a[ ]*\\bhref=", "[url=", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                rep = Regex.Replace(rep, ">", "]", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                Text = Text.Replace(orginal, rep);
            }
            return Text;
        }

        /// <summary>
        /// remove <img scr="..." /> that has no image src
        /// </summary>
        /// <param name="Text"></param>
        /// <returns></returns>
        public string RemoveBlankImage()
        {
            string result = _html;
            List<HtmlTag> list = FindTags("img");
            string src;
            foreach (HtmlTag tag in list)
                if (tag.ImgSrc(out src))
                    if (src == "")
                        result = result.Replace(tag.ToString(), "");
            return result;
        }

        public static string RemoveBlankImage(string Text)
        {
            HtmlTree tree = new HtmlTree(Text, "img/");
            return tree.RemoveBlankImage();
        }

        private static string ExtractImgSrc(string Text, string Ext)
        {
            string pat = string.Format("img((?!src=\")(?!src=').)*src=\"?'?((?!\\.{0}).)*\\.{0}", Ext);
            Regex r = new Regex(pat, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Match m = r.Match(Text);
            if (!m.Success)
                return string.Empty;
            return Regex.Replace(m.Value, "img((?!src=\")(?!src=').)*src=\"?'?", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        private static string ExtractImgSrc(string Text, string Ext, object sep, params string[] Words)
        {
            while (true)
            {
                bool good = true;
                string src = ExtractImgSrc(Text, Ext);
                if (src == string.Empty)
                    return string.Empty;
                if (Words == null)
                    return src;
                foreach (string w in Words)
                    good = good & src.ToLower().Contains(src.ToLower());
                if (good)
                    return src;
                Text = Text.Replace(src, string.Empty);
            }
        }

        public static string ExtractImgSrc(string Text)
        {
            return ExtractImgSrc(Text, (object)null, null);
        }

        public static string ExtractImgSrc(string Text, object sep, params string[] Words)
        {
            string[] IMAGES = { "jpg", "jpeg", "bmp", "gif", "png", "webp" };
            foreach (string s in IMAGES)
            {
                string result = ExtractImgSrc(Text, s, sep, Words);
                if (result != string.Empty)
                    return result;
            }
            return string.Empty;
        }

        /// <summary>
        /// extract url from <a href="url" ...>
        /// </summary>
        /// <param name="Text"></param>
        /// <param name="Src"></param>
        /// <returns></returns>
        public static string ExtractHRef(string Text)
        {
            string result = "";
            Regex rgx = new Regex("href=[\"']{1}((?!\")(?!').)*[\"']{1}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match m = rgx.Match(Text);
            if (!m.Success)
                return "";
            rgx = new Regex("href=", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            result = rgx.Replace(m.Value, "");
            result = result.Replace("\"", "").Replace("'", "").Trim();
            return result;
        }

        public static List<string> FindLinks(string Text)
        {
            List<HtmlTag> tags = FindTags(Text, "a");
            List<string> result = new List<string>();
            foreach (HtmlTag tag in tags)
                result.Add(ExtractHRef(tag.FullTagBegin()));
            return result;
        }

        public static string ExtractNameEquValue(string Text, string Name)
        {
            string result = ExtractNameEquValue(Text, Name, '\"');
            if (string.IsNullOrEmpty(result))
                result = ExtractNameEquValue(Text, Name, '\'');
            return result;
        }

        public static string ExtractNameEquValue(string Text, string Name, char Quote)
        {
            Match m = Regex.Match(Text, string.Format("{0}\\s*=\\s*{1}((?!{1}).)*{1}", Name, Quote), RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!m.Success)
                return "";
            m = Regex.Match(m.Value, string.Format("{0}((?!{0}).)*{0}", Quote, RegexOptions.Singleline));
            if (!m.Success)
                return "";
            return m.Value.Replace(Quote.ToString(), "");
        }

        public static string RemoveTagSection(string Text, string Tag)
        {
            return RemoveTagSection(Text, Tag, (object)null, "");
        }

        public static string RemoveTagSection(string Text, string Tag, object NotUse, params string[] Words)
        {
            List<HtmlTag> tags = HtmlTree.FindTags(Text, Tag, NotUse, Words);
            foreach (HtmlTag tag in tags)
                Text = Text.Replace(tag.ToString(), "");
            return Text;
        }

        private static bool HiddenIsExclude(string Text, string Exclude)
        {
            return string.Compare(ExtractNameEquValue(Text, "name"), Exclude, true) == 0;
        }

        private static string ExtractHiddenValue(string Text, params string[] Exclude)
        {
            Match m = Regex.Match(Text, "name=\"((?!\").)*\"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!m.Success)
                return "";
            if (Exclude != null)
                foreach (string s in Exclude)
                    if (HiddenIsExclude(m.Value, s))
                        return "";
            string name = Regex.Replace(m.Value, "name=", "", RegexOptions.IgnoreCase).Replace("\"", "").Trim();
            if (name == "")
                return "";
            m = Regex.Match(Text, "value=\"((?!\").)*\"", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!m.Success)
                return "";
            string value = Regex.Replace(m.Value, "value=", "", RegexOptions.IgnoreCase).Replace("\"", "").Trim();
            if (value == "")
                return "";
            return string.Format("&{0}={1}", HttpUtility.UrlEncode(name), HttpUtility.UrlEncode(value));
        }

        public static string ExtractHiddenValues(string Text, params string[] Exclude)
        {
            MatchCollection mm = Regex.Matches(Text, "<input type=\"hidden\"\\s+((?!>).)*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            string result = "";
            foreach (Match m in mm)
                result += ExtractHiddenValue(m.Value, Exclude);
            return result;
        }

        public static string ExtractHiddenValues(string Text)
        {
            return ExtractHiddenValues(Text, null);
        }
    }
}


