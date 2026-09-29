/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Cli;

/// <summary>
/// Parses the API parameters written on the command line, in the order they appear:
/// <c>--key value</c>, <c>--key=value</c>, and <c>--key</c> alone (sent as <c>true</c>).
/// </summary>
internal static class ApiParameters
{
    /// <summary>
    /// Splits tokens into API parameters and positional values. A <c>--key</c> takes the next token as its
    /// value unless that token is another <c>--key</c>; tokens not taken as a value are positional.
    /// </summary>
    /// <exception cref="ArgumentException">A parameter is given more than once.</exception>
    public static (List<KeyValuePair<string, string>> Parameters, List<string> Positional) Parse(IReadOnlyList<string> tokens)
    {
        var parameters = new List<KeyValuePair<string, string>>();
        var positional = new List<string>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!IsKey(token))
            {
                positional.Add(token);
                continue;
            }

            var body = token[2..];
            string key, value;
            var eq = body.IndexOf('=');
            if (eq > 0)
            {
                key = body[..eq];
                value = body[(eq + 1)..];
            }
            else if (i + 1 < tokens.Count && !IsKey(tokens[i + 1]))
            {
                key = body;
                value = tokens[++i];
            }
            else
            {
                key = body;
                value = "true";
            }

            if (parameters.Any(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Parameter '--{key}' is given more than once.");
            }

            parameters.Add(new(key, value));
        }

        return (parameters, positional);
    }

    /// <summary>
    /// Removes the options handled by cv4pve-cli itself, so they are not sent to the API.
    /// Also removes the <c>--option=value</c> and <c>--option:value</c> forms.
    /// </summary>
    /// <param name="tokens">Tokens in command-line order.</param>
    /// <param name="flags">Options without a value, e.g. <c>--wait</c>.</param>
    /// <param name="withValue">Options followed by a value, e.g. <c>--output</c>.</param>
    public static List<string> RemoveOptions(IReadOnlyList<string> tokens, ICollection<string> flags, ICollection<string> withValue)
    {
        var ret = new List<string>();
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (flags.Contains(token)) { continue; }
            if (withValue.Contains(token)) { i++; continue; }
            if (withValue.Any(o => token.StartsWith(o + "=", StringComparison.Ordinal)
                                   || token.StartsWith(o + ":", StringComparison.Ordinal))) { continue; }
            ret.Add(token);
        }
        return ret;
    }

    /// <summary>
    /// Parameters in the <c>key:value</c> form expected by <c>ApiExplorerHelper.CreateParameterResource</c>.
    /// </summary>
    public static IEnumerable<string> ToKeyValue(IEnumerable<KeyValuePair<string, string>> parameters)
        => parameters.Select(a => $"{a.Key}:{a.Value}");

    private static bool IsKey(string token) => token.Length > 2 && token.StartsWith("--", StringComparison.Ordinal);
}
