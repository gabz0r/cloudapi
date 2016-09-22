using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace CloudApiHost.MongoDb
{
    class CABsonMapper
    {
        public static IEnumerable<T>  Find<T>(string iJsonQuery, string iCollectionName)
        {
            var items = CAMongoManager.Instance.GetCollection<T>(iCollectionName).Find(BsonSerializer.Deserialize<BsonDocument>(iJsonQuery));
            return items.ToEnumerable();
        }
    }
}
