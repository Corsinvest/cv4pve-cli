/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.CommandLine;
using System.Runtime.InteropServices;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;

namespace Corsinvest.ProxmoxVE.Cli;

/// <summary>
/// Shell tab-completion registration and suggest support.
/// </summary>
internal static class CompletionHelper
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cv4pve", "cli");

    private static readonly string ShimFile = Path.Combine(ConfigDir,
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "completion.ps1"
            : "completion.bash");

    private static readonly string BashShim = """
# cv4pve-cli tab completion
_cv4pve_cli_complete()
{
    local cur=${COMP_WORDS[COMP_CWORD]}
    local IFS=$'\n'
    mapfile -t COMPREPLY < <(cv4pve-cli complete -- "${COMP_LINE}" 2>/dev/null | grep -i "^${cur}")
    if [[ ${#COMPREPLY[@]} -gt 0 && ${COMPREPLY[0]} == /* ]]; then
        compopt -o nospace
    fi
}
complete -F _cv4pve_cli_complete cv4pve-cli
""";

    private static readonly string ZshShim = """
# cv4pve-cli tab completion
_cv4pve_cli_complete()
{
    local line="${words[1,CURRENT-1]} ${words[CURRENT]}"
    local -a suggestions
    suggestions=(${(f)"$(cv4pve-cli complete -- "$line" 2>/dev/null)"})
    [[ ${#suggestions[@]} -eq 0 ]] && return
    if [[ ${suggestions[1]} == /* ]]; then
        compadd -Q -S '' -a suggestions
    else
        compadd -a suggestions
    fi
}
compdef _cv4pve_cli_complete cv4pve-cli
""";

    private const string PwshShim = """
        # cv4pve-cli tab completion
        Register-ArgumentCompleter -Native -CommandName @('cv4pve-cli') -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)
            # Use $wordToComplete (unmangled by PS) to reconstruct the correct last token
            $line = $commandAst.Extent.ToString()
            if ($cursorPosition -gt $line.Length) { $line = $line.PadRight($cursorPosition) }
            # Replace last token with raw $wordToComplete to undo PS path mangling of '/' args
            if ($wordToComplete -ne '' -and $line -notmatch [regex]::Escape($wordToComplete)) {
                $lastSpace = $line.LastIndexOf(' ')
                if ($lastSpace -ge 0) { $line = $line.Substring(0, $lastSpace + 1) + $wordToComplete }
            }
            # TrimEnd only when last token is a path ending with '/' — prevents parser
            # treating trailing space as "new argument" which shifts completions to options
            if ($wordToComplete -match '/$') { $line = $line.TrimEnd() }
            $results = @(cv4pve-cli complete -- "$line" 2>$null)
            if ($results.Count -gt 0) {
                $results | ForEach-Object {
                    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
                }
            } else {
                # Null completion result suppresses PowerShell filesystem fallback
                [System.Management.Automation.CompletionResult]::new("`t", "`t", 'ParameterValue', '')
            }
        }
        """;

    /// <summary>
    /// Registers shell completion shims on first run.
    /// </summary>
    public static void EnsureRegistered()
    {
        if (File.Exists(ShimFile)) { return; }

        try
        {
            Directory.CreateDirectory(ConfigDir);
            RegisterShims();
        }
        catch
        {
            // Never fail startup due to completion registration errors
        }
    }

    private static void RegisterShims()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            RegisterPwsh();
        }
        else
        {
            RegisterBash();
            RegisterZsh();
        }
    }

    private static void RegisterBash()
    {
        var shimFile = Path.Combine(ConfigDir, "completion.bash");
        File.WriteAllText(shimFile, BashShim.ReplaceLineEndings("\n"));

        var bashrc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".bashrc");
        AppendSourceLine(bashrc, shimFile);
    }

    private static void RegisterZsh()
    {
        var shimFile = Path.Combine(ConfigDir, "completion.zsh");
        File.WriteAllText(shimFile, ZshShim.ReplaceLineEndings("\n"));

        var zshrc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".zshrc");
        AppendSourceLine(zshrc, shimFile);
    }

    private static void RegisterPwsh()
    {
        var shimFile = Path.Combine(ConfigDir, "completion.ps1");
        File.WriteAllText(shimFile, PwshShim);

        var profile = GetPwshProfilePath();
        if (profile == null) { return; }

        var marker = "# cv4pve-cli completion";
        var sourceLine = $". \"{shimFile}\"";

        if (File.Exists(profile) && File.ReadAllText(profile).Contains(marker)) { return; }

        Directory.CreateDirectory(Path.GetDirectoryName(profile)!);
        File.AppendAllText(profile, $"\n{marker}\n{sourceLine}\n");
    }

    private static string? GetPwshProfilePath()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("pwsh", "-NoProfile -Command $PROFILE")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            var output = proc?.StandardOutput.ReadToEnd()?.Trim();
            proc?.WaitForExit();
            if (!string.IsNullOrWhiteSpace(output)) { return output; }
        }
        catch { }

        // Fallback: standard location
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(docs, "PowerShell", "Microsoft.PowerShell_profile.ps1");
    }

    private static void AppendSourceLine(string profilePath, string shimFile, string sourceCmd = "source ")
    {
        var marker = "# cv4pve-cli completion";
        var line = $"{sourceCmd}\"{shimFile}\"";

        if (File.Exists(profilePath) && File.ReadAllText(profilePath).Contains(marker)) { return; }

        Directory.CreateDirectory(Path.GetDirectoryName(profilePath)!);
        File.AppendAllText(profilePath, $"\n{marker}\n{line}\n");
    }

    /// <summary>
    /// Deletes all registered completion shims so they are recreated on next run.
    /// </summary>
    public static void ResetRegistered()
    {
        foreach (var f in new[] { "completion.ps1", "completion.bash", "completion.zsh" })
        {
            var path = Path.Combine(ConfigDir, f);
            if (File.Exists(path)) { File.Delete(path); }
        }
        if (File.Exists(ShimFile)) { File.Delete(ShimFile); }
    }

    /// <summary>
    /// Adds the 'completion' subcommand (with 'reset' sub-subcommand) to the root command.
    /// </summary>
    public static void AddCompletionCommand(RootCommand app)
    {
        var cmd = app.AddCommand("completion", "Manage shell tab-completion");
        var reset = cmd.AddCommand("reset", "Re-register shell completion shims");
        reset.SetAction((_) =>
        {
            ResetRegistered();
            Directory.CreateDirectory(ConfigDir);
            RegisterShims();
            Console.WriteLine("Completion shims re-registered. Reload your shell session to apply.");
        });
    }

    /// <summary>
    /// Adds the 'complete' subcommand to the root command.
    /// </summary>
    public static void AddCompleteCommand(RootCommand app)
    {
        var cmd = app.AddCommand("complete", "Output shell completion suggestions");
        cmd.Hidden = true;
        var argLine = cmd.AddArgument<string>("line", "The current command line input");

        cmd.SetAction((action) =>
        {
            var line = action.GetValue(argLine) ?? string.Empty;
            // Strip the command name prefix (e.g. "cv4pve-cli ") if present
            var spaceIdx = line.IndexOf(' ');

            var input = spaceIdx >= 0
                            ? line[(spaceIdx + 1)..]
                            : string.Empty;

            var parseResult = app.Parse(input);
            var position = input.Length;

            // Exclude built-in help/version/alias flags injected by System.CommandLine
            var builtIn = new HashSet<string>([ShellCommands.ArgHelpAlt, ShellCommands.ArgHelpShort,
                                               ShellCommands.ArgHelpSlashQ, ShellCommands.ArgHelpSlashH,
                                               ShellCommands.ArgHelpLong, ShellCommands.ArgVersion],
                                               StringComparer.OrdinalIgnoreCase);

            // CLI-level options (not API params) — suppress when user hasn't typed '--' yet,
            // so TAB after a resource path goes straight to API parameters/paths.
            var cliOptions = new HashSet<string>([ShellCommands.ArgOutputLong, ShellCommands.ArgWait,
                                                  ShellCommands.ArgVerboseLong, ShellCommands.ArgReturnsLong,
                                                  ShellCommands.ArgOutputShort, ShellCommands.ArgVerboseShort,
                                                  ShellCommands.ArgReturnsShort],
                                                  StringComparer.OrdinalIgnoreCase);

            var word = input.Length > 0 && !input.EndsWith(' ')
                        ? input.Split(' ')[^1]
                        : string.Empty;

            var suppressCli = !word.StartsWith('-');
            var completions = parseResult.GetCompletions(position)
                                         .Where(c => c.Label.Length > 0 && !builtIn.Contains(c.Label))
                                         .Where(c => !suppressCli || !cliOptions.Contains(c.Label))
                                         .DistinctBy(c => c.Label)
                                         .ToList();

            // Determine previous token to detect "value after --param" context
            var tokens = input.TrimEnd().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var prevInputToken = tokens.Length >= 2
                                    ? tokens[^(word.Length > 0 ? 2 : 1)]
                                    : string.Empty;

            var expectingParamValue = prevInputToken.StartsWith("--");
            if (expectingParamValue)
            {
                // Keep only enum/bool values (non --options), suppress stale positional suggestions
                // Also exclude values that are already used as positional tokens in the command line
                var usedTokens = new HashSet<string>(tokens, StringComparer.OrdinalIgnoreCase);
                completions = [.. completions.Where(c => !c.Label.StartsWith('-') && !usedTokens.Contains(c.Label))];
            }
            else if (completions.Any(c => c.Label.StartsWith("--")))
            {
                // Suppress stale positional suggestions when --options are available
                completions = [.. completions.Where(c => c.Label.StartsWith('-'))];
            }

            foreach (var c in completions)
            {
                Console.WriteLine(c.Label);
            }
        });
    }
}
