/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System;
using System.CommandLine;
using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Metadata;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Microsoft.Extensions.Logging;

namespace Corsinvest.ProxmoxVE.Cli
{
    /// <summary>
    /// Api commands
    /// </summary>
    public class ShellCommands
    {
        private static ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initialize commands
        /// </summary>
        /// <param name="command"></param>
        public static void CreateCommands(RootCommand command)
        {
            _loggerFactory = ConsoleHelper.CreateLoggerFactory<Program>(command.GetLogLevelFromDebug());

            ApiCommands(command, null, null);
            InteractiveShell(command);
        }

        /// <summary>
        /// Interactive Shell
        /// </summary>
        /// <param name="command"></param>
        public static void InteractiveShell(RootCommand command)
        {
            var cmd = command.AddCommand("sh", "Interactive shell");
            var optFile = cmd.AddOption("--script|-s", "Script file name").AddValidatorExistFile();
            var optOnlyResult = cmd.AddOption("--only-result|-r", "Only result");
            cmd.SetHandler(async () =>
            {
                await InteractiveShellCommands.Start(await command.ClientTryLogin(_loggerFactory),
                                                     optFile.GetValue(),
                                                     optOnlyResult.HasValue());
            });
        }

        private static async Task<ClassApi> GetClassApiRoot(PveClient client)
            => await GeneratorClassApi.Generate(client.Host, client.Port);

        /// <summary>
        /// Sub commands
        /// </summary>
        /// <param name="command"></param>
        /// <param name="client"></param>
        /// <param name="classApiRoot"></param>
        internal static void ApiCommands(RootCommand command, PveClient client, ClassApi classApiRoot)
        {
            Execute(command, MethodType.Get, "Get (GET) from resource", client, classApiRoot);
            Execute(command, MethodType.Set, "Set (PUT) from resource", client, classApiRoot);
            Execute(command, MethodType.Create, "Create (POST) from resource", client, classApiRoot);
            Execute(command, MethodType.Delete, "Delete (DELETE) from resource", client, classApiRoot);

            Usage(command, classApiRoot);
            List(command, client, classApiRoot);
        }

        private static Argument<string> CreateResourceArgument(Command command) => command.AddArgument<string>("resource", "Resource api request");

        private static void Execute(RootCommand command,
                                    MethodType methodType,
                                    string description,
                                    PveClient client,
                                    ClassApi classApiRoot)
        {
            var cmd = new Command(methodType.ToString().ToLower(), description);
            var optVerbose = cmd.VerboseOption();
            var argResource = CreateResourceArgument(cmd);
            var argParameters = cmd.AddArgument<string[]>("parameters", "Parameter for resource format key:value (Multiple).");
            argParameters.SetDefaultValue(null);

            var optOutput = cmd.TableOutputOption();
            var optWait = cmd.AddOption<bool>("--wait", "Wait for task finish");
            command.AddCommand(cmd);

            cmd.SetHandler(async (string resource,
                                  string[] parameters,
                                  TableGenerator.Output output,
                                  bool verbose,
                                  bool wait) =>
            {
                client ??= await command.ClientTryLogin(_loggerFactory);
                classApiRoot ??= await GetClassApiRoot(client);
                var (ResultCode, ResultText) = await ApiExplorerHelper.Execute(client,
                                                                               classApiRoot,
                                                                               resource,
                                                                               methodType,
                                                                               ApiExplorerHelper.CreateParameterResource(parameters),
                                                                               wait,
                                                                               output,
                                                                               verbose);

                Console.Out.Write(ResultText);
            }, argResource, argParameters, optOutput, optVerbose, optWait);
        }

        private static void Usage(RootCommand command, ClassApi classApiRoot)
        {
            var cmd = command.AddCommand("usage", "Usage resource");
            var argResource = CreateResourceArgument(cmd);
            var optVerbose = cmd.VerboseOption();
            var optCommand = cmd.AddOption<MethodType?>("--command|-c", "API command");
            var optReturns = cmd.AddOption<bool>("--returns|-r", "Including schema for returned data.");
            var optOutput = cmd.TableOutputOption();

            cmd.SetHandler(async (string resource,
                                  TableGenerator.Output output,
                                  bool returns,
                                  MethodType? methodCommand,
                                  bool verbose) =>
            {
                var ret = ApiExplorerHelper.Usage(classApiRoot ?? await GetClassApiRoot(await command.ClientTryLogin(_loggerFactory)),
                                                  resource,
                                                  output,
                                                  returns,
                                                  methodCommand?.ToString(),
                                                  verbose);

                Console.Out.Write(ret);
            }, argResource, optOutput, optReturns, optCommand, optVerbose);
        }

        private static void List(RootCommand command, PveClient client, ClassApi classApiRoot)
        {
            var cmd = command.AddCommand("ls", "List child objects on <api_path>.");
            var argResource = CreateResourceArgument(cmd);
            cmd.SetHandler(async (string resource) =>
            {
                client ??= await command.ClientTryLogin(_loggerFactory);
                Console.Out.Write(await ApiExplorerHelper.List(client,
                                                               classApiRoot ?? await GetClassApiRoot(client),
                                                               resource));
            }, argResource);
        }
    }
}