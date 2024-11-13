using System;
using System.Collections;
using System.Drawing;
using System.Security.Cryptography;
using System.Web.Configuration;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Web.Security;
using System.Configuration;

namespace ShareLib5
{

    public enum BrowserKind { IE, FireFox, Chrome, Opera };

    public class ExceptionEx : Exception
    {
    }

    public class ArrayListEx : ArrayList
    {
        public string Text
        {
            get
            {
                return ToText();
            }
            set
            {
                FromText(value);
            }
        }

        private void FromText(string Text)
        {
            Clear();
            if (Text == "")
                return;
            string[] Strs = Text.Split("\n".ToCharArray());
            foreach (string s in Strs)
                Add(s);
        }

        private string ToText()
        {
            string Result = "";
            foreach (object o in this)
                Result += o.ToString() + "\n";
            return Result;
        }

    }

    public static partial class MyFunc
    {
        private static Hashtable FHtmlTagTable;
        private static readonly string MY_RSA_KEY = "BEIJINGVICTORIAANDREWLUELLA";

        public static void Init()
        {
            if (FHtmlTagTable != null)
                return;
            FHtmlTagTable = new Hashtable();
            FHtmlTagTable.Add("[[&br]]", "<br>");
            FHtmlTagTable.Add("[[&space]]", "&nbsp;");
            FHtmlTagTable.Add("[[*", "<");
            FHtmlTagTable.Add("*]]", ">");
        }

        public static string TextToHtmlTag(string sText)
        {
            foreach (object key in FHtmlTagTable.Keys)
                sText = sText.Replace(key.ToString(), FHtmlTagTable[key].ToString());
            return sText;
        }

        public static string HtmlTagToText(string sText)
        {
            foreach (object key in FHtmlTagTable.Keys)
                sText = sText.Replace(FHtmlTagTable[key].ToString(), key.ToString());
            return sText;
        }

        public static Color GetColorByNameHex(object ColorName, object ColorHex)
        {
            if (ColorName != null)
                if (!(ColorName is DBNull))
                    return Color.FromName(ColorName.ToString());
            if (ColorHex == null)
                return Color.Empty;
            if (ColorHex is DBNull)
                return Color.Empty;
            string r = ColorHex.ToString().Substring(0, 2);
            string g = ColorHex.ToString().Substring(2, 2);
            string b = ColorHex.ToString().Substring(4, 2);
            Color color = Color.Empty;
            try
            {
                int ri
                    = Int32.Parse(r, System.Globalization.NumberStyles.HexNumber);
                int gi
                    = Int32.Parse(g, System.Globalization.NumberStyles.HexNumber);
                int bi
                    = Int32.Parse(b, System.Globalization.NumberStyles.HexNumber);
                color = Color.FromArgb(ri, gi, bi);
            }
            catch
            {
                return Color.Empty;
            }
            return color;
        }

        public static string MD5Hash(string Password)
        {
            byte[] data = new byte[Password.Length];
            for (int i = 0; i < Password.Length; i++)
                data[i] = (byte)Password[i];
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(data);
            string s = "";
            for (int i = 0; i < result.Length; i++)
                s += result[i].ToString();
            return s;
        }

        public static string Encrypt(string toEncrypt, bool useHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            string key = MY_RSA_KEY;
            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                hashmd5.Clear();
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(key);

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            tdes.Clear();
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }
        /// <summary>
        /// DeCrypt a string using dual encryption method. Return a DeCrypted clear string
        /// </summary>
        /// <param name="cipherString">encrypted string</param>
        /// <param name="useHashing">Did you use hashing to encrypt this data? pass true is yes</param>
        /// <returns></returns>
        public static string Decrypt(string cipherString, bool useHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(cipherString);

            string key = MY_RSA_KEY;
            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                hashmd5.Clear();
            }
            else
                keyArray = UTF8Encoding.UTF8.GetBytes(key);

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            tdes.Clear();
            return UTF8Encoding.UTF8.GetString(resultArray);
        }

        public static string GetObjectValue(object Obj, string Default)
        {
            if (Obj == null)
                return Default;
            if (string.IsNullOrEmpty(Obj.ToString()))
                return Default;
            return Obj.ToString();
        }

        public static int GetObjectValue(object Obj, int Default)
        {
            int result = Default;
            if (int.TryParse(GetObjectValue(Obj, ""), out result))
                return result;
            return Default;
        }

        public static double GetObjectValue(object Obj, double Default)
        {
            double result = Default;
            if (double.TryParse(GetObjectValue(Obj, ""), out result))
                return result;
            return Default;
        }

        public static bool GetObjectValue(object Obj, bool Default)
        {
            bool result = Default;
            if (bool.TryParse(GetObjectValue(Obj, ""), out result))
                return result;
            return Default;
        }

        public static DateTime GetObjectValue(object Obj, DateTime Default)
        {
            if (Obj == null)
                return Default;
            try { return Convert.ToDateTime(Obj); }
            catch { }
            return Default;
        }

        public static Decimal GetObjectValue(object Obj, Decimal Default)
        {
            Decimal result = Default;
            if (Decimal.TryParse(GetObjectValue(Obj, ""), out result))
                return result;
            return Default;
        }

        public static object GetWebconfigValue(string Key)
        {
            return WebConfigurationManager.AppSettings[Key];
        }

        public static string GetWebconfigValue(string Key, string Default)
        {
            return GetObjectValue(WebConfigurationManager.AppSettings[Key], Default);
        }

        public static int GetWebconfigValue(string Key, int Default)
        {
            return GetObjectValue(WebConfigurationManager.AppSettings[Key], Default);
        }

