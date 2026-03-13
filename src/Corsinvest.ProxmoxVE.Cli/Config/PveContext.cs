/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Cli.Config;

internal class PveContext
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 8006;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ApiToken { get; set; }
    public bool? ValidateCertificate { get; set; }
    public int? Timeout { get; set; }
}
