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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Shell;
using Corsinvest.ProxmoxVE.Api.Extension.Utility;
using Corsinvest.ProxmoxVE.Api.Extension.Helpers;
using Corsinvest.ProxmoxVE.Api.Extension.Helpers.Shell;
using Corsinvest.ProxmoxVE.Api.Metadata;
using McMaster.Extensions.CommandLineUtils;

namespace Corsinvest.ProxmoxVE.Cli
{
    /// <summary>
    /// Interactive Shell
    /// </summary>
    public class IntercativeShellCommands
    {
        /// <summary>
        /// Start interactive shell
        /// </summary>
        /// <param name="client"></param>
        /// <param name="fileScript"></param>
        /// <param name="onlyResult"></param>
        public static void Start(PveClient client, string fileScript, bool onlyResult)
        {
            if (!onlyResult)
            {
                Console.Out.WriteLine($@"Corsinvest Interactive Shell for Proxmox VE ({DateTime.Now.ToLongDateString()})
Type '<TAB>' for completion word
Type 'help', 'quit' or 'CTRL+C' to close the application.");
            }

            #region ClassApi Metadata
            var watch = Stopwatch.StartNew();
            if (!onlyResult) { Console.Out.Write("Initialization metadata..."); }

            //get api metadata
            var classApiRoot = GeneretorClassApi.Generate(client.Hostname, client.Port);

            watch.Stop();
            if (!onlyResult) { Console.Out.WriteLine($" {watch.ElapsedMilliseconds}ms"); }
            #endregion

            if (!onlyResult) { Console.Out.WriteLine(Environment.NewLine + ShellHelper.REMEMBER_THESE_THINGS); }

            //Auto Completion
            ReadLine.AutoCompletionHandler = new AutoCompletionHandler()
            {
                Client = client,
                ClassApiRoot = classApiRoot
            };

            LoadHistory();

            var aliasManager = new AliasManager()
            {
                FileName = Path.Combine(ShellHelper.GetApplicationDataDirectory(Program.APP_NAME), "alias.txt")
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
        private static string GetHistoryFile() => Path.Combine(ShellHelper.GetApplicationDataDirectory(Program.APP_NAME),
                                                               "history.txt");

        private static void LoadHistory()
        {
            var file = GetHistoryFile();
            if (!File.Exists(file)) { File.WriteAllLines(file, new string[] { }); }

            ReadLine.HistoryEnabled = true;
            ReadLine.AddHistory(File.ReadAllLines(file));
        }

        private static void SaveHistory()
        {
            if (ReadLine.HistoryEnabled)
            {
                var data = ReadLine.GetHistory().Where(a => !string.IsNullOrWhiteSpace(a));
                File.WriteAllLines(GetHistoryFile(), data.Skip(Math.Max(0, data.Count() - 100)));
            }
        }
        #endregion

        private static bool ParseLine(string input,
                                      PveClient client,
                                      ClassApi classApiRoot,
                                      AliasManager aliasManager,
                                      bool onlyResult)
        {
            if (string.IsNullOrWhiteSpace(input)) { return false; }
            input = input.Trim();

            //comment
            if (input.StartsWith("#")) { return false; }

            using (var app = new CommandLineApplication())
            {
                var exit = false;

                app.Name = "";
                app.Description = "Corsinvest Interactive Shell API for Proxmox VE";
                app.DebugOption();
                app.DryRunOption();
                app.UsePagerForHelpText = false;
                app.HelpOption(true);

                ShellCommands.SumCommands(app, client, classApiRoot);

                //fix help text
                foreach (var command in app.Commands)
                {
                    command.FullName = app.Description;
                    command.ExtendedHelpText = "";
                    command.UsePagerForHelpText = false;
                }

                //create command from alias
                CreateCommandFromAlias(app, client, classApiRoot, aliasManager, onlyResult);

                #region Commands base
                app.Command("quit", cmd =>
                {
                    cmd.AddName("exit");
                    cmd.Description = "Close application";
                    cmd.OnExecute(() => exit = true);
                });

                app.Command("clear", cmd =>
                {
                    cmd.AddName("cls");
                    cmd.Description = "Clear screen";
                    cmd.OnExecute(() => Console.Clear());
                });

                app.Command("help", cmd =>
                {
                    cmd.Description = "Show help information";
                    cmd.OnExecute(() => app.ShowHelp());
                });

                CmdAlias(app, aliasManager);
                CmdHistory(app, onlyResult);
                #endregion

                app.OnExecute(() => app.ShowHint());

                //execute command
                try { app.Execute(StringHelper.TokenizeCommandLineToList(input).ToArray()); }
                catch (CommandParsingException ex) { Console.Out.WriteLine(ex.Message); }
                catch (Exception) { }

                return exit;
            }
        }

        private static void CreateCommandFromAlias(CommandLineApplication parent,
                                                   PveClient client,
                                                   ClassApi classApiRoot,
                                                   AliasManager aliasManager,
                                                   bool onlyResult)
        {
            foreach (var item in aliasManager.Alias)
            {
                parent.Command(item.Names[0], cmd =>
                {
                    foreach (var name in item.Names) { cmd.AddName(name); }
                    cmd.Description = item.Description;
                    cmd.ShowInHelpText = false;
                    cmd.ExtendedHelpText = Environment.NewLine + "Alias command: " + item.Command;

                    //create argument
                    foreach (var arg in StringHelper.GetArgumentTags(item.Command)) { cmd.Argument(arg, arg, false).IsRequired(); }

                    cmd.OnExecute(() =>
                    {
                        var title = item.Description;
                        var command = item.Command;

                        //replace value into argument
                        foreach (var arg in cmd.Arguments)
                        {
                            title += $" {arg.Name}: {arg.Value}";
                            command = command.Replace(StringHelper.CreateArgumentTag(arg.Name), arg.Value);
                        }

                        if (!onlyResult)
                        {
                            Console.Out.WriteLine(title);
                            Console.Out.WriteLine("Command: " + command);
                        }

                        ParseLine(command, client, classApiRoot, aliasManager, onlyResult);
                    });
                });
            }
        }

        private static void CmdHistory(CommandLineApplication app, bool onlyResult)
        {
            app.Command("history", cmd =>
            {
                cmd.Description = "Show history command";
                cmd.AddName("h");

                var optEnabled = cmd.OptionEnum("--enabled|-e", "Enabled/Disable history", "0", "1");

                cmd.OnExecute(() =>
                {
                    if (optEnabled.HasValue())
                    {
                        ReadLine.HistoryEnabled = optEnabled.Value() == "1";
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
            });
        }

        private static void CmdAlias(CommandLineApplication parent, AliasManager aliasManager)
        {
            parent.Command("alias", cmd =>
            {
                cmd.Description = "Alias commands";
                cmd.AddFullNameLogo();

                var optCreate = cmd.Option("--create|-c", "Create new", CommandOptionType.NoValue);
                var optRemove = cmd.Option("--remove|-r", "Delete", CommandOptionType.NoValue);
                var optVerbose = cmd.VerboseOption();

                cmd.OnExecute(() =>
                {
                    /// <summary>
                    /// Get name alias
                    /// </summary>
                    /// <param name="title"></param>
                    /// <param name="create"></param>
                    /// <returns></returns>
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

                            if ((create && AliasDef.IsValid(name) && !exists) ||
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

                    if (optCreate.HasValue())
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
                    else if (optRemove.HasValue())
                    {
                        //remove
                        var name = GetName("Remove alias", false);
                        if (string.IsNullOrWhiteSpace(name)) { return; }
                        aliasManager.Remove(name);
                        Console.Out.WriteLine($"Alias '{name}' removed!");
                    }
                    else
                    {
                        Console.Out.Write(aliasManager.ToTable(optVerbose.HasValue()));
                    }
                });
            });
        }

        class AutoCompletionHandler : IAutoCompleteHandler
        {
            public PveClient Client { get; set; }
            public ClassApi ClassApiRoot { get; set; }
            public char[] Separators { get; set; } = new char[] { '/' };

            public string[] GetSuggestions(string text, int index)
            {
                if (text.StartsWith("ls /") ||
                    text.StartsWith("create /") ||
                    text.StartsWith("delete /") ||
                    text.StartsWith("get /") ||
                    text.StartsWith("set /") ||
                    text.StartsWith("usage /"))
                {
                    var resource = text.Substring(text.IndexOf("/")).Trim();
                    var ret = ApiExplorer.ListValues(Client, ClassApiRoot, resource);

                    if (!string.IsNullOrWhiteSpace(ret.Error))
                    {
                        //try previous slash
                        var pos = resource.LastIndexOf('/');
                        ret = ApiExplorer.ListValues(Client, ClassApiRoot, resource.Substring(0, pos));

                        return ret.Values.Where(a => a.Value.StartsWith(resource.Substring(pos + 1)))
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