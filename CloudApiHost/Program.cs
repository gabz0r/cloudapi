using System;
using CloudApiHost.AssemblyManager;
using CloudApiHost.FileWatchdog;
using CloudApiHost.HttpManager;
using CloudApiHost.MongoDb;
using CloudApiLib;
using MongoDB.Bson;

namespace CloudApiHost
{
	class MainClass
	{
		public static void Main (string[] args)
		{
            CloudApi.Initialize("http://localhost:9091/cloudapi/", "SERVER");

            Console.WriteLine("CloudApiHost (b/{0})", CAHostConfig.CLOUD_API_HOST_BUILD);
            Console.WriteLine("===========================");
            CAMongoManager.Instance.Connect();

            Console.WriteLine("New assembly has been deployed!");
            CAAssemblyLoader.LoadFromFile(CAHostConfig.CLOUD_API_ASSEMBLY_PATH + CAHostConfig.CLOUD_API_ASSEMBLY_NAME);

            CAHttpServer.Instance.Begin();
            Console.WriteLine("===========================");
            Console.ReadKey ();
		}
	}
}
