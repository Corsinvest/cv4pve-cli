using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Utils.Shell;
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
        public static void Start(Client client, string fileScript, bool onlyResult)
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

            //Auto Completion
            ReadLine.AutoCompletionHandler = new AutoCompletionHandler()
            {
                Client = client,
                ClassApiRoot = classApiRoot
            };

            LoadHistory();

            var alias = new AliasManager()
            {
                FileName = Path.Combine(GetApplicationDirectory(), "alias.txt")
            };
            alias.Load();

            if (File.Exists(fileScript))
            {
                //script file
                foreach (var line in File.ReadAllLines(fileScript))
                {
                    if (ParseLine(line, client, classApiRoot, alias, onlyResult)) { break; }
                }
            }
            else
            {
                //Interactive
                while (true)
                {
                    var input = ReadLine.Read(">>> ");
                    var exit = ParseLine(input, client, classApiRoot, alias, onlyResult);

                    SaveHistory();
                    alias.Save();

                    if (exit) { break; }
                }
            }
        }

        private static string GetApplicationDirectory()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cv4pve-sh");
            if (!Directory.Exists(path))
            {
                var dir = Directory.CreateDirectory(path);
                dir.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }

            return path;
        }

        #region History
        private static string GetHistoryFile() => Path.Combine(GetApplicationDirectory(), "history.txt");

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
                                      Client client,
                                      ClassApi classApiRoot,
                                      AliasManager alias,
                                      bool onlyResult)
        {
            if (string.IsNullOrWhiteSpace(input)) { return false; }

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

                foreach (var command in app.Commands)
                {
                    command.FullName = app.Description;
                    command.ExtendedHelpText = "";
                }

                CreateCommandFromAlias(app, client, classApiRoot, alias, onlyResult);

                #region Commands
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

                CmdAlias(app, alias);
                CmdHistory(app, onlyResult);
                #endregion

                app.OnExecute(() => app.ShowHint());

                //execute command
                try { app.Execute(TokenizeCommandLineToList(input).ToArray()); }
                catch (CommandParsingException ex) { Console.Out.WriteLine(ex.Message); }
                catch (Exception) { }

                return exit;
            }
        }

        private static void CreateCommandFromAlias(CommandLineApplication parent,
                                                   Client client,
                                                   ClassApi classApiRoot,
                                                   AliasManager alias,
                                                   bool onlyResult)
        {
            foreach (var item in alias.Data)
            {
                parent.Command(item.Names[0], cmd =>
                {
                    foreach (var name in item.Names) { cmd.AddName(name); }
                    cmd.Description = item.Description;
                    cmd.ShowInHelpText = false;
                    cmd.ExtendedHelpText = Environment.NewLine + "Alias command: " + item.Command;

                    //create argument
                    foreach (var arg in item.GetArguments()) { cmd.Argument(arg, arg, false).IsRequired(); }

                    cmd.OnExecute(() =>
                    {
                        var title = item.Description;
                        var command = item.Command;

                        //replace value into argument
                        foreach (var arg in cmd.Arguments)
                        {
                            title += $" {arg.Name}: {arg.Value}";
                            command = command.Replace("{" + arg.Name + "}", arg.Value);
                        }

                        if (!onlyResult)
                        {
                            Console.Out.WriteLine(title);
                            Console.Out.WriteLine("Command: " + command);
                        }

                        ParseLine(command, client, classApiRoot, alias, onlyResult);
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

        private static void CmdAlias(CommandLineApplication parent, AliasManager alias)
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

                            var exists = alias.Exists(name);

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

                        alias.Create(name, description, command, false);
                        
                        Console.Out.WriteLine($"Alias '{name}' created!");
                    }
                    else if (optRemove.HasValue())
                    {
                        //remove
                        var name = GetName("Remove alias", false);
                        if (string.IsNullOrWhiteSpace(name)) { return; }
                        alias.Remove(name);
                        Console.Out.WriteLine($"Alias '{name}' removed!");
                    }
                    else
                    {
                        var columns = optVerbose.HasValue() ?
                                      new string[] { "name", "description", "command", "args", "sys" } :
                                      new string[] { "name", "description", "sys" };

                        var rows = alias.Data.OrderByDescending(a => a.System)
                                             .ThenBy(a => a.Name)
                                             .Select(a => optVerbose.HasValue() ?
                                                            new object[] { a.Name, a.Description, a.Command, string.Join(",", a.GetArguments()), a.System ? "X" : "" } :
                                                            new object[] { a.Name, a.Description, a.System ? "X" : "" });

                        Console.Out.Write(TableHelper.CreateTable(columns, rows, false));
                    }
                });
            });
        }

        class AutoCompletionHandler : IAutoCompleteHandler
        {
            public Client Client { get; set; }
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
                    var ret = Commands.ListValues(Client, ClassApiRoot, resource);

                    if (!string.IsNullOrWhiteSpace(ret.Error))
                    {
                        //try previous slash
                        var pos = resource.LastIndexOf('/');
                        ret = Commands.ListValues(Client, ClassApiRoot, resource.Substring(0, pos));

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

        private static List<string> TokenizeCommandLineToList(string commandLine)
        {

            var tokens = new List<string>();
            var token = new StringBuilder(255);
            var sections = commandLine.Split(' ');

            for (int curPart = 0; curPart < sections.Length; curPart++)
            {
                // We are in a quoted section!!
                if (sections[curPart].StartsWith("\""))
                {
                    //remove leading "
                    token.Append(sections[curPart].Substring(1));
                    var quoteCount = 0;

                    //Step backwards from the end of the current section to find the count of quotes from the end.
                    //This will exclude looking at the first character, which was the " that got us here in the first place.
                    for (; quoteCount < sections[curPart].Length - 1; quoteCount++)
                    {
                        if (sections[curPart][sections[curPart].Length - 1 - quoteCount] != '"') { break; }
                    }

                    // if we didn't have a leftover " (i.e. the 2N+1), then we should continue adding in the next section to the current token.
                    while (quoteCount % 2 == 0 && (curPart != sections.Length - 1))
                    {
                        quoteCount = 0;
                        curPart++;

                        //Step backwards from the end of the current token to find the count of quotes from the end.
                        for (; quoteCount < sections[curPart].Length; quoteCount++)
                        {
                            if (sections[curPart][sections[curPart].Length - 1 - quoteCount] != '"') { break; }
                        }

                        token.Append(' ').Append(sections[curPart]);
                    }

                    //remove trailing " if we had a leftover
                    //if we didn't have a leftover then we go to the end of the command line without an enclosing " 
                    //so it gets treated as a quoted argument anyway
                    if (quoteCount % 2 != 0) { token.Remove(token.Length - 1, 1); }
                    token.Replace("\"\"", "\"");
                }
                else
                {
                    //Not a quoted section so this is just a boring parameter
                    token.Append(sections[curPart]);
                }

                //strip whitespace (because).
                if (!string.IsNullOrEmpty(token.ToString().Trim())) { tokens.Add(token.ToString().Trim()); }

                token.Clear();
            }

            //return the array in the same format args[] usually turn up to main in.
            return tokens;
        }
    }
}