using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 

namespace CloudApiLib
{
    internal class CAConfig
    {
        public static string CLOUD_API_URL = "http://localhost:9091/cloudapi/";

        public static Dictionary<string, Type> RegisteredTypes = new Dictionary<string, Type>();

        public static string CLOUD_API_MODE = "CLIENT";
    }
}
