/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.CommandLine;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Shell;
using Corsinvest.ProxmoxVE.Api.Metadata;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Cli.Config;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using GRE = Corsinvest.ProxmoxVE.Cli.Config.GuestResolutionEngine;

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
    internal const string ArgWaitTimeout = "--wait-timeout";
    internal const string ArgAllColumns = "--all-columns";
    internal const string ArgAllColumnsShort = "-A";
    private const string AllColumnsDescription = "Show every column of a list, not only those the API schema names (-o json always has every column)";
    internal const string ArgHumanReadable = "--human-readable";
    private const string HumanReadableDescription = "Sizes, percentages, durations and dates as text, as pvesh does (default true; false or 0 for the values as the API returns them; -o json always has them)";

    /// <summary>The command line being run, after alias expansion: API parameters are read from it in order.</summary>
    internal static string[] CommandLine { get; set; } = [];

    /// <summary>The alias the command line was rewritten from, to explain an API error.</summary>
    internal static PveConfigManager.PveAlias? ResolvedAlias { get; set; }

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
    /// Get PveClient from context file: singleton per process to avoid multiple logins.
    /// </summary>
    private static PveClient? _cachedClient;
    internal static async Task<PveClient> GetClientAsync()
    {
        if (_cachedClient != null) { return _cachedClient; }
        var context = PveConfigManager.GetCurrentContext()
                        ?? throw new CliException("No context configured. Run 'config add' first.", ExitCode.Auth);
        _cachedClient = await PveConfigManager.CreateClientAsync(context, _loggerFactory);
        return _cachedClient;
    }

    private static readonly string CacheDir
        = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cv4pve", "cli", "cache");

    // The format version of the SDK is in the file name: a cache written by an older cv4pve-cli is not read.
    private static readonly string FlatFileSuffix = $"-flat-v{GeneratorClassApi.FlatCacheFormatVersion}.json";

    private static async Task<ClassApi> GetClassApiRootAsync(PveClient client)
    {
        var version = (await client.Version.GetAsync()).Version;
        var flatFile = Path.Combine(CacheDir, version + FlatFileSuffix);
        var flat = File.Exists(flatFile) ? GeneratorClassApi.LoadFlatCache(await File.ReadAllTextAsync(flatFile)) : null;

        if (flat == null)
        {
            // Download, build flat, save only flat, discard raw JSON
            var json = await GeneratorClassApi.GetJsonSchemaFromApiDocAsync(client.Host, client.Port);
            var tmp = new ClassApi();
            foreach (var token in Newtonsoft.Json.Linq.JArray.Parse(json)) { _ = new ClassApi(token, tmp); }
            var text = GeneratorClassApi.BuildFlatCache(tmp);

            Directory.CreateDirectory(CacheDir);
            foreach (var old in Directory.GetFiles(CacheDir, "*-flat*.json").Where(a => !a.EndsWith(FlatFileSuffix, StringComparison.Ordinal)))
            {
                File.Delete(old);
            }

            await File.WriteAllTextAsync(flatFile, text);
            flat = GeneratorClassApi.LoadFlatCache(text)!;
        }

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
            var tags = ApiCommandLine.GetPlaceholders(alias.Command).ToArray();
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
            if (leafParent.Subcommands.Any(c => c.Name == finalName))
            {
                continue;
            }

            var cmdDesc = guestResolution != GRE.GuestResolution.None
                            ? $"{alias.Description}\nTip: use {GRE.ArgGuestLong} <id|name> to resolve guest info automatically"
                            : alias.Description;

            var cmd = leafParent.AddCommand(finalName, cmdDesc);

            // Single variadic argument: no required-arg validation by System.CommandLine
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

                        // After an option that takes a value, the word is that value, not an argument.
                        if (prevToken.StartsWith('-') && !IsAliasFlag(prevToken)) { return []; }

                        // With --guest the node, VM ID and type come from the guest: no slot to fill here.
                        var (openTags, argTokens) = GetAliasPositional(tags, WrittenTokens(allTokens, word));
                        if (openTags.Length != tags.Length || argTokens.Length != tagIndex) { return []; }

                        // vmtype has only two possible values: no live call needed
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
                        return [.. GetLiveIndexedValues(parentPath, classApiRoot)];
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
                    var (openTags, filledPositional) = GetAliasPositional(tags, WrittenTokens(allTokens, word));
                    if (filledPositional.Length < openTags.Length) { return []; }

                    // Placeholders filled by --guest stay in the path: the schema finds the path with them too.
                    var tokens = SplitArgs(ExpandTags(alias.Command, openTags, filledPositional));
                    var methodType = HttpVerbToMethodType(tokens[0]);
                    var resource = tokens[1];

                    // If prevToken is a --param with enum values, don't propose --options (enum source handles it)
                    var prevToken = GetPrevToken(allTokens, word);
                    if (prevToken.StartsWith("--"))
                    {
                        if (GetParameterValues(classApiRoot, resource, methodType, prevToken[2..]).Count > 0) { return []; }
                    }

                    return (ApiSchema.GetMethod(classApiRoot, resource, methodType)?.Parameters ?? [])
                                            .Select(p => $"--{p.Name}")
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
                    var (openTags, positionalTokens) = GetAliasPositional(tags, WrittenTokens(allTokens, word));
                    if (positionalTokens.Length < openTags.Length) { return []; }

                    var expandedTokens = SplitArgs(ExpandTags(alias.Command, openTags, positionalTokens));
                    return GetParameterValues(classApiRoot,
                                              expandedTokens[1],
                                              HttpVerbToMethodType(expandedTokens[0]),
                                              paramName)
                                            .Where(v => v.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                                            .ToArray();
                }
                catch { return []; }
            });

            // Declared for --help and tab completion. The alias itself runs through ResolveAliasArgs,
            // called by Program before parsing, which rewrites it into an "api" command.
            if (alias.Confirm) { cmd.AddOption<bool>("--yes|-y", "Confirm: this alias changes the cluster"); }
            ApiOutputOption(cmd);
            cmd.VerboseOption();
            cmd.AddOption<bool>(ArgWait, "Wait for the async task (UPID) to finish");
            cmd.AddOption<bool>($"{ArgAllColumns}|{ArgAllColumnsShort}", AllColumnsDescription);
            HumanReadableOption(cmd);

            cmd.TreatUnmatchedTokensAsErrors = false;
            cmd.SetAction(async (action) =>
            {
                // Reached only when the command line was not recognised as this alias before parsing.
                var (effectiveArgs, exitCode) = ResolveAliasArgs([.. action.Tokens.Select(t => t.Value)]);
                if (effectiveArgs == null) { return exitCode; }
                CommandLine = effectiveArgs;
                return await root.Parse(effectiveArgs).InvokeAsync();
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
        try { return GetClientAsync().GetAwaiter().GetResult(); }
        catch { return null; }
    }

    private static IEnumerable<string> GetLivePathCompletions(string parentPath, ClassApi classApiRoot)
    {
        try
        {
            var client = GetLiveClient();
            if (client == null) { return []; }

            var result = ApiSchema.GetChildrenAsync(client, classApiRoot, parentPath).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(result.Error)) { return []; }

            var realParent = string.IsNullOrEmpty(parentPath) ? string.Empty : parentPath;
            return result.Children.Select(v => realParent + "/" + v.Name);
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

            var result = ApiSchema.GetChildrenAsync(client, classApiRoot, parentPath).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(result.Error)) { return []; }

            // GetChildrenAsync mixes indexed and static values: keep only indexed ones
            var staticNames = parentNode.SubClasses.Where(c => !c.IsIndexed).Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return result.Children.Where(v => !staticNames.Contains(v.Name)).Select(v => v.Name);
        }
        catch { return []; }
    }

    private static IReadOnlyList<string> GetParameterValues(ClassApi classApiRoot, string resource, MethodType methodType, string name)
    {
        var parameter = ApiSchema.GetMethod(classApiRoot, resource, methodType)?
                                 .Parameters
                                 .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        return parameter == null ? [] : ApiSchema.GetAllowedValues(parameter);
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
        // Newest Proxmox VE version first: file names sort "8.4.9" after "8.4.21".
        var flatFile = Directory.GetFiles(CacheDir, "*" + FlatFileSuffix)
                                .OrderByDescending(f => Version.TryParse(Path.GetFileName(f).Split('-')[0], out var v) ? v : new Version())
                                .FirstOrDefault();
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

            // Keys are path parameters (already in the URL): skip them
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
                return param == null
                        ? []
                        : ApiSchema.GetAllowedValues(param).Where(v => v.StartsWith(word, StringComparison.OrdinalIgnoreCase));
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
        var optWait = cmd.AddOption<bool>(ArgWait, "Wait for the task (UPID) to finish; exit code 5 if it fails");
        var optWaitTimeout = cmd.AddOption<int>(ArgWaitTimeout, "Seconds to wait with --wait (default: until the task ends)");
        var optAllColumns = cmd.AddOption<bool>($"{ArgAllColumns}|{ArgAllColumnsShort}", AllColumnsDescription);
        HumanReadableOption(cmd);

        cmd.SetAction(async (action) =>
        {
            try
            {
                var resource = action.GetValue(argResource)!;
                CheckResource(resource);
                var parameters = ReadApiParameters(CommandLine, cmd.Name, resource);

                if (action.GetValue<bool>($"--{CommandOptionExtension.DryRunOptionName}"))
                {
                    Console.Out.Write(FormatDryRun(methodType, resource, parameters));
                    return (int)ExitCode.Ok;
                }

                var command = new ApiCommand(methodType, resource, parameters.ToDictionary(a => a.Key, a => (object)a.Value));
                var c = client ?? await GetClientAsync();
                var waitSeconds = action.GetValue(optWaitTimeout);
                var response = await ApiRequest.ExecuteAsync(c,
                                                             command,
                                                             action.GetValue(optWait)
                                                                ? new ApiWaitOptions(waitSeconds > 0 ? TimeSpan.FromSeconds(waitSeconds) : null)
                                                                : null);

                if (!response.IsSuccess)
                {
                    var errorText = string.Join('\n', new[] { response.Error ?? string.Empty }
                                                        .Concat(response.ParameterErrors.Select(a => $"{a.Key} : {a.Value}")));
                    return ExitCodeHelper.FailHttp(response.StatusCode, errorText, FormatApiError(response, ResolvedAlias));
                }

                // Warnings (hidden columns) after the table, where they are read.
                var warnings = new StringWriter();
                Console.Out.Write(action.GetValue(optVerbose)
                                    ? JsonConvert.SerializeObject(response.Raw, Formatting.Indented) + Environment.NewLine
                                    : await FormatDataAsync(response.Data,
                                                            () => GetClassApiRootAsync(c),
                                                            resource,
                                                            action.GetValue(optOutput),
                                                            action.GetValue(optAllColumns),
                                                            ReadHumanReadable(CommandLine),
                                                            warnings));
                Console.Error.Write(warnings);

                if (response.Task is { } task)
                {
                    Console.Out.WriteLine();
                    if (!task.Finished)
                    {
                        Console.Error.WriteLine("Error: the task did not finish in time, or its status could not be read.");
                        return (int)ExitCode.TaskFailed;
                    }
                    Console.Out.WriteLine(task.ExitStatus);
                    return task.Succeeded ? (int)ExitCode.Ok : (int)ExitCode.TaskFailed;
                }

                return (int)ExitCode.Ok;
            }
            catch (Exception ex) { return ExitCodeHelper.Fail(ex); }
        });
    }

    /// <summary>--human-readable [true|false|1|0]: without a value it is true, and true is the default.</summary>
    private static Option<bool> HumanReadableOption(Command command)
    {
        var opt = command.AddOption<bool>(ArgHumanReadable, HumanReadableDescription);
        opt.DefaultValueFactory = (_) => true;
        return opt;
    }

    /// <summary>
    /// Value of --human-readable on the command line (the last one wins; true when absent). Read here, not by the
    /// parser, which gives a bool option only true or false and would leave "0" out.
    /// </summary>
    internal static bool ReadHumanReadable(IReadOnlyList<string> commandLine)
    {
        var ret = true;
        for (var i = 0; i < commandLine.Count; i++)
        {
            var token = commandLine[i];
            if (token == ArgHumanReadable)
            {
                ret = i + 1 < commandLine.Count && IsHumanReadableValue(commandLine[i + 1])
                        ? ParseHumanReadable(commandLine[++i])
                        : true;
            }
            else if (token.StartsWith(ArgHumanReadable + "=", StringComparison.Ordinal)
                     || token.StartsWith(ArgHumanReadable + ":", StringComparison.Ordinal))
            {
                ret = ParseHumanReadable(token[(ArgHumanReadable.Length + 1)..]);
            }
        }

        return ret;
    }

    /// <summary>Value of --human-readable: none, true or 1 is true; false or 0 is false.</summary>
    /// <exception cref="ArgumentException">Any other value.</exception>
    internal static bool ParseHumanReadable(string? value)
        => value?.Trim().ToLowerInvariant() switch
        {
            null or "" or "true" or "1" => true,
            "false" or "0" => false,
            _ => throw new ArgumentException($"{ArgHumanReadable} takes true, false, 1 or 0, not '{value}'."),
        };

    /// <summary>The token is a value of --human-readable.</summary>
    internal static bool IsHumanReadableValue(string token) => token.ToLowerInvariant() is "true" or "false" or "1" or "0";

    /// <summary>
    /// The data of an answer as text. A list shows the columns the API schema names, as pvesh does, and the others
    /// are listed on <paramref name="warnings"/>; <paramref name="allColumns"/> or a Json output show every column.
    /// Values are rendered as the schema says (sizes, percentages…) unless <paramref name="humanReadable"/> is false
    /// or the output is Json. The schema is read only for an object or a list; when it cannot be read (the call has
    /// already run), the data is printed as JSON with a warning.
    /// </summary>
    internal static async Task<string> FormatDataAsync(object? data,
                                                       Func<Task<ClassApi>> getSchema,
                                                       string resource,
                                                       TableGenerator.Output output,
                                                       bool allColumns,
                                                       bool humanReadable,
                                                       TextWriter warnings)
    {
        if (data is not (IDictionary<string, object> or System.Collections.IList)) { return data + string.Empty; }

        ClassApi root;
        try { root = await getSchema(); }
        catch (Exception ex)
        {
            warnings.WriteLine($"Warning: the API schema cannot be read ({ex.Message}): the answer is shown as JSON.");
            return JsonConvert.SerializeObject(data, Formatting.Indented) + Environment.NewLine;
        }

        var json = output is TableGenerator.Output.Json or TableGenerator.Output.JsonPretty;
        var table = ApiSchema.ToTable(data,
                                      root,
                                      resource,
                                      new ApiTableOptions(AllColumns: allColumns || json, HumanReadable: humanReadable && !json),
                                      out var hidden);
        if (hidden.Count > 0)
        {
            warnings.WriteLine($"+ {hidden.Count} more columns: {string.Join(", ", hidden)} (use {ArgAllColumns} or -o json)");
        }

        return table?.To(output) ?? (data + string.Empty);
    }

    private static readonly string[] ApiFlags = [ArgWait, ArgVerboseLong, ArgVerboseShort, "--debug", "--dry-run", ArgAllColumns, ArgAllColumnsShort];
    private static readonly string[] ApiOptionsWithValue = [ArgOutputLong, ArgOutputShort, ArgWaitTimeout, "--log-level"];

    /// <summary>
    /// Reads the API parameters from the command line, in the order they were written: the tokens after
    /// <c>api &lt;method&gt;</c>, without the resource and the options cv4pve-cli handles itself.
    /// </summary>
    /// <exception cref="ArgumentException">A value without a <c>--key</c>, or a repeated parameter.</exception>
    internal static List<KeyValuePair<string, string>> ReadApiParameters(string[] commandLine, string method, string resource)
    {
        var start = -1;
        for (var i = 0; i + 1 < commandLine.Length; i++)
        {
            if (commandLine[i] == "api" && string.Equals(commandLine[i + 1], method, StringComparison.OrdinalIgnoreCase))
            {
                start = i + 2;
                break;
            }
        }
        if (start < 0) { return []; }

        var tokens = RemoveCliOptions(commandLine[start..]);
        var idx = tokens.IndexOf(resource);
        if (idx >= 0) { tokens.RemoveAt(idx); }

        var (parameters, positional) = ApiCommandLine.ParseParameters(tokens);
        return positional.Count > 0
                ? throw new ArgumentException($"Unexpected argument '{positional[0]}': write API parameters as --key value.")
                : [.. parameters];
    }

    /// <summary>
    /// Removes the options cv4pve-cli handles itself, so they are not sent to Proxmox VE as parameters,
    /// including their <c>--option=value</c> and <c>--option:value</c> forms.
    /// </summary>
    private static List<string> RemoveCliOptions(IReadOnlyList<string> tokens)
    {
        var ret = new List<string>();
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (ApiFlags.Contains(token)) { continue; }
            if (token == ArgHumanReadable)
            {
                if (i + 1 < tokens.Count && IsHumanReadableValue(tokens[i + 1])) { i++; }
                continue;
            }
            if (token.StartsWith(ArgHumanReadable + "=", StringComparison.Ordinal)
                || token.StartsWith(ArgHumanReadable + ":", StringComparison.Ordinal)) { continue; }
            if (ApiOptionsWithValue.Contains(token)) { i++; continue; }
            if (ApiOptionsWithValue.Any(a => token.StartsWith(a + "=", StringComparison.Ordinal)
                                             || token.StartsWith(a + ":", StringComparison.Ordinal))) { continue; }
            ret.Add(token);
        }

        return ret;
    }

    /// <summary>
    /// Error refused by Proxmox VE, with the call that was sent and, for an alias, what the alias runs:
    /// a parameter written in the alias itself is an error of the alias, not of the user.
    /// </summary>
    internal static string FormatApiError(ApiResponse response, PveConfigManager.PveAlias? alias)
    {
        var ret = new System.Text.StringBuilder();
        ret.AppendLine($"Proxmox VE refused the call (HTTP {response.StatusCode}):");
        if (!string.IsNullOrWhiteSpace(response.Error)) { ret.AppendLine($"  {response.Error}"); }
        foreach (var (name, error) in response.ParameterErrors) { ret.AppendLine($"  {name} : {error}"); }

        ret.AppendLine();
        ret.AppendLine($"Call:  {ToMethodName(response.Command.Method)} {response.Command.Resource}"
                       + string.Concat(response.Command.Parameters.Select(a => $" --{a.Key} {a.Value}")));

        if (alias != null)
        {
            ret.AppendLine($"Alias: '{alias.Name}' runs '{alias.Command}'");
            var fromAlias = SplitArgs(alias.Command).Where(a => a.StartsWith("--") && response.ParameterErrors.ContainsKey(a[2..])).ToArray();
            if (fromAlias.Length > 0)
            {
                ret.AppendLine($"       {string.Join(", ", fromAlias)} is written in the alias, not by you: the alias is wrong.");
                ret.AppendLine(alias.IsBuiltin
                                ? "       Please report it: https://github.com/Corsinvest/cv4pve-cli/issues"
                                : "       Fix it with 'alias remove' and 'alias add'.");
            }
        }

        if (response.StatusCode == 400)
        {
            ret.AppendLine($"Parameters it accepts: cv4pve-cli api usage {response.Command.Resource} {response.Command.Method.ToString().ToLower()} -v");
        }
        return ret.ToString();
    }

    /// <summary>
    /// What --dry-run prints instead of calling the API.
    /// </summary>
    internal static string FormatDryRun(MethodType methodType, string resource, IEnumerable<KeyValuePair<string, string>> parameters)
    {
        var ret = new System.Text.StringBuilder();
        ret.AppendLine("Dry run, nothing sent to Proxmox VE:");
        ret.AppendLine($"{ToMethodName(methodType)} {resource}");
        foreach (var item in parameters) { ret.AppendLine($"  {item.Key}: {item.Value}"); }
        return ret.ToString();
    }

    /// <summary>
    /// Stops a path the API schema does not know, e.g. a typo: not found.
    /// </summary>
    private static void CheckResourceInSchema(ClassApi classApiRoot, string resource)
    {
        if (ClassApi.GetFromResource(classApiRoot, resource) == null)
        {
            throw new CliException($"no such resource '{resource}'", ExitCode.NotFound);
        }
    }

    /// <summary>
    /// Stops a resource that is not an API path because Git Bash (MSYS) turned it into a Windows path.
    /// </summary>
    internal static void CheckResource(string resource)
    {
        if (resource.Length >= 3 && char.IsAsciiLetter(resource[0]) && resource[1] == ':' && resource[2] is '/' or '\\')
        {
            throw new CliException($"'{resource}' is not an API path: Git Bash turned it into a Windows path. Run the command with MSYS_NO_PATHCONV=1.",
                                   ExitCode.Validation);
        }
    }

    private static MethodType HttpVerbToMethodType(string verb)
        => Enum.Parse<MethodType>(verb.ToLower() switch
        {
            "put" => "set",
            "post" => "create",
            var v => v
        },
        ignoreCase: true);

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

    // Options of an alias that take no value.
    private static bool IsAliasFlag(string token)
        => AliasArguments.PassThroughFlags.Contains(token)
           || token is "--yes" or "-y" or ArgVerboseLong or ArgVerboseShort or ArgHumanReadable;

    // The tokens written before the word being completed.
    private static string[] WrittenTokens(string[] allTokens, string word)
        => word.Length > 0 && allTokens.Length > 0 && allTokens[^1] == word ? allTokens[..^1] : allTokens;

    /// <summary>
    /// Placeholders of an alias still to write, and the values written for them, read as the alias runs: the options
    /// of cv4pve-cli and their values, --guest and its value, --yes and the --key value parameters are not arguments.
    /// With --guest the node, VM ID and type come from the guest and are not in the returned placeholders.
    /// </summary>
    internal static (string[] OpenTags, string[] Values) GetAliasPositional(string[] tags, IReadOnlyList<string> tokens)
    {
        List<string> rest;
        try { rest = AliasArguments.Parse(tokens).Rest; }
        catch (ArgumentException) { rest = [.. tokens]; }

        var guest = false;
        var clean = new List<string>();
        for (var i = 0; i < rest.Count; i++)
        {
            if (IsGuestToken(rest[i])) { guest = true; i++; }
            else if (rest[i] is not ("--yes" or "-y")) { clean.Add(rest[i]); }
        }

        IReadOnlyList<string> positional;
        try { positional = ApiCommandLine.ParseParameters(clean).Positional; }
        catch (ArgumentException) { positional = [.. clean.Where(a => !a.StartsWith('-'))]; }

        var openTags = guest
                        ? tags.Where(a => a is not (GRE.SegNode or GRE.SegVmId or GRE.SegVmType)).ToArray()
                        : tags;
        return (openTags, [.. positional]);
    }

    private static string[] SplitArgs(string s)
    {
        // Split on spaces but respect single and double quoted strings
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        char? inQuote = null;
        foreach (var c in s)
        {
            if (inQuote.HasValue)
            {
                if (c == inQuote.Value) { inQuote = null; }
                else { current.Append(c); }
            }
            else if (c == '"' || c == '\'') { inQuote = c; }
            else if (c == ' ')
            {
                if (current.Length > 0) { result.Add(current.ToString()); current.Clear(); }
            }
            else { current.Append(c); }
        }
        if (current.Length > 0) { result.Add(current.ToString()); }
        return [.. result];
    }

    private static string ExpandTags(string command, string[] tags, string[] values)
    {
        var result = command;
        for (var i = 0; i < Math.Min(tags.Length, values.Length); i++)
        {
            var val = values[i];
            // Quote values containing spaces so SplitArgs keeps them as a single token
            if (val.Contains(' ')) { val = $"\"{val}\""; }
            result = result.Replace($"{{{tags[i]}}}", val);
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

        cmd.SetAction(async (action) =>
        {
            try
            {
                CheckResource(action.GetValue(argResource)!);
                var root = classApiRoot ?? await GetClassApiRootAsync(await GetClientAsync());
                var resource = action.GetValue(argResource)!;
                CheckResourceInSchema(root, resource);
                var method = action.GetValue(argMethod);
                var methods = (ApiSchema.GetMethods(root, resource) ?? []).Where(a => method == null || a.Method == method);
                Console.Out.Write(ApiSchemaText.Usage(resource,
                                                      methods,
                                                      action.GetValue(optVerbose),
                                                      action.GetValue(optReturns),
                                                      action.GetValue(optOutput)));
                return (int)ExitCode.Ok;
            }
            catch (Exception ex) { return ExitCodeHelper.Fail(ex); }
        });
    }

    private static void List(Command parent, PveClient? client = null, ClassApi? classApiRoot = null)
    {
        var cmd = parent.AddCommand("ls", "List child objects on <api_path>");
        var argResource = CreateResourceArgument(cmd);
        cmd.SetAction(async (action) =>
        {
            try
            {
                CheckResource(action.GetValue(argResource)!);
                var c = client ?? await GetClientAsync();
                var root = classApiRoot ?? await GetClassApiRootAsync(c);
                var resource = action.GetValue(argResource)!;
                CheckResourceInSchema(root, resource);
                Console.Out.Write(ApiSchemaText.List(await ApiSchema.GetChildrenAsync(c, root, resource)));
                return (int)ExitCode.Ok;
            }
            catch (Exception ex) { return ExitCodeHelper.Fail(ex); }
        });
    }

    /// <summary>
    /// Finds the alias the command line starts with (after any --debug, --log-level, --dry-run) and rewrites
    /// it into the equivalent "api" command: placeholders filled with the positional arguments or --guest,
    /// then the API parameters, then the options for the api command.
    /// Returns (null, 0) when the command line is not an alias or asks for its help; (null, code) on error.
    /// </summary>
    public static (string[]? effectiveArgs, int exitCode) ResolveAliasArgs(string[] args)
    {
        var (leading, rest) = AliasArguments.SplitLeadingGlobalOptions(args);
        if (rest.Length == 0 || rest[0].StartsWith('-')) { return (null, 0); }

        var alias = FindAlias(rest);
        if (alias == null) { return (null, 0); }

        try
        {
            var arguments = AliasArguments.Parse([.. rest.Skip(SplitArgs(alias.Name).Length)]);
            if (arguments.Help) { return (null, 0); }

            var aliasTokens = SplitArgs(alias.Command);
            if (arguments.Verbose)
            {
                // Usage of the alias path as written, placeholders included: api usage accepts them.
                return (["api", "usage", aliasTokens[1], HttpVerbToMethodType(aliasTokens[0]).ToString().ToLower(), ArgVerboseLong, .. leading], 0);
            }

            var dryRun = arguments.DryRun || leading.Contains("--dry-run");
            var needsClient = arguments.Rest.Contains(GRE.ArgGuestLong) || arguments.Rest.Contains(GRE.ArgGuestShort);
            var expanded = ApiCommandLine.ExpandAliasAsync(new ApiAlias(alias.Name, alias.Description, alias.Command, alias.Confirm),
                                                           dryRun ? [.. arguments.Rest, "--yes"] : arguments.Rest,
                                                           needsClient ? GetClientAsync().GetAwaiter().GetResult() : null)
                                         .GetAwaiter()
                                         .GetResult();

            if (expanded.Error is { } error)
            {
                throw new CliException(DescribeAliasError(alias, error, expanded.Detail), ExitCodeHelper.FromCommandError(error));
            }

            var command = expanded.Command!;
            ResolvedAlias = alias;
            return (["api",
                     command.Method.ToString().ToLower(),
                     command.Resource,
                     // --key=value: a value such as "-v" or "-o" is not read as an option of cv4pve-cli.
                     .. command.Parameters.Select(p => $"--{p.Key}={p.Value}"),
                     .. arguments.PassThrough,
                     .. leading], 0);
        }
        catch (Exception ex) { return (null, ExitCodeHelper.Fail(ex)); }
    }

    private static string DescribeAliasError(PveConfigManager.PveAlias alias, ApiCommandError error, string? detail)
        => error switch
        {
            ApiCommandError.MissingArguments
                => $"missing arguments: {detail}\nUsage: {alias.Name} {string.Join(" ", ApiCommandLine.GetPlaceholders(alias.Command).Select(t => $"<{t}>"))}"
                   + (GRE.Detect(alias.Command).Resolution != GRE.GuestResolution.None
                        ? $"\nTip: use {GRE.ArgGuestLong} <id|name> to resolve guest info automatically"
                        : string.Empty),
            ApiCommandError.UnexpectedArgument => $"Unexpected argument '{detail}': write API parameters as --key value.",
            ApiCommandError.ConfirmationRequired => $"'{alias.Name}' changes the cluster: add --yes to confirm.",
            ApiCommandError.GuestNotFound => $"Guest '{detail}' not found.",
            _ => detail ?? error.ToString(),
        };

    private static PveConfigManager.PveAlias? FindAlias(string[] args)
        => PveConfigManager.LoadAliases()
                           .OrderByDescending(a => SplitArgs(a.Name).Length)
                           .FirstOrDefault(a =>
                           {
                               var parts = SplitArgs(a.Name);
                               return parts.Length <= args.Length
                                       && parts.Zip(args).All(x => string.Equals(x.First, x.Second, StringComparison.OrdinalIgnoreCase));
                           });
}
