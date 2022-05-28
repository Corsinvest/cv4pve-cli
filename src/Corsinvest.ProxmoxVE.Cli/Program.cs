/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;

namespace Corsinvest.ProxmoxVE.Cli
{
    class Program
    {
        public static readonly string AppName = "cv4pve-cli";

        static async Task<int> Main(string[] args)
        {
            var rc = ConsoleHelper.CreateApp(AppName, "Command line for Proxmox VE");
            ShellCommands.CreateCommands(rc);
            return await rc.ExecuteApp(args);
        }
    }
}