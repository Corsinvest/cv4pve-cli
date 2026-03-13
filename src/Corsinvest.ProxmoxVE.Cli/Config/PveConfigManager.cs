/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Corsinvest.ProxmoxVE.Cli.Config;

/// <summary>
/// Manages the cv4pve configuration file (~/.cv4pve/config)
/// </summary>
internal static class PveConfigManager
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cv4pve", "cli");

    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config");

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static PveConfig Load()
    {
        if (!File.Exists(ConfigFile)) { return new PveConfig(); }

        try
        {
            return Deserializer.Deserialize<PveConfig>(File.ReadAllText(ConfigFile)) ?? new PveConfig();
        }
        catch
        {
            return new PveConfig();
        }
    }

    public static void Save(PveConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigFile, Serializer.Serialize(config));

        // chmod 600 on Linux/macOS
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(ConfigFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public static async Task<PveClient> CreateClientAsync(PveContext ctx, ILoggerFactory? loggerFactory = null)
    {
        var timeoutMs = (ctx.Timeout ?? 30) * 1000;
        var client = await ClientHelper.GetClientAndTryLoginAsync($"{ctx.Host}:{ctx.Port}",
                                                                  string.IsNullOrWhiteSpace(ctx.ApiToken) ? ctx.Username ?? string.Empty : string.Empty,
                                                                  string.IsNullOrWhiteSpace(ctx.ApiToken) ? ctx.Password ?? string.Empty : string.Empty,
                                                                  ctx.ApiToken ?? string.Empty,
                                                                  ctx.ValidateCertificate ?? true,
                                                                  loggerFactory!,
                                                                  timeoutMs);
        client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
        return client;
    }

    public static PveContext? GetCurrentContext()
    {
        var config = Load();
        if (string.IsNullOrWhiteSpace(config.CurrentContext)) { return null; }
        return config.Contexts.FirstOrDefault(c => c.Name == config.CurrentContext);
    }

    public static void UseContext(string name)
    {
        var config = Load();
        if (!config.Contexts.Any(c => c.Name == name))
        {
            throw new InvalidOperationException($"Context '{name}' not found.");
        }
        config.CurrentContext = name;
        Save(config);
    }

    public static void AddContext(PveContext context)
    {
        var config = Load();
        var existing = config.Contexts.FirstOrDefault(c => c.Name == context.Name);
        if (existing != null) { config.Contexts.Remove(existing); }
        config.Contexts.Add(context);
        if (string.IsNullOrWhiteSpace(config.CurrentContext)) { config.CurrentContext = context.Name; }
        Save(config);
    }

    public static void DeleteContext(string name)
    {
        var config = Load();
        var context = config.Contexts.FirstOrDefault(c => c.Name == name)
                        ?? throw new InvalidOperationException($"Context '{name}' not found.");
        config.Contexts.Remove(context);
        if (config.CurrentContext == name) { config.CurrentContext = config.Contexts.FirstOrDefault()?.Name ?? string.Empty; }
        Save(config);
    }

    public static void RenameContext(string oldName, string newName)
    {
        var config = Load();
        var context = config.Contexts.FirstOrDefault(c => c.Name == oldName)
                        ?? throw new InvalidOperationException($"Context '{oldName}' not found.");
        context.Name = newName;
        if (config.CurrentContext == oldName) { config.CurrentContext = newName; }
        Save(config);
    }

    #region Alias
    private static readonly string AliasFile = Path.Combine(ConfigDir, "alias");

    public record PveAlias(string Name, string Description, string Command, bool IsBuiltin = false, bool Confirm = false);

    private static PveAlias[]? _builtinAliases;
    private static PveAlias[] BuiltinAliases
    {
        get
        {
            if (_builtinAliases != null) { return _builtinAliases; }
            var asm = typeof(PveConfigManager).Assembly;
            var resName = asm.GetManifestResourceNames().First(n => n.EndsWith("builtin-aliases.yaml"));
            using var stream = asm.GetManifestResourceStream(resName)!;
            using var reader = new StreamReader(stream);
            var data = Deserializer.Deserialize<AliasFileData>(reader) ?? new AliasFileData();
            _builtinAliases = data.Aliases.Select(a => new PveAlias(a.Name, a.Description, a.Command, true, a.Confirm)).ToArray();
            return _builtinAliases;
        }
    }


    public static bool IsBuiltinAlias(string name)
        => BuiltinAliases.Any(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<string> GetUserAliasNames()
    {
        if (!File.Exists(AliasFile)) { return []; }
        try
        {
            var data = Deserializer.Deserialize<AliasFileData>(File.ReadAllText(AliasFile)) ?? new AliasFileData();
            return data.Aliases.Select(a => a.Name);
        }
        catch { return []; }
    }

    public static IEnumerable<PveAlias> LoadAliases()
    {
        var aliases = new List<PveAlias>(BuiltinAliases);
        if (!File.Exists(AliasFile)) { return aliases; }
        try
        {
            var data = Deserializer.Deserialize<AliasFileData>(File.ReadAllText(AliasFile)) ?? new AliasFileData();
            foreach (var a in data.Aliases)
            {
                if (!IsBuiltinAlias(a.Name)) { aliases.Add(new PveAlias(a.Name, a.Description, a.Command, false, a.Confirm)); }
            }
        }
        catch { }
        return aliases;
    }

    public static void AddUserAlias(PveAlias alias)
    {
        var data = LoadUserAliasFileData();
        var existing = data.Aliases.FirstOrDefault(a => string.Equals(a.Name, alias.Name, StringComparison.OrdinalIgnoreCase));
        if (existing != null) { throw new InvalidOperationException($"Alias '{alias.Name}' already exists."); }
        data.Aliases.Add(new()
        {
            Name = alias.Name,
            Description = alias.Description,
            Command = alias.Command
        });
        SaveUserAliasFileData(data);
    }

    public static void RemoveUserAlias(string name)
    {
        var data = LoadUserAliasFileData();
        var existing = data.Aliases.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException($"Alias '{name}' not found.");
        data.Aliases.Remove(existing);
        SaveUserAliasFileData(data);
    }

    private static AliasFileData LoadUserAliasFileData()
    {
        if (!File.Exists(AliasFile)) { return new AliasFileData(); }
        try { return Deserializer.Deserialize<AliasFileData>(File.ReadAllText(AliasFile)) ?? new AliasFileData(); }
        catch { return new AliasFileData(); }
    }

    private static void SaveUserAliasFileData(AliasFileData data)
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(AliasFile, Serializer.Serialize(data));
    }

    private class AliasFileData
    {
        public List<AliasEntry> Aliases { get; set; } = [];
    }

    private class AliasEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public bool Confirm { get; set; } = false;
    }
    #endregion
}
