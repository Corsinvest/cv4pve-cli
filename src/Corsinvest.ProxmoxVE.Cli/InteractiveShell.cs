/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.CommandLine;
using System.Diagnostics;
using System.Text;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Metadata;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Newtonsoft.Json.Linq;

namespace Corsinvest.ProxmoxVE.Cli;

/// <summary>
/// Interactive Shell
/// </summary>
internal class InteractiveShell
{
    private const string Castle = @"
                                  .-.
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
    public static async Task StartAsync(PveClient client, string fileScript, bool onlyResult)
    {
        if (!onlyResult)
        {
            Console.Out.WriteLine($@"Corsinvest CLI for Proxmox VE ({DateTime.Now.ToLongDateString()})
Type '<TAB>' for completion word
Type 'help', 'quit' to close the application.");
        }

        #region ClassApi Metadata
        var watch = Stopwatch.StartNew();
        if (!onlyResult) { Console.Out.Write("Initialization metadata"); }

        //get api metadata with cache
        var classApiRoot = await GetClassApiWithCacheAsync(client, onlyResult);

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
            FileName = Path.Combine(CommonHelper.GetApplicationDataDirectory(ShellCommands.AppName), "alias.txt")
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

    #region API Metadata Cache
    private static string GetCacheDirectory() => CommonHelper.GetApplicationDataDirectory(ShellCommands.AppName);

    private static string GetCacheFileName(string host, string version)
        => Path.Combine(GetCacheDirectory(), $"api-cache_{host.Replace(":", "_")}_{version}.json");

    private static async Task<ClassApi> GetClassApiWithCacheAsync(PveClient client, bool onlyResult)
    {
        // Get PVE version
        var version = (await client.Version.GetAsync()).Version;
        var cacheFile = GetCacheFileName(client.Host, version);

        if (File.Exists(cacheFile))
        {
            try
            {
                if (!onlyResult) { Console.Out.Write($" from cache ({Path.GetFileName(cacheFile)})..."); }
                var json = await File.ReadAllTextAsync(cacheFile);
                return GenerateClassApiFromJson(json);
            }
            catch
            {
                // Cache corrupted, delete and download
                if (!onlyResult) { Console.Out.Write(" cache corrupted, reloading..."); }
                File.Delete(cacheFile);
            }
        }

        // Download and cache
        if (!onlyResult) { Console.Out.Write(" from server..."); }
        var apiJson = await GetJsonSchemaFromApiDocAsync(client.Host, client.Port);

        // Save to cache
        await File.WriteAllTextAsync(cacheFile, apiJson);
        if (!onlyResult) { Console.Out.Write($" saved to {Path.GetFileName(cacheFile)}"); }

        return GenerateClassApiFromJson(apiJson);
    }

    private static ClassApi GenerateClassApiFromJson(string json)
    {
        var classApi = new ClassApi();
        foreach (var token in JArray.Parse(json)) { _ = new ClassApi(token, classApi); }
        return classApi;
    }

    private static async Task<string> GetJsonSchemaFromApiDocAsync(string host, int port)
    {
        var url = $"https://{host}:{port}/pve-docs/api-viewer/apidoc.js";
        var json = new StringBuilder();

        using var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        using var httpClient = new HttpClient(httpClientHandler);
        using var response = await httpClient.GetAsync(url);

        var data = await response.Content.ReadAsStringAsync();
        data = data[data.IndexOf('[')..];

        foreach (var line in data.Split('\n'))
        {
            json.Append(line);
            if (line.StartsWith(']')) { break; }
        }

        return json.ToString();
    }

    private static int ClearCache()
    {
        var cacheDir = GetCacheDirectory();
        var files = Directory.GetFiles(cacheDir, "api-cache_*.json");
        foreach (var file in files) { File.Delete(file); }
        return files.Length;
    }
    #endregion

    #region History
    private static string GetHistoryFile() => Path.Combine(CommonHelper.GetApplicationDataDirectory(ShellCommands.AppName), "history.txt");

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
            ReadLine.AddHistory([.. data]);

            File.WriteAllLines(GetHistoryFile(), data.Skip(Math.Max(0, data.Count() - 100)));
        }
    }

