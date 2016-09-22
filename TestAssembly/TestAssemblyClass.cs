using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using CloudApiLib;
using CloudApiLib.Triggers;
using MongoDB.Bson;

namespace TestAssembly
{
	public class TestAssemblyClass
	{
	    public TestAssemblyClass()
	    {
	        CloudApi.RegisterObjectType<TestObject>();
            CloudApi.RegisterObjectType<NestedObject>();
	    }

	    static TestObject TestObj;

		public void DeleteTestObject()
		{
		    TestObj?.Delete();
		}

	    public string CreateTestObject()
	    {
            TestObj = new TestObject
            {
                test1 = 1,
                test2 = "Hallo"
            };
	        TestObj.Save();
	        return TestObj.Id.ToString();
	    }

	    public void WriteDbgLine(string iLine)
	    {
	        Console.WriteLine(iLine);
	    }

	    public int TestReturnValue()
	    {
	        return 1;
	    }

        //   [CATrigger(CATriggerType.PreCreate, "TestObject")]
        //public TestObject TestPreCreate(TestObject iDocument)
        //   {
        //    Console.WriteLine("PreCreate Trigger has been triggered!");
        //       return iDocument;
        //   }

        //   [CATrigger(CATriggerType.PostCreate, "TestObject")]
        //public void TestPostCreate(TestObject iDocument)
        //{
        //    Console.WriteLine("PostCreate Trigger has been triggered!");
        //}

        [CATrigger(CATriggerType.PreUpdate, "TestObject")]
        public TestObject TestPreUpdate(TestObject iOldDoc, TestObject iNewDoc)
        {
            Console.WriteLine("PreUpdate Trigger has been triggered!");
            return iNewDoc;
        }

        [CATrigger(CATriggerType.PostUpdate, "TestObject")]
        public void TestPostUpdate(TestObject iOldDoc, TestObject iNewDoc)
        {
            Console.Write("PostUpdate trigger has been triggered!");
        }

        //   [CATrigger(CATriggerType.PreDelete, "TestObject")]
        //public TestObject TestPreDelete(TestObject iOldDoc)
        //{
        //       Console.Write("PreDelete trigger has been triggered!");
        //       return iOldDoc;
        //}

        //   [CATrigger(CATriggerType.PostDelete, "TestObject")]
        //public void TestPostDelete(TestObject iOldDoc)
        //{
        //       Console.Write("PostDelete trigger has been triggered!");
        //}
    }
}

