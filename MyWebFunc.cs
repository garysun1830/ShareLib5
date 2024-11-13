using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Reflection;
using System.Net.Mail;
using System.Runtime.Caching;
using System.ComponentModel;

namespace ShareLib5
{

    public class PageEx : Page
    {
        public StateBag ViewStateEx { get { return base.ViewState; } }
    }

    public static partial class MyFunc
    {
        public static string ReadWeb(string Url, Encoding Code, out string Error)
        {
            string tmp = null;
            return ReadWeb(Url, Code, null, ref tmp, out Error);
        }

        public static string ReadWeb(string Url, Encoding Code, CookieContainer container, ref string LastPage, out string Error)
        {
            return ReadWeb(Url, Code, container, null, 0, ref LastPage, 0, out Error);
        }

        public static string ReadWeb(string Url, string ProxyUrl, int port)
        {
            string Error;
            return ReadWeb(Url, null, ProxyUrl, port, out Error);
        }

        public static string ReadWeb(string Url, Encoding Code, string ProxyUrl, int port)
        {
            string Error;
            return ReadWeb(Url, Code, ProxyUrl, port, out Error);
        }

        public static string ReadWeb(string Url, Encoding Code, string ProxyUrl, int port, out string Error)
        {
            string tmp = null;
            return ReadWeb(Url, Code, null, ProxyUrl, port, ref tmp, 0, out Error);
        }

        public static string ReadWeb(string Url, Encoding Code, string ProxyUrl, int port, int Timeout, out string Error)
        {
            string tmp = null;
            return ReadWeb(Url, Code, null, ProxyUrl, port, ref tmp, Timeout, out Error);
        }

