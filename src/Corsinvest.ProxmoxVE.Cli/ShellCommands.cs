/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.CommandLine;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Metadata;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Cli.Config;
using Microsoft.Extensions.Logging;
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
    /// Get PveClient from context file — singleton per process to avoid multiple logins.
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
            if (leafParent.Subcommands.Any(c => c.Name == finalName))
            {
                continue;
            }

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

            // Declared for --help and tab completion. The alias itself runs through ResolveAliasArgs,
            // called by Program before parsing, which rewrites it into an "api" command.
            if (alias.Confirm) { cmd.AddOption<bool>("--yes|-y", "Confirm: this alias changes the cluster"); }
            ApiOutputOption(cmd);
            cmd.VerboseOption();
            cmd.AddOption<bool>(ArgWait, "Wait for the async task (UPID) to finish");

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
        // Newest Proxmox VE version first: file names sort "8.4.9" after "8.4.21".
        var flatFile = Directory.GetFiles(CacheDir, "*-flat.json")
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

                var c = client ?? await GetClientAsync();
                var (statusCode, resultText) = await ApiExplorerHelper.ExecuteAsync(c,
                                                                                    await GetClassApiRootAsync(c),
                                                                                    resource,
                                                                                    methodType,
                                                                                    ApiExplorerHelper.CreateParameterResource(ApiParameters.ToKeyValue(parameters)),
                                                                                    action.GetValue(optWait),
                                                                                    action.GetValue(optOutput),
                                                                                    action.GetValue(optVerbose));

                if (statusCode is < 200 or > 299)
                {
                    return ExitCodeHelper.FailHttp(statusCode,
                                                   resultText,
                                                   FormatApiError(statusCode, resultText, methodType, resource, parameters, ResolvedAlias));
                }

                Console.Out.Write(resultText);
                return (int)ExitCode.Ok;
            }
            catch (Exception ex) { return ExitCodeHelper.Fail(ex); }
        });
    }

    private static readonly string[] ApiFlags = [ArgWait, ArgVerboseLong, ArgVerboseShort, "--debug", "--dry-run"];
    private static readonly string[] ApiOptionsWithValue = [ArgOutputLong, ArgOutputShort, "--log-level"];

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

        var tokens = ApiParameters.RemoveOptions(commandLine[start..], ApiFlags, ApiOptionsWithValue);
        var idx = tokens.IndexOf(resource);
        if (idx >= 0) { tokens.RemoveAt(idx); }

        var (parameters, positional) = ApiParameters.Parse(tokens);
        return positional.Count > 0
                ? throw new ArgumentException($"Unexpected argument '{positional[0]}': write API parameters as --key value.")
                : parameters;
    }

    /// <summary>
    /// Error refused by Proxmox VE, with the call that was sent and, for an alias, what the alias runs:
    /// a parameter written in the alias itself is an error of the alias, not of the user.
    /// </summary>
    internal static string FormatApiError(int statusCode,
                                          string errorText,
                                          MethodType methodType,
                                          string resource,
                                          IEnumerable<KeyValuePair<string, string>> parameters,
                                          PveConfigManager.PveAlias? alias)
    {
        var ret = new System.Text.StringBuilder();
        ret.AppendLine($"Proxmox VE refused the call (HTTP {statusCode}):");
        foreach (var line in errorText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            ret.AppendLine($"  {line}");
        }

        ret.AppendLine();
        ret.AppendLine($"Call:  {ToMethodName(methodType)} {resource}{string.Concat(parameters.Select(a => $" --{a.Key} {a.Value}"))}");

        if (alias != null)
        {
            ret.AppendLine($"Alias: '{alias.Name}' runs '{alias.Command}'");

            // Parameters named in the error ("name : message") that the alias writes itself.
            var refused = errorText.Split('\n')
                                   .Select(a => a.Split(" : ")[0].Trim())
                                   .Where(a => a.Length > 0 && !a.Contains(' '))
                                   .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var fromAlias = SplitArgs(alias.Command).Where(a => a.StartsWith("--") && refused.Contains(a[2..])).ToArray();
            if (fromAlias.Length > 0)
            {
                ret.AppendLine($"       {string.Join(", ", fromAlias)} is written in the alias, not by you: the alias is wrong.");
                ret.AppendLine(alias.IsBuiltin
                                ? "       Please report it: https://github.com/Corsinvest/cv4pve-cli/issues"
                                : "       Fix it with 'alias remove' and 'alias add'.");
            }
        }

        if (statusCode == 400)
        {
            ret.AppendLine($"Parameters it accepts: cv4pve-cli api usage {resource} {methodType.ToString().ToLower()} -v");
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
                CheckResourceInSchema(root, action.GetValue(argResource)!);
                Console.Out.Write(ApiExplorerHelper.Usage(root,
                                                          action.GetValue(argResource),
                                                          action.GetValue(optOutput),
                                                          action.GetValue(optReturns),
                                                          action.GetValue(argMethod)?.ToString()?.ToLower(),
                                                          action.GetValue(optVerbose),
                                                          optionStyle: true));
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
                CheckResourceInSchema(root, action.GetValue(argResource)!);
                Console.Out.Write(await ApiExplorerHelper.ListAsync(c, root, action.GetValue(argResource)));
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

            var dryRun = arguments.DryRun || leading.Contains("--dry-run");
            if (alias.Confirm && !arguments.Yes && !arguments.Verbose && !dryRun)
            {
                throw new CliException($"'{alias.Name}' changes the cluster: add --yes to confirm.", ExitCode.Validation);
            }

            var expanded = alias.Command;
            if (arguments.Guest != null)
            {
                var (guestResolution, _) = GRE.Detect(alias.Command);
                expanded = GRE.ExpandAsync(expanded, GetClientAsync().GetAwaiter().GetResult(), arguments.Guest, guestResolution)
                              .GetAwaiter()
                              .GetResult();
            }

            var tags = ApiExplorerHelper.GetArgumentTags(expanded);
            if (arguments.Positional.Count > tags.Length)
            {
                throw new ArgumentException($"Unexpected argument '{arguments.Positional[tags.Length]}': write API parameters as --key value.");
            }
            expanded = ExpandTags(expanded, tags, [.. arguments.Positional]);

            var tokens = SplitArgs(expanded);
            if (arguments.Verbose)
            {
                return (["api", "usage", tokens[1], HttpVerbToMethodType(tokens[0]).ToString().ToLower(), ArgVerboseLong, .. leading], 0);
            }

            var missing = tags.Skip(arguments.Positional.Count).ToArray();
            if (missing.Length > 0)
            {
                var message = $"missing arguments: {string.Join(", ", missing.Select(t => $"{{{t}}}"))}"
                              + $"\nUsage: {alias.Name} {string.Join(" ", ApiExplorerHelper.GetArgumentTags(alias.Command).Select(t => $"<{t}>"))}";
                if (GRE.Detect(alias.Command).Resolution != GRE.GuestResolution.None)
                {
                    message += $"\nTip: use {GRE.ArgGuestLong} <id|name> to resolve guest info automatically";
                }
                throw new CliException(message, ExitCode.Validation);
            }

            ResolvedAlias = alias;
            return (["api",
                     HttpVerbToMethodType(tokens[0]).ToString().ToLower(),
                     .. tokens.Skip(1),
                     .. arguments.Parameters.SelectMany(p => new[] { $"--{p.Key}", p.Value }),
                     .. arguments.PassThrough,
                     .. leading], 0);
        }
        catch (Exception ex) { return (null, ExitCodeHelper.Fail(ex)); }
    }

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