        public static bool GetWebconfigValue(string Key, bool Default)
        {
            return GetObjectValue(WebConfigurationManager.AppSettings[Key], Default);
        }

        public static double GetWebconfigValue(string Key, double Default)
        {
            return GetObjectValue(WebConfigurationManager.AppSettings[Key], Default);
        }

        public static string GetObjectToken(object[] Objects)
        {
            if (Objects == null)
                return "";
            StringBuilder sb = new StringBuilder();
            foreach (object o in Objects)
            {
                string s = o == null ? "" : o.ToString();
                sb.Append(s);
                sb.Append(",");
            }
            return sb.ToString();
        }

        public static byte[] Serialize(object Data)
        {
            Stream stream = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(stream, Data);
            byte[] array = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(array, 0, (int)stream.Length);
            stream.Close();
            return array;
        }

        public static object Deserialize(byte[] Data)
        {
            if (Data.Length == 0)
                return null;
            MemoryStream stream = new MemoryStream(Data);
            BinaryFormatter bf = new BinaryFormatter();
            object result = bf.Deserialize(stream);
            stream.Close();
            return result;
        }

        public static string SerializeToString(object Data)
        {
            byte[] bytes = Serialize(Data);
            return Convert.ToBase64String(bytes);
        }

        public static object DeserializeFromString(string Data)
        {
            byte[] bytes = Convert.FromBase64String(Data);
            return Deserialize(bytes);
        }

        public static string WordFirstUppercase(string Text)
        {
            if (string.IsNullOrEmpty(Text))
                return string.Empty;
            char[] letters = Text.ToCharArray();
            letters[0] = char.ToUpper(letters[0]);
            return new string(letters);
        }

        public static string SentenceWordFirstUppercase(string Text)
        {
            if (string.IsNullOrEmpty(Text))
                return Text;
            string result = "";
            for (int i = Text.Length - 1; i > 0; i--)
            {
                char c = Text[i];
                switch (Text[i - 1])
                {
                    case ' ':
                    case ',':
                    case '\t':
                        c = char.ToUpper(c);
                        break;
                }
                result = c + result;
            }
            return char.ToUpper(Text[0]).ToString() + result;
        }

        public static void SaveToFile(string Text, string FileName)
        {
            string dir = Path.GetDirectoryName(FileName);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(FileName, Text);
        }

        public static void SaveToFile(string Text, string FileName, Encoding encoding)
        {
            string dir = Path.GetDirectoryName(FileName);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(FileName, Text, encoding);
        }

        public static void AppendToFile(string Text, string FileName)
        {
            if (!File.Exists(FileName))
            {
                SaveToFile(Text, FileName);
                return;
            }
            using (StreamWriter file = File.AppendText(FileName))
            {
                file.WriteLine(Text);
            }
        }

        public static string LoadFromTextFile(string FileName, Encoding encoding)
        {
            return File.ReadAllText(FileName, encoding);
        }

        public static string LoadFromTextFile(string FileName)
        {
            return File.ReadAllText(FileName);
        }

        public static string GetFirstString(List<string> List, string Default)
        {
            if (List == null)
                return Default;
            if (List.Count == 0)
                return Default;
            return List[0];
        }

        public static string RemoveCtrlChars(string Text)
        {
            Regex rgx = new Regex("\\s");
            return rgx.Replace(Text, "");
        }

        public static string TrimTextAll(string Text)
        {
            string result = Text.Replace("\r", "\n");
            result = Regex.Replace(result, "\\n\\s+\\n", "\n");
            result = Regex.Replace(result, "\n+", "\n").Trim();
            result = Regex.Replace(result, "&\\w+;", "");
            return result;
        }

        public static string QuoteEncode(string Text)
        {
            return Text.Replace("\"", "\"\"");
        }

        public static int GetAppConfigValue(string Prop, int Default)
        {
            string s = GetAppConfigValue(Prop, "");
            if (string.IsNullOrEmpty(s))
                return Default;
            int i = Default;
            if (!int.TryParse(s, out i))
                return Default;
            return i;
        }

        public static string GetAppConfigValue(string Prop, string Default)
        {
            string s = ConfigurationManager.AppSettings[Prop];
            if (string.IsNullOrEmpty(s))
                return Default;
            return s;
        }

        public static void SaveAppConfigValue(string Prop, int Value)
        {
            SaveAppConfigValue(Prop, Value.ToString());
        }

        public static void SaveAppConfigValue(string Prop, string Value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Remove(Prop);
            config.AppSettings.Settings.Add(Prop, Value);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static bool ConvertDbValue(object Value, bool Default)
        {
            if (Convert.IsDBNull(Value))
                return Default;
            return MyFunc.GetObjectValue(Value, Default);
        }

        public static string ConvertDbValue(object Value, string Default)
        {
            if (Convert.IsDBNull(Value))
                return Default;
            return MyFunc.GetObjectValue(Value, Default);
        }

        public static int ConvertDbValue(object Value, int Default)
        {
            if (Convert.IsDBNull(Value))
                return Default;
            return MyFunc.GetObjectValue(Value, Default);
        }

        public static DateTime ConvertDbValue(object Value, DateTime Default)
        {
            if (Convert.IsDBNull(Value))
                return Default;
            return MyFunc.GetObjectValue(Value, Default);
        }

        public static decimal ConvertDbValue(object Value, decimal Default)
        {
            if (Convert.IsDBNull(Value))
                return Default;
            return MyFunc.GetObjectValue(Value, Default);
        }

        public static double ConvertDbValue(object Value, double Default)
        {
            if (Convert.IsDBNull(Value))
                return Default;
            return MyFunc.GetObjectValue(Value, Default);
        }

    }

}
