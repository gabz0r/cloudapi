using System;
using System.Net;
using CloudApiHost.Helper;

namespace CloudApiHost.HttpManager
{
	public class CAHttpServer : CASingleton<CAHttpServer>
	{
		private readonly HttpListener _listener;

		public CAHttpServer ()
		{
			Console.WriteLine ("Hosting server on port {0}", CAHostConfig.CLOUD_API_PORT);
			_listener = new HttpListener ();
			_listener.Prefixes.Add (CAHostConfig.CLOUD_API_BIND_URL);
		}

		public void Begin ()
		{
			if (_listener == null)
				return;

			_listener.Start ();
			_listener.BeginGetContext (CAHttpAsyncCallbacks.OnHttpRequest, _listener);
			Console.WriteLine ("Server running");
		}
	}
}

