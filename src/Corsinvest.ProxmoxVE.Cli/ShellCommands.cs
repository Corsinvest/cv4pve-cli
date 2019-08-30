using System;
using System.Collections.Generic;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Utils.Shell;
using Corsinvest.ProxmoxVE.Api.Metadata;
using McMaster.Extensions.CommandLineUtils;

namespace Corsinvest.ProxmoxVE.Cli
{
    /// <summary>
    /// Api commands
    /// </summary>
    public class ShellCommands
    {
        /// <summary>
        /// Initialize commands
        /// </summary>
        /// <param name="parent"></param>
        public static void Commands(CommandLineApplication parent)
        {
            SumCommands(parent, null, null);
            IntercativeShell(parent);
        }

        /// <summary>
        /// Interactive Shell
        /// </summary>
        /// <param name="parent"></param>
        public static void IntercativeShell(CommandLineApplication parent)
        {
            parent.Command("sh", cmd =>
            {
                cmd.Description = "Interactive shell";
                cmd.AddFullNameLogo();

                var optFile = cmd.Option("--script|-s", "Script file name", CommandOptionType.SingleValue);
                optFile.Accepts().ExistingFile();

                var optOnlyResult = cmd.Option("--only-result|-r", "Only result", CommandOptionType.NoValue);

                cmd.OnExecute(() => IntercativeShellCommands.Start(parent.ClientTryLogin(),
                                                                   optFile.Value(),
                                                                   optOnlyResult.HasValue()));
            });
        }

        internal static int PrintResult(CommandLineApplication parent,
                                        string resource,
                                        Dictionary<string, object> parameters)
        {
            var (host, port) = parent.GetHostAndPort();

            var ret = Cli.Commands.Execute(parent.ClientTryLogin(),
                                          GeneretorClassApi.Generate(host, port),
                                          resource,
                                          MethodType.Get,
                                          parameters);

            Console.Out.Write(ret.ResultText);
            return ret.ResultCode == 200 ? 0 : 1;
        }

        private static ClassApi GetClassApiRoot(CommandLineApplication parent)
        {
            var (host, port) = parent.GetHostAndPort();
            return GeneretorClassApi.Generate(host, port);
        }

        /// <summary>
        /// Sub commands
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="client"></param>
        /// <param name="classApiRoot"></param>
        internal static void SumCommands(CommandLineApplication parent,
                                         Client client,
                                         ClassApi classApiRoot)
        {
            HttpMethod(parent, MethodType.Get, "Get (GET) from resource", client, classApiRoot);
            HttpMethod(parent, MethodType.Set, "Set (PUT) from resource", client, classApiRoot);
            HttpMethod(parent, MethodType.Create, "Create (POST) from resource", client, classApiRoot);
            HttpMethod(parent, MethodType.Delete, "Delete (DELETE) from resource", client, classApiRoot);

            Usage(parent, classApiRoot);
            List(parent, client, classApiRoot);
        }

        private static CommandArgument CreateResourceArgument(CommandLineApplication parent)
            => parent.Argument("resource", "Resource api request", false).IsRequired();

        private static void HttpMethod(CommandLineApplication parent,
                                       MethodType methodType,
                                       string description,
                                       Client client,
                                       ClassApi classApiRoot)
        {
            parent.Command(methodType.ToString().ToLower(), cmd =>
            {
                cmd.Description = description;
                cmd.AddFullNameLogo();

                var optVerbose = cmd.VerboseOption();
                var argResource = CreateResourceArgument(cmd);
                var argParameters = cmd.Argument("parameters", "Parameter for resource format key:value (Multiple)", true);
                var optOutput = cmd.OptionEnum<OutputType>("--output|-o", "Type output (default: text)");
                var optWait = cmd.WaitOption();

                cmd.OnExecute(() =>
                {
                    //creation parameter
                    var parameters = new Dictionary<string, object>();
                    foreach (var value in argParameters.Values)
                    {
                        var pos = value.IndexOf(":");
                        parameters.Add(value.Substring(0, pos), value.Substring(pos + 1));
                    }

                    var ret = Cli.Commands.Execute(client ?? parent.ClientTryLogin(),
                                                  classApiRoot ?? GetClassApiRoot(parent),
                                                  argResource.Value,
                                                  methodType,
                                                  parameters,
                                                  optWait.HasValue(),
                                                  optOutput.GetEnumValue<OutputType>(),
                                                  optVerbose.HasValue());

                    Console.Out.Write(ret.ResultText);
                    return ret.ResultCode;
                });
            });
        }

        private static void Usage(CommandLineApplication parent, ClassApi classApiRoot)
        {
            parent.Command("usage", cmd =>
            {
                cmd.Description = "Usage resource";
                cmd.AddFullNameLogo();

                var argResource = CreateResourceArgument(cmd);
                var optVerbose = cmd.VerboseOption();
                var optCommand = cmd.OptionEnum<MethodType>("--command|-c", "API command");
                var optResturns = cmd.Option("--returns|-r", "Including schema for returned data.", CommandOptionType.NoValue);

                cmd.OnExecute(() =>
                {
                    Console.Out.Write(Cli.Commands.Usage(classApiRoot ?? GetClassApiRoot(parent),
                                                        argResource.Value,
                                                        optResturns.HasValue(),
                                                        optCommand.Value(),
                                                        optVerbose.HasValue()));
                });
            });
        }

        private static void List(CommandLineApplication parent, Client client, ClassApi classApiRoot)
        {
            parent.Command("ls", cmd =>
            {
                cmd.Description = "List child objects on <api_path>.";
                cmd.AddFullNameLogo();

                var argResource = CreateResourceArgument(cmd);
                cmd.OnExecute(() => Console.Out.Write(Cli.Commands.List(client ?? cmd.ClientTryLogin(),
                                                                       classApiRoot ?? GetClassApiRoot(parent),
                                                                       argResource.Value)));
            });
        }
    }
}