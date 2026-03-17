/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.CommandLine;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Metadata;
using Corsinvest.ProxmoxVE.Cli.Config;
using Microsoft.Extensions.Logging;
using GRE = Corsinvest.ProxmoxVE.Cli.Config.GuestResolutionEngine;
using Corsinvest.ProxmoxVE.Api.Extension;

namespace Corsinvest.ProxmoxVE.Cli;

/// <summary>
/// Api commands
/// </summary>
internal class ShellCommands
{
    private static ILoggerFactory _loggerFactory = null!;

    internal const string ArgVerboseLong = "--verbose";
    internal const string ArgVerboseShort = "-v";
    internal const string ArgHelpLong = "--help";
    internal const string ArgHelpShort = "-h";
    internal const string ArgHelpAlt = "-?";
    internal const string ArgHelpSlashH = "/h";
    internal const string ArgHelpSlashQ = "/?";
    internal const string ArgVersion = "--version";
    internal const string ArgReturnsLong = "--returns";
    internal const string ArgReturnsShort = "-r";
    internal const string ArgOutputLong = "--output";
    internal const string ArgOutputShort = "-o";
    internal const string ArgWait = "--wait";

    /// <summary>
    /// Initialize commands
    /// </summary>
    /// <param name="command"></param>
    /// <param name="loggerFactory"></param>
    public static void CreateCommands(RootCommand command, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;

        ConfigCommands.AddConfigCommands(command);
        CompletionHelper.AddCompletionCommand(command);
        ApiCommands(command);
        AliasCommands.AddAliasCommands(command);
        RegisterAliasCommands(command);
    }

    /// <summary>
    /// Get PveClient from context file or fallback to CLI options (--host/--user/--password)
    /// </summary>
    private static async Task<PveClient> GetClientAsync()
    {
        var context = PveConfigManager.GetCurrentContext()
                        ?? throw new InvalidOperationException("No context configured. Run 'config add-context' first.");

        return await PveConfigManager.CreateClientAsync(context, _loggerFactory);
    }

    private static readonly string CacheDir
        = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cv4pve", "cli", "cache");

    private static async Task<ClassApi> GetClassApiRootAsync(PveClient client)
    {
        var version = (await client.Version.GetAsync()).Version;
        var flatFile = Path.Combine(CacheDir, $"{version}-flat.json");

        if (!File.Exists(flatFile))
        {
            // Download, build flat, save only flat — discard raw JSON
            var json = await GeneratorClassApi.GetJsonSchemaFromApiDocAsync(client.Host, client.Port);
            var tmp = new ClassApi();
            foreach (var token in Newtonsoft.Json.Linq.JArray.Parse(json)) { _ = new ClassApi(token, tmp); }
            Directory.CreateDirectory(CacheDir);
            await File.WriteAllTextAsync(flatFile, GeneratorClassApi.BuildFlatCache(tmp));
        }

        var flat = GeneratorClassApi.LoadFlatCache(await File.ReadAllTextAsync(flatFile))!;
        return GeneratorClassApi.BuildClassApiFromFlat(flat);
    }


    /// <summary>
    /// Raw API commands under 'api' subcommand (for top-level CLI)
    /// </summary>
    public static void ApiCommands(RootCommand command)
        => AddApiSubCommands(command.AddCommand("api", "Raw API access (GET/SET/CREATE/DELETE)"));

