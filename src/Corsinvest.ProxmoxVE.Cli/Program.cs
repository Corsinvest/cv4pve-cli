/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.CommandLine;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Cli;
using Microsoft.Extensions.Logging;

var app = new RootCommand("CLI for Proxmox VE");
app.AddFullNameLogo();
app.AddDebugOption();
app.AddDryRunOption();

var loggerFactory = ConsoleHelper.CreateLoggerFactory<Program>(app.GetLogLevelFromDebug());

ShellCommands.CreateCommands(app, loggerFactory);
SpecialCommands.AddCommands(app);
CompletionHelper.AddCompleteCommand(app);
CompletionHelper.EnsureRegistered();
app.SetAction(ctx => app.Parse("--help").Invoke());

var (effectiveArgs, exitCode) = ShellCommands.ResolveAliasArgs(args);
if (effectiveArgs == null && exitCode != 0) { return exitCode; }

return await app.ExecuteAppAsync(effectiveArgs ?? args, loggerFactory.CreateLogger<Program>());
