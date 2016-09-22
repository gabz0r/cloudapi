using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CloudApiLib.ExtensionMethods;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace CloudApiLib.Triggers
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CATrigger : Attribute
    {
        public CATrigger(CATriggerType iTriggerType, string iTargetCollection)
        {
            TriggerType = iTriggerType;
            TargetCollection = iTargetCollection;
        }

        public CATriggerType TriggerType { get; set; }
        public string TargetCollection { get; set; }

        public static string CallTrigger(MethodInfo iTriggerMethod, object iTargetObj, string iCollectionName, params string[] iTriggerDocs)
        {
            if (CAConfig.RegisteredTypes.ContainsKey(iCollectionName))
            {
                var collType = CAConfig.RegisteredTypes[iCollectionName];
                var docInstance = Activator.CreateInstance(collType);
                var docInstanceNew = Activator.CreateInstance(collType);

                switch (iTriggerDocs.Length)
                {
                    case 1:
                    {
                        var docConstructor = collType.GetMethod("FromJson");

                        docInstance = docConstructor?.Invoke(docInstance, new object[] { iTriggerDocs[0] });
                        //var bsonDoc = BsonSerializer.Deserialize<BsonDocument>(iTriggerDocs[0]);
                        //foreach (var prop in collType.GetProperties())
                        //{
                        //    var propName = prop.Name == "Id" ? "_id" : prop.Name;
                        //    prop.SetValue(docInstance, bsonDoc[propName]?.MapType());
                        //}

                        var retVal = iTriggerMethod.Invoke(iTargetObj, new[] { docInstance });
                        if (retVal == null) return null;

                        var objJson = Convert.ToString(retVal.GetType().GetMethod("ToJson").Invoke(retVal, new[] { (object)false }));

                        return objJson;
                    }
                    case 2:
                    {
                        var docConstructor = collType.GetMethod("FromJson");
                        //TODO generic untersuchen (ist nestedTestObject bla)
                        docInstance = docConstructor?.Invoke(docInstance, new object[] { iTriggerDocs[0] });
                        docInstanceNew = docConstructor?.Invoke(docInstanceNew, new object[] {iTriggerDocs[1]});


                        var retVal = iTriggerMethod.Invoke(iTargetObj, new[] { docInstance, docInstanceNew });
                        if (retVal == null) return null;
                        var objJson = Convert.ToString(retVal.GetType().GetMethod("ToJson").Invoke(retVal, new[] { (object)true }));

                        return objJson;
                    }
                }
            }
            return null;
        }
    }
}
