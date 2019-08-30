using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Extension.Utils.Shell;
using Corsinvest.ProxmoxVE.Api.Metadata;

namespace Corsinvest.ProxmoxVE.Cli
{
    /// <summary>
    /// Methods\
    /// </summary>
    public class Commands
    {
        /// <summary>
        /// Execute methods
        /// </summary>
        /// <param name="client"></param>
        /// <param name="classApiRoot"></param>
        /// <param name="resource"></param>
        /// <param name="methodType"></param>
        /// <param name="parameters"></param>
        /// <param name="wait"></param>
        /// <param name="output"></param>
        /// <param name="verbose"></param>
        public static (int ResultCode, string ResultText) Execute(Client client,
                                                                  ClassApi classApiRoot,
                                                                  string resource,
                                                                  MethodType methodType,
                                                                  Dictionary<string, object> parameters,
                                                                  bool wait = false,
                                                                  OutputType output = OutputType.Text,
                                                                  bool verbose = false)
        {
            var currResponseType = client.ResponseType;
            if (output == OutputType.Png) { client.ResponseType = ResponseType.Png; }

            //create result
            Result result = null;
            switch (methodType)
            {
                case MethodType.Get: result = client.Get(resource, parameters); break;
                case MethodType.Set: result = client.Set(resource, parameters); break;
                case MethodType.Create: result = client.Create(resource, parameters); break;
                case MethodType.Delete: result = client.Delete(resource, parameters); break;
            }

            //restore prev ResponseType
            currResponseType = client.ResponseType;

            var resultText = new StringBuilder();
            if (result != null && !result.IsSuccessStatusCode)
            {
                resultText.AppendLine(result.ReasonPhrase);
                resultText.AppendLine(verbose ?
                                      Client.ObjectToJson((string)result.Response.errors) :
                                      result.GetError());
            }
            else if (result.InError())
            {
                resultText.AppendLine(result.ReasonPhrase);
            }
            else
            {
                //print result
                if (verbose)
                {
                    //verbose full response json
                    resultText.AppendLine(Client.ObjectToJson(result.Response));
                }
                else
                {
                    switch (output)
                    {
                        case OutputType.Png: resultText.AppendLine(result.Response); break;
                        case OutputType.Json: resultText.AppendLine(Client.ObjectToJson(result.Response.data, false)); break;
                        case OutputType.JsonPretty: resultText.AppendLine(Client.ObjectToJson(result.Response.data)); break;

                        case OutputType.Text:
                            var data = result.Response.data;

                            var classApi = ClassApi.GetFromResource(classApiRoot, resource);
                            if (classApi == null)
                            {
                                resultText.AppendLine($"no such resource '{resource}'");
                            }
                            else
                            {
                                var returnParameters = classApi.Methods
                                                               .Where(a => a.IsGet)
                                                               .FirstOrDefault()
                                                               .ReturnParameters;

                                if (returnParameters.Count == 0)
                                {
                                    //no return defined
                                    resultText.Append(TableHelper.CreateTable(data));
                                }
                                else
                                {
                                    var keys = returnParameters.OrderBy(a => a.Optional)
                                                               .ThenBy(a => a.Name)
                                                               .Select(a => a.Name)
                                                               .ToArray();

                                    resultText.Append(TableHelper.CreateTable(data, keys, returnParameters));
                                }
                            }
                            break;

                        default: break;
                    }
                }

                if (wait)
                {
                    var task = (string)result.Response.data;
                    client.WaitForTaskToFinish(task.Split(':')[1], task, 1000, 30000);
                }
            }

            return ((int)result.StatusCode, resultText.ToString());
        }

        private static void CreateTable(IEnumerable<ParameterApi> parameters, StringBuilder resultText)
        {
            if (parameters.Count() > 0)
            {
                var values = new List<object[]>();
                foreach (var param in parameters)
                {
                    var partsComment = JoinWord(param.Description
                                                     .Replace(StringHelper.NewLineUnix + "", " ")
                                                     .Trim()
                                                     .Split(new string[] { " " }, StringSplitOptions.None), 45, " ");

                    //type
                    var partsType = new string[] { param.Type };
                    if (!string.IsNullOrWhiteSpace(param.TypeText))
                    {
                        //explicit text
                        partsType = JoinWord(param.TypeText.Split(' '), 18, "");
                    }
                    else if (param.EnumValues.Count() > 0)
                    {
                        //enums
                        partsType = JoinWord(param.EnumValues, 18, ",");
                    }

                    var row = 0;
                    for (row = 0; row < Math.Max(partsType.Length, partsComment.Length); row++)
                    {
                        values.Add(new[] { row == 0 ? param.Name : "",
                                           row < partsType.Length ? partsType[row] : "",
                                           row < partsComment.Length ? partsComment[row] : "" });
                    }
                }

                resultText.Append(TableHelper.CreateTable(new[] { "param", "type", "description" }, values, false));
            }
        }

