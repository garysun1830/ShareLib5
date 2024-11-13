using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ShareLib5
{
    public class WebClientEx : WebClient
    {

        private DateTime lastSave;
        private bool downloading;
        private bool downError;
        private string downMessage;
        private StringBuilder downloadText;
        private CookieContainer container;
        public string LastPage { set; get; }

        public WebClientEx(CookieContainer Container)
        {
            container = Container;
            Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            Headers.Add("Accept-Language", "zh-CN,en-US;q=0.7,en;q=0.3");
            Headers.Add("User-Agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)");
        }

        public WebClientEx(CookieContainer Container, string LastPage, string ProxyUrl, int port) : this(Container)
        {
            this.LastPage = LastPage;
            if (!string.IsNullOrEmpty(ProxyUrl))
            {
                IWebProxy proxy = port == 0 ? new WebProxy(ProxyUrl) : new WebProxy(ProxyUrl, port);
                proxy.Credentials = CredentialCache.DefaultCredentials;
                Proxy = proxy;
            }
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest r = base.GetWebRequest(address);
            if (container != null)
            {
                var request = r as HttpWebRequest;
                if (request != null)
                {
                    request.CookieContainer = container;
                    request.Referer = LastPage;
                }
            }
            LastPage = address.ToString();
            return r;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            WebResponse response = base.GetWebResponse(request, result);
            if (container != null)
                ReadCookies(response);
            return response;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = base.GetWebResponse(request);
            if (container != null)
                ReadCookies(response);
            return response;
        }

        private void ReadCookies(WebResponse r)
        {
            var response = r as HttpWebResponse;
            if (response != null)
            {
                CookieCollection cookies = response.Cookies;
                container.Add(cookies);
            }
        }
        public string ReadWebToStringAsync(string Url, int Timeout, out string Error)
        {
            Error = null;
            downloadText = new StringBuilder();
            DownloadStringAsync(new Uri(Url));
            DateTime lastSave = DateTime.Now;
            while (true)
            {

            }
            while (downloading)
            {
                if (downError || (DateTime.Now > lastSave.AddMilliseconds(Timeout)))
                {
                    return null;
                    Error = "Timeout.";
                    CancelAsync();
                }
            }
            return downloadText.ToString();
        }

        protected override void OnDownloadProgressChanged(DownloadProgressChangedEventArgs e)
        {
            base.OnDownloadProgressChanged(e);
            lastSave = DateTime.Now;
        }

        protected override void OnDownloadStringCompleted(DownloadStringCompletedEventArgs e)
        {
            base.OnDownloadStringCompleted(e);
            if (!downloading)
                return;
            if (!downError)
            {
                downError = e.Cancelled || e.Error != null;
                if (e.Error != null)
                {
                    downMessage = e.Error.Message;
                }
            }
            downloading = false;
        }

    }

}
