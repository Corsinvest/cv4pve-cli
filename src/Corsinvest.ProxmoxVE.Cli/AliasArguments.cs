/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Cli;

/// <summary>
/// What follows the name of an alias on the command line, read in order: its positional arguments,
/// the API parameters, and the options cv4pve-cli handles itself.
/// </summary>
internal sealed class AliasArguments
{
    /// <summary>Options passed on to the <c>api</c> command, with a value.</summary>
    internal static readonly string[] PassThroughWithValue = ["--output", "-o", "--log-level"];

    /// <summary>Options passed on to the <c>api</c> command, without a value.</summary>
    internal static readonly string[] PassThroughFlags = ["--wait", "--debug", "--dry-run"];

    private static readonly string[] GuestOptions = ["--guest", "-g"];
    private static readonly string[] YesOptions = ["--yes", "-y"];
    private static readonly string[] VerboseOptions = ["--verbose", "-v"];
    private static readonly string[] HelpOptions = ["--help", "-h", "-?", "/h", "/?"];

    /// <summary>Values for the placeholders of the alias, in order.</summary>
    public List<string> Positional { get; } = [];

    /// <summary>API parameters (<c>--key value</c>).</summary>
    public List<KeyValuePair<string, string>> Parameters { get; } = [];

    /// <summary>Options for the <c>api</c> command (<c>--wait</c>, <c>--output</c>…), as written.</summary>
    public List<string> PassThrough { get; } = [];

    /// <summary>Value of <c>--guest</c>.</summary>
    public string? Guest { get; private set; }

    /// <summary><c>--yes</c> was given.</summary>
    public bool Yes { get; private set; }

    /// <summary><c>--verbose</c> was given.</summary>
    public bool Verbose { get; private set; }

    /// <summary><c>--help</c> was given.</summary>
    public bool Help { get; private set; }

    /// <summary><c>--dry-run</c> was given.</summary>
    public bool DryRun => PassThrough.Contains("--dry-run");

    /// <summary>
    /// Reads the tokens after the alias name.
    /// </summary>
    /// <exception cref="ArgumentException">An option misses its value, or a parameter is repeated.</exception>
    public static AliasArguments Parse(IReadOnlyList<string> tokens)
    {
        var ret = new AliasArguments();
        var rest = new List<string>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (GuestOptions.Contains(token))
            {
                if (i + 1 >= tokens.Count) { throw new ArgumentException($"Option '{token}' needs a VM ID or a name."); }
                ret.Guest = tokens[++i];
            }
            else if (YesOptions.Contains(token)) { ret.Yes = true; }
            else if (VerboseOptions.Contains(token)) { ret.Verbose = true; }
            else if (HelpOptions.Contains(token)) { ret.Help = true; }
            else if (PassThroughFlags.Contains(token)) { ret.PassThrough.Add(token); }
            else if (PassThroughWithValue.Contains(token))
            {
                if (i + 1 >= tokens.Count) { throw new ArgumentException($"Option '{token}' needs a value."); }
                ret.PassThrough.Add(token);
                ret.PassThrough.Add(tokens[++i]);
            }
            else { rest.Add(token); }
        }

        var (parameters, positional) = ApiParameters.Parse(rest);
        ret.Parameters.AddRange(parameters);
        ret.Positional.AddRange(positional);
        return ret;
    }

    /// <summary>
    /// Splits the options valid on every command (<c>--debug</c>, <c>--log-level</c>, <c>--dry-run</c>)
    /// written before the command from the rest of the command line.
    /// </summary>
    public static (List<string> Leading, string[] Remaining) SplitLeadingGlobalOptions(string[] args)
    {
        var leading = new List<string>();
        var i = 0;
        while (i < args.Length)
        {
            if (args[i] is "--debug" or "--dry-run") { leading.Add(args[i++]); }
            else if (args[i] == "--log-level" && i + 1 < args.Length) { leading.Add(args[i++]); leading.Add(args[i++]); }
            else { break; }
        }
        return (leading, args[i..]);
    }
}
