using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudApiProxy
{
    public class CAHttpServer : CASingleton<CAHttpServer>
    {
        private readonly HttpListener _listener;

        public CAHttpServer()
        {
            Console.WriteLine("Hosting server on port {0}", CAHostConfig.CLOUD_API_PORT);
            _listener = new HttpListener();
            _listener.Prefixes.Add(CAHostConfig.CLOUD_API_URL);
        }

        public void Begin()
        {
            if (_listener == null)
                return;

            _listener.Start();
            _listener.BeginGetContext(OnHttpRequest, _listener);
            Console.WriteLine("Server running");
        }

        public void OnHttpRequest(IAsyncResult iResult)
        {
            var asyncState = iResult.AsyncState as HttpListener;
            if (asyncState != null)
            {
                var requestContext = asyncState.EndGetContext(iResult);

                try
                {
                    var body = new StreamReader(requestContext.Request.InputStream).ReadToEnd();
                    
                    ThreadPool.QueueUserWorkItem(ctx =>
                    {
                        Process(body, (HttpListenerResponse)ctx);
                    }, requestContext.Response);

                    asyncState.BeginGetContext(OnHttpRequest, asyncState);
                }
                catch (Exception ex)
                {
                    requestContext.Response.Close();
                    asyncState.BeginGetContext(OnHttpRequest, asyncState);
                }
            }
        }

        public async void Process(string iData, HttpListenerResponse iResponse)
        {
            var bodyList = iData.Split('#');
            var bodyHead = bodyList[0];
            var bodyBody = bodyList[1];

            var auth = new Dictionary<string, string>().FromQueryString(bodyHead)["AUTH"];
            Console.WriteLine("REQ {0}", auth);
            var hostName = GetHostFromAuthKey(auth);

            var bodyHeadNoAuth = bodyHead.Substring(63);
            var resp = await CAHttpManager.Instance.HttpPostAsync(hostName, $"{bodyHeadNoAuth}#{bodyBody}");

            SendResponse(resp, 200, iResponse);
        }

        public string GetHostFromAuthKey(string iKey)
        {
            var appKey = iKey.Substring(0, 32);
            var appId = iKey.Substring(31, 32);
            //Ermittle Host
            return "http://127.0.0.1:9091/cloudapi/";
        }

        private static void SendResponse(string iData, decimal iStatusCode, HttpListenerResponse iResponse)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(iData);
            iResponse.ContentLength64 = buffer.Length;
            iResponse.ContentType = "application/json";

            iResponse.StatusCode = Convert.ToInt32(iStatusCode);
            iResponse.OutputStream.Write(buffer, 0, buffer.Length);
            iResponse.OutputStream.Close();

            iResponse.Close();
        }
    }
}