    private static void RegisterAliasCommands(RootCommand root)
    {
        foreach (var alias in PveConfigManager.LoadAliases().OrderBy(a => a.Name))
        {
            var nameParts = SplitArgs(alias.Name);
            var tags = ApiExplorerHelper.GetArgumentTags(alias.Command);
            var (guestResolution, _) = GRE.Detect(alias.Command);
            var skipTokens = nameParts.Length;

            // Navigate/create the command hierarchy for multi-word alias names
            // e.g. "get nodes" → root["get"]["nodes"]
            // e.g. "get snapshots vm" → root["get"]["snapshots"]["vm"]
            Command leafParent = root;
            foreach (var part in nameParts.SkipLast(1))
            {
                var existing = leafParent.Subcommands.FirstOrDefault(c => c.Name == part);
                var desc = part switch
                {
                    "do" => "Execute an action",
                    "get" => "Read or list",
                    "set" => "Update configuration",
                    "create" => "Create a resource",
                    "delete" => "Delete a resource",
                    "show" => "Show details",
                    "vm" => "Virtual machines",
                    "ct" => "Containers (LXC)",
                    "node" => "Cluster nodes",
                    "cluster" => "Cluster-wide operations",
                    "guest" => "VMs and containers",
                    "security" => "Access and security",
                    "storage" => "Storage management",
                    "pool" => "Resource pools",
                    "ha" => "High availability",
                    "mapping" => "Hardware mappings",
                    "notification" => "Notifications",
                    "notifications" => "Notifications",
                    "tfa" => "Two-factor authentication",
                    "agent" => "Guest agent",
                    "backup" => "Backup management",
                    "hardware" => "Hardware devices",
                    _ => $"{char.ToUpper(part[0])}{part[1..]}"
                };
                leafParent = existing ?? leafParent.AddCommand(part, desc);
            }

            var finalName = nameParts[^1];

            // Skip if leaf already registered
            if (leafParent.Subcommands.Any(c => c.Name == finalName)) { continue; }

            var cmdDesc = guestResolution != GRE.GuestResolution.None
                            ? $"{alias.Description}\nTip: use {GRE.ArgGuestLong} <id|name> to resolve guest info automatically"
                            : alias.Description;

            var cmd = leafParent.AddCommand(finalName, cmdDesc);

            // Single variadic argument — no required-arg validation by System.CommandLine
            // Positional values come first, then --key value pairs
            var argAll = cmd.AddArgument<string[]>("args",
                                                   tags.Length > 0
                                                        ? $"Arguments: {string.Join(" ", tags.Select(t => $"<{t}>"))} [--key value ...]"
                                                        : "Extra --key value pairs");
            argAll.Arity = ArgumentArity.ZeroOrMore;
            argAll.HelpName = tags.Length > 0
                                ? string.Join(" ", tags.Select(t => $"<{t}>"))
                                : "[--key value ...]";

            argAll.Hidden = tags.Length == 0;
            argAll.CompletionSources.Clear();

            // Completion for --guest/-g value
            if (guestResolution != GRE.GuestResolution.None)
            {
                var capturedResolution = guestResolution;
                argAll.CompletionSources.Add((ctx) =>
                {
                    var allTokens = ctx.ParseResult.Tokens.Select(t => t.Value).ToArray();
                    var word = ctx.WordToComplete ?? string.Empty;
                    if (!IsGuestToken(GetPrevToken(allTokens, word))) { return []; }
                    return GRE.GetCompletions(capturedResolution, GetLiveClient()).ToArray();
                });
            }

            // Completion per positional slot
            for (var i = 0; i < tags.Length; i++)
            {
                var tagIndex = i;
                argAll.CompletionSources.Add((ctx) =>
                {
                    try
                    {
                        var classApiRoot = BuildClassApiFromCache();
                        if (classApiRoot == null) { return []; }

                        var allTokens = ctx.ParseResult.Tokens.Skip(skipTokens).Select(t => t.Value).ToArray();
                        var word = ctx.WordToComplete ?? string.Empty;
                        var prevToken = GetPrevToken(allTokens, word);
                        if (IsGuestToken(prevToken)) { return []; }

                        // Exclude the word being completed (partial token) from positional count
                        var argTokens = allTokens.Where(t => !t.StartsWith('-'))
                                                 .Where(t => word.Length == 0 || t != word)
                                                 .ToArray();

                        if (argTokens.Length != tagIndex) { return []; }

                        // vmtype has only two possible values — no live call needed
                        if (tags[tagIndex] == GRE.SegVmType) { return [GRE.TypeQemu, GRE.TypeLxc]; }

                        var expanded = ExpandTags(alias.Command, tags[..tagIndex], argTokens);
                        var segs = expanded.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
                        var parentSegs = new List<string>();
                        foreach (var seg in segs)
                        {
                            if (seg.Contains('{')) { break; }
                            parentSegs.Add(seg);
                        }
                        var parentPath = parentSegs.Count == 0 ? string.Empty : "/" + string.Join("/", parentSegs);
                        return GetLiveIndexedValues(parentPath, classApiRoot).ToArray();
                    }
                    catch { return []; }
                });
            }

            // Completion for --param options after all positional tags are filled
            argAll.CompletionSources.Add((ctx) =>
            {
                try
                {
                    var classApiRoot = BuildClassApiFromCache();
                    if (classApiRoot == null) { return []; }

                    var word = ctx.WordToComplete ?? string.Empty;
                    var allTokens = ctx.ParseResult.Tokens.Skip(skipTokens).Select(t => t.Value).ToArray();
                    var argTokens = allTokens.Where(t => !t.StartsWith('-')).ToArray();
                    var filledPositional = (word.Length == 0 || word.StartsWith('-'))
                                            ? argTokens
                                            : [.. argTokens.SkipLast(1)];
                    if (filledPositional.Length < tags.Length) { return []; }

                    var tokens = SplitArgs(ExpandTags(alias.Command, tags, filledPositional));
                    var methodType = HttpVerbToMethodType(tokens[0]);
                    var resource = tokens[1];

                    // If prevToken is a --param with enum values, don't propose --options (enum source handles it)
                    var prevToken = GetPrevToken(allTokens, word);
                    if (prevToken.StartsWith("--"))
                    {
                        var enumVals = ApiExplorerHelper.GetMethodParameterEnumValues(classApiRoot,
                                                                                      resource,
                                                                                      methodType,
                                                                                      prevToken[2..]);
                        if (enumVals.Length > 0) { return []; }
                    }

                    return ApiExplorerHelper.GetMethodParameters(classApiRoot, resource, methodType)
                                            .Select(p => $"--{p}")
                                            .Where(p => p.StartsWith(word))
                                            .ToArray();
                }
                catch { return []; }
            });

            // Completion for enum values after --param
            argAll.CompletionSources.Add((ctx) =>
            {
                try
                {
                    var classApiRoot = BuildClassApiFromCache();
                    if (classApiRoot == null) { return []; }

                    var word = ctx.WordToComplete ?? string.Empty;
                    if (word.StartsWith('-')) { return []; }

                    var allTokens = ctx.ParseResult.Tokens.Skip(skipTokens).Select(t => t.Value).ToArray();
                    var prevToken = GetPrevToken(allTokens, word);
                    if (!prevToken.StartsWith("--")) { return []; }
                    var paramName = prevToken[2..];

                    // Need all positional tags filled to build the resource
                    var positionalTokens = allTokens.Where(t => !t.StartsWith('-')).ToArray();
                    if (positionalTokens.Length < tags.Length) { return []; }

                    var expandedTokens = SplitArgs(ExpandTags(alias.Command, tags, positionalTokens));
                    return ApiExplorerHelper.GetMethodParameterEnumValues(classApiRoot,
                                                                          expandedTokens[1],
                                                                          HttpVerbToMethodType(expandedTokens[0]),
                                                                          paramName)
                                            .Where(v => v.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                                            .ToArray();
                }
                catch { return []; }
            });

            var optYes = alias.Confirm
                            ? cmd.AddOption<bool>("--yes|-y", "Confirm execution of dangerous operation")
                            : null;

            var optOutput = ApiOutputOption(cmd);
            var optVerbose = cmd.VerboseOption();

            cmd.TreatUnmatchedTokensAsErrors = false;
            cmd.SetAction(async (action) =>
            {
                if (alias.Confirm && optYes != null && !action.GetValue(optYes))
                {
                    Console.Error.WriteLine($"Error: '{alias.Name}' is a dangerous operation. Use --yes / -y to confirm.");
                    return;
                }

                var positional = (action.GetValue(argAll) ?? []).ToArray();
                var kvTokens = action.UnmatchedTokens.ToList();

                var tokens = SplitArgs(ExpandTags(alias.Command, tags, positional)).ToList();
                var allExtra = tokens.Skip(2).Concat(kvTokens).ToList();
                var extraParams = ConvertUnmatchedToKeyValue(allExtra);
                var c = await GetClientAsync();
                var (_, resultText) = await ApiExplorerHelper.ExecuteAsync(c,
                                                                           await GetClassApiRootAsync(c),
                                                                           tokens[1],
                                                                           HttpVerbToMethodType(tokens[0]),
                                                                           ApiExplorerHelper.CreateParameterResource(extraParams),
                                                                           false,
                                                                           action.GetValue(optOutput),
                                                                           action.GetValue(optVerbose));
                Console.Out.Write(resultText);
            });
        }
    }

    private static void AddApiSubCommands(Command parent, PveClient? client = null, ClassApi? classApiRoot = null)
    {
        Execute(parent, MethodType.Get, "GET request on resource", client);
        Execute(parent, MethodType.Set, "SET (PUT) request on resource", client);
        Execute(parent, MethodType.Create, "CREATE (POST) request on resource", client);
        Execute(parent, MethodType.Delete, "DELETE request on resource", client);

        Usage(parent, classApiRoot);
        List(parent, client, classApiRoot);
    }

    private static Argument<string> CreateResourceArgument(Command command)
    {
        var arg = command.AddArgument<string>("resource", "Resource api request");
        arg.HelpName = "resource";
        arg.CompletionSources.Add((ctx) =>
        {
            var word = ctx.WordToComplete ?? string.Empty;
            var classApiRoot = BuildClassApiFromCache();
            if (classApiRoot == null) { return []; }

            // word is empty → suggest root children directly (avoids shell treating "/" as filesystem path)
            return string.IsNullOrEmpty(word)
                    ? ctx.ParseResult.Tokens.Any(t => t.Value.StartsWith('/'))
                            ? []
                            : GetApiPathCompletions("/", classApiRoot)
                    : GetApiPathCompletions(word, classApiRoot);

        });
        return arg;
    }

    private static IEnumerable<string> GetApiPathCompletions(string prefix, ClassApi classApiRoot)
    {
        try
        {
            // Parent real path = completed segments
            // prefix="/nodes/" → parentPath="/nodes"
            // prefix="/nodes/cc" → parentPath="/nodes"  (incomplete last segment)
            // prefix="/nodes" → parentPath="" (root)
            var segs = prefix.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var parentSegs = prefix.EndsWith('/') ? segs : [.. segs.SkipLast(1)];
            var parentPath = parentSegs.Length == 0 ? string.Empty : "/" + string.Join("/", parentSegs);

            var parentNode = string.IsNullOrEmpty(parentPath)
                                ? classApiRoot
                                : ClassApi.GetFromResource(classApiRoot, parentPath);

            if (parentNode == null) { return []; }

            // Real segments of the parent (e.g. ["nodes","cc01","qemu","1006"])
            var realSegs = parentPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Map a schema Resource (e.g. /nodes/{node}/qemu/{vmid}/config) to real path
            // by replacing schema segments with real values at each position
            string ToRealPath(string schemaResource)
            {
                var schemaSegs = schemaResource.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var result = new string[schemaSegs.Length];
                for (var i = 0; i < schemaSegs.Length; i++)
                {
                    result[i] = i < realSegs.Length ? realSegs[i] : schemaSegs[i];
                }

                return "/" + string.Join("/", result);
            }

            var staticChildren = parentNode.SubClasses.Where(c => !c.IsIndexed);
            var dynamicChildren = parentNode.SubClasses.Where(c => c.IsIndexed);

            var staticPaths = staticChildren.Select(c => ToRealPath(c.Resource))
                                            .Where(r => r.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                            .OrderBy(r => r);

            if (dynamicChildren.Any())
            {
                var livePaths = GetLivePathCompletions(parentPath, classApiRoot);
                if (livePaths.Any()) { return [.. staticPaths, .. livePaths.OrderBy(p => p)]; }
            }

            return staticPaths;
        }
        catch { return []; }
    }

    private static PveClient? GetLiveClient()
    {
        var context = PveConfigManager.GetCurrentContext();
        if (context == null) { return null; }
        return PveConfigManager.CreateClientAsync(context).GetAwaiter().GetResult();
    }

    private static IEnumerable<string> GetLivePathCompletions(string parentPath, ClassApi classApiRoot)
    {
        try
        {
            var client = GetLiveClient();
            if (client == null) { return []; }

            var (values, error) = ApiExplorerHelper.ListValuesAsync(client, classApiRoot, parentPath).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(error)) { return []; }

            var realParent = string.IsNullOrEmpty(parentPath) ? string.Empty : parentPath;
            return values.Select(v => realParent + "/" + v.Value);
        }
        catch { return []; }
    }

    // Returns only the indexed (dynamic) values at parentPath (e.g. node names, vmids)
    private static IEnumerable<string> GetLiveIndexedValues(string parentPath, ClassApi classApiRoot)
    {
        try
        {
            var client = GetLiveClient();
            if (client == null) { return []; }

            var parentNode = string.IsNullOrEmpty(parentPath)
                                ? classApiRoot
                                : ClassApi.GetFromResource(classApiRoot, parentPath);

            if (parentNode == null || !parentNode.SubClasses.Any(c => c.IsIndexed)) { return []; }

            var (values, error) = ApiExplorerHelper.ListValuesAsync(client, classApiRoot, parentPath).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(error)) { return []; }

            // ListValuesAsync mixes indexed and static values — keep only indexed ones
            var staticNames = parentNode.SubClasses.Where(c => !c.IsIndexed).Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return values.Where(v => !staticNames.Contains(v.Value)).Select(v => v.Value);
        }
        catch { return []; }
    }

