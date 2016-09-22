using System.Collections.Generic;

namespace CloudApiHost.ExtensionMethods
{
	public static class DictionaryExtensions
	{
		public static Dictionary<string, string> FromQueryString (this Dictionary<string, string> iDict, string iQueryString)
		{
			var kvpList = iQueryString.Split (new[] { '&' });
			var retDict = new Dictionary<string, string> ();

			foreach (var kvpString in kvpList) {
				var kvp = kvpString.Split (new[] { '=' });
				if (kvp.Length == 2) {
					retDict.Add (kvp [0], kvp [1]);
				}
			}

			return retDict;
		}
	}
}

