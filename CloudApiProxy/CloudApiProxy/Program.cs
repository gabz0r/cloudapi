using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            CAHttpServer.Instance.Begin();
            Console.ReadKey();
        }
    }
}
