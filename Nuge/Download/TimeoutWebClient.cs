using System;
using System.Net;

namespace nuge
{
    public class TimeoutWebClient:WebClient
    {
        private int TimeOut { get; }

        public TimeoutWebClient(int timeout)
        {
            TimeOut = timeout;
        }

        public TimeoutWebClient()
        {
            TimeOut = 1000;
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            if (w != null)
            {
                w.Timeout = TimeOut;
                
                if(w is HttpWebRequest request)
                {
                    request.AllowAutoRedirect = true;
                    
                    request.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                    
                    request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                }
                return w;
            }

            return null;
        }
       
        
        
    }
}