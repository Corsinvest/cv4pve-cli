/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Cli.Tests;

/// <summary>
/// Alias rewriting that needs no cluster: built-in aliases, no --guest.
/// </summary>
public class AliasTests
{
    private static (string[]? Args, int ExitCode) Resolve(params string[] args) => ShellCommands.ResolveAliasArgs(args);

    /// <summary>The api command an alias is rewritten into.</summary>
    private static string[] Api(params string[] args)
    {
        var (ret, exitCode) = Resolve(args);
        Assert.Equal(0, exitCode);
        return Assert.IsType<string[]>(ret);
    }

    [Fact]
    public void AliasBecomesApiCommand()
        => Assert.Equal(["api", "create", "/nodes/pve01/qemu/100/status/start"],
                        Api("do", "start", "vm", "pve01", "100"));

    [Fact]
    public void ArgumentEqualToAParameterValueIsKept()
        => Assert.Equal(["api", "get", "/nodes/pve01/qemu/1012/status/current", "--foo", "1012"],
                        Api("get", "vm", "status", "pve01", "1012", "--foo", "1012"));

    [Fact]
    public void CliOptionsArePassedOnNotSentAsParameters()
        => Assert.Equal(["api", "create", "/nodes/pve01/qemu/100/status/shutdown", "--timeout", "120", "--wait", "-o", "json"],
                        Api("do", "shutdown", "vm", "pve01", "100", "--timeout", "120", "--wait", "-o", "json", "--yes"));

    [Fact]
    public void ValueWithSpacesStaysOneArgument()
        => Assert.Equal(["api", "create", "/nodes/pve01/qemu/100/snapshot", "--snapname", "s1", "--description", "before update"],
                        Api("create", "vm", "snapshot", "pve01", "100", "s1", "before update"));

    [Fact]
    public void ConfirmAliasWithoutYesIsRefused()
        => Assert.Equal((null, (int)ExitCode.Validation), Resolve("delete", "vm", "snapshot", "pve01", "100", "s1"));

    [Fact]
    public void ConfirmAliasWithYesRunsAndYesIsNotSent()
        => Assert.Equal(["api", "delete", "/nodes/pve01/qemu/100/snapshot/s1"],
                        Api("delete", "vm", "snapshot", "pve01", "100", "s1", "-y"));

    [Fact]
    public void ConfirmAliasWithDryRunNeedsNoYes()
        => Assert.Equal(["api", "delete", "/nodes/pve01/qemu/100/snapshot/s1", "--dry-run"],
                        Api("--dry-run", "delete", "vm", "snapshot", "pve01", "100", "s1"));

    [Fact]
    public void OptionsBeforeTheAliasAreKept()
        => Assert.Equal(["api", "get", "/nodes/pve01/qemu/100/status/current", "--log-level", "Trace", "--debug"],
                        Api("--log-level", "Trace", "--debug", "get", "vm", "status", "pve01", "100"));

    [Fact]
    public void MissingArgumentIsValidationError()
        => Assert.Equal((null, (int)ExitCode.Validation), Resolve("get", "vm", "status", "pve01"));

    [Fact]
    public void ExtraArgumentIsValidationError()
        => Assert.Equal((null, (int)ExitCode.Validation), Resolve("get", "vm", "status", "pve01", "100", "extra"));

    [Fact]
    public void VerboseShowsUsage()
        => Assert.Equal(["api", "usage", "/nodes/pve01/qemu/100/status/current", "get", "--verbose"],
                        Api("get", "vm", "status", "pve01", "100", "-v"));

    [Fact]
    public void HelpIsLeftToTheParser() => Assert.Equal((null, 0), Resolve("get", "vm", "status", "--help"));

    [Fact]
    public void NotAnAlias() => Assert.Equal((null, 0), Resolve("api", "get", "/nodes"));

    [Fact]
    public void ErrorOfAParameterWrittenInTheAliasBlamesTheAlias()
    {
        var alias = new Config.PveConfigManager.PveAlias("get vm tags", "", "get /nodes/{node}/qemu/{vmid}/config --property tags", true);
        var text = ShellCommands.FormatApiError(400,
                                                "Parameter verification failed.\nproperty : property is not defined in schema",
                                                Corsinvest.ProxmoxVE.Api.MethodType.Get,
                                                "/nodes/pve01/qemu/100/config",
                                                [new("property", "tags")],
                                                alias);
        Assert.Contains("Call:  GET /nodes/pve01/qemu/100/config --property tags", text);
        Assert.Contains("--property is written in the alias, not by you", text);
        Assert.Contains("cv4pve-cli api usage /nodes/pve01/qemu/100/config get -v", text);
    }

    [Fact]
    public void ErrorOfAUserParameterDoesNotBlameTheAlias()
    {
        var alias = new Config.PveConfigManager.PveAlias("get vm status", "", "get /nodes/{node}/qemu/{vmid}/status/current", true);
        var text = ShellCommands.FormatApiError(400, "foo : property is not defined in schema", Corsinvest.ProxmoxVE.Api.MethodType.Get,
                                                "/nodes/pve01/qemu/100/status/current", [new("foo", "1")], alias);
        Assert.DoesNotContain("the alias is wrong", text);
    }

    [Fact]
    public void ServerErrorHasNoUsageHint()
        => Assert.DoesNotContain("api usage",
                                 ShellCommands.FormatApiError(500, "hostname lookup 'pve99' failed", Corsinvest.ProxmoxVE.Api.MethodType.Get, "/nodes/pve99/status", [], null));
}