    private static void ClearHistory()
    {
        ReadLine.ClearHistory();
        var file = GetHistoryFile();
        if (File.Exists(file)) { File.Delete(file); }
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
        if (input.StartsWith('#')) { return false; }

        var rc = new RootCommand("Corsinvest CLI for Proxmox VE");
        var exit = false;

        rc.AddFullNameLogo();
        rc.AddDebugOption();
        rc.AddDryRunOption();

        ShellCommands.ApiCommands(rc, client, classApiRoot);

        //create command from alias
        CreateCommandFromAlias(rc, client, classApiRoot, aliasManager, onlyResult);

        #region Commands base
        rc.AddCommand("quit|exit", "Close application").SetAction((_) => exit = true);
        rc.AddCommand("clear|cls", "Clear screen").SetAction((_) => Console.Clear());
        rc.AddCommand("clear-cache", "Clear API metadata cache").SetAction((_) =>
        {
            var count = ClearCache();
            Console.Out.WriteLine($"Cache cleared ({count} file(s) deleted)");
        });
        rc.AddCommand("clear-history", "Clear command history").SetAction((_) =>
        {
            ClearHistory();
            Console.Out.WriteLine("History cleared");
        });
        var castle = rc.AddCommand("castle", "");
        castle.SetAction((_) => Console.Out.WriteLine(Castle));
        castle.Hidden = true;

        CmdAlias(rc, aliasManager);
        CmdHistory(rc, onlyResult);
        #endregion

        //execute command
        try { rc.Parse(input).Invoke(); }
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
            cmd.Hidden = true;
            //cmd.ExtendedHelpText = Environment.NewLine + "Alias command: " + item.Command;

            //create argument
            foreach (var arg in ApiExplorerHelper.GetArgumentTags(item.Command)) { cmd.AddArgument(arg, arg); }

            cmd.SetAction((action) =>
            {
                var title = new StringBuilder(item.Description);
                var command = item.Command;

                //replace value into argument
                foreach (var arg in cmd.Arguments)
                {
                    var argValue = action.GetValue((Argument<string>)arg);
                    title.Append($" {arg.Name}: {argValue}");
                    command = command.Replace(ApiExplorerHelper.CreateArgumentTag(arg.Name), argValue);
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
        cmd.SetAction((action) =>
        {
            var enabled = action.GetValue(optEnabled);
            if (enabled)
            {
                ReadLine.HistoryEnabled = enabled;
                if (ReadLine.HistoryEnabled)
                {
                    LoadHistory();
                }
                else
                {
                    ReadLine.ClearHistory();
                }
            }
            else if (!ReadLine.HistoryEnabled)
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
        });
    }

    private static void CmdAlias(RootCommand command, ApiExplorerHelper.AliasManager aliasManager)
    {
        var cmd = command.AddCommand("alias", "Alias commands");
        var optCreate = cmd.AddOption<bool>("--create|-c", "Create new");
        var optRemove = cmd.AddOption<bool>("--remove|-r", "Delete");
        var optVerbose = cmd.VerboseOption();

        cmd.SetAction((action) =>
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

                    if ((create && ApiExplorerHelper.AliasDef.IsValid(name) && !exists)
                        || (!create && exists))
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

            if (action.GetValue(optCreate))
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
            else if (action.GetValue(optRemove))
            {
                //remove
                var name = GetName("Remove alias", false);
                if (string.IsNullOrWhiteSpace(name)) { return; }
                aliasManager.Remove(name);
                Console.Out.WriteLine($"Alias '{name}' removed!");
            }
            else
            {
                Console.Out.Write(aliasManager.ToTable(action.GetValue(optVerbose), TableGenerator.Output.Text));
            }
        });
    }

    class AutoCompletionHandler : IAutoCompleteHandler
    {
        public PveClient Client { get; set; } = default!;
        public ClassApi ClassApiRoot { get; set; } = default!;
        public char[] Separators { get; set; } = ['/'];

        public string[] GetSuggestions(string text, int index)
        {
            if (text.StartsWith("ls /") ||
                text.StartsWith("create /") ||
                text.StartsWith("delete /") ||
                text.StartsWith("get /") ||
                text.StartsWith("set /") ||
                text.StartsWith("usage /"))
            {
                var resource = text[text.IndexOf('/')..].Trim();

                var ret = ApiExplorerHelper.ListValuesAsync(Client, ClassApiRoot, resource).Result;
                if (!string.IsNullOrWhiteSpace(ret.Error))
                {
                    //try previous slash
                    var pos = resource.LastIndexOf('/');
                    ret = ApiExplorerHelper.ListValuesAsync(Client, ClassApiRoot, resource[..pos]).Result;

                    return [.. ret.Values.Where(a => a.Value.StartsWith(resource[(pos + 1)..])).Select(a => a.Value)];
                }
                else
                {
                    return [.. ret.Values.Select(a => a.Value)];
                }
            }
            else
            {
                return null!;
            }
        }
    }
}
