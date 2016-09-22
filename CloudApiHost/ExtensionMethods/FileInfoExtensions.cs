using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiHost.ExtensionMethods
{
    public static class FileInfoExtensions
    {
        public static string CalculateMd5(this FileInfo iFileInfo)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(iFileInfo.FullName))
                    {
                        return Encoding.Default.GetString(md5.ComputeHash(stream));
                    }
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }

        }
    }
}