    private static Option<TableGenerator.Output> ApiOutputOption(Command command)
    {
        var opt = command.AddOption<TableGenerator.Output>($"{ArgOutputLong}|{ArgOutputShort}", "Type output");
        opt.DefaultValueFactory = (_) => TableGenerator.Output.Text;
        opt.CompletionSources.Clear();
        opt.CompletionSources.Add((_) => Enum.GetNames<TableGenerator.Output>().Select(n => n.ToLower()));
        return opt;
    }

    private static ClassApi? _cachedClassApi;
    private static ClassApi? BuildClassApiFromCache()
    {
        if (_cachedClassApi != null) { return _cachedClassApi; }
        var flat = LoadFlatCacheFromDisk();
        if (flat == null) { return null; }
        _cachedClassApi = GeneratorClassApi.BuildClassApiFromFlat(flat);
        return _cachedClassApi;
    }

    private static Dictionary<string, FlatResourceInfo>? LoadFlatCacheFromDisk()
    {
        if (!Directory.Exists(CacheDir)) { return null; }
        var flatFile = Directory.GetFiles(CacheDir, "*-flat.json").LastOrDefault();
        if (flatFile == null) { return null; }
        return GeneratorClassApi.LoadFlatCache(File.ReadAllText(flatFile));
    }

