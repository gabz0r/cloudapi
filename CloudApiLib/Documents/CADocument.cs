using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CloudApiLib.ExtensionMethods;
using CloudApiLib.HttpManager;
using CtrlValidationLib;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace CloudApiLib.Documents
{
    public class CADocument<T> where T : CADocument<T>, new()
    {
        public ObjectId Id { get; set; }

        internal bool IsUpdateDocument;

        public List<CtrlValidationResult> InvalidFields;
        
        public T FromJson(string iJsonDoc)
        {
            var bson = BsonSerializer.Deserialize<BsonDocument>(iJsonDoc);

            var retObj = new T();
            var props = retObj.GetAllProperties();

            if (bson.ElementAt(0).Name == "$set")
            {
                bson = bson[0].ToBsonDocument();
                retObj.IsUpdateDocument = true;
            }

            foreach (var prop in props)
            {
                if (bson != null)
                {
                    var propName = prop.Name;

                    if (prop.Name == "Id")
                        propName = "_id";

                    if (propName == "_id" && !bson.Contains("_id")) //Document gets either parsed from an update - document or is a nested document with no id
                        continue;

                    try
                    {
                        if (bson.Contains(propName))
                        {
                            var bsonValue = bson[propName];
                            prop?.SetValue(retObj, bsonValue.MapType(prop.PropertyType));
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                }
            }

            return retObj;
        } 

        public async Task Save()
        {
            var requestBuilder = new StringBuilder();
            var bson = new BsonDocument();
            var typeName = GetType().Name;

            var exists = (await FindOne(doc => doc.Id.Equals(Id))) != null;
            if (exists)
            {
                await Update();
                return;
            }

            if (CAConfig.CLOUD_API_MODE == "CLIENT")
            {
                var valRes = await Validate(CADocumentModification.Create);
                if (valRes.Count > 0)
                {
                    InvalidFields = valRes;
                    return;
                }
                InvalidFields = new List<CtrlValidationResult>();
            }

            requestBuilder.Append($"OPCODE=CLOUD_API_DATA&COLLECTION={typeName}&ACTION=CREATE#CDOC=");

            foreach (var propInfo in GetAllProperties())
            {
                var propName = propInfo.Name;
                var propVal = propInfo.GetValue(this);

                if (propName == "Id") propName = "_id"; //Umbenennen für JSON Serialisierung
                
                if (propInfo.PropertyType.GetTypeInfo().BaseType != null && 
                    propInfo.PropertyType.GetTypeInfo().BaseType.IsGenericType && 
                    propInfo.PropertyType.GetTypeInfo().BaseType ==
                    typeof(CADocument<>).MakeGenericType(propInfo.PropertyType))
                {
                    var propObj = propInfo.GetValue(this);
                    if (propObj == null) continue;

                    var propValDoc = propInfo.PropertyType.GetMethod("ToNestedDocument").Invoke(propObj, null);
                    bson[propName] = BsonValue.Create(propValDoc);
                }
                else if (propVal is decimal)
                {
                    var stringRep = Convert.ToString(propVal);
                    bson[propName] = BsonValue.Create(stringRep);
                    //throw new Exception("Decimals are not supported by cloudapi, sorry! (Use double instead)");
                }
                else if (propVal is Dictionary<string, string>)
                {
                    var saveString = new StringBuilder();
                    foreach (var kv in ((Dictionary<string, string>) propVal))
                    {
                        saveString.Append($"{kv.Key}§${kv.Value}$§");
                    }
                    bson[propName] = BsonValue.Create(saveString.ToString());
                } 
                else
                {
                    bson[propName] = BsonValue.Create(propVal);
                }
            }
            
            requestBuilder.Append(bson.ToJson().Replace("\\", ""));
            var res = await CAHttpManager.Instance.HttpPostAsync(CAConfig.CLOUD_API_URL, requestBuilder.ToString());

            var resDoc = BsonDocument.Parse(res);

            Id = ObjectId.Parse(Convert.ToString(resDoc.GetValue("_id")));
        }

        public async Task Delete()
        {
            var filterDoc = new BsonDocument { ["_id"] = Id };
            
            var typeName = GetType().Name;
            var cmd = $"OPCODE=CLOUD_API_DATA&COLLECTION={typeName}&ACTION=DELETE#DDOC={filterDoc.ToJson()}";

            await CAHttpManager.Instance.HttpPostAsync(CAConfig.CLOUD_API_URL, cmd);
        }

        public static async Task ClearCollection()
        {
            var typeName = typeof(T).Name;
            var cmd = $"OPCODE=CLOUD_API_DATA&COLLECTION={typeName}&ACTION=DELETE#DDOC={{}}";

            await CAHttpManager.Instance.HttpPostAsync(CAConfig.CLOUD_API_URL, cmd);
        }

        public async Task Update()
        {
            //    Filter    ============Update============
            // [{_id : 1}, {$set: {"name" : "Gabriel", "age" : 21}}]

            var filter = Builders<T>.Filter.Where(obj => obj.Id.Equals(Id)).RenderToBsonDocument().ToJson();//Where(iCompareCriteria).ToJson();
            var typeName = typeof(T).Name;

            var updObj = new BsonDocument();

            if (CAConfig.CLOUD_API_MODE == "CLIENT")
            {
                var valRes = await Validate(CADocumentModification.Update);
                if (valRes.Count > 0)
                {
                    InvalidFields = valRes;
                    return;
                }
                InvalidFields = new List<CtrlValidationResult>();
            }

            foreach (var propInfo in GetAllProperties())
            {
                var propName = propInfo.Name;
                var propVal = propInfo.GetValue(this);

                if (propName == "Id") continue;

                if (propInfo.PropertyType.GetTypeInfo().BaseType != null &&
                    propInfo.PropertyType.GetTypeInfo().BaseType.IsGenericType &&
                    propInfo.PropertyType.GetTypeInfo().BaseType ==
                    typeof(CADocument<>).MakeGenericType(propInfo.PropertyType))
                {
                    var propObj = propInfo.GetValue(this);
                    if (propObj == null) continue;

                    var propValDoc = propInfo.PropertyType.GetMethod("ToNestedDocument").Invoke(propObj, null);
                    updObj[propName] = BsonValue.Create(propValDoc);
                }
                else if (propVal is decimal)
                {
                    var stringRep = Convert.ToString(propVal);
                    updObj[propName] = BsonValue.Create(stringRep);
                    //throw new Exception("Decimals are not supported by cloudapi, sorry! (Use double instead)");
                }
                else if (propVal is Dictionary<string, string>)
                {
                    var saveString = new StringBuilder();
                    foreach (var kv in ((Dictionary<string, string>)propVal))
                    {
                        saveString.Append($"{kv.Key}§${kv.Value}$-§");
                    }
                    updObj[propName] = BsonValue.Create(saveString.ToString());
                }
                else
                {
                    updObj[propName] = BsonValue.Create(propVal);
                }
            }

            var updObjJson = $"[{filter}, {{$set: {updObj.ToJson()}}}]";
            var cmd = $"OPCODE=CLOUD_API_DATA&COLLECTION={typeName}&ACTION=UPDATE#UDOC={updObjJson}";

            await CAHttpManager.Instance.HttpPostAsync(CAConfig.CLOUD_API_URL, cmd);
        }

        public static async Task<T> FindOne(Expression<Func<T, bool>> iCompareCriteria)
        {
            //var query = Query<T>.Where(iCompareCriteria);
            var query = Builders<T>.Filter.Where(iCompareCriteria).RenderToBsonDocument().ToJson();
            var typeName = typeof (T).Name;

            var cmd = $"OPCODE=CLOUD_API_DATA&COLLECTION={typeName}&ACTION=READ#RDOC={query}";
            var objJson = await CAHttpManager.Instance.HttpPostAsync(CAConfig.CLOUD_API_URL, cmd);
            if (!string.IsNullOrEmpty(objJson))
            {
                var bsonArray = BsonSerializer.Deserialize<BsonArray>(objJson);
                if (bsonArray == null || bsonArray.Count == 0) return null;

                var bson = bsonArray[0]?.AsBsonDocument;
                var retObj = new T();
                var props = retObj.GetAllProperties();

                foreach (var prop in props)
                {
                    if (bson != null)
                    {
                        var propName = prop.Name;

                        if (prop.Name == "Id")
                            propName = "_id";

                        if (bson.Contains(propName))
                        {
                            var bsonValue = bson[propName];
                            prop?.SetValue(retObj, bsonValue.MapType(prop.PropertyType));
                        }
                    }
                }

                return retObj;
            }

            return null;
        }

        public static async Task<ObservableCollection<T>> Find(Expression<Func<T, bool>> iCompareCriteria)
        {
            var query = Builders<T>.Filter.Where(iCompareCriteria).RenderToBsonDocument().ToJson();
            var typeName = typeof(T).Name;

            var cmd = $"OPCODE=CLOUD_API_DATA&COLLECTION={typeName}&ACTION=READ#RDOC={query}";
            var objJson = await CAHttpManager.Instance.HttpPostAsync(CAConfig.CLOUD_API_URL, cmd);

            if (!string.IsNullOrEmpty(objJson))
            {
                var bson = BsonSerializer.Deserialize<BsonArray>(objJson);
                var retList = new ObservableCollection<T>();

                foreach (var bsonDoc in bson)
                {
                    if(bsonDoc == null) continue;

                    var retObj = new T();
                    var props = retObj.GetAllProperties();
                   
                    foreach (var prop in props)
                    {
                        var propName = prop.Name;

                        if (prop.Name == "Id")
                            propName = "_id";

                        try
                        {
                            var bsonValue = bsonDoc[propName];
                            prop?.SetValue(retObj, bsonValue.MapType(prop.PropertyType));
                        }
                        catch { }
                    }

                    retList.Add(retObj);
                }
                return retList;
            }

            return null;
        }

        public static async Task<ObservableCollection<T>> All()
        {
            return await Find(x => true);
        }

        public string ToJson(bool iUpdate = false)
        {
            var updObj = new BsonDocument();

            foreach (var propInfo in GetAllProperties())
            {
                var propName = propInfo.Name;
                var propVal = propInfo.GetValue(this);

                if (propName == "Id") continue;

                if (propInfo.PropertyType.GetTypeInfo().BaseType != null &&
                    propInfo.PropertyType.GetTypeInfo().BaseType.IsGenericType &&
                    propInfo.PropertyType.GetTypeInfo().BaseType ==
                    typeof(CADocument<>).MakeGenericType(propInfo.PropertyType))
                {
                    var propObj = propInfo.GetValue(this);
                    if (propObj == null) continue;

                    var propValDoc = propInfo.PropertyType.GetMethod("ToNestedDocument").Invoke(propObj, null);
                    updObj[propName] = BsonValue.Create(propValDoc);
                }
                else if (propVal is decimal)
                {
                    var stringRep = Convert.ToString(propVal);
                    updObj[propName] = BsonValue.Create(stringRep);
                    //throw new Exception("Decimals are not supported by cloudapi, sorry! (Use double instead)");
                }
                else if (propVal is Dictionary<string, string>)
                {
                    var saveString = new StringBuilder();
                    foreach (var kv in ((Dictionary<string, string>)propVal))
                    {
                        saveString.Append($"{kv.Key}§${kv.Value}$&");
                    }
                    updObj[propName] = BsonValue.Create(saveString.ToString());
                }
                else
                {
                    updObj[propName] = BsonValue.Create(propVal);
                }
            }

            var ret = updObj.ToJson();

            if (iUpdate)
            {
                ret = $"{{$set: {ret}}}";
            }
            return ret;
        }

        public BsonDocument ToNestedDocument()
        {
            var updObj = new BsonDocument();

            foreach (var propInfo in GetAllProperties())
            {
                var propName = propInfo.Name;
                var propVal = propInfo.GetValue(this);

                if (propName == "Id") continue;

                if (propInfo.PropertyType.GetTypeInfo().BaseType != null &&
                    propInfo.PropertyType.GetTypeInfo().BaseType.IsGenericType &&
                    propInfo.PropertyType.GetTypeInfo().BaseType ==
                    typeof(CADocument<>).MakeGenericType(propInfo.PropertyType))
                {
                    var propObj = propInfo.GetValue(this);
                    if (propObj == null) continue;

                    var propValJson = Convert.ToString(propInfo.PropertyType.GetMethod("ToJson").Invoke(propObj, new object[] { false }));
                    updObj[propName] = BsonValue.Create(propValJson);
                }
                else if (propVal is decimal)
                {
                    var stringRep = Convert.ToString(propVal);
                    updObj[propName] = BsonValue.Create(stringRep);
                    //throw new Exception("Decimals are not supported by cloudapi, sorry! (Use double instead)");
                }
                else if (propVal is Dictionary<string, string>)
                {
                    var saveString = new StringBuilder();
                    foreach (var kv in ((Dictionary<string, string>)propVal))
                    {
                        saveString.Append($"{kv.Key}§${kv.Value}$&");
                    }
                    updObj[propName] = BsonValue.Create(saveString.ToString());
                }
                else
                {
                    updObj[propName] = BsonValue.Create(propVal);
                }
            }

            return updObj;
        }

        public virtual async Task<List<CtrlValidationResult>> Validate(CADocumentModification iModificationType)
        {
            var retList = new List<CtrlValidationResult>();

            foreach (var prop in GetAllProperties())
            {
                var result = await CtrlValidation.Validate(this, prop, iModificationType);

                if (!result.IsValid)
                {
                    retList.Add(result);
                }
            }

            return retList;
        }

        public bool IsValid()
        {
            return (InvalidFields != null && InvalidFields.Count == 0);
        }

        private PropertyInfo[] GetAllProperties()
        {
            return GetType().GetProperties();
        }
    }
}
