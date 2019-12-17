/*
 * This file is part of the cv4pve-cli https://github.com/Corsinvest/cv4pve-cli,
 *
 * This source file is available under two different licenses:
 * - GNU General Public License version 3 (GPLv3)
 * - Corsinvest Enterprise License (CEL)
 * Full copyright and license information is available in
 * LICENSE.md which is distributed with this source code.
 *
 * Copyright (C) 2016 Corsinvest Srl	GPLv3 and CEL
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Metadata;
using McMaster.Extensions.CommandLineUtils;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Corsinvest.ProxmoxVE.Api.Shell.Utility;

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
            ApiCommands(parent, null, null);
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

                cmd.OnExecute(() => IntercativeShellCommands.Start(parent.Out,
                                                                   parent.ClientTryLogin(),
                                                                   optFile.Value(),
                                                                   optOnlyResult.HasValue()));
            });
        }

        private static ClassApi GetClassApiRoot(PveClient client)
            => GeneretorClassApi.Generate(client.Hostname, client.Port);

        /// <summary>
        /// Sub commands
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="client"></param>
        /// <param name="classApiRoot"></param>
        internal static void ApiCommands(CommandLineApplication parent,
                                         PveClient client,
                                         ClassApi classApiRoot)
        {
            Execute(parent, MethodType.Get, "Get (GET) from resource", client, classApiRoot);
            Execute(parent, MethodType.Set, "Set (PUT) from resource", client, classApiRoot);
            Execute(parent, MethodType.Create, "Create (POST) from resource", client, classApiRoot);
            Execute(parent, MethodType.Delete, "Delete (DELETE) from resource", client, classApiRoot);

            Usage(parent, classApiRoot);
            List(parent, client, classApiRoot);
        }

        private static CommandArgument CreateResourceArgument(CommandLineApplication parent)
            => parent.Argument("resource", "Resource api request", false).IsRequired();

        private static void Execute(CommandLineApplication parent,
                                    MethodType methodType,
                                    string description,
                                    PveClient client,
                                    ClassApi classApiRoot)
        {
            parent.Command(methodType.ToString().ToLower(), cmd =>
            {
                cmd.Description = description;
                cmd.AddFullNameLogo();

                var optVerbose = cmd.VerboseOption();
                var argResource = CreateResourceArgument(cmd);
                var argParameters = cmd.Argument("parameters",
                                                 "Parameter for resource format key:value (Multiple)." +
                                                 " If value have space or special charapter using quote 'key:value'",
                                                 true);
                var optOutput = cmd.OptionEnum<ApiExplorer.OutputType>("--output|-o", "Type output (default: unicode)");
                var optWait = cmd.WaitOption();

                cmd.OnExecute(() =>
                {
                    client = client ?? parent.ClientTryLogin();
                    classApiRoot = classApiRoot ?? GetClassApiRoot(client);
                    var ret = ApiExplorer.Execute(client,
                                                  classApiRoot,
                                                  argResource.Value,
                                                  methodType,
                                                  ApiExplorer.CreateParameterResource(argParameters.Values),
                                                  optWait.HasValue(),
                                                  optOutput.GetEnumValue<ApiExplorer.OutputType>(),
                                                  optVerbose.HasValue());

                    parent.Out.Write(ret.ResultText);
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
                var optOutput = cmd.OptionEnum<ApiExplorer.OutputType>("--output|-o", "Type output (default: unicode)");

                cmd.OnExecute(() =>
                {
                    parent.Out.Write(ApiExplorer.Usage(classApiRoot ?? GetClassApiRoot(parent.ClientTryLogin()),
                                                       argResource.Value,
                                                       optOutput.GetEnumValue<ApiExplorer.OutputType>(),
                                                       optResturns.HasValue(),
                                                       optCommand.Value(),
                                                       optVerbose.HasValue()));
                });
            });
        }

        private static void List(CommandLineApplication parent, PveClient client, ClassApi classApiRoot)
        {
            parent.Command("ls", cmd =>
            {
                cmd.Description = "List child objects on <api_path>.";
                cmd.AddFullNameLogo();

                var argResource = CreateResourceArgument(cmd);

                cmd.OnExecute(() =>
                {
                    client = client ?? cmd.ClientTryLogin();
                    parent.Out.Write(ApiExplorer.List(client,
                                                      classApiRoot ?? GetClassApiRoot(client),
                                                      argResource.Value));

                });
            });
        }
    }
}