        public static string ReadWeb(string Url, Encoding Code, CookieContainer container, string ProxyUrl, int port, ref string LastPage, int Timeout, out string Error)
        {
            Error = null;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {
                Url = Url.Trim().ToLower();
                if (!Regex.IsMatch(Url, "^https?://", RegexOptions.IgnoreCase))
                    Url = "http://" + Url;
                Url = Url.Replace("&amp;", "&");
                if (string.IsNullOrWhiteSpace(LastPage))
                {
                    LastPage = Url;
                }
                using (WebClientEx web = new WebClientEx(container, LastPage, ProxyUrl, port))
                {
                    if (Code != null)
                        web.Encoding = Code;
                    web.Credentials = CredentialCache.DefaultCredentials;
                    web.Headers.Add("User-Agent: Other");
                    web.Headers.Add("user-agent", " Mozilla/5.0 (Windows NT 6.1; WOW64; rv:25.0) Gecko/20100101 Firefox/25.0");
                    string result = web.DownloadString(Url);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                return "";
            }
        }

        public static string ReadWeb(string Url, Encoding Code)
        {
            string error;
            return ReadWeb(Url, Code, out error);
        }

        public static string ReadWeb(string Url, out string Error)
        {
            return ReadWeb(Url, null, out Error);
        }

        public static string ReadWeb(string Url)
        {
            return ReadWeb(Url, null);
        }

        public static string ReadWeb(string Url, string ProxyUrl, int port, int Timeout, out string Error)
        {
            string LastPage = null;
            return ReadWeb(Url, null, null, ProxyUrl, port, ref LastPage, Timeout, out Error);
        }

        public static void ReadWebToFile(string Url, string FileName)
        {
            ReadWebToFile(Url, FileName, null);
        }

        public static void ReadWebToFile(string Url, string FileName, CookieContainer container)
        {
            Url = Url.Trim();
            if (!Url.ToLower().StartsWith("http://") && !Url.ToLower().StartsWith("https://"))
                Url = "http://" + Url;
            Url = Url.Replace("&amp;", "&");
            WebClient web = new WebClientEx(container);
            web.Credentials = CredentialCache.DefaultCredentials;
                web.DownloadFile(Url, FileName);
        }

        public static string DirectPrintHtml(string Text)
        {
            if (string.IsNullOrEmpty(Text))
                return "";
            string s = Regex.Replace(Text, "^[\\s]+", "");
            s = Regex.Replace(s, "[\\s]+$", "");
            s = s.Replace("\r", "\n").Replace("\n\n", "\n").Replace("\n\n", "\n").Replace("\n\n", "\n");
            s = s.Replace("\n", "<br/>");
            return s;
        }

        public static BrowserKind GetBrowserName()
        {
            string Browser = CurrentRequest().Browser.Browser;
            if (Browser.IndexOf("Firefox", StringComparison.CurrentCultureIgnoreCase) != -1)
                return BrowserKind.FireFox;
            if (Browser.IndexOf("Chrome", StringComparison.CurrentCultureIgnoreCase) != -1)
                return BrowserKind.Chrome;
            if (Browser.IndexOf("Safari", StringComparison.CurrentCultureIgnoreCase) != -1)
                return BrowserKind.Chrome;
            return BrowserKind.IE;
        }

        public static HttpResponse CurrentResponse()
        {
            return HttpContext.Current.Response;
        }

        public static Page CurrentPage()
        {
            Page page = HttpContext.Current.Handler as Page;
            return page == null ? null : page;
        }


        public static HttpServerUtility CurrentServer()
        {
            return HttpContext.Current.Server;
        }

        public static HttpRequest CurrentRequest()
        {
            return HttpContext.Current.Request;
        }

        public static HttpSessionState CurrentSession()
        {
            return HttpContext.Current.Session;
        }

        public static void SaveSessionData(string DataId, object Data)
        {
            if (HttpContext.Current == null)
                return;
            if (HttpContext.Current.Session == null)
                return;
            HttpContext.Current.Session[DataId] = Data;
        }

        public static object GetSessionData(string DataId)
        {
            if (HttpContext.Current == null)
                return null;
            if (HttpContext.Current.Session == null)
                return null;
            return HttpContext.Current.Session[DataId];
        }

        public static string GetSessionData(string DataId, string Default)
        {
            return MyFunc.GetObjectValue(GetSessionData(DataId), Default);
        }

        public static bool GetSessionData(string DataId, bool Default)
        {
            return MyFunc.GetObjectValue(GetSessionData(DataId), Default);
        }

        public static int GetSessionData(string DataId, int Default)
        {
            return MyFunc.GetObjectValue(GetSessionData(DataId), Default);
        }

        public static DateTime GetSessionData(string DataId, DateTime Default)
        {
            return MyFunc.GetObjectValue(GetSessionData(DataId), Default);
        }

        public static void CurrentSessionAbandon()
        {
            CurrentSession().Abandon();
        }

        public static int AutoSelectItem(DataList List, int Index)
        {
            if (List.Items.Count == 0)
                return -1;
            List.SelectedIndex = 0;
            if (Index != 0)
            {
                for (int i = 0; i < List.Items.Count; i++)
                {
                    if (Index == int.Parse(List.DataKeys[i].ToString()))
                    {
                        List.SelectedIndex = i;
                        break;
                    }
                }
            }
            return int.Parse(List.DataKeys[List.SelectedIndex].ToString());
        }

        public static void SetControlSize(WebControl AControl, int Width)
        {
            AControl.Style["Width"] = Width.ToString();
        }

        public static void SetControlSize(WebControl AControl, int Width, int Height)
        {
            SetControlSize(AControl, Width);
            AControl.Style["Height"] = Height.ToString();
        }

        public static string QueryURL(string Arg)
        {
            return QueryURL(CurrentRequest(), Arg);
        }

        public static string QueryURL(string Arg, string Default)
        {
            return MyFunc.GetObjectValue(QueryURL(Arg), Default);
        }

        public static int QueryURL(string Arg, int Default)
        {
            return MyFunc.GetObjectValue(QueryURL(Arg), Default);
        }

        public static bool QueryURL(string Arg, bool Default)
        {
            return MyFunc.GetObjectValue(QueryURL(Arg), Default);
        }

        public static string QueryURL(HttpRequest Request, string Arg)
        {
            return Request.QueryString[Arg];
        }

        public static string PopMessageHtml(object PictureIndex)
        {
            return
                "<a href=\"Message.aspx?Picture=" + PictureIndex.ToString() + "\"\n" +
                "onclick=\"window.open('Message.aspx?Picture=" + PictureIndex.ToString() + "'," +
                "'popup','width=500,height=400,scrollbars=no');return(false);\"> \n" +
                "<img src=\"ask_questions.gif\" " +
                "style=\"BORDER-TOP-STYLE: none; BORDER-RIGHT-STYLE: none; BORDER-LEFT-STYLE: none; " +
                "BORDER-BOTTOM-STYLE: none\"></a>";
        }

        public static bool IsValidEmailAddress(string sEmail)
        {
            if (sEmail == null)
            {
                return false;
            }

            int nFirstAT = sEmail.IndexOf('@');
            int nLastAT = sEmail.LastIndexOf('@');

            if ((nFirstAT > 0) && (nLastAT == nFirstAT) &&
            (nFirstAT < (sEmail.Length - 1)))
            {
                // address is ok regarding the single @ sign
                return (Regex.IsMatch(sEmail, @"(\w+)@(\w+)\.(\w+)"));
            }
            else
            {
                return false;
            }
        }

        public static bool IsGoodEmail(string Text)
        {
            return Regex.IsMatch(Text, @"^[a-zA-Z'.]{1,40}$");
        }

        private static void AddConfirmEvent(Control AControl, string CommandName, string ClientEvent)
        {
            foreach (Control control in AControl.Controls)
                AddConfirmEvent(control, CommandName, ClientEvent);
            LinkButton lkbtn = AControl as LinkButton;
            if (lkbtn != null)
                if (lkbtn.CommandName.Equals(CommandName, StringComparison.OrdinalIgnoreCase))
                {
                    lkbtn.OnClientClick = ClientEvent;
                    return;
                }
            Button btn = AControl as Button;
            if (btn != null)
                if (btn.CommandName.Equals(CommandName, StringComparison.OrdinalIgnoreCase))
                {
                    btn.OnClientClick = ClientEvent;
                    return;
                }
        }

        public static void SetButtonClientEvent(Control AControl, string CommandName, string Message)
        {
            AddConfirmEvent(AControl, CommandName, string.Format("return confirm('{0}');", Message));
        }

        public static HtmlGenericControl FindMainBody(Page webPage)
        {
            foreach (object ctrl in webPage.Controls)
            {
                HtmlGenericControl htmlCtrl = ctrl as HtmlGenericControl;
                if (htmlCtrl != null)
                    if (htmlCtrl.ID == "MainBody")
                        return htmlCtrl;
            }
            return null;
        }

        public static void SetControlCssClass(Control Ctrl, string className)
        {
            foreach (Control c in Ctrl.Controls)
                SetControlCssClass(c, className);
            WebControl webCtrl = Ctrl as WebControl;
            if (webCtrl != null)
                webCtrl.CssClass = className;
        }

        public static void SetPageCssClass(Page webPage, string className)
        {
            foreach (Control c in webPage.Controls)
                SetControlCssClass(c, className);
        }

        public static void PrintVar(string Message)
        {
            CurrentResponse().Write(Message + "<br/>\n");
        }

        public static void PrintVar(string Message, object Value)
        {
            PrintVar(string.Format("{0}={1}", Message, Value));
        }

        public static string GetCurrentPageFileName()
        {
            return Path.GetFileName(CurrentRequest().PhysicalPath);
        }

        public static string UrlEncode(string Url)
        {
            return CurrentServer().UrlEncode(Url);
        }

        public static string UrlDecode(string Url)
        {
            return CurrentServer().UrlDecode(Url);
        }

        private static StateBag GetViewStateBag()
        {
            Page page = CurrentPage();
            if (page == null)
                return null;
            Type type = page.GetType();
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            PropertyInfo pinfo = type.GetProperty("ViewState", bindingFlags);
            if (pinfo == null)
                return null;
            return (StateBag)pinfo.GetValue(page, null);
        }

        public static void SaveViewState(string DataId, object Value)
        {
            StateBag bag = GetViewStateBag();
            if (bag != null)
                bag[DataId] = Value;
        }

        public static object GetViewState(string DataId)
        {
            Page page = HttpContext.Current.Handler as Page;
            if (page == null)
                return null;
            Type type = page.GetType();
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            PropertyInfo pinfo = type.GetProperty("ViewState", bindingFlags);
            if (pinfo == null)
                return null;
            StateBag state = (StateBag)pinfo.GetValue(page, null);
            StateBag bag = GetViewStateBag();
            if (bag != null)
                return bag[DataId];
            return null;
        }

        public static string GetViewState(string DataId, string Default)
        {
            object o = GetViewState(DataId);
            if (o == null)
                return Default;
            return o.ToString();
        }

        public static DateTime GetViewState(string DataId, DateTime Default)
        {
            object o = GetViewState(DataId);
            if (o == null)
                return Default;
            return (DateTime)o;
        }

        public static int GetViewState(string DataId, int Default)
        {
            string s = GetViewState(DataId, "");
            if (string.IsNullOrEmpty(s))
                return Default;
            int.TryParse(s, out Default);
            return Default;
        }


        public static Cookie[] GetAllCookies(CookieContainer Container)
        {
            Cookie[] cookies = new Cookie[0];
            Hashtable table = (Hashtable)Container.GetType().InvokeMember("m_domainTable",
                BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, Container, new object[] { });
            foreach (object pathList in table.Values)
            {
                SortedList lstCookieCol = (SortedList)pathList.GetType().InvokeMember("m_list",
                    BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, pathList, new object[] { });
                foreach (CookieCollection colCookies in lstCookieCol.Values)
                    foreach (Cookie c in colCookies)
                    {
                        int i = cookies.Length;
                        Array.Resize(ref cookies, i + 1);
                        cookies[i] = c;
                    }
            }
            return cookies;
        }

        public static string CurrentPageName()
        {
            string pn = CurrentRequest().Url.AbsolutePath;
            int i = pn.LastIndexOf('/');
            if (i > -1)
                pn = pn.Substring(i + 1);
            return pn;
        }

        private static void DoEmailByGoogle(string SenderEmail, string SendToEmail, string Password, string Subject, string Body, List<string> FileList)
        {
            //we will use Smtp client which allows us to send email using SMTP Protocol
            //i have specified the properties of SmtpClient smtp within{}
            //gmails smtp server name is smtp.gmail.com and port number is 587
            SmtpClient smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(SenderEmail, Password),
                Timeout = 300000
            };
            MailMessage message = new MailMessage(SenderEmail, SendToEmail, Subject, Body);
            if (FileList != null)
                foreach (string fn in FileList)
                    message.Attachments.Add(new Attachment(fn));
            smtp.Send(message);
        }

