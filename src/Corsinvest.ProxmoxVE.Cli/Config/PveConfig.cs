/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Cli.Config;

internal class PveConfig
{
    public string CurrentContext { get; set; } = string.Empty;
    public List<PveContext> Contexts { get; set; } = [];
}
