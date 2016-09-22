using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CloudApiHost.Helper;
using CloudApiLib.Documents;
using CloudApiLib.Triggers;
using Microsoft.CSharp;

namespace CloudApiHost.AssemblyManager
{
    class CAAssemblyGenerator : CASingleton<CAAssemblyGenerator>
    {
        private string _generatorTemplate = 
@"using CloudApiLib.Documents;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudApiLib 
{ 
    <<#classes>>
}";

        private readonly List<Type> _assemblyTypes = new List<Type>(); 
        private readonly List<MethodInfo> _assemblyMethods = new List<MethodInfo>();

        public void ParseAssembly(Assembly iAssembly)
        {
            foreach (var type in iAssembly.DefinedTypes)
            {
                AddType(type);
            }
        }

        public void AddType(Type iType)
        {
            _assemblyTypes.Add(iType);
        }

        public void GenerateSource()
        {
            var allClassesStringBuilder = new StringBuilder();

            //Build type source
            foreach (var type in _assemblyTypes)
            {
                try
                {
                    if (type.GetTypeInfo().BaseType != typeof(CADocument<>).MakeGenericType(type))
                    {
                        continue;
                    }
                }
                catch (ArgumentException)
                {
                    continue;
                }
                
                var currentTypeString = 
$@"public class {type.Name} : CADocument<{type.Name}>
    {{
<<#props>>
<<#methods>>
    }}";

                var propsStringBuilder = new StringBuilder();

                foreach (var prop in type.GetProperties())
                {
                    if(prop.PropertyType.Name == "ObjectId") continue; //Derived from CADocument

                    var typeName = GetFullName(prop.PropertyType);
                    propsStringBuilder.AppendLine($"\t\tpublic {typeName} {prop.Name} {{ get; set; }}");
                }

                currentTypeString = currentTypeString.Replace("<<#props>>", propsStringBuilder.ToString());

                
                var methodStringBuilder = new StringBuilder();

                foreach (var method in type.GetTypeInfo().DeclaredMethods
                            .Where(m => m.GetCustomAttributes(typeof(CATrigger), false).Length == 0 &&
                                        m.GetCustomAttributes(typeof(CAMethod), false).Length > 0 &&
                                       !(m.IsSpecialName && (m.Name.StartsWith("set_") || m.Name.StartsWith("get_")))) //Skip property accessors
                            .ToArray())
                {
                    var returnType = method.ReturnType == typeof(Task) ? method.ReturnType.Name : GetFullName(method.ReturnType);

                    var currentMethodString = $"\t\tpublic static async {returnType} {method.Name}(<<#params>>) \n\t\t{{<<#source>>\n\t\t}}\n";

                    var paramsStringBuilder = new StringBuilder();
                    var reqParamsStringBuilder = new StringBuilder();
                    for (int i = 0; i < method.GetParameters().Length; i++)
                    {
                        if (i != method.GetParameters().Length)
                        {
                            paramsStringBuilder.Append($"{method.GetParameters()[i].ParameterType.Name} {method.GetParameters()[i].Name}");

                            reqParamsStringBuilder.Append($"{method.GetParameters()[i].Name}=\" + {method.GetParameters()[i].Name} + \"");

                            if (i != method.GetParameters().Length - 1)
                            {
                                paramsStringBuilder.Append(", ");
                                reqParamsStringBuilder.Append("&");
                            }
                        }
                    }

                    currentMethodString = currentMethodString.Replace("<<#params>>", paramsStringBuilder.ToString());

                    var reqString = $"OPCODE=CLOUD_API_METHOD&TYPE={method.DeclaringType}&METHOD={method.Name}#";
                    if (reqParamsStringBuilder.Length > 0)
                    {
                        reqString += $"{reqParamsStringBuilder}";
                    }

                    var returnStatement = method.ReturnType != typeof(void)
                        ? $"return Convert.To{method.ReturnType.Name}(retDt);"
                        : "return;";

                    if (method.ReturnType.Name.Contains("Task"))
                    {
                        var args = method.ReturnType.GetGenericArguments();
                        if (args.Length == 1)
                        {
                            returnStatement = $"return Convert.To{args[0].Name}(retDt);";
                        }
                        else if(args.Length == 0)
                        {
                            returnStatement = "return;";
                        }
                    }

                    //if (method.ReturnType.GetTypeInfo().BaseType == typeof(CADocument<>).MakeGenericType(method.ReturnType))
                    //{
                    //    //TODO return CADocument<>
                    //    //returnStatement = $"return "
                    //}

                    var source = @"
        var req = (HttpWebRequest) WebRequest.Create(""" + CAHostConfig.CLOUD_API_URL + @""");
        req.ContentType = ""cloudapi/body-encoded"";
        req.Method = ""POST"";

        var data = Encoding.UTF8.GetBytes(""" + reqString + @""");
        var stream = req.GetRequestStream();

        await stream.WriteAsync(data, 0, data.Length);
        await stream.FlushAsync();
        stream.Close();
        using(var response = await req.GetResponseAsync())
        {
            using(var respStream = response.GetResponseStream())
            {
                var sr = new StreamReader(respStream);
                var retDt = sr.ReadToEnd();" +
                returnStatement + @"
            }
        }";

                    currentMethodString = currentMethodString.Replace("<<#source>>", source);
                    methodStringBuilder.AppendLine(currentMethodString);
                }

                currentTypeString = currentTypeString.Replace("<<#methods>>", methodStringBuilder.ToString());
                allClassesStringBuilder.AppendLine(currentTypeString);
            }

            _generatorTemplate = _generatorTemplate.Replace("<<#classes>>", allClassesStringBuilder.ToString());
        }

        public void CompileAssembly(string iOutputFileName)
        {
            File.WriteAllText("tempOutput.cs", _generatorTemplate);

            var codeProvider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
            var compParams = new CompilerParameters();

            compParams.ReferencedAssemblies.Add("CloudApiLib.dll");
            compParams.ReferencedAssemblies.Add("System.dll");
            compParams.ReferencedAssemblies.Add("System.Net.dll");
            compParams.ReferencedAssemblies.Add("System.Collections.dll");
            compParams.ReferencedAssemblies.Add("System.Threading.dll");
            compParams.ReferencedAssemblies.Add("mscorlib.dll");

            compParams.GenerateInMemory = false;
            compParams.OutputAssembly = iOutputFileName;

            var result = codeProvider.CompileAssemblyFromSource(compParams, _generatorTemplate);

            if (result.Errors.HasErrors)
            {
                foreach (CompilerError error in result.Errors)
                {
                    Console.WriteLine("Error ({0}) Line{1}: {2}", error.ErrorNumber, error.Line, error.ErrorText);
                }
            }
        }

        static string GetFullName(Type t)
        {
            if (!t.IsGenericType)
                return t.Name;
            StringBuilder sb = new StringBuilder();

            sb.Append(t.Name.Substring(0, t.Name.LastIndexOf("`")));
            sb.Append(t.GetGenericArguments().Aggregate("<",

                delegate (string aggregate, Type type)
                {
                    return aggregate + (aggregate == "<" ? "" : ",") + GetFullName(type);
                }
                ));
            sb.Append(">");

            return sb.ToString();
        }
    }
}
