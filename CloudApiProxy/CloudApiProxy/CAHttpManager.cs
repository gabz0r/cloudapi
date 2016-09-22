using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiProxy
{
    internal class CAHttpManager : CASingleton<CAHttpManager>
    {
        public async Task<string> HttpPostAsync(string iUrl, string iParameters)
        {
            var req = (HttpWebRequest) WebRequest.Create(iUrl);
            //Add these, as we're doing a POST
            req.ContentType = "cloudapi/body-encoded";
            req.Method = "POST";

            var stream = req.GetRequestStream();

            var data = Encoding.UTF8.GetBytes(iParameters);

            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
            stream.Close();

            using (var response = await req.GetResponseAsync())
            {
                using (var respStream = response.GetResponseStream())
                {
                    var sr = new StreamReader(respStream);
                    var retDt = sr.ReadToEnd();

                    return retDt;
                }
            }
        }
    }
}