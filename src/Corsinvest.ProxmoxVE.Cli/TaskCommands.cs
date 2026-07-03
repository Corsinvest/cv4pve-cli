/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.CommandLine;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Cli;

/// <summary>
/// Task subcommands (list, show, wait, log, stop) for async Proxmox tasks (UPID).
/// </summary>
internal static class TaskCommands
{
    private static readonly string[] ListColumns = ["upid", "type", "id", "status", "node", "user"];

    public static void AddTaskCommands(this RootCommand root)
    {
        var cmd = root.AddCommand("task", "Manage async tasks (UPID)");
        CmdList(cmd);
        CmdShow(cmd);
        CmdWait(cmd);
        CmdLog(cmd);
        CmdStop(cmd);
    }

    private static Argument<string> AddUpidArgument(Command cmd)
    {
        var arg = cmd.AddArgument<string>("upid", "Task identifier (UPID)");
        arg.HelpName = "upid";
        return arg;
    }

    private static void CmdList(Command parent)
    {
        var cmd = parent.AddCommand("list", "List recent tasks");
        var optNode = cmd.AddOption<string?>("--node", "Limit to a specific node (default: whole cluster)");
        var optOutput = cmd.TableOutputOption();
        cmd.SetAction(async (action) =>
        {
            try
            {
                var client = await ShellCommands.GetClientAsync();
                var node = action.GetValue(optNode);
                var result = string.IsNullOrWhiteSpace(node)
                                ? await client.Cluster.Tasks.Tasks()
                                : await client.Nodes[node].Tasks.NodeTasks();

                var rows = result.ToEnumerable()
                                 .Select(t => ListColumns.Select(c => Field((object)t, c)).ToArray())
                                 .ToArray();
                Console.Out.WriteLine(TableGenerator.To(ListColumns, rows, action.GetValue(optOutput)));
                return (int)ExitCode.Ok;
            }
            catch (Exception ex) { return ExitCodeHelper.Fail(ex); }
        });
    }

    private static void CmdShow(Command parent)
    {
        var cmd = parent.AddCommand("show", "Show status of a task");
        var argUpid = AddUpidArgument(cmd);
        cmd.SetAction(async (action) =>
        {
            try
            {
                var client = await ShellCommands.GetClientAsync();
                var upid = action.GetValue(argUpid)!;
                var status = await client.Nodes[PveClientBase.GetNodeFromTask(upid)].Tasks[upid].Status.GetAsync();

                Console.Out.WriteLine($"UPID:       {status.Upid}");
                Console.Out.WriteLine($"Node:       {status.Node}");
                Console.Out.WriteLine($"Type:       {status.Type}");
                Console.Out.WriteLine($"Id:         {status.Id}");
                Console.Out.WriteLine($"User:       {status.User}");
                Console.Out.WriteLine($"Status:     {status.Status}");
                Console.Out.WriteLine($"ExitStatus: {status.ExitStatus}");

                // running → 0; finished OK → 0; finished with error → task-failed
                if (status.Status != "running" && !IsOk(status.ExitStatus)) { return (int)ExitCode.TaskFailed; }
                return (int)ExitCode.Ok;
            }
            catch (Exception ex) { return ExitCodeHelper.Fail(ex); }
        });
    }

    private static void CmdWait(Command parent)
    {
        var cmd = parent.AddCommand("wait", "Wait for a task to finish");
        var argUpid = AddUpidArgument(cmd);
        var optTimeout = cmd.AddOption<int>("--timeout", "Timeout in seconds");
        optTimeout.DefaultValueFactory = (_) => 300;
        cmd.SetAction(async (action) =>
        {
            try
            {
                var client = await ShellCommands.GetClientAsync();
                var upid = action.GetValue(argUpid)!;
                var timeoutMs = action.GetValue(optTimeout) * 1000L;

                var finished = await client.WaitForTaskToFinishAsync(upid, wait: 2000, timeout: timeoutMs);
                if (!finished)
                {
                    Console.Error.WriteLine("Error: timeout waiting for task to finish.");
                    return (int)ExitCode.TaskFailed;
                }

                var exit = await client.GetExitStatusTaskAsync(upid);
                Console.Out.WriteLine(exit);
                return IsOk(exit) ? (int)ExitCode.Ok : (int)ExitCode.TaskFailed;
            }
            catch (Exception ex) { return ExitCodeHelper.Fail(ex); }
        });
    }

    private static void CmdLog(Command parent)
    {
        var cmd = parent.AddCommand("log", "Print the log of a task");
        var argUpid = AddUpidArgument(cmd);
        var optFollow = cmd.AddOption<bool>("--follow|-f", "Follow the log until the task finishes");
        var optLimit = cmd.AddOption<int>("--limit", "Max lines per poll");
        optLimit.DefaultValueFactory = (_) => 500;
        var optInterval = cmd.AddOption<int>("--interval", "Seconds between polls when following (min: 1)");
        optInterval.DefaultValueFactory = (_) => 2;
        cmd.SetAction(async (action) =>
        {
            try
            {
                var client = await ShellCommands.GetClientAsync();
                var upid = action.GetValue(argUpid)!;
                var follow = action.GetValue(optFollow);
                var limit = action.GetValue(optLimit);
                var intervalMs = Math.Max(1, action.GetValue(optInterval)) * 1000;

                var item = client.Nodes[PveClientBase.GetNodeFromTask(upid)].Tasks[upid];
                var start = 0;

                while (true)
                {
                    // Read status BEFORE draining the log, so the final poll still
                    // captures lines written between the last poll and task end.
                    var status = await item.Status.GetAsync();
                    var running = status.Status == "running";

                    var lines = (await item.Log.GetAsync(limit, start)).ToList();
                    foreach (var line in lines) { Console.Out.WriteLine(line); }
                    start += lines.Count;

                    if (!running || !follow) { return running || IsOk(status.ExitStatus) ? (int)ExitCode.Ok : (int)ExitCode.TaskFailed; }
                    await Task.Delay(intervalMs);
                }
            }
            catch (Exception ex) { return ExitCodeHelper.Fail(ex); }
        });
    }

    private static void CmdStop(Command parent)
    {
        var cmd = parent.AddCommand("stop", "Stop (kill) a running task");
        var argUpid = AddUpidArgument(cmd);
        var optYes = cmd.AddOption<bool>("--yes|-y", "Confirm stopping the task");
        cmd.SetAction(async (action) =>
        {
            try
            {
                if (!action.GetValue(optYes))
                {
                    Console.Error.WriteLine("Error: stopping a task is disruptive. Use --yes / -y to confirm.");
                    return (int)ExitCode.Validation;
                }
                var client = await ShellCommands.GetClientAsync();
                var upid = action.GetValue(argUpid)!;
                await client.Nodes[PveClientBase.GetNodeFromTask(upid)].Tasks[upid].StopTask();
                Console.Out.WriteLine($"Task '{upid}' stop requested.");
                return (int)ExitCode.Ok;
            }
            catch (Exception ex) { return ExitCodeHelper.Fail(ex); }
        });
    }

    // PVE exitstatus is "OK" or "OK: ..." on success; anything else is a failure.
    private static bool IsOk(string? exitStatus)
        => !string.IsNullOrWhiteSpace(exitStatus)
           && exitStatus.StartsWith("OK", StringComparison.OrdinalIgnoreCase);

    // Read a property from a dynamic task row (ExpandoObject), empty string if absent.
    private static object Field(object row, string name)
        => row is IDictionary<string, object> dict && dict.TryGetValue(name, out var v) && v != null
                ? v
                : string.Empty;
}