        public static void EmailByGoogle(string SenderEmail, string SendToEmail, string Password, string Subject, string Body, int Retry)
        {
            EmailByGoogle(SenderEmail, SendToEmail, Password, Subject, Body, Retry, null);
        }

        public static void EmailByGoogle(string SenderEmail, string SendToEmail, string Password, string Subject, string Body, int Retry, List<string> FileList)
        {
            for (int i = 0; i < Retry - 1; i++)
            {
                try
                {
                    DoEmailByGoogle(SenderEmail, SendToEmail, Password, Subject, Body, FileList);
                    return;
                }
                catch (Exception ex)
                {
                    Body = ex.Message;
                }
            }
            DoEmailByGoogle(SenderEmail, SendToEmail, Password, Subject, Body, FileList);
        }

        public static void EmailByGoogle(string SenderEmail, string SendToEmail, string Password, string Subject, string Body, List<string> Files)
        {
            EmailByGoogle(SenderEmail, SendToEmail, Password, Subject, Body, 3, Files);
        }

        public static void EmailByGoogle(string SenderEmail, string SendToEmail, string Password, string Subject, string Body)
        {
            EmailByGoogle(SenderEmail, SendToEmail, Password, Subject, Body, 3);
        }

        public static string GetNameValueFromQueryString(string Text, string Name)
        {
            Match m = Regex.Match(Text, string.Format("{0}=((?!&).)*&", Name), RegexOptions.IgnoreCase);
            if (!m.Success)
                m = Regex.Match(Text, string.Format("{0}=.*", Name), RegexOptions.IgnoreCase);
            if (!m.Success)
                return "";
            return Regex.Replace(m.Value, string.Format("{0}=", Name), "", RegexOptions.IgnoreCase).Replace("&", "");
        }

