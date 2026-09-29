/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Cli.Config;

namespace Corsinvest.ProxmoxVE.Cli.Tests;

public class ConfigTests
{
    [Theory]
    [InlineData("pve1", 8006, "pve1:8006")]
    [InlineData("pve1:8007", 8006, "pve1:8007")]
    [InlineData("pve1,pve2", 8007, "pve1:8007,pve2:8007")]
    [InlineData("pve1, pve2:8008", 8007, "pve1:8007,pve2:8008")]
    [InlineData("fd00::11", 8006, "[fd00::11]:8006")]
    [InlineData("[fd00::11]", 8006, "[fd00::11]:8006")]
    [InlineData("[fd00::11]:8007", 8006, "[fd00::11]:8007")]
    public void BuildHostList(string hosts, int port, string expected)
        => Assert.Equal(expected, PveConfigManager.BuildHostList(hosts, port));

    [Fact]
    public void ApiTokenSecretIsHidden()
        => Assert.Equal("ab@pve!t=****", PveConfigManager.MaskApiToken("ab@pve!t=12345678-aaaa-bbbb-cccc-1234567890ab"));

    [Theory]
    [InlineData(400, "Parameter verification failed.", 6)]
    [InlineData(401, "", 2)]
    [InlineData(403, "Permission check failed (/vms/100, VM.PowerMgmt)", 2)]
    [InlineData(501, "Method 'GET /nodex' not implemented", 3)]
    [InlineData(500, "Configuration file 'nodes/pve01/qemu-server/100.conf' does not exist", 3)]
    [InlineData(500, "hostname lookup 'pve99' failed", 4)]
    public void HttpStatusToExitCode(int status, string message, int expected)
        => Assert.Equal(expected, (int)ExitCodeHelper.FromHttpStatus(status, message));
}
