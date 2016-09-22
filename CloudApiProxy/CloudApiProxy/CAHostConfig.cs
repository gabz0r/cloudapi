using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiProxy
{
    class CAHostConfig
    {
        public static decimal CLOUD_API_PORT = 9019;
        public static string CLOUD_API_URL = $"http://*:{CLOUD_API_PORT}/cloudapi/";
    }
}
