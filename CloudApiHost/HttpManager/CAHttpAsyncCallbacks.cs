using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using CloudApiHost.AssemblyManager;
using CloudApiHost.ExtensionMethods;
using CloudApiHost.MongoDb;
using CloudApiLib.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace CloudApiHost.HttpManager
{
	public static class CAHttpAsyncCallbacks
	{
		public static void OnHttpRequest (IAsyncResult iHttpResult)
		{
			var asyncState = iHttpResult.AsyncState as HttpListener;
		    if (asyncState != null)
		    {
		        var requestContext = asyncState.EndGetContext (iHttpResult);

		        try {
		            var body = new StreamReader (requestContext.Request.InputStream).ReadToEnd ();
		            var bodyList = body.Split ('#');
		            var bodyHead = bodyList [0];
		            var bodyBody = bodyList [1];

		            var headDict = new Dictionary<string, string> ().FromQueryString (bodyHead);
		            var dict = new Dictionary<string, string> ().FromQueryString (bodyBody);

		            ThreadPool.QueueUserWorkItem(ctx => 
                    {
		                ProcessBody(headDict, dict, (HttpListenerResponse) ctx);
		            }, requestContext.Response);

                    asyncState.BeginGetContext(OnHttpRequest, asyncState);
                } catch (Exception ex) {
		            ex.WriteToStream (requestContext.Response.OutputStream);
                    requestContext.Response.Close();
                    asyncState.BeginGetContext(OnHttpRequest, asyncState);
                }
		    }
		}

	    public static void ProcessBody(Dictionary<string, string> iBodyHead, Dictionary<string, string> iBodyBody, HttpListenerResponse iResponse)
	    {
	        var opcode = iBodyHead["OPCODE"];

	        switch (opcode)
	        {
                case "CLOUD_API_METHOD":
	            {
                    var typeName = iBodyHead["TYPE"];
                    var method   = iBodyHead["METHOD"];

                    Console.WriteLine("Calling method {0} on type {1}", method, typeName);
                    
                    CAAssemblyLoader.CallMethod(typeName, method, iBodyBody, iResponse);
                    break;
	            }
                case "CLOUD_API_DATA":
	            {
	                var collectionName = iBodyHead["COLLECTION"];
	                var action = iBodyHead["ACTION"];

	                switch (action)
	                {
                        case "CREATE":
	                    {
	                        var newDocument = iBodyBody["CDOC"];
	                        var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(newDocument);
                                   
	                        if (CAMongoManager.Instance.CollectionTriggers.ContainsKey(collectionName) &&
                                CAMongoManager.Instance.CollectionTriggers[collectionName].PreCreate != null)
	                        {
	                            bsonDocument = BsonSerializer.Deserialize<BsonDocument>(CAMongoManager.Instance.CollectionTriggers[collectionName].PreCreate(newDocument));
	                        }

                            CAMongoManager.Instance.GetCollection<BsonDocument>(collectionName).InsertOne(bsonDocument);
                            Console.WriteLine("Creating document(s) in collection {0}: {1}", collectionName, bsonDocument.ToJson());

                            if (CAMongoManager.Instance.CollectionTriggers.ContainsKey(collectionName) &&
                                CAMongoManager.Instance.CollectionTriggers[collectionName].PostCreate != null)
                            {
                                CAMongoManager.Instance.CollectionTriggers[collectionName].PostCreate(bsonDocument.ToJson());
                            }

                            SendResponse($"{{ \"_id\" : {bsonDocument.GetValue("_id").ToJson()} }}", 200, iResponse);

	                        break;
	                    }
                        case "READ":
	                    {
                            var queryDocument = iBodyBody["RDOC"];

	                        var bsonQuery = BsonSerializer.Deserialize<BsonDocument>(queryDocument);
	                        var coll = CAMongoManager.Instance.GetCollection<BsonDocument>(collectionName).Find(bsonQuery).ToEnumerable();
                            
                            var arrayBuilder = new StringBuilder();
	                        arrayBuilder.Append("[");
                                    
	                        foreach (var bson in coll)
	                        {
	                            arrayBuilder.Append($"{bson.ToString()},");
	                        }

	                        if (arrayBuilder.Length > 1)
	                        {
                                arrayBuilder.Remove(arrayBuilder.Length - 1, 1); //Letztes Komma entfernen
                            }
	                        
	                        arrayBuilder.Append("]");

                            var responseWriter = new StreamWriter(iResponse.OutputStream) {AutoFlush = true};
                            responseWriter.Write(arrayBuilder.ToString());

                            iResponse.StatusCode = 200;
                            iResponse.Close();
                            
                            Console.WriteLine("Querying document(s) from collection {0}: {1}", collectionName, bsonQuery.ToJson());
                            
                            break;
	                    }
                        case "UPDATE":
	                    {
                            //    Filter    ============Update============
                            // [{_id : 1}, {$set: {"name" : "Gabriel", "age" : 21}}]
                            // Parse mit FilterDefinition und UpdateDocument

	                        var fullDocument = iBodyBody["UDOC"];
	                        var children = BsonSerializer.Deserialize<BsonArray>(fullDocument);

	                        if (children.Count != 2) return;
                            
	                        var filterDocument = children[0].ToString();
	                        var updateDocument = children[1].ToString();

	                        var filterBson = BsonSerializer.Deserialize<BsonDocument>(filterDocument);
                            var updateBson = BsonSerializer.Deserialize<BsonDocument>(updateDocument);

                            var oldDocForTrigger =
                                    CAMongoManager.Instance.GetCollection<BsonDocument>(collectionName).Find(filterBson).ToEnumerable().ToArray()[0];

                                    if (CAMongoManager.Instance.CollectionTriggers.ContainsKey(collectionName) &&
                                CAMongoManager.Instance.CollectionTriggers[collectionName].PreUpdate != null)
                            {
                                updateBson = BsonSerializer.Deserialize<BsonDocument>(CAMongoManager.Instance.CollectionTriggers[collectionName].PreUpdate(oldDocForTrigger.ToJson(), updateDocument));
                            }

                            CAMongoManager.Instance.GetCollection<BsonDocument>(collectionName).UpdateOne(filterBson, updateBson);
	                        var newDocAfterUpdate = CAMongoManager.Instance.GetCollection<BsonDocument>(collectionName).Find(filterBson).FirstOrDefault();

                            Console.WriteLine("Updating document(s) in collection {0}: {1}{2}", collectionName, filterDocument, updateBson.ToJson());

                            if (CAMongoManager.Instance.CollectionTriggers.ContainsKey(collectionName) &&
                                CAMongoManager.Instance.CollectionTriggers[collectionName].PostUpdate != null)
                            {
                                CAMongoManager.Instance.CollectionTriggers[collectionName].PostUpdate(oldDocForTrigger.ToJson(), newDocAfterUpdate.ToJson());
                            }

                            iResponse.StatusCode = 200;
                            iResponse.Close();

                            break;
	                    }
                        case "DELETE":
	                    {
	                        var filterDoc = iBodyBody["DDOC"];
	                        var bsonFilter = BsonSerializer.Deserialize<BsonDocument>(filterDoc);

                            var oldDocs = CAMongoManager.Instance.GetCollection<BsonDocument>(collectionName).Find(bsonFilter).ToEnumerable().ToArray();

                            if (CAMongoManager.Instance.CollectionTriggers.ContainsKey(collectionName) &&
                                CAMongoManager.Instance.CollectionTriggers[collectionName].PreDelete != null)
                            {
                                foreach (var doc in oldDocs)
                                {
                                    CAMongoManager.Instance.CollectionTriggers[collectionName].PreDelete(doc.ToJson());
                                } 
                            }

                            CAMongoManager.Instance.GetCollection<BsonDocument>(collectionName).DeleteMany(bsonFilter);


                            Console.WriteLine("Removing document(s) from collection {0}: {1}", collectionName, bsonFilter.ToJson());

                            if (CAMongoManager.Instance.CollectionTriggers.ContainsKey(collectionName) &&
                                CAMongoManager.Instance.CollectionTriggers[collectionName].PostDelete != null)
                            {
                                foreach (var doc in oldDocs)
                                {
                                    CAMongoManager.Instance.CollectionTriggers[collectionName].PostDelete(doc.ToJson());
                                }
                            }

                            iResponse.StatusCode = 200;
                            iResponse.Close();
                                    
                            break;
	                    }
	                }
                    
	                break;
	            }
	        }
	    }

	    private static void SendResponse(string iData, decimal iStatusCode, HttpListenerResponse iResponse)
	    {
            byte[] buffer = Encoding.UTF8.GetBytes(iData);
            iResponse.ContentLength64 = buffer.Length;
	        iResponse.ContentType = "application/json";

	        iResponse.StatusCode = Convert.ToInt32(iStatusCode);
            iResponse.OutputStream.Write(buffer, 0, buffer.Length);
            iResponse.OutputStream.Close();

            iResponse.Close();
        }
	}
}

