using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace CloudApiHost.ExtensionMethods
{
    public static class MethodInfoExtensions
    {
        public static object[] ReadParameters(this MethodInfo iMethodInfo, Dictionary<string, string> iParams)
        {
            var objParamArray = new List<object>();
            var paramsSorted = (ParameterInfo[]) iMethodInfo.GetParameters().Clone();
            Array.Sort(paramsSorted,
                (x1, x2) => x1.Position.CompareTo(x2.Position));

            foreach (var paramInfo in paramsSorted)
            {
                if (iParams.ContainsKey(paramInfo.Name))
                {
                    switch (paramInfo.ParameterType.Name)
                    {
                        case "Int32":
                        {
                            int parameterToAdd;
                            if (!int.TryParse(iParams[paramInfo.Name], out parameterToAdd))
                            {
                                return null;
                            }
                            objParamArray.Add(parameterToAdd);
                            break;
                        }
                        case "String":
                        {
                            objParamArray.Add(Convert.ToString(iParams[paramInfo.Name]));
                            break;
                        }
                    }
                    
                }
            }

            return objParamArray.ToArray();
        }
    }
}