        public static string GetDataListItemDbValue(Control Item, string ColName, string Default)
        {
            DataListItem container = Item.NamingContainer as DataListItem;
            return MyFunc.ConvertDbValue(DataBinder.Eval(container.DataItem, ColName), Default);
        }

        public static int GetDataListItemDbValue(Control Item, string ColName, int Default)
        {
            DataListItem container = Item.NamingContainer as DataListItem;
            return MyFunc.ConvertDbValue(DataBinder.Eval(container.DataItem, ColName), Default);
        }

        public static DateTime GetDataListItemDbValue(Control Item, string ColName, DateTime Default)
        {
            DataListItem container = Item.NamingContainer as DataListItem;
            return MyFunc.ConvertDbValue(DataBinder.Eval(container.DataItem, ColName), Default);
        }

        private static void SendEmailBySMTP(string Server, int Port, string UserName, string Password, string SenderEmail, string SendToEmail, string Subject, string Body, List<string> FileList)
        {
            //we will use Smtp client which allows us to send email using SMTP Protocol
            //i have specified the properties of SmtpClient smtp within{}
            //gmails smtp server name is smtp.gmail.com and port number is 587

            SmtpClient smtp = new SmtpClient
            {
                Host = Server,
                Port = Port,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 300000
            };
            if (string.IsNullOrEmpty(UserName))
                smtp.Credentials = new NetworkCredential();
            else
                smtp.Credentials = new NetworkCredential(UserName, Password);
            MailMessage message = new MailMessage(SenderEmail, SendToEmail, Subject, Body);
            if (FileList != null)
                foreach (string fn in FileList)
                    message.Attachments.Add(new Attachment(fn));
            smtp.Send(message);
        }

