using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.LightWeight;
using Newtonsoft.Json;

namespace nuge
{
    public class PatientDownloader
    {
        public ILogger Logger { get; set; } = new LoggerAdapter(s => { });
        public string Proxy { get; set; } = null;

        private async Task<DownloadResult<T>> DownloadData<T>(string url, int timeout,
            Func<WebClient, string, Task<T>> pickData)
        {
            Logger.LogDebug("Downloading {Url}...", url);

            Exception exception = null;

            using (var net = new TimeoutWebClient(timeout))
            {
                
                
                if (Proxy != null)
                {
                    Logger.LogDebug("Using Proxy: {Proxy}", Proxy);

                    net.Proxy = new WebProxy(Proxy);
                }

                //net.Headers.Add("Connection","keep-alive");
                
                net.Headers.Add("Accept-Encoding","gzip, deflate");
                
                net.Headers.Add("Accept-Language","en-US,en;q=0.5");
                
                net.Headers.Add("Referer","http://litbid.ir/");

                net.Headers.Add("User-Agent",
                    " Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:105.0) Gecko/20100101 Firefox/105.0");

                try
                {
                    var data = await pickData(net, url);

                    Logger.LogInformation("{Url} Has been downloaded Successfully.", url);

                    return new DownloadResult<T>().Succeed(data);
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }

            return new DownloadResult<T>().Fail(exception);
        }

        public async Task<DownloadResult<byte[]>> DownloadFile(string url, int timeout)
        {
            return await DownloadData(url, timeout, (client, cUrl) => client.DownloadDataTaskAsync(cUrl));
        }

        public async Task<DownloadResult<string>> DownloadString(string url, int timeout)
        {
            return await DownloadData(url, timeout, (client, cUrl) => client.DownloadStringTaskAsync(cUrl));
        }

        public async Task<DownloadResult<T>> DownloadObject<T>(string url, int timeout)
        {
            var result = await DownloadString(url, timeout);

            if (!result)
            {
                return new DownloadResult<T>().Fail(result.Exception);
            }

            try
            {
                var downloadedObject = JsonConvert.DeserializeObject<T>(result.Value);

                return new DownloadResult<T>().Succeed(downloadedObject);
            }
            catch (Exception e)
            {
                return new DownloadResult<T>().Fail(e);
            }
        }

        private async Task<DownloadResult<T>> Retry<T>(string url, int timeout, int tries,
            Func<string, int, Task<DownloadResult<T>>> method)
        {
            DownloadResult<T> result = new DownloadResult<T>().Fail(new Exception());

            int count = 0;

            while (!result.Success && count < tries)
            {
                result = await method(url, timeout);

                if (result)
                {
                    return result;
                }
                Logger.LogDebug("Retrying {Count}",count);
                
                count += 1;
            }
            
            Logger.LogError("Unable to download {Url}, Exception: {Exception}",url,result.Exception);

            return result;
        }


        public async Task<DownloadResult<byte[]>> DownloadFile(string url, int timeout, int retries)
        {
            return await Retry(url, timeout, retries, DownloadFile);
        }
        
        public async Task<DownloadResult<string>> DownloadString(string url, int timeout, int retries)
        {
            return await Retry(url, timeout, retries, DownloadString);
        }
        
        public async Task<DownloadResult<T>> DownloadObject<T>(string url, int timeout, int retries)
        {
            return await Retry(url, timeout, retries, DownloadObject<T>);
        }
        
    }
}