    private static string ToMethodName(MethodType methodType) => methodType switch
    {
        MethodType.Get => "GET",
        MethodType.Set => "PUT",
        MethodType.Create => "POST",
        MethodType.Delete => "DELETE",
        _ => "GET"
    };

    private static IEnumerable<string> GetParameterCompletions(ClassApi classApiRoot, ClassApi node, MethodType methodType, string word)
    {
        try
        {
            var method = node.Methods.FirstOrDefault(m => m.MethodType == ToMethodName(methodType));
            if (method == null) { return []; }

            // Keys are path parameters (already in the URL) — skip them
            var pathKeys = new HashSet<string>(node.Keys, StringComparer.OrdinalIgnoreCase);

            var results = new List<string>();
            foreach (var param in method.Parameters)
            {
                if (pathKeys.Contains(param.Name)) { continue; }
                var candidate = $"--{param.Name}";
                if (candidate.StartsWith(word, StringComparison.OrdinalIgnoreCase)) { results.Add(candidate); }
            }

            return results.OrderBy(r => r);
        }
        catch { return []; }
    }

    private static void Execute(Command parent, MethodType methodType, string description, PveClient? client = null)
    {
        var cmd = parent.AddCommand(methodType.ToString().ToLower(), description);
        cmd.TreatUnmatchedTokensAsErrors = false;
        var optVerbose = cmd.VerboseOption();
        var argResource = CreateResourceArgument(cmd);
        var argParameters = cmd.AddArgument<string[]>("parameters", "Parameter for resource (Multiple) format --key value.");
        argParameters.DefaultValueFactory = (_) => null!;
        argParameters.CompletionSources.Add((ctx) =>
        {
            var word = ctx.WordToComplete ?? string.Empty;
            var resourceToken = ctx.ParseResult.Tokens.FirstOrDefault(t => t.Value.StartsWith('/'))?.Value;
            if (resourceToken == null) { return []; }
            var classApiRoot = BuildClassApiFromCache();
            if (classApiRoot == null) { return []; }
            var node = ClassApi.GetFromResource(classApiRoot, resourceToken);
            if (node == null || node.IsRoot) { return []; }

            // If previous token is --key, suggest enum values for that key
            var tokens = ctx.ParseResult.Tokens.Select(t => t.Value).ToList();
            var prevIdx = tokens.Count - (string.IsNullOrEmpty(word) ? 1 : 2);
            var prevToken = prevIdx >= 0 ? tokens[prevIdx] : string.Empty;
            if (prevToken.StartsWith("--") && prevToken.Length > 2 && !word.StartsWith("--"))
            {
                var paramName = prevToken[2..];
                var method = node.Methods.FirstOrDefault(m => m.MethodType == ToMethodName(methodType));
                var param = method?.Parameters.FirstOrDefault(p => string.Equals(p.Name, paramName, StringComparison.OrdinalIgnoreCase));
                if (param?.EnumValues.Length > 0)
                {
                    return param.EnumValues.Where(v => v.StartsWith(word, StringComparison.OrdinalIgnoreCase));
                }
                return [];
            }

            // Keys already present in the command line
            var usedKeys = tokens
                .Where(t => t.StartsWith("--") && t.Length > 2)
                .Select(t => t[2..])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return GetParameterCompletions(classApiRoot, node, methodType, word)
                .Where(c => !usedKeys.Contains(c.TrimStart('-')));
        });

        var optOutput = ApiOutputOption(cmd);
        var optWait = cmd.AddOption<bool>(ArgWait, "Wait for task finish");

        cmd.SetAction(async (action) =>
        {
            var c = client ?? await GetClientAsync();

            var allTokens = (action.GetValue(argParameters) ?? []).Concat(action.UnmatchedTokens).ToList();
            var allParams = ConvertUnmatchedToKeyValue(allTokens);

            var (_, ResultText) = await ApiExplorerHelper.ExecuteAsync(c,
                                                                       await GetClassApiRootAsync(c),
                                                                       action.GetValue(argResource),
                                                                       methodType,
                                                                       ApiExplorerHelper.CreateParameterResource(allParams),
                                                                       action.GetValue(optWait),
                                                                       action.GetValue(optOutput),
                                                                       action.GetValue(optVerbose));

            Console.Out.Write(ResultText);
        });
    }

