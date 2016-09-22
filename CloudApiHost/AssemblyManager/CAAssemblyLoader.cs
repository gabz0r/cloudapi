using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Threading;
using CloudApiHost.ExtensionMethods;
using CloudApiHost.MongoDb;
using CloudApiLib.Documents;
using CloudApiLib.Triggers;

namespace CloudApiHost.AssemblyManager
{
	public static class CAAssemblyLoader
	{
		public static Assembly CurrentAssembly { get; set; }

		public static void LoadFromFile (string iPath)
		{
			CurrentAssembly = Assembly.LoadFrom (iPath);

			if (CurrentAssembly == null)
				return;

		    CAMongoManager.Instance.CollectionTriggers = GetCollectionTriggers();
            CAAssemblyGenerator.Instance.ParseAssembly(CurrentAssembly);


            CAAssemblyGenerator.Instance.GenerateSource();
            CAAssemblyGenerator.Instance.CompileAssembly(CAHostConfig.CLOUD_API_ASSEMBLY_NAME.Split('.')[0] + "Sdk.dll");


            Console.WriteLine ("Assembly loaded");
            System.Diagnostics.Process.Start("deploy.bat");
        }

		public static void CallMethod (string iTypeName, string iMethodName, Dictionary<string, string> iParameter, HttpListenerResponse iResponse)
		{
            //old: var type = CurrentAssembly.GetType($"{CurrentAssembly.GetName().Name}.{iTypeName}");
            var type = CurrentAssembly.GetType ($"{iTypeName}");

		    var method = type?.GetMethod (iMethodName);
			if (method == null)
				return;

            //Parameterliste auf Basis der Methode und des Dictionarys erstellen
            var parameters = method.ReadParameters(iParameter);

            var typeInstance = Activator.CreateInstance (type);
			if (typeInstance == null)
				return;

		    var retObj = method.Invoke(typeInstance, parameters);

		    if (retObj is Task)
		    {
		        var task = retObj as Task;
                task.GetAwaiter().OnCompleted(() =>
                {
                    Dispatcher.CurrentDispatcher.Invoke(() => 
                    {
                        var ret = Convert.ToString(retObj);
                        var sr = new StreamWriter(iResponse.OutputStream);
                        sr.WriteLine(ret);
                        sr.Flush();
                        sr.Close();
                        iResponse.Close();

                    }, DispatcherPriority.Send);

                });
		    }

		    //if (retObj != null &&
		    //    method.ReturnType.BaseType.GetTypeInfo().BaseType == typeof(CADocument<>).MakeGenericType(method.ReturnType))
		    //{
		    //    //TODO Convert returned object to json and send to client
		    //}
		}

	    public static Dictionary<string, CACollectionTrigger> GetCollectionTriggers()
	    {
            var retDict = new Dictionary<string, CACollectionTrigger>();
            var types = CurrentAssembly.GetTypes();

	        foreach (var type in types)
	        {
	            var methods = type.GetMethods().Where(m => m.GetCustomAttributes(typeof (CATrigger), false).Length > 0).ToArray();
	            if (methods.Length == 0) continue;

                var typeInstance = Activator.CreateInstance(type);

                if(typeInstance != null) {
                    foreach (var method in methods)
                    {
                        var attr = (CATrigger) method.GetCustomAttribute(typeof (CATrigger));
                        switch (attr.TriggerType)
                        {
                            case CATriggerType.PreCreate:
                            {
                                if (retDict.ContainsKey(attr.TargetCollection))
                                {
                                    retDict[attr.TargetCollection].PreCreate =
                                        document => CATrigger.CallTrigger(method, typeInstance, attr.TargetCollection, Convert.ToString(document));
                                    //Convert.ToString(method.Invoke(typeInstance, new[] {document}));
                                }
                                else
                                {
                                    retDict.Add(attr.TargetCollection, new CACollectionTrigger
                                    {
                                        PreCreate = document => CATrigger.CallTrigger(method, typeInstance, attr.TargetCollection, Convert.ToString(document))
                                        //Convert.ToString(method.Invoke(typeInstance, new[] { document }))
                                    });
                                }
                                
                                break;
                            }
                            case CATriggerType.PostCreate:
                            {
                                if (retDict.ContainsKey(attr.TargetCollection))
                                {
                                    retDict[attr.TargetCollection].PostCreate =
                                        document => CATrigger.CallTrigger(method, typeInstance, attr.TargetCollection, Convert.ToString(document));
                                    }
                                else
                                {
                                    retDict.Add(attr.TargetCollection, new CACollectionTrigger
                                    {
                                        PostCreate = document => CATrigger.CallTrigger(method, typeInstance, attr.TargetCollection, Convert.ToString(document))
                                    });
                                }
                                
                                break;
                            }
                            case CATriggerType.PreUpdate:
                            {
                                if (retDict.ContainsKey(attr.TargetCollection))
                                {
                                    retDict[attr.TargetCollection].PreUpdate = (oldDocument, newDocument) => CATrigger.CallTrigger(method, typeInstance, attr.TargetCollection, Convert.ToString(oldDocument), Convert.ToString(newDocument));
                                }
                                else
                                {
                                    retDict.Add(attr.TargetCollection, new CACollectionTrigger
                                    {
                                        PreUpdate = (oldDocument, newDocument) => CATrigger.CallTrigger(method, typeInstance, attr.TargetCollection, Convert.ToString(oldDocument), Convert.ToString(newDocument))
                                    });
                                }
                                
                                break;
                            }
                            case CATriggerType.PostUpdate:
                            {
                                if (retDict.ContainsKey(attr.TargetCollection))
                                {
                                    retDict[attr.TargetCollection].PostUpdate = (oldDocument, newDocument) => CATrigger.CallTrigger(method, typeInstance, attr.TargetCollection, Convert.ToString(oldDocument), Convert.ToString(newDocument));
                                    }
                                else
                                {
                                    retDict.Add(attr.TargetCollection, new CACollectionTrigger
                                    {
                                        PostUpdate = (oldDocument, newDocument) => CATrigger.CallTrigger(method, typeInstance, attr.TargetCollection, Convert.ToString(oldDocument), Convert.ToString(newDocument))
                                    });
                                }
                                
                                break;
                            }
                            case CATriggerType.PreDelete:
                            {
                                if (retDict.ContainsKey(attr.TargetCollection))
                                {
                                    retDict[attr.TargetCollection].PreDelete =
                                        document => CATrigger.CallTrigger(method, typeInstance, attr.TargetCollection, Convert.ToString(document));
                                    }
                                else
                                {
                                    retDict.Add(attr.TargetCollection, new CACollectionTrigger
                                    {
                                        PreDelete = document => CATrigger.CallTrigger(method, typeInstance, attr.TargetCollection, Convert.ToString(document))
                                    });
                                }
                                
                                break;
                            }
                            case CATriggerType.PostDelete:
                            {
                                if (retDict.ContainsKey(attr.TargetCollection))
                                {
                                    retDict[attr.TargetCollection].PostDelete =
                                        document =>
                                            CATrigger.CallTrigger(method, typeInstance, attr.TargetCollection,
                                                Convert.ToString(document));
                                }
                                else
                                {
                                    retDict.Add(attr.TargetCollection, new CACollectionTrigger
                                    {
                                        PostDelete = document => CATrigger.CallTrigger(method, typeInstance, attr.TargetCollection, Convert.ToString(document))
                                    });
                                }
                                
                                break;
                            }
                        }
                    }
                }
	        }

	        return retDict;
	    }
	}
}

