/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.CommandLine;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Cli;
using Microsoft.Extensions.Logging;

var app = new RootCommand("CLI for Proxmox VE");
app.AddFullNameLogo();
app.AddDebugOption();
app.AddLogLevelOption();
app.AddDryRunOption();

var logLevel = app.GetLogLevelFromDebug();
ExitCodeHelper.IsDebug = logLevel <= LogLevel.Debug;
// As ConsoleHelper.CreateLoggerFactory, but on stderr: stdout carries only the output (tables, -o json, completions).
var loggerFactory = LoggerFactory.Create(builder => builder.AddFilter("Microsoft", LogLevel.Warning)
                                                           .AddFilter("System", LogLevel.Warning)
                                                           .AddFilter(typeof(PveClientBase).FullName, logLevel)
                                                           .AddFilter(typeof(Program).FullName, logLevel)
                                                           .AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace));

ShellCommands.CreateCommands(app, loggerFactory);
app.AddTaskCommands();
SpecialCommands.AddCommands(app);
CompletionHelper.AddCompleteCommand(app);
CompletionHelper.EnsureRegistered();
app.SetAction(ctx => app.Parse(ShellCommands.ArgHelpLong).Invoke());

var (effectiveArgs, exitCode) = ShellCommands.ResolveAliasArgs(args);
if (effectiveArgs == null && exitCode != 0) { return exitCode; }

ShellCommands.CommandLine = effectiveArgs ?? args;
return await app.ExecuteAppAsync(ShellCommands.CommandLine, loggerFactory.CreateLogger<Program>());
