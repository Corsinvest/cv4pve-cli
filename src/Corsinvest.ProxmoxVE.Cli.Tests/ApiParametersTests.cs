/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Metadata;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Cli.Tests;

public class ApiParametersTests
{
    [Fact]
    public void ReadApiParametersDropsCliOptionsWithTheirValueForms()
    {
        var parameters = ShellCommands.ReadApiParameters(["api", "set", "/nodes/pve01/qemu/100/config", "--output=text",
                                                          "--memory", "4096", "--wait-timeout", "60", "-v", "--log-level:debug"],
                                                         "set",
                                                         "/nodes/pve01/qemu/100/config");
        Assert.Equal([new("memory", "4096")], parameters);
    }

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

    [Fact]
    public async Task TaskIdIsPrintedWithoutReadingTheSchema()
        => Assert.Equal("UPID:pve01:1:2:3:qmstart:100:root@pam:",
                        await ShellCommands.FormatDataAsync("UPID:pve01:1:2:3:qmstart:100:root@pam:",
                                                            () => throw new InvalidOperationException("schema read"),
                                                            "/nodes/pve01/qemu/100/status/start",
                                                            TableGenerator.Output.Text,
                                                            false,
                                                            true,
                                                            TextWriter.Null));

    [Fact]
    public async Task DataIsPrintedAsJsonWhenTheSchemaCannotBeRead()
    {
        var data = new Dictionary<string, object> { ["vmid"] = 107L };
        var warnings = new StringWriter();
        var text = await ShellCommands.FormatDataAsync(data,
                                                       () => throw new HttpRequestException("apidoc.js not found"),
                                                       "/nodes/pve01/qemu",
                                                       TableGenerator.Output.Text,
                                                       false,
                                                       true,
                                                       warnings);

        Assert.Contains("\"vmid\": 107", text);
        Assert.Contains("apidoc.js not found", warnings.ToString());
    }

    private static Task<ClassApi> Nodes()
        => Task.FromResult(GeneratorClassApi.BuildClassApiFromFlat(GeneratorClassApi.LoadFlatCache("""
            {"/nodes":{"methods":{"get":{"returnType":"array","returnParams":[{"name":"node","type":"string"},
                {"name":"maxmem","type":"integer","renderer":"bytes"}]}}}}
            """)!));

    private static List<object> Memory() => [new Dictionary<string, object> { ["node"] = "pve01", ["maxmem"] = 1536L }];

    [Theory]
    [InlineData(TableGenerator.Output.Text, true, "1.50 KiB")]
    [InlineData(TableGenerator.Output.Text, false, "1536")]
    [InlineData(TableGenerator.Output.Json, true, "\"maxmem\":1536")]
    public async Task ValuesAreHumanReadableExceptInJsonOrWhenAsked(TableGenerator.Output output, bool humanReadable, string expected)
        => Assert.Contains(expected,
                           await ShellCommands.FormatDataAsync(Memory(), Nodes, "/nodes", output, false, humanReadable, TextWriter.Null));

    [Theory]
    [InlineData("--human-readable", "false")]
    [InlineData("--human-readable", "0")]
    [InlineData("--human-readable=false")]
    [InlineData("--human-readable:0")]
    [InlineData("--human-readable")]
    public void HumanReadableIsNotSentToProxmox(params string[] option)
        => Assert.Equal([new("memory", "4096")],
                        ShellCommands.ReadApiParameters(["api", "set", "/nodes/pve01/qemu/100/config", .. option, "--memory", "4096"],
                                                        "set",
                                                        "/nodes/pve01/qemu/100/config"));

    [Theory]
    [InlineData(true, "api", "get", "/nodes")]
    [InlineData(false, "api", "get", "/nodes", "--human-readable", "0")]
    [InlineData(false, "api", "get", "/nodes", "--human-readable", "false")]
    [InlineData(true, "api", "get", "/nodes", "--human-readable", "1")]
    [InlineData(true, "api", "get", "--human-readable", "/nodes")]
    [InlineData(false, "api", "get", "/nodes", "--human-readable=0")]
    [InlineData(false, "api", "get", "/nodes", "--human-readable:false")]
    public void HumanReadableFromTheCommandLine(bool expected, params string[] commandLine)
        => Assert.Equal(expected, ShellCommands.ReadHumanReadable(commandLine));

    [Theory]
    [InlineData(null, true)]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("FALSE", false)]
    public void HumanReadableValue(string? value, bool expected) => Assert.Equal(expected, ShellCommands.ParseHumanReadable(value));

    private static List<object> NodeList()
        => [new Dictionary<string, object> { ["node"] = "pve01", ["uptime"] = 100L, ["ssl_fingerprint"] = "AA:BB" }];

    [Fact]
    public async Task ListShowsTheSchemaColumnsAndSaysWhichAreHidden()
    {
        var warnings = new StringWriter();
        var text = await ShellCommands.FormatDataAsync(NodeList(), Nodes, "/nodes", TableGenerator.Output.Text, false, true, warnings);

        Assert.Contains("pve01", text);
        Assert.DoesNotContain("AA:BB", text);
        Assert.Equal($"+ 2 more columns: ssl_fingerprint, uptime (use --all-columns or -o json){Environment.NewLine}", warnings.ToString());
    }

    [Theory]
    [InlineData(TableGenerator.Output.Text, true)]
    [InlineData(TableGenerator.Output.Json, false)]
    [InlineData(TableGenerator.Output.JsonPretty, false)]
    public async Task AllColumnsOrJsonShowEveryColumn(TableGenerator.Output output, bool allColumns)
    {
        var warnings = new StringWriter();
        var text = await ShellCommands.FormatDataAsync(NodeList(), Nodes, "/nodes", output, allColumns, true, warnings);

        Assert.Contains("AA:BB", text);
        Assert.Empty(warnings.ToString());
    }
}
