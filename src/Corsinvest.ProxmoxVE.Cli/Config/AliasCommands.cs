/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.CommandLine;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Cli.Config;

/// <summary>
/// Alias subcommands (alias list, alias add, alias remove)
/// </summary>
internal static class AliasCommands
{
    public static void AddAliasCommands(this RootCommand root)
    {
        var cmd = root.AddCommand("alias", "Manage command aliases");

        CmdList(cmd);
        CmdAdd(cmd, root);
        CmdRemove(cmd);
    }

    private static readonly string[] Columns = ["name", "description"];
    private static readonly string[] ColumnsVerbose = ["name", "description", "command", "args"];

    private static IEnumerable<string> GetUserAliasNames()
    {
        try { return PveConfigManager.GetUserAliasNames(); }
        catch { return []; }
    }

    private static void CmdList(Command parent)
    {
        var cmd = parent.AddCommand("list", "List all aliases");
        var optVerbose = cmd.VerboseOption();
        var optOutput = cmd.TableOutputOption();
        var optSearch = cmd.AddOption<string>("--search|-s", "Filter by name, description or command");

        cmd.SetAction((action) =>
        {
            var aliases = PveConfigManager.LoadAliases().OrderBy(a => a.Name).AsEnumerable();
            var search = action.GetValue(optSearch);
            var verbose = action.GetValue(optVerbose);
            var output = action.GetValue(optOutput);

            if (!string.IsNullOrWhiteSpace(search))
            {
                aliases = aliases.Where(a => a.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                                          || a.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
                                          || a.Command.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var list = aliases.ToList();
            if (list.Count == 0) { Console.WriteLine("No aliases found."); return; }

            var columns = verbose ? ColumnsVerbose : Columns;
            var rows = list.Select(a => verbose
                ? (IEnumerable<object>)[a.Name, a.Description, a.Command, GetTags(a.Command)]
                : [a.Name, a.Description]);

            Console.Write(TableGenerator.To(columns, rows, output));
        });
    }

    private static string GetTags(string command)
    {
        var tags = new List<string>();
        foreach (var seg in command.Split('/'))
        {
            if (seg.StartsWith('{') && seg.EndsWith('}'))
            {
                tags.Add(seg[1..^1]);
            }

        }
        return string.Join(",", tags);
    }

    private static void CmdAdd(Command parent, RootCommand root)
    {
        var cmd = parent.AddCommand("add", "Add a new alias");
        var argName = cmd.AddArgument<string>("name", "Alias name (no spaces)");
        var optCommand = cmd.AddOption<string>("--command|-c", "API command (e.g. 'get /nodes')");
        var optDescription = cmd.AddOption<string>("--description|-d", "Description");
        optCommand.Required = true;

        cmd.SetAction((action) =>
        {
            try
            {
                var name = action.GetValue(argName)!;
                if (name.Contains(' ')) { throw new InvalidOperationException("Alias name cannot contain spaces."); }
                if (PveConfigManager.IsBuiltinAlias(name)) { throw new InvalidOperationException($"'{name}' is a built-in alias and cannot be overridden."); }
                var reserved = root.Subcommands.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (reserved.Contains(name)) { throw new InvalidOperationException($"'{name}' is a reserved command name."); }

                PveConfigManager.AddUserAlias(new PveConfigManager.PveAlias(name,
                                                                            action.GetValue(optDescription) ?? string.Empty,
                                                                            action.GetValue(optCommand)!));
                Console.WriteLine($"Alias '{name}' added.");
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        });
    }

    private static void CmdRemove(Command parent)
    {
        var cmd = parent.AddCommand("remove", "Remove a user alias");
        var argName = cmd.AddArgument<string>("name", "Alias name");
        argName.CompletionSources.Add((_) => GetUserAliasNames());
        cmd.SetAction((action) =>
        {
            try
            {
                var name = action.GetValue(argName)!;
                if (PveConfigManager.IsBuiltinAlias(name)) { throw new InvalidOperationException($"'{name}' is a built-in alias and cannot be removed."); }
                PveConfigManager.RemoveUserAlias(name);
                Console.WriteLine($"Alias '{name}' removed.");
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        });
    }
}
