/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Corsinvest.ProxmoxVE.Cli;
using Microsoft.Extensions.Logging;

var app = ConsoleHelper.CreateApp(ShellCommands.AppName, "Command line for Proxmox VE");
var loggerFactory = ConsoleHelper.CreateLoggerFactory<Program>(app.GetLogLevelFromDebug());

ShellCommands.CreateCommands(app, loggerFactory);
return await app.ExecuteAppAsync(args, loggerFactory.CreateLogger(typeof(Program)));