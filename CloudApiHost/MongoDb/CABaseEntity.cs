using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace CloudApiHost.MongoDb
{
	public abstract class CABaseEntity<T>
	{
		[BsonRepresentation (BsonType.ObjectId)]
		public string Id { get; set; }

		public abstract void Save ();

		public abstract void Update ();

		public static IEnumerable<T> GetAll ()
		{
			return CAMongoManager.Instance.GetCollection<T> ().AsQueryable ().AsEnumerable ();
		}

		public static IEnumerable<T> Get (Func<T, bool> iCondition)
		{
			return CAMongoManager.Instance.GetCollection<T> ().AsQueryable ().Where (iCondition);
		}

		public static T GetSingle (Func<T, bool> iCondition)
		{
			return CAMongoManager.Instance.GetCollection<T> ().AsQueryable ().FirstOrDefault (iCondition);
		}
	}
}

