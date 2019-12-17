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

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Helpers;
using Corsinvest.ProxmoxVE.Api.Metadata;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Corsinvest.ProxmoxVE.Api.Shell.Utility;
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
        /// <param name="output"></param>
        /// <param name="client"></param>
        /// <param name="fileScript"></param>
        /// <param name="onlyResult"></param>
        public static void Start(TextWriter output,
                                 PveClient client,
                                 string fileScript,
                                 bool onlyResult)
        {
            if (!onlyResult)
            {
                output.WriteLine($@"Corsinvest Interactive Shell for Proxmox VE ({DateTime.Now.ToLongDateString()})
Type '<TAB>' for completion word
Type 'help', 'quit' to close the application.");
            }

            #region ClassApi Metadata
            var watch = Stopwatch.StartNew();
            if (!onlyResult) { output.Write("Initialization metadata..."); }

            //get api metadata
            var classApiRoot = GeneretorClassApi.Generate(client.Hostname, client.Port);

            watch.Stop();
            if (!onlyResult) { output.WriteLine($" {watch.ElapsedMilliseconds}ms"); }
            #endregion

            if (!onlyResult) { output.WriteLine(Environment.NewLine + ShellHelper.REMEMBER_THESE_THINGS); }

            //Auto Completion
            ReadLine.AutoCompletionHandler = new AutoCompletionHandler
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
                    ParseLine(output,
                              line,
                              client,
                              classApiRoot,
                              aliasManager,
                              onlyResult);
                }
            }
            else
            {
                //Interactive
                while (true)
                {
                    var input = ReadLine.Read(">>> ");
                    var exit = ParseLine(output,
                                         input,
                                         client,
                                         classApiRoot,
                                         aliasManager,
                                         onlyResult);

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

        private static bool ParseLine(TextWriter output,
                                      string input,
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

                ShellCommands.ApiCommands(app, client, classApiRoot);

                //fix help text
                foreach (var command in app.Commands)
                {
                    command.FullName = app.Description;
                    command.ExtendedHelpText = "";
                    command.UsePagerForHelpText = false;
                }

                //create command from alias
                CreateCommandFromAlias(output,
                                       app,
                                       client,
                                       classApiRoot,
                                       aliasManager,
                                       onlyResult);

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

                CmdAlias(output, app, aliasManager);
                CmdHistory(output, app, onlyResult);
                #endregion

                app.OnExecute(() => app.ShowHint());

                //execute command
                try { app.Execute(StringHelper.TokenizeCommandLineToList(input).ToArray()); }
                catch (CommandParsingException ex) { output.WriteLine(ex.Message); }
                catch (Exception) { }

                return exit;
            }
        }

        private static void CreateCommandFromAlias(TextWriter output,
                                                   CommandLineApplication parent,
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
                            output.WriteLine(title);
                            output.WriteLine("Command: " + command);
                        }

                        ParseLine(output,
                                  command,
                                  client,
                                  classApiRoot,
                                  aliasManager,
                                  onlyResult);
                    });
                });
            }
        }

        private static void CmdHistory(TextWriter output,
                                       CommandLineApplication app,
                                       bool onlyResult)
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
                            if (!onlyResult) { output.WriteLine("History disabled!"); }
                        }
                        else
                        {
                            var lineNum = 0;
                            foreach (var item in ReadLine.GetHistory())
                            {
                                output.WriteLine($"{lineNum} {item}");
                                lineNum++;
                            }
                        }
                    }
                });
            });
        }

        private static void CmdAlias(TextWriter output,
                                     CommandLineApplication parent,
                                     AliasManager aliasManager)
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
                        output.WriteLine(title);
                        var name = " ";
                        while (true)
                        {
                            name = ReadLine.Read("Name: ", name.Trim());
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                output.WriteLine($"Abort {title}");
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
                                output.WriteLine($"Alias '{name}' already exists!");
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
                            output.WriteLine("Abort create alias");
                            return;
                        }

                        aliasManager.Create(name, description, command, false);

                        output.WriteLine($"Alias '{name}' created!");
                    }
                    else if (optRemove.HasValue())
                    {
                        //remove
                        var name = GetName("Remove alias", false);
                        if (string.IsNullOrWhiteSpace(name)) { return; }
                        aliasManager.Remove(name);
                        output.WriteLine($"Alias '{name}' removed!");
                    }
                    else
                    {
                        output.Write(aliasManager.ToTable(optVerbose.HasValue(), TableOutputType.Unicode));
                    }
                });
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