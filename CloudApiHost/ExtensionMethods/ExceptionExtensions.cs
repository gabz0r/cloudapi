using System;
using System.IO;

namespace CloudApiHost.ExtensionMethods
{
	public static class ExceptionExtensions
	{
		public static void WriteToStream (this Exception iException, Stream iOutputStream)
		{
		    var output = new StreamWriter(iOutputStream) {AutoFlush = true};
		    output.Write (iException.Message);
			iOutputStream.Close ();
		}
	}
}