        /// <summary>
        /// Usage resource
        /// </summary>
        /// <param name="classApiRoot"></param>
        /// <param name="resource"></param>
        /// <param name="returns"></param>
        /// <param name="command"></param>
        /// <param name="verbose"></param>
        /// <returns></returns>
        public static string Usage(ClassApi classApiRoot,
                                   string resource,
                                   bool returns = false,
                                   string command = null,
                                   bool verbose = false)
        {
            var ret = new StringBuilder();
            var classApi = ClassApi.GetFromResource(classApiRoot, resource);
            if (classApi == null)
            {
                ret.AppendLine($"no such resource '{resource}'");
            }
            else
            {
                foreach (var method in classApi.Methods.OrderBy(a => a.MethodType))
                {
                    //exclude other command
                    if (!string.IsNullOrWhiteSpace(command) && method.GetMethodTypeHumanized().ToLower() != command) { continue; }

                    ret.Append($"USAGE: {method.GetMethodTypeHumanized()} {resource}");

                    //only parameters no keys
                    var parameters = method.Parameters.Where(a => !classApi.Keys.Contains(a.Name));

                    var opts = string.Join("", parameters.Where(a => !a.Optional).Select(a => $" {a.Name}:<{a.Type}>"));
                    if (!string.IsNullOrWhiteSpace(opts)) { ret.Append(opts); }

                    //optional parameter
                    if (parameters.Where(a => a.Optional).Count() > 0) { ret.Append(" [OPTIONS]"); }

                    ret.AppendLine();

                    if (verbose)
                    {
                        ret.AppendLine().AppendLine("  " + method.Comment);
                        CreateTable(parameters, ret);
                    }

                    if (returns)
                    {
                        //show returns
                        ret.AppendLine("RETURNS:");
                        CreateTable(method.ReturnParameters, ret);
                    }

                    if (verbose) { ret.AppendLine(); }
                }
            }

            return ret.ToString();
        }

        private static string[] JoinWord(string[] words, int numChar, string separator)
        {
            var ret = new List<string>();
            var line = "";
            foreach (var item in words)
            {
                if (!string.IsNullOrWhiteSpace(line)) { line += separator; }
                line += item;
                if (line.Length >= numChar)
                {
                    ret.Add(line.Trim());
                    line = "";
                }
            }

            if (!string.IsNullOrWhiteSpace(line)) { ret.Add(line.Trim()); }
            return ret.ToArray();
        }

        /// <summary>
        /// List values resource
        /// </summary>
        /// <param name="client"></param>
        /// <param name="classApiRoot"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static (IEnumerable<(string Attribute, string Value)> Values, string Error) ListValues(Client client,
                                                                                                      ClassApi classApiRoot,
                                                                                                      string resource)
        {
            var values = new List<(string Attribute, string Value)>();
            var error = "";

            var classApi = ClassApi.GetFromResource(classApiRoot, resource);
            if (classApi == null)
            {
                error = $"no such resource '{resource}'";
            }
            else
            {
                if (classApi.SubClasses.Count == 0)
                {
                    error = $"resource '{resource}' does not define child links";
                }
                else
                {
                    string key = null;
                    foreach (var subClass in classApi.SubClasses.OrderBy(a => a.Name))
                    {
                        var attribute = string.Join("",
                                                    new string[] { subClass.SubClasses.Count > 0 ? "D" : "-",
                                                                   "r--",
                                                                   subClass.Methods.Any(a => a.IsPost)? "c" : "-"});

                        if (subClass.IsIndexed)
                        {
                            var result = client.Get(resource);
                            if (result.InError())
                            {
                                error = result.GetError();
                            }
                            else
                            {
                                if (key == null)
                                {
                                    key = classApi.Methods.Where(a => a.IsGet)
                                                          .FirstOrDefault()
                                                          .ReturnLinkHRef
                                                          .Replace("{", "")
                                                          .Replace("}", "");
                                }

                                if (result.Response.data != null)
                                {
                                    var data = new List<object>();
                                    foreach (IDictionary<string, object> item in result.Response.data) { data.Add(item[key]); }
                                    foreach (var item in data.OrderBy(a => a)) { values.Add((attribute, item + "")); }
                                }
                            }
                        }
                        else
                        {
                            values.Add((attribute, subClass.Name));
                        }
                    }
                }
            }

            return (values, error);
        }

        /// <summary>
        /// List structure
        /// </summary>
        /// <param name="client"></param>
        /// <param name="classApiRoot"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static string List(Client client, ClassApi classApiRoot, string resource)
        {
            var (Values, Error) = ListValues(client, classApiRoot, resource);
            return string.Join(Environment.NewLine, Values.Select(a => $"{a.Attribute}        {a.Value}")) +
                   (string.IsNullOrWhiteSpace(Error) ? "" : Environment.NewLine + Error) +
                   Environment.NewLine;
        }
    }
}