    /// <summary>
    /// Converts ["--cf", "AVERAGE", "--timeframe", "day"] to internal key:value format.
    /// Tokens without a following value (e.g. "--flag") become "flag:true".
    /// </summary>
    private static IEnumerable<string> ConvertUnmatchedToKeyValue(IReadOnlyList<string> tokens)
    {
        var result = new List<string>();
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!token.StartsWith("--") || token.Length <= 2) { continue; }
            var key = token[2..];
            if (i + 1 < tokens.Count && !tokens[i + 1].StartsWith("--"))
            {
                result.Add($"{key}:{tokens[i + 1]}");
                i++; // skip value token
            }
            else
            {
                result.Add($"{key}:true");
            }
        }
        return result;
    }

    private static MethodType HttpVerbToMethodType(string verb)
        => Enum.Parse<MethodType>(verb.ToLower() switch { "put" => "set", "post" => "create", var v => v }, ignoreCase: true);

    private static string GetPrevToken(string[] allTokens, string word)
        => word.Length == 0
            ? (allTokens.Length >= 1
                ? allTokens[^1]
                : string.Empty)

            : (allTokens.Length >= 2
                ? allTokens[^2]
                : string.Empty);

    private static bool IsGuestToken(string token)
        => token == GRE.ArgGuestLong || token == GRE.ArgGuestShort;

    private static string[] SplitArgs(string s)
        => s.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static string ExpandTags(string command, string[] tags, string[] values)
    {
        var result = command;
        for (var i = 0; i < Math.Min(tags.Length, values.Length); i++)
        {
            result = result.Replace($"{{{tags[i]}}}", values[i]);
        }
        return result;
    }

    private static void Usage(Command parent, ClassApi? classApiRoot = null)
    {
        var cmd = parent.AddCommand("usage", "Show usage for a resource (e.g. 'usage /nodes' or 'usage /nodes get')");
        var argResource = CreateResourceArgument(cmd);

        var argMethod = cmd.AddArgument<MethodType?>("method", "Optional API method (create/delete/get/set)");
        argMethod.DefaultValueFactory = (_) => null;
        argMethod.CompletionSources.Clear();
        argMethod.CompletionSources.Add((_) => Enum.GetNames<MethodType>().Select(n => n.ToLower()));

        var optVerbose = cmd.VerboseOption();
        var optReturns = cmd.AddOption<bool>("--returns|-r", "Including schema for returned data.");
        var optOutput = ApiOutputOption(cmd);

        cmd.SetAction(async (action)
            => Console.Out.Write(ApiExplorerHelper.Usage(classApiRoot ?? await GetClassApiRootAsync(await GetClientAsync()),
                                                         action.GetValue(argResource),
                                                         action.GetValue(optOutput),
                                                         action.GetValue(optReturns),
                                                         action.GetValue(argMethod)?.ToString()?.ToLower(),
                                                         action.GetValue(optVerbose),
                                                         optionStyle: true)));
    }

    private static void List(Command parent, PveClient? client = null, ClassApi? classApiRoot = null)
    {
        var cmd = parent.AddCommand("ls", "List child objects on <api_path>");
        var argResource = CreateResourceArgument(cmd);
        cmd.SetAction(async (action) =>
        {
            var c = client ?? await GetClientAsync();
            Console.Out.Write(await ApiExplorerHelper.ListAsync(c,
                                                                classApiRoot ?? await GetClassApiRootAsync(c),
                                                                action.GetValue(argResource)));
        });
    }

    /// <summary>
    /// Resolves alias arguments: expands placeholders, handles --help/--verbose, validates required args.
    /// Returns null if no alias matched, or the effective args to pass to ExecuteAppAsync.
    /// On missing required args writes to Console and returns an error sentinel.
    /// </summary>
    public static (string[]? effectiveArgs, int exitCode) ResolveAliasArgs(string[] args)
    {
        if (args.Length == 0 || args[0].StartsWith('-')) { return (null, 0); }

        var alias = PveConfigManager.LoadAliases()
                                    .OrderByDescending(a => a.Name.Split(' ').Length)
                                    .FirstOrDefault(a =>
                                    {
                                        var parts = SplitArgs(a.Name);
                                        return parts.Length <= args.Length
                                                && parts.Zip(args)
                                                        .All(x => string.Equals(x.First, x.Second, StringComparison.OrdinalIgnoreCase));
                                    });

        if (alias == null) { return (null, 0); }

        var nameParts = SplitArgs(alias.Name);
        var extraArgs = args.Skip(nameParts.Length).ToList();
        var isVerbose = extraArgs.Contains(ArgVerboseLong) || extraArgs.Contains(ArgVerboseShort);
        var isHelp = extraArgs.Contains(ArgHelpLong) || extraArgs.Contains(ArgHelpShort) || extraArgs.Contains(ArgHelpAlt);

        var expanded = alias.Command;

        // --guest/-g → resolve {node}, {vmid}, {vmtype} via GRE (must run before positional split)
        var (guestResolution, _) = GRE.Detect(alias.Command);
        for (var i = 0; i < extraArgs.Count - 1; i++)
        {
            if (!IsGuestToken(extraArgs[i])) { continue; }
            var guestValue = extraArgs[i + 1];
            extraArgs.RemoveRange(i, 2);
            var client = GetLiveClient();
            if (client == null) { Console.WriteLine("Error: no context configured."); return (null, 1); }
            expanded = GRE.ExpandAsync(expanded, client, guestValue, guestResolution).GetAwaiter().GetResult();
            break;
        }

        var positionalArgs = extraArgs.Where(a => !a.StartsWith('-')).ToArray();
        var kvArgs = extraArgs.Where(a => a.StartsWith('-')
                                            && a != ArgVerboseLong
                                            && a != ArgVerboseShort
                                            && a != ArgHelpLong
                                            && a != ArgHelpShort
                                            && a != ArgHelpAlt).ToArray();

        var tags = ApiExplorerHelper.GetArgumentTags(expanded);
        var index = Math.Min(tags.Length, positionalArgs.Length);
        expanded = ExpandTags(expanded, tags, positionalArgs);

        if (isHelp) { return (null, 0); }
        if (isVerbose)
        {
            var tokens = SplitArgs(expanded);
            var method = HttpVerbToMethodType(tokens[0]).ToString().ToLower();
            var resource = tokens[1];
            return (["api", "usage", resource, method, ArgVerboseLong], 0);
        }

        var missing = tags.Skip(index).ToArray();
        if (missing.Length > 0)
        {
            Console.WriteLine($"Error: missing arguments: {string.Join(", ", missing.Select(t => $"{{{t}}}"))}");
            Console.WriteLine($"Usage: {alias.Name} {string.Join(" ", tags.Select(t => $"<{t}>"))}");
            if (guestResolution != GRE.GuestResolution.None)
            {
                Console.WriteLine($"Tip: use {GRE.ArgGuestLong} <id|name> to resolve guest info automatically");
            }

            return (null, 1);
        }

        if (index < positionalArgs.Length) { expanded += " " + string.Join(' ', positionalArgs.Skip(index)); }
        if (kvArgs.Length > 0) { expanded += " " + string.Join(' ', kvArgs); }

        return (["api", .. SplitArgs(expanded)], 0);
    }
}