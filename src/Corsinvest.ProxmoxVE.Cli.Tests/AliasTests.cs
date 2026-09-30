/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Extension.Shell;

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
        => Assert.Equal(["api", "get", "/nodes/pve01/qemu/1012/status/current", "--foo=1012"],
                        Api("get", "vm", "status", "pve01", "1012", "--foo", "1012"));

    [Fact]
    public void CliOptionsArePassedOnNotSentAsParameters()
        => Assert.Equal(["api", "create", "/nodes/pve01/qemu/100/status/shutdown", "--timeout=120", "--wait", "-o", "json"],
                        Api("do", "shutdown", "vm", "pve01", "100", "--timeout", "120", "--wait", "-o", "json", "--yes"));

    [Fact]
    public void ValueWithSpacesStaysOneArgument()
        => Assert.Equal(["api", "create", "/nodes/pve01/qemu/100/snapshot", "--snapname=s1", "--description=before update"],
                        Api("create", "vm", "snapshot", "pve01", "100", "s1", "before update"));

    [Fact]
    public void ValueThatLooksLikeACliOptionReachesProxmox()
    {
        var args = Api("get", "vm", "status", "pve01", "100", "--foo=-v", "--bar=-o", "--baz", "1");
        Assert.Equal([new("foo", "-v"), new("bar", "-o"), new("baz", "1")],
                     ShellCommands.ReadApiParameters(args, "get", "/nodes/pve01/qemu/100/status/current"));
    }

    [Fact]
    public void NumberedParametersWithEqualsInTheValueReachProxmox()
    {
        var args = Api("get", "vm", "status", "pve01", "100",
                       "--usb0", "host=1-2", "--net0=virtio=AA:BB:CC:DD:EE:FF,bridge=vmbr0", "--scsi1", "local-lvm:4");
        Assert.Equal([new("usb0", "host=1-2"), new("net0", "virtio=AA:BB:CC:DD:EE:FF,bridge=vmbr0"), new("scsi1", "local-lvm:4")],
                     ShellCommands.ReadApiParameters(args, "get", "/nodes/pve01/qemu/100/status/current"));
    }

    [Fact]
    public void HumanReadableIsPassedOnNotSentAsParameter()
    {
        var args = Api("get", "vm", "status", "pve01", "100", "--human-readable", "0", "--foo", "1");
        Assert.Contains("--human-readable", args);
        Assert.Equal([new("foo", "1")], ShellCommands.ReadApiParameters(args, "get", "/nodes/pve01/qemu/100/status/current"));
    }

    [Theory]
    [InlineData("--all-columns")]
    [InlineData("-A")]
    public void AllColumnsIsPassedOnNotSentAsParameter(string option)
    {
        var args = Api("get", "vm", "status", "pve01", "100", option);
        Assert.Contains(option, args);
        Assert.Empty(ShellCommands.ReadApiParameters(args, "get", "/nodes/pve01/qemu/100/status/current"));
    }

    [Theory]
    [InlineData("node,vmid", "pve01|100", "node,vmid", "pve01|100")]
    [InlineData("node,vmid", "--guest|9999|--onb", "", "")]
    [InlineData("node,vmid", "-g|web01", "", "")]
    [InlineData("node,vmid,snapname", "--guest|100|s1", "snapname", "s1")]
    [InlineData("node,vmid", "pve01|--foo|1", "node,vmid", "pve01")]
    [InlineData("node,vmid", "-o|json|pve01", "node,vmid", "pve01")]
    [InlineData("node,vmid", "--human-readable|0|pve01|--wait", "node,vmid", "pve01")]
    [InlineData("node,vmid", "--yes|pve01|100", "node,vmid", "pve01|100")]
    public void PositionalArgumentsForCompletion(string tags, string tokens, string open, string values)
    {
        static string[] Split(string text, char separator) => text.Length == 0 ? [] : text.Split(separator);

        var (openTags, written) = ShellCommands.GetAliasPositional(Split(tags, ','), Split(tokens, '|'));

        Assert.Equal(Split(open, ','), openTags);
        Assert.Equal(Split(values, '|'), written);
    }

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
        => Assert.Equal(["api", "usage", "/nodes/{node}/qemu/{vmid}/status/current", "get", "--verbose"],
                        Api("get", "vm", "status", "pve01", "100", "-v"));

    [Fact]
    public void HelpIsLeftToTheParser() => Assert.Equal((null, 0), Resolve("get", "vm", "status", "--help"));

    [Fact]
    public void NotAnAlias() => Assert.Equal((null, 0), Resolve("api", "get", "/nodes"));

    private static ApiResponse Refused(int status, string resource, Dictionary<string, object> parameters, Dictionary<string, string> errors)
        => new()
        {
            Command = new ApiCommand(Corsinvest.ProxmoxVE.Api.MethodType.Get, resource, parameters),
            StatusCode = status,
            Error = status == 400 ? "Parameter verification failed." : "hostname lookup 'pve99' failed",
            ParameterErrors = errors,
        };

    [Fact]
    public void ErrorOfAParameterWrittenInTheAliasBlamesTheAlias()
    {
        var alias = new Config.PveConfigManager.PveAlias("get vm tags", "", "get /nodes/{node}/qemu/{vmid}/config --property tags", true);
        var text = ShellCommands.FormatApiError(Refused(400, "/nodes/pve01/qemu/100/config",
                                                        new() { ["property"] = "tags" },
                                                        new() { ["property"] = "property is not defined in schema" }),
                                                alias);
        Assert.Contains("Call:  GET /nodes/pve01/qemu/100/config --property tags", text);
        Assert.Contains("--property is written in the alias, not by you", text);
        Assert.Contains("cv4pve-cli api usage /nodes/pve01/qemu/100/config get -v", text);
    }

    [Fact]
    public void ErrorOfAUserParameterDoesNotBlameTheAlias()
    {
        var alias = new Config.PveConfigManager.PveAlias("get vm status", "", "get /nodes/{node}/qemu/{vmid}/status/current", true);
        var text = ShellCommands.FormatApiError(Refused(400, "/nodes/pve01/qemu/100/status/current",
                                                        new() { ["foo"] = "1" },
                                                        new() { ["foo"] = "property is not defined in schema" }),
                                                alias);
        Assert.DoesNotContain("the alias is wrong", text);
    }

    [Fact]
    public void ServerErrorHasNoUsageHint()
        => Assert.DoesNotContain("api usage", ShellCommands.FormatApiError(Refused(500, "/nodes/pve99/status", [], []), null));

    [Fact]
    public void WaitTimeoutIsPassedOn()
        => Assert.Equal(["api", "create", "/nodes/pve01/qemu/100/status/start", "--wait", "--wait-timeout", "60"],
                        Api("do", "start", "vm", "pve01", "100", "--wait", "--wait-timeout", "60"));
}
