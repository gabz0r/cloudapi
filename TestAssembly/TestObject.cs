using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudApiLib.Documents;

namespace TestAssembly
{
    public class TestObject : CADocument<TestObject>
    {
        public string Name { get; set; }

        public decimal Age { get; set; }

        public decimal test1 { get; set; }
        public string test2 { get; set; }

        public List<string> test3 { get; set; }

        public NestedObject nestedTest { get; set; }
     }
}
