/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Corsinvest.ProxmoxVE.Cli.Config;

namespace Corsinvest.ProxmoxVE.Cli.Tests;

/// <summary>
/// Every built-in alias checked against the API of the latest Proxmox VE release, read from the schema
/// of the official API viewer: the path exists, accepts the method, and knows every --key the alias
/// writes. Needs internet access to pve.proxmox.com.
/// </summary>
public partial class BuiltinAliasesTests
{
    private const string ApiSchemaUrl = "https://pve.proxmox.com/pve-docs/api-viewer/apidoc.js";

    /// <summary>Path with placeholders normalised to "{}" → method (GET, POST…) → parameter names.</summary>
    private static readonly Lazy<Dictionary<string, Dictionary<string, HashSet<string>>>> Schema = new(LoadSchema);

    private static Dictionary<string, Dictionary<string, HashSet<string>>> LoadSchema()
    {
        string text;
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(1) };
            text = client.GetStringAsync(ApiSchemaUrl).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Could not download the API schema from {ApiSchemaUrl}: {ex.Message}", ex);
        }

        // apidoc.js is "const apiSchema = [ … ];" followed by the viewer code: read only the array.
        var bytes = Encoding.UTF8.GetBytes(text[text.IndexOf('[')..]);
        var reader = new Utf8JsonReader(bytes);
        using var document = JsonDocument.ParseValue(ref reader);

        var ret = new Dictionary<string, Dictionary<string, HashSet<string>>>();
        void Walk(JsonElement node)
        {
            var methods = ret.TryGetValue(Normalize(node.GetProperty("path").GetString()!), out var existing)
                            ? existing
                            : ret[Normalize(node.GetProperty("path").GetString()!)] = [];

            if (node.TryGetProperty("info", out var info))
            {
                foreach (var method in info.EnumerateObject())
                {
                    var names = method.Value.TryGetProperty("parameters", out var parameters)
                                && parameters.TryGetProperty("properties", out var properties)
                                    ? properties.EnumerateObject().Select(a => a.Name)
                                    : [];
                    methods[method.Name] = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
                }
            }

            if (node.TryGetProperty("children", out var children))
            {
                foreach (var child in children.EnumerateArray()) { Walk(child); }
            }
        }

        foreach (var node in document.RootElement.EnumerateArray()) { Walk(node); }
        return ret;
    }

    private static string Normalize(string path) => PlaceholderRegex().Replace(path, "{}");

    [GeneratedRegex(@"\{[^}]*\}")]
    private static partial Regex PlaceholderRegex();

    public static TheoryData<string> BuiltinAliases()
        => [.. PveConfigManager.LoadAliases().Where(a => a.IsBuiltin).Select(a => a.Name)];

    [Theory]
    [MemberData(nameof(BuiltinAliases))]
    public void MatchesTheApiSchema(string name)
    {
        var alias = PveConfigManager.LoadAliases().Single(a => a.IsBuiltin && a.Name == name);
        var tokens = alias.Command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var method = tokens[0] switch
        {
            "get" => "GET",
            "set" => "PUT",
            "create" => "POST",
            "delete" => "DELETE",
            var other => throw new Xunit.Sdk.XunitException($"Unknown method '{other}'"),
        };

        // A {vmtype} path must work for both guest types.
        foreach (var path in tokens[1].Contains("{vmtype}")
                                ? new[] { tokens[1].Replace("{vmtype}", "qemu"), tokens[1].Replace("{vmtype}", "lxc") }
                                : [tokens[1]])
        {
            Assert.True(Schema.Value.TryGetValue(Normalize(path), out var methods), $"'{name}': path {path} is not in the API");
            Assert.True(methods.TryGetValue(method, out var known), $"'{name}': {path} does not accept {method}");

            foreach (var key in tokens.Skip(2).Where(a => a.StartsWith("--")).Select(a => a[2..]))
            {
                Assert.True(known.Contains(key), $"'{name}': {method} {path} has no parameter --{key}");
            }
        }
    }
}
