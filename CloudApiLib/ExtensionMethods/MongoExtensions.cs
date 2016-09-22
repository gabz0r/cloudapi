using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CloudApiLib.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace CloudApiLib.ExtensionMethods
{
    public static class MongoExtensions
    {
        public static BsonDocument RenderToBsonDocument<T>(this FilterDefinition<T> filter)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<T>();
            return filter.Render(documentSerializer, serializerRegistry);
        }

        internal static object MapType(this BsonValue iValue, Type iTargetType)
        {
            switch (iValue.BsonType)
            {
                case BsonType.ObjectId:
                    return iValue.AsObjectId;
                case BsonType.String:
                    if (iTargetType == typeof(decimal))
                    {
                        return decimal.Parse(iValue.AsString);
                    }
                    if (iTargetType == typeof (Dictionary<string, string>))
                    {
                        var retDict = new Dictionary<string, string>();

                        var pairs = iValue.AsString.Split(new [] { "$§" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var kv in pairs)
                        {
                            var kvArray = kv.Split(new[] { "§$" }, StringSplitOptions.None);
                            retDict.Add(kvArray[0], kvArray[1]);
                        }

                        return retDict;
                    }
                    return iValue.AsString;
                case BsonType.Int32:
                    return iValue.AsInt32;
                case BsonType.Int64:
                    return iValue.AsInt64;
                case BsonType.Boolean:
                    return iValue.AsBoolean;
                case BsonType.Double:
                    return iValue.AsDouble;
                case BsonType.Array:
                {
                    var objList = (List<object>)BsonTypeMapper.MapToDotNetValue(iValue);
                    var retList = Activator.CreateInstance(typeof(List<>).MakeGenericType(iTargetType.GenericTypeArguments[0]));

                    if (objList != null)
                    {
                        foreach (var obj in objList)
                        {
                            iTargetType.GetMethod("Add").Invoke(retList, new[] {obj});
                        }
                        return retList;
                    }
                    return null;
                }
                case BsonType.Null:
                {
                    return null;
                }
                default:
                    if (iTargetType.GetTypeInfo().BaseType == typeof(CADocument<>).MakeGenericType(iTargetType))
                    {
                        var retObj = Activator.CreateInstance(iTargetType);
                        retObj = iTargetType.GetMethod("FromJson").Invoke(retObj, new object[] {iValue.AsBsonDocument.ToJson()});

                        return retObj;
                    }
                    return null;
            }
        }
    }
}
