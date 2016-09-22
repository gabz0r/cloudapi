using System;
using System.Collections.Generic;
using CloudApiHost.Helper;
using MongoDB.Driver;

namespace CloudApiHost.MongoDb
{
	public class CAMongoManager : CASingleton<CAMongoManager>
	{
		private MongoClient DataClient { get; set; }

		private IMongoDatabase Database { get; set; }

		public bool Connect ()
		{
			DataClient = new MongoClient (CAHostConfig.MONGO_DB_URI);
			if (DataClient == null)
				return false;

            Console.WriteLine("Connected to database host");

			Database = DataClient.GetDatabase (CAHostConfig.MONGO_DB_DATABASE);

            Console.WriteLine("Resolved database: {0}", CAHostConfig.MONGO_DB_DATABASE);
			return Database != null;
		}

		public IMongoCollection<T> GetCollection<T> ()
		{
			if (Database == null)
				throw new Exception ("Not connected to Mongo!");
			return Database.GetCollection<T> (typeof(T).Name);
		}

		public IMongoCollection<T> GetCollection<T> (string iTypeName)
		{
			if (Database == null)
				throw new Exception ("Not connected to Mongo!");
			return Database.GetCollection<T> (iTypeName);
		}

	    public Dictionary<string, CACollectionTrigger> CollectionTriggers;
	}
}

