/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Cli;

/// <summary>
/// What follows the name of an alias on the command line: the options cv4pve-cli handles itself, and the
/// rest (arguments, --key value, --guest, --yes) left in order for the SDK to expand the alias.
/// </summary>
internal sealed class AliasArguments
{
    /// <summary>Options passed on to the <c>api</c> command, with a value.</summary>
    internal static readonly string[] PassThroughWithValue = ["--output", "-o", "--wait-timeout", "--log-level"];

    /// <summary>Options passed on to the <c>api</c> command, without a value.</summary>
    internal static readonly string[] PassThroughFlags = ["--wait", "--debug", "--dry-run", "--all-columns", "-A"];

    private static readonly string[] VerboseOptions = ["--verbose", "-v"];
    private static readonly string[] HelpOptions = ["--help", "-h", "-?", "/h", "/?"];

    /// <summary>Tokens for the SDK (arguments, --key value, --guest, --yes), in order.</summary>
    public List<string> Rest { get; } = [];

    /// <summary>Options for the <c>api</c> command (<c>--wait</c>, <c>--output</c>…), as written.</summary>
    public List<string> PassThrough { get; } = [];

    /// <summary><c>--verbose</c> was given.</summary>
    public bool Verbose { get; private set; }

    /// <summary><c>--help</c> was given.</summary>
    public bool Help { get; private set; }

    /// <summary><c>--dry-run</c> was given.</summary>
    public bool DryRun => PassThrough.Contains("--dry-run");

    /// <summary>
    /// Reads the tokens after the alias name.
    /// </summary>
    /// <exception cref="ArgumentException">An option misses its value.</exception>
    public static AliasArguments Parse(IReadOnlyList<string> tokens)
    {
        var ret = new AliasArguments();
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (VerboseOptions.Contains(token)) { ret.Verbose = true; }
            else if (HelpOptions.Contains(token)) { ret.Help = true; }
            else if (PassThroughFlags.Contains(token)) { ret.PassThrough.Add(token); }
            else if (token == ShellCommands.ArgHumanReadable)
            {
                // Its value is optional: take the next token only when it is true, false, 1 or 0.
                ret.PassThrough.Add(token);
                if (i + 1 < tokens.Count && ShellCommands.IsHumanReadableValue(tokens[i + 1])) { ret.PassThrough.Add(tokens[++i]); }
            }
            else if (token.StartsWith(ShellCommands.ArgHumanReadable + "=", StringComparison.Ordinal)
                     || token.StartsWith(ShellCommands.ArgHumanReadable + ":", StringComparison.Ordinal)) { ret.PassThrough.Add(token); }
            else if (PassThroughWithValue.Contains(token))
            {
                if (i + 1 >= tokens.Count) { throw new ArgumentException($"Option '{token}' needs a value."); }
                ret.PassThrough.Add(token);
                ret.PassThrough.Add(tokens[++i]);
            }
            else { ret.Rest.Add(token); }
        }
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
