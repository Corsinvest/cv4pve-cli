/*
 * This file is part of the cv4pve-cli https://github.com/Corsinvest/cv4pve-cli,
 *
 * This source file is available under two different licenses:
 * - GNU General Public License version 3 (GPLv3)
 * - Corsinvest Enterprise License (CEL)
 * Full copyright and license information is available in
 * LICENSE.md which is distributed with this source code.
 *
 * Copyright (C) 2016 Corsinvest Srl	GPLv3 and CEL
 */

using System;
using Corsinvest.ProxmoxVE.Api.Extension.Helpers.Shell;

namespace Corsinvest.ProxmoxVE.Cli
{
    class Program
    {
        public static readonly string APP_NAME = "cv4pve-cli";

        static int Main(string[] args)
        {
            var app = ShellHelper.CreateConsoleApp(APP_NAME, "Command line for Proxmox VE");
            ShellCommands.Commands(app);
            return app.ExecuteConsoleApp(Console.Out, args);
        }
    }
}