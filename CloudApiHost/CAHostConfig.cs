using System;
using System.Reflection;

namespace CloudApiHost
{
	public static class CAHostConfig
	{
	    public static int CLOUD_API_HOST_BUILD => Assembly.GetExecutingAssembly().GetName().Version.Build;

	    public static string CLOUD_API_ASSEMBLY_PATH = "modules/";

        public static string CLOUD_API_ASSEMBLY_NAME = "CtrlBusinessLib.dll";
	    public static decimal CLOUD_API_PORT = 9091;
        public static string CLOUD_API_BIND_URL = $"http://*:{CLOUD_API_PORT}/cloudapi/";
	    public static string CLOUD_API_URL = $"http://localhost:{CLOUD_API_PORT}/cloudapi/";

        private static string MONGO_DB_USER = "ctrl";
        private static string MONGO_DB_PASS = "ctrl";
        public static string MONGO_DB_URI = $"mongodb://{MONGO_DB_USER}:{MONGO_DB_PASS}@ds023303.mlab.com:23303/ctrl_dev";
	    public static string MONGO_DB_DATABASE = "ctrl_dev";
	}
}