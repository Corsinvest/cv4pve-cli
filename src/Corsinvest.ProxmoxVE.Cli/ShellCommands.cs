/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.CommandLine;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Metadata;
using Microsoft.Extensions.Logging;

namespace Corsinvest.ProxmoxVE.Cli;

/// <summary>
/// Api commands
/// </summary>
internal class ShellCommands
{
    public static readonly string AppName = "cv4pve-cli";

    private static ILoggerFactory _loggerFactory = null!;

    /// <summary>
    /// Initialize commands
    /// </summary>
    /// <param name="command"></param>
    /// <param name="loggerFactory"></param>
    public static void CreateCommands(RootCommand command, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;

        ApiCommands(command, null!, null!);
        InteractiveShell(command);
    }

    /// <summary>
    /// Interactive Shell
    /// </summary>
    /// <param name="command"></param>
    public static void InteractiveShell(RootCommand command)
    {
        var cmd = command.AddCommand("sh", "Interactive shell");
        var optScriptFile = cmd.AddOption<string>("--script|-s", "Script file name").AddValidatorExistFile();
        var optOnlyResult = cmd.AddOption<bool>("--only-result|-r", "Only result");
        cmd.SetAction(async (action) =>
        {
            await Cli.InteractiveShell.StartAsync(await command.ClientTryLoginAsync(_loggerFactory),
                                                  action.GetValue(optScriptFile)!,
                                                  action.GetValue(optOnlyResult));
        });
    }

    private static async Task<ClassApi> GetClassApiRootAsync(PveClient client)
        => await GeneratorClassApi.GenerateAsync(client.Host, client.Port);

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
        argParameters.DefaultValueFactory = (_) => null!;

        var optOutput = cmd.TableOutputOption();
        var optWait = cmd.AddOption<bool>("--wait", "Wait for task finish");
        command.Add(cmd);

        cmd.SetAction(async (action) =>
        {
            client ??= await command.ClientTryLoginAsync(_loggerFactory);
            classApiRoot ??= await GetClassApiRootAsync(client);

            var (_, ResultText) = await ApiExplorerHelper.ExecuteAsync(client,
                                                                       classApiRoot,
                                                                       action.GetValue(argResource),
                                                                       methodType,
                                                                       ApiExplorerHelper.CreateParameterResource(action.GetValue(argParameters)),
                                                                       action.GetValue(optWait),
                                                                       action.GetValue(optOutput),
                                                                       action.GetValue(optVerbose));

            Console.Out.Write(ResultText);
        });
    }

    private static void Usage(RootCommand command, ClassApi classApiRoot)
    {
        var cmd = command.AddCommand("usage", "Usage resource");
        var argResource = CreateResourceArgument(cmd);
        var optVerbose = cmd.VerboseOption();
        var optCommand = cmd.AddOption<MethodType?>("--command|-c", "API command");
        var optReturns = cmd.AddOption<bool>("--returns|-r", "Including schema for returned data.");
        var optOutput = cmd.TableOutputOption();

        cmd.SetAction(async (action) =>
        {
            var ret = ApiExplorerHelper.Usage(classApiRoot ?? await GetClassApiRootAsync(await command.ClientTryLoginAsync(_loggerFactory)),
                                              action.GetValue(argResource),
                                              action.GetValue(optOutput),
                                              action.GetValue(optReturns),
                                              action.GetValue(optCommand)?.ToString(),
                                              action.GetValue(optVerbose));

            Console.Out.Write(ret);
        });
    }

    private static void List(RootCommand command, PveClient client, ClassApi classApiRoot)
    {
        var cmd = command.AddCommand("ls", "List child objects on <api_path>.");
        var argResource = CreateResourceArgument(cmd);
        cmd.SetAction(async (action) =>
        {
            client ??= await command.ClientTryLoginAsync(_loggerFactory);
            Console.Out.Write(await ApiExplorerHelper.ListAsync(client,
                                                                classApiRoot ?? await GetClassApiRootAsync(client),
                                                                action.GetValue(argResource)));
        });
    }
}