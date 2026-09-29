/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;

namespace Corsinvest.ProxmoxVE.Cli.Tests;

public class ApiParametersTests
{
    private static Dictionary<string, string> Parse(params string[] tokens)
        => ApiParameters.Parse(tokens).Parameters.ToDictionary(a => a.Key, a => a.Value);

    [Fact]
    public void KeyValuePairs()
        => Assert.Equal(new Dictionary<string, string> { ["memory"] = "4096", ["cores"] = "2" },
                        Parse("--memory", "4096", "--cores", "2"));

    [Fact]
    public void KeyEqualsValue()
        => Assert.Equal(new Dictionary<string, string> { ["usb0"] = "spice", ["name"] = "a=b" },
                        Parse("--usb0=spice", "--name=a=b"));

    [Fact]
    public void FlagInTheMiddleDoesNotShiftValues()
        => Assert.Equal(new Dictionary<string, string> { ["force"] = "true", ["memory"] = "4096" },
                        Parse("--force", "--memory", "4096"));

    [Fact]
    public void FlagAtTheEndIsTrue()
        => Assert.Equal(new Dictionary<string, string> { ["memory"] = "4096", ["force"] = "true" },
                        Parse("--memory", "4096", "--force"));

    [Fact]
    public void NegativeNumberIsAValue()
        => Assert.Equal("-1", Parse("--limit", "-1")["limit"]);

    [Fact]
    public void TokensNotTakenAsValueArePositional()
    {
        var (parameters, positional) = ApiParameters.Parse(["pve01", "100", "--timeout", "100"]);
        Assert.Equal(["pve01", "100"], positional);
        Assert.Equal("100", parameters.Single().Value);
    }

    [Fact]
    public void RepeatedParameterIsAnError()
        => Assert.Throws<ArgumentException>(() => ApiParameters.Parse(["--type", "vm", "--type", "node"]));

    [Fact]
    public void RemoveOptions()
        => Assert.Equal(["/nodes", "--memory", "4096"],
                        ApiParameters.RemoveOptions(["/nodes", "-o", "json", "--wait", "--memory", "4096", "--output=text"],
                                                    ["--wait"],
                                                    ["-o", "--output"]));

    [Fact]
    public void ReadApiParametersKeepsTheOrderAndDropsCliOptions()
    {
        var parameters = ShellCommands.ReadApiParameters(["--debug", "api", "set", "/nodes/pve01/qemu/100/config",
                                                          "--delete", "--memory", "4096", "-o", "json", "--wait"],
                                                         "set",
                                                         "/nodes/pve01/qemu/100/config");
        Assert.Equal([new("delete", "true"), new("memory", "4096")], parameters);
    }

    [Fact]
    public void ReadApiParametersRejectsAValueWithoutKey()
        => Assert.Throws<ArgumentException>(() => ShellCommands.ReadApiParameters(["api", "get", "/nodes", "pve01"], "get", "/nodes"));

    [Fact]
    public void DryRunShowsMethodPathAndParameters()
        => Assert.Equal($"Dry run, nothing sent to Proxmox VE:{Environment.NewLine}POST /nodes/pve01/qemu/100/snapshot{Environment.NewLine}  snapname: s1{Environment.NewLine}",
                        ShellCommands.FormatDryRun(MethodType.Create, "/nodes/pve01/qemu/100/snapshot", [new("snapname", "s1")]));

    [Theory]
    [InlineData("C:/Program Files/Git/version")]
    [InlineData(@"C:\Program Files\Git\nodes")]
    public void GitBashPathIsRejected(string resource)
        => Assert.Equal(ExitCode.Validation, Assert.Throws<CliException>(() => ShellCommands.CheckResource(resource)).Code);

    [Fact]
    public void ApiPathIsAccepted() => ShellCommands.CheckResource("/version");
}
