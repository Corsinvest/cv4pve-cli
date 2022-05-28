/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Metadata;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;

namespace Corsinvest.ProxmoxVE.Cli
{
    /// <summary>
    /// Interactive Shell
    /// </summary>
    public class IntercativeShellCommands
    {
        private const string Castle = @"                                  .-.
                                 /___\
                                 |___|
                                 |]_[|
                                 / I \
                              JL/  |  \JL
   .-.                    i   ()   |   ()   i                    .-.
   |_|     .^.           /_\  LJ=======LJ  /_\           .^.     |_|
._/___\._./___\_._._._._.L_J_/.-.     .-.\_L_J._._._._._/___\._./___\._._._
       ., |-,-| .,       L_J  |_| [I] |_|  L_J       ., |-,-| .,        .,
       JL |-O-| JL       L_J%%%%%%%%%%%%%%%L_J       JL |-O-| JL        JL
IIIIII_HH_'-'-'_HH_IIIIII|_|=======H=======|_|IIIIII_HH_'-'-'_HH_IIIIII_HH_
-------[]-------[]-------[_]----\.=I=./----[_]-------[]-------[]--------[]-
 _/\_  ||\\_I_//||  _/\_ [_] []_/_L_J_\_[] [_] _/\_  ||\\_I_//||  _/\_  ||\
 |__|  ||=/_|_\=||  |__|_|_|   _L_L_J_J_   |_|_|__|  ||=/_|_\=||  |__|  ||-
 |__|  |||__|__|||  |__[___]__--__===__--__[___]__|  |||__|__|||  |__|  |||
IIIIIII[_]IIIII[_]IIIIIL___J__II__|_|__II__L___JIIIII[_]IIIII[_]IIIIIIII[_]
 \_I_/ [_]\_I_/[_] \_I_[_]\II/[]\_\I/_/[]\II/[_]\_I_/ [_]\_I_/[_] \_I_/ [_]
./   \.L_J/   \L_J./   L_JI  I[]/     \[]I  IL_J    \.L_J/   \L_J./   \.L_J
|     |L_J|   |L_J|    L_J|  |[]|     |[]|  |L_J     |L_J|   |L_J|     |L_J
|_____JL_JL___JL_JL____|-||  |[]|     |[]|  ||-|_____JL_JL___JL_JL_____JL_J
";

        /// <summary>
        /// Start interactive shell
        /// </summary>
        /// <param name="client"></param>
        /// <param name="fileScript"></param>
        /// <param name="onlyResult"></param>
        public static async Task Start(PveClient client, string fileScript, bool onlyResult)
        {
            if (!onlyResult)
            {
                Console.Out.WriteLine($@"Corsinvest Interactive Shell for Proxmox VE ({DateTime.Now.ToLongDateString()})
Type '<TAB>' for completion word
Type 'help', 'quit' to close the application.");
            }

            #region ClassApi Metadata
            var watch = Stopwatch.StartNew();
            if (!onlyResult) { Console.Out.Write("Initialization metadata..."); }

            //get api metadata
            var classApiRoot = await GeneretorClassApi.Generate(client.Host, client.Port);

            watch.Stop();
            if (!onlyResult) { Console.Out.WriteLine($" {watch.ElapsedMilliseconds}ms"); }
            #endregion

            if (!onlyResult) { Console.Out.WriteLine(Environment.NewLine + ConsoleHelper.RememberTheseThings); }

            //Auto Completion
            ReadLine.AutoCompletionHandler = new AutoCompletionHandler
            {
                Client = client,
                ClassApiRoot = classApiRoot
            };

            LoadHistory();

            var aliasManager = new ApiExplorerHelper.AliasManager()
            {
                FileName = Path.Combine(FilesystemHelper.GetApplicationDataDirectory(Program.AppName), "alias.txt")
            };
            aliasManager.Load();

            if (File.Exists(fileScript))
            {
                //script file
                foreach (var line in File.ReadAllLines(fileScript))
                {
                    ParseLine(line, client, classApiRoot, aliasManager, onlyResult);
                }
            }
            else
            {
                //Interactive
                while (true)
                {
                    var input = ReadLine.Read(">>> ");
                    var exit = ParseLine(input, client, classApiRoot, aliasManager, onlyResult);

                    SaveHistory();
                    aliasManager.Save();

                    if (exit) { break; }
                }
            }
        }

        #region History
        private static string GetHistoryFile()
            => Path.Combine(FilesystemHelper.GetApplicationDataDirectory(Program.AppName), "history.txt");

        private static void LoadHistory()
        {
            var file = GetHistoryFile();
            if (!File.Exists(file)) { File.WriteAllLines(file, Array.Empty<string>()); }

            ReadLine.HistoryEnabled = true;
            ReadLine.AddHistory(File.ReadAllLines(file));
        }

        private static void SaveHistory()
        {
            if (ReadLine.HistoryEnabled)
            {
                //remove empty line
                var data = ReadLine.GetHistory().Where(a => !string.IsNullOrWhiteSpace(a));
                ReadLine.ClearHistory();
                ReadLine.AddHistory(data.ToArray());

                File.WriteAllLines(GetHistoryFile(), data.Skip(Math.Max(0, data.Count() - 100)));
            }
        }
        #endregion

        private static bool ParseLine(string input,
                                      PveClient client,
                                      ClassApi classApiRoot,
                                      ApiExplorerHelper.AliasManager aliasManager,
                                      bool onlyResult)
        {
            if (string.IsNullOrWhiteSpace(input)) { return false; }
            input = input.Trim();

            //comment
            if (input.StartsWith("#")) { return false; }

            var rc = new RootCommand();
            var exit = false;

            rc.Name = Program.AppName;
            rc.Description = "Corsinvest Interactive Shell API for Proxmox VE";
            rc.AddFullNameLogo();
            rc.DebugOption();
            rc.DryRunOption();

            ShellCommands.ApiCommands(rc, client, classApiRoot);

            //create command from alias
            CreateCommandFromAlias(rc, client, classApiRoot, aliasManager, onlyResult);

            #region Commands base
            rc.AddCommand("quit|exit", "Close application").SetHandler(() => exit = true);
            rc.AddCommand("clear|cls", "Clear screen").SetHandler(() => Console.Clear());
            var castle = rc.AddCommand("castle", "");
            castle.SetHandler(() => Console.Out.WriteLine(Castle));
            castle.IsHidden = true;

            CmdAlias(rc, aliasManager);
            CmdHistory(rc, onlyResult);
            #endregion

            //execute command
            try { rc.Invoke(input); }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            return exit;
        }

        private static void CreateCommandFromAlias(Command command,
                                                   PveClient client,
                                                   ClassApi classApiRoot,
                                                   ApiExplorerHelper.AliasManager aliasManager,
                                                   bool onlyResult)
        {
            foreach (var item in aliasManager.Alias)
            {
                var cmd = command.AddCommand(string.Join("|", item.Names), item.Description);
                cmd.IsHidden = true;
                //cmd.ExtendedHelpText = Environment.NewLine + "Alias command: " + item.Command;

                //create argument
                foreach (var arg in ApiExplorerHelper.GetArgumentTags(item.Command)) { cmd.AddArgument(arg, arg); }

                cmd.SetHandler(() =>
                {
                    var title = item.Description;
                    var command = item.Command;

                    //replace value into argument
                    foreach (var arg in cmd.Arguments)
                    {
                        title += $" {arg.Name}: {arg.GetValue()}";
                        command = command.Replace(ApiExplorerHelper.CreateArgumentTag(arg.Name), arg.GetValue());
                    }

                    if (!onlyResult)
                    {
                        Console.Out.WriteLine(title);
                        Console.Out.WriteLine("Command: " + command);
                    }

                    ParseLine(command, client, classApiRoot, aliasManager, onlyResult);
                });

            }
        }

        private static void CmdHistory(RootCommand command, bool onlyResult)
        {
            var cmd = command.AddCommand("history|h", "Show history command");
            var optEnabled = cmd.AddOption<bool>("--enabled|-e", "Enabled/Disable history");
            cmd.SetHandler(() =>
            {
                if (optEnabled.GetValue())
                {
                    ReadLine.HistoryEnabled = optEnabled.GetValue();
                    if (ReadLine.HistoryEnabled)
                    {
                        LoadHistory();
                    }
                    else
                    {
                        ReadLine.ClearHistory();
                    }
                }
                else
                {

                    if (!ReadLine.HistoryEnabled)
                    {
                        if (!onlyResult) { Console.Out.WriteLine("History disabled!"); }
                    }
                    else
                    {
                        var lineNum = 0;
                        foreach (var item in ReadLine.GetHistory())
                        {
                            Console.Out.WriteLine($"{lineNum} {item}");
                            lineNum++;
                        }
                    }
                }
            });
        }

        private static void CmdAlias(RootCommand command, ApiExplorerHelper.AliasManager aliasManager)
        {
            var cmd = command.AddCommand("alias", "Alias commands");
            var optCreate = cmd.AddOption<bool>("--create|-c", "Create new");
            var optRemove = cmd.AddOption<bool>("--remove|-r", "Delete");
            var optVerbose = cmd.VerboseOption();

            cmd.SetHandler(() =>
            {
                string GetName(string title, bool create)
                {
                    Console.Out.WriteLine(title);
                    var name = " ";
                    while (true)
                    {
                        name = ReadLine.Read("Name: ", name.Trim());
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            Console.Out.WriteLine($"Abort {title}");
                            break;
                        }

                        var exists = aliasManager.Exists(name);

                        if ((create && ApiExplorerHelper.AliasDef.IsValid(name) && !exists) ||
                            (!create && exists))
                        {
                            break;
                        }
                        else
                        {
                            Console.Out.WriteLine($"Alias '{name}' already exists!");
                        }
                    }

                    return name;
                }

                if (optCreate.GetValue())
                {
                    //create
                    var name = GetName("Create alias (using comma to more name)", true);
                    if (string.IsNullOrWhiteSpace(name)) { return; }

                    var description = ReadLine.Read("Description: ");

                    var command = ReadLine.Read("Command: ");
                    if (string.IsNullOrWhiteSpace(command))
                    {
                        Console.Out.WriteLine("Abort create alias");
                        return;
                    }

                    aliasManager.Create(name, description, command, false);

                    Console.Out.WriteLine($"Alias '{name}' created!");
                }
                else if (optRemove.GetValue())
                {
                    //remove
                    var name = GetName("Remove alias", false);
                    if (string.IsNullOrWhiteSpace(name)) { return; }
                    aliasManager.Remove(name);
                    Console.Out.WriteLine($"Alias '{name}' removed!");
                }
                else
                {
                    Console.Out.Write(aliasManager.ToTable(optVerbose.HasValue(), TableGenerator.Output.Text));
                }
            });
        }

        class AutoCompletionHandler : IAutoCompleteHandler
        {
            public PveClient Client { get; set; }
            public ClassApi ClassApiRoot { get; set; }
            public char[] Separators { get; set; } = new[] { '/' };

            public string[] GetSuggestions(string text, int index)
            {
                if (text.StartsWith("ls /") ||
                    text.StartsWith("create /") ||
                    text.StartsWith("delete /") ||
                    text.StartsWith("get /") ||
                    text.StartsWith("set /") ||
                    text.StartsWith("usage /"))
                {
                    var resource = text[text.IndexOf("/")..].Trim();

                    var ret = ApiExplorerHelper.ListValues(Client, ClassApiRoot, resource).Result;
                    if (!string.IsNullOrWhiteSpace(ret.Error))
                    {
                        //try previous slash
                        var pos = resource.LastIndexOf('/');
                        ret = ApiExplorerHelper.ListValues(Client, ClassApiRoot, resource[..pos]).Result;

                        return ret.Values.Where(a => a.Value.StartsWith(resource[(pos + 1)..]))
                                         .Select(a => a.Value)
                                         .ToArray();
                    }
                    else
                    {
                        return ret.Values.Select(a => a.Value).ToArray();
                    }
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
