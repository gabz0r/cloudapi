using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiLib
{
    public class CloudApi
    {
        public static void Initialize(string iBackendHost, string iMode = "CLIENT")
        {
            if (!string.IsNullOrEmpty(iBackendHost))
                CAConfig.CLOUD_API_URL = iBackendHost;

            CAConfig.CLOUD_API_MODE = iMode;
        }

        public static void RegisterObjectType<T>()
        {
            if (!CAConfig.RegisteredTypes.ContainsKey(typeof (T).Name))
            {
                CAConfig.RegisteredTypes.Add(typeof(T).Name, typeof(T));
            }
        }
    }
}