        public static void EmailBySMTP(string Server, int Port, string UserName, string Password, string SenderEmail, string SendToEmail, string Subject, string Body, int Retry, List<string> FileList)
        {
            for (int i = 0; i < Retry - 1; i++)
            {
                try
                {
                    SendEmailBySMTP(Server, Port, UserName, Password, SenderEmail, SendToEmail, Subject, Body, FileList);
                    return;
                }
                catch { }
            }
        }

        public static void EmailBySMTP(string Server, int Port, string UserName, string Password, string SenderEmail, string SendToEmail, string Subject, string Body, int Retry)
        {
            EmailBySMTP(Server, Port, UserName, Password, SenderEmail, SendToEmail, Subject, Body, Retry, null);
        }

        public static object GetSessionCache(string Id)
        {
            if (HttpContext.Current == null)
                return null;
            if (HttpContext.Current.Session == null)
                return null;
            if (HttpContext.Current.Session.IsNewSession)
                return null;
            ObjectCache cache = MemoryCache.Default;
            return cache.Get(HttpContext.Current.Session.SessionID + Id);
        }

        public static void SaveSessionCache(string Id, object Data, double TimeOut)
        {
            if (HttpContext.Current == null)
                return;
            if (HttpContext.Current.Session == null)
                return;
            ObjectCache cache = MemoryCache.Default;
            CacheItemPolicy policy = new CacheItemPolicy();
            policy.AbsoluteExpiration = DateTimeOffset.Now.AddHours(TimeOut);
            cache.Add(HttpContext.Current.Session.SessionID + Id, Data, policy);
        }

        public static void SaveSessionCache(string Id, object Data)
        {
            SaveSessionCache(Id, Data, 0.5);
        }

        public static void RemoveSessionCache(string Id)
        {
            if (HttpContext.Current == null)
                return;
            if (HttpContext.Current.Session == null)
                return;
            ObjectCache cache = MemoryCache.Default;
            cache.Remove(HttpContext.Current.Session.SessionID + Id);
        }

        public static WebClient ReadWebToFileAsync(string Url, string FileName, string ProxyUrl, int port, DownloadProgressChangedEventHandler OnChange, AsyncCompletedEventHandler OnComplete)
        {
            WebClient downloader = new WebClient();
            if (!string.IsNullOrEmpty(ProxyUrl))
            {
                IWebProxy proxy = port == 0 ? new WebProxy(ProxyUrl) : new WebProxy(ProxyUrl, port);
                proxy.Credentials = CredentialCache.DefaultCredentials;
                downloader.Proxy = proxy;
            }
            downloader.DownloadFileCompleted += OnComplete;
            downloader.DownloadProgressChanged += OnChange;
            downloader.DownloadFileAsync(new Uri(Url), FileName);
            return downloader;
        }

        public static WebClient ReadWebToFileAsync(string Url, string FileName, string ProxyUrl, DownloadProgressChangedEventHandler OnChange, AsyncCompletedEventHandler OnComplete)
        {
            return ReadWebToFileAsync(Url, FileName, ProxyUrl, 0, OnChange, OnComplete);
        }

        public static WebClient ReadWebToFileAsync(string Url, string FileName, DownloadProgressChangedEventHandler OnChange, AsyncCompletedEventHandler OnComplete)
        {
            return ReadWebToFileAsync(Url, FileName, null, 0, OnChange, OnComplete);
        }

        public static string ReadWebToStringAsync(string Url, Encoding Code, CookieContainer container, string ProxyUrl, int port, ref string LastPage, int Timeout, out string Error)
        {
            Error = null;
            try
            {
                Url = Url.Trim().ToLower();
                if (!Regex.IsMatch(Url, "^https?://", RegexOptions.IgnoreCase))
                    Url = "http://" + Url;
                Url = Url.Replace("&amp;", "&");
                using (WebClientEx web = new WebClientEx(container, LastPage, ProxyUrl, port))
                {
                    if (Code != null)
                        web.Encoding = Code;
                    web.Credentials = CredentialCache.DefaultCredentials;
                    string result = web.ReadWebToStringAsync(Url, 1000, out Error);
                    LastPage = web.LastPage;
                    return result;
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                return "";
            }
        }

    }

}
