using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiProxy
{
    public static class DictionaryExtensions
    {
        public static Dictionary<string, string> FromQueryString(this Dictionary<string, string> iDict, string iQueryString)
        {
            var kvpList = iQueryString.Split(new[] { '&' });
            var retDict = new Dictionary<string, string>();

            foreach (var kvpString in kvpList)
            {
                var kvp = kvpString.Split(new[] { '=' });
                if (kvp.Length == 2)
                {
                    retDict.Add(kvp[0], kvp[1]);
                }
            }

            return retDict;
        }
    }
}
