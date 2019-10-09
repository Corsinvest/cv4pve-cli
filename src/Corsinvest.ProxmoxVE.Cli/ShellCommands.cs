/*
 * This file is part of the cv4pve-cli https://github.com/Corsinvest/cv4pve-cli,
 * Copyright (C) 2016 Corsinvest Srl
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Utility;
using Corsinvest.ProxmoxVE.Api.Extension.Helpers.Shell;
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
                                                 " If value have space or special charapter using 'key:value'",
                                                 true);
                var optOutput = cmd.OptionEnum<ApiExplorer.OutputType>("--output|-o", "Type output (default: text)");
                var optWait = cmd.WaitOption();

                cmd.OnExecute(() =>
                {
                    var ret = ApiExplorer.Execute(client ?? parent.ClientTryLogin(),
                                                  classApiRoot ?? GetClassApiRoot(parent),
                                                  argResource.Value,
                                                  methodType,
                                                  ApiExplorer.CreateParameterResource(argParameters.Values),
                                                  optWait.HasValue(),
                                                  optOutput.GetEnumValue<ApiExplorer.OutputType>(),
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
                    Console.Out.Write(ApiExplorer.Usage(classApiRoot ?? GetClassApiRoot(parent),
                                                        argResource.Value,
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
                cmd.OnExecute(() => Console.Out.Write(ApiExplorer.List(client ?? cmd.ClientTryLogin(),
                                                                       classApiRoot ?? GetClassApiRoot(parent),
                                                                       argResource.Value)));
            });
        }
    }
}