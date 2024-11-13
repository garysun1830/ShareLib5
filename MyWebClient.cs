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
    public class MyWebClient : WebClient
    {
        public int timeOut { get; set; }

        public MyWebClient(int Timeout)
        {
            timeOut = Timeout;
        }
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest webRequest = base.GetWebRequest(uri);
            if (timeOut == 0)
                return webRequest;
            webRequest.Timeout = timeOut;
            ((HttpWebRequest)webRequest).ReadWriteTimeout = timeOut;
            return webRequest;
        }
    }


}
