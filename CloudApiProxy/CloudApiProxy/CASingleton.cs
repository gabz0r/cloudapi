using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiProxy
{
    public class CASingleton<T> where T : class
    {
        private static readonly Lazy<T> InstanceHolder = new Lazy<T>(CreateInstance);
        public static T Instance => InstanceHolder.Value;

        private static T CreateInstance()
        {
            return Activator.CreateInstance(typeof(T), true) as T;
        }
    }
}
