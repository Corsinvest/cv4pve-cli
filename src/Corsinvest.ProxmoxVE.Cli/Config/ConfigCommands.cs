/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.CommandLine;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Extension;

namespace Corsinvest.ProxmoxVE.Cli.Config;

/// <summary>
/// Config subcommands (use-context, get-contexts, add-context, ...)
/// </summary>
internal static class ConfigCommands
{
    private static async Task ValidateAndPrint(PveContext ctx)
    {
        try
        {
            var client = await PveConfigManager.CreateClientAsync(ctx);
            Console.WriteLine($"Connected to {ctx.Host} — PVE version: {(await client.Version.GetAsync()).Version}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: could not connect to '{ctx.Host}': {ex.Message}");
        }
    }

    public static void AddConfigCommands(this RootCommand root)
    {
        var cmd = root.AddCommand("config", "Manage connection contexts");

        CmdUse(cmd);
        CmdCurrent(cmd);
        CmdList(cmd);
        CmdAdd(cmd);
        CmdSet(cmd);
        CmdDelete(cmd);
        CmdRename(cmd);
        CmdVerify(cmd);
        CmdView(cmd);
    }

    private static IEnumerable<string> GetContextNames()
    {
        try { return PveConfigManager.Load().Contexts.Select(c => c.Name); }
        catch { return []; }
    }

    private static Argument<string> AddContextArgument(Command cmd, string description = "Context name")
    {
        var arg = cmd.AddArgument<string>("name", description);
        arg.HelpName = "name";
        arg.CompletionSources.Add((_) => GetContextNames());
        return arg;
    }

    private static void CmdUse(Command parent)
    {
        var cmd = parent.AddCommand("use", "Set the current context");
        var argName = AddContextArgument(cmd);
        cmd.SetAction((action) =>
        {
            try
            {
                PveConfigManager.UseContext(action.GetValue(argName)!);
                Console.WriteLine($"Switched to context '{action.GetValue(argName)}'.");
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        });
    }

    private static void CmdCurrent(Command parent)
    {
        var cmd = parent.AddCommand("current", "Show the current context");
        cmd.SetAction((_) =>
        {
            var config = PveConfigManager.Load();
            Console.WriteLine(string.IsNullOrWhiteSpace(config.CurrentContext)
                                ? "No current context set."
                                : config.CurrentContext);
        });
    }

    private static void CmdList(Command parent)
    {
        var cmd = parent.AddCommand("list", "List all contexts");
        cmd.SetAction((_) =>
        {
            var config = PveConfigManager.Load();
            if (config.Contexts.Count == 0) { Console.WriteLine("No contexts defined."); return; }

            foreach (var ctx in config.Contexts)
            {
                var current = ctx.Name == config.CurrentContext ? "* " : "  ";
                Console.WriteLine($"{current}{ctx.Name,-20} {ctx.Host}:{ctx.Port}  {ctx.Username}{(string.IsNullOrWhiteSpace(ctx.ApiToken) ? "" : " (api-token)")}");
            }
        });
    }

    private static void CmdAdd(Command parent)
    {
        var cmd = parent.AddCommand("add", "Add or update a context");
        var argName = cmd.AddArgument<string>("name", "Context name");
        var optHost = cmd.AddOption<string>("--host", "Host[:port]");
        var optPort = cmd.AddOption<int>("--port", "Port");
        var optUsername = cmd.AddOption<string>("--username", "Username (user@realm)");
        var optPassword = cmd.AddOption<string>("--password", "Password");
        var optApiToken = cmd.AddOption<string>("--api-token", "API token (USER@REALM!TOKENID=UUID)");
        var optValidate = cmd.AddOption<bool?>("--validate-certificate", "Validate SSL certificate (default: true)");
        var optTimeout = cmd.AddOption<int?>("--timeout", "Request timeout in seconds (default: 30)");

        optPort.DefaultValueFactory = (_) => 8006;
        optHost.Required = true;

        cmd.SetAction(async (action) =>
        {
            try
            {
                var ctx = new PveContext
                {
                    Name = action.GetValue(argName)!,
                    Host = action.GetValue(optHost)!,
                    Port = action.GetValue(optPort),
                    Username = action.GetValue(optUsername),
                    Password = action.GetValue(optPassword),
                    ApiToken = action.GetValue(optApiToken),
                    ValidateCertificate = action.GetValue(optValidate),
                    Timeout = action.GetValue(optTimeout),
                };
                PveConfigManager.AddContext(ctx);
                Console.WriteLine($"Context '{ctx.Name}' saved.");
                await ValidateAndPrint(ctx);
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        });
    }

    private static void CmdSet(Command parent)
    {
        var cmd = parent.AddCommand("set", "Update specific fields of an existing context");
        var argName = AddContextArgument(cmd);
        var optHost = cmd.AddOption<string>("--host", "Host");
        var optPort = cmd.AddOption<int?>("--port", "Port");
        var optUsername = cmd.AddOption<string>("--username", "Username (user@realm)");
        var optPassword = cmd.AddOption<string>("--password", "Password");
        var optApiToken = cmd.AddOption<string>("--api-token", "API token (USER@REALM!TOKENID=UUID)");
        var optValidate = cmd.AddOption<bool?>("--validate-certificate", "Validate SSL certificate");
        var optTimeout = cmd.AddOption<int?>("--timeout", "Request timeout in seconds");

        cmd.SetAction(async (action) =>
        {
            try
            {
                var config = PveConfigManager.Load();
                var ctx = config.Contexts.FirstOrDefault(c => c.Name == action.GetValue(argName))
                            ?? throw new InvalidOperationException($"Context '{action.GetValue(argName)}' not found.");

                var host = action.GetValue(optHost);
                var port = action.GetValue(optPort);
                var username = action.GetValue(optUsername);
                var password = action.GetValue(optPassword);
                var apiToken = action.GetValue(optApiToken);
                var validate = action.GetValue(optValidate);
                var timeout = action.GetValue(optTimeout);

                if (host != null) { ctx.Host = host; }
                if (port != null) { ctx.Port = port.Value; }
                if (username != null) { ctx.Username = username; }
                if (password != null) { ctx.Password = password; }
                if (apiToken != null) { ctx.ApiToken = apiToken; }
                if (validate != null) { ctx.ValidateCertificate = validate.Value; }
                if (timeout != null) { ctx.Timeout = timeout.Value; }

                PveConfigManager.Save(config);
                Console.WriteLine($"Context '{action.GetValue(argName)}' updated.");
                await ValidateAndPrint(ctx);
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        });
    }

    private static void CmdDelete(Command parent)
    {
        var cmd = parent.AddCommand("delete", "Delete a context");
        var argName = AddContextArgument(cmd);
        cmd.SetAction((action) =>
        {
            try
            {
                PveConfigManager.DeleteContext(action.GetValue(argName)!);
                Console.WriteLine($"Context '{action.GetValue(argName)}' deleted.");
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        });
    }

    private static void CmdRename(Command parent)
    {
        var cmd = parent.AddCommand("rename", "Rename a context");
        var argOld = AddContextArgument(cmd, "Current context name");
        var argNew = cmd.AddArgument<string>("new-name", "New context name");
        cmd.SetAction((action) =>
        {
            try
            {
                PveConfigManager.RenameContext(action.GetValue(argOld)!, action.GetValue(argNew)!);
                Console.WriteLine($"Context renamed to '{action.GetValue(argNew)}'.");
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        });
    }

    private static void CmdVerify(Command parent)
    {
        var cmd = parent.AddCommand("verify", "Test the connection to a context");
        var argName = cmd.AddArgument<string?>("name", "Context name (default: current context)");
        argName.Arity = ArgumentArity.ZeroOrOne;
        argName.HelpName = "name";
        argName.CompletionSources.Add((_) => GetContextNames());
        cmd.SetAction(async (action) =>
        {
            try
            {
                var name = action.GetValue(argName);
                PveContext? ctx;
                if (string.IsNullOrWhiteSpace(name))
                {
                    ctx = PveConfigManager.GetCurrentContext()
                            ?? throw new InvalidOperationException("No current context set.");
                }
                else
                {
                    var config = PveConfigManager.Load();
                    ctx = config.Contexts.FirstOrDefault(c => c.Name == name)
                            ?? throw new InvalidOperationException($"Context '{name}' not found.");
                }
                await ValidateAndPrint(ctx);
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        });
    }

    private static void CmdView(Command parent)
    {
        var cmd = parent.AddCommand("view", "Show current configuration (passwords hidden)");
        cmd.SetAction((_) =>
        {
            var config = PveConfigManager.Load();
            Console.WriteLine($"current-context: {config.CurrentContext}");
            Console.WriteLine("contexts:");
            foreach (var ctx in config.Contexts)
            {
                Console.WriteLine($"  - name:                 {ctx.Name}");
                Console.WriteLine($"    host:                 {ctx.Host}");
                Console.WriteLine($"    port:                 {ctx.Port}");
                if (!string.IsNullOrWhiteSpace(ctx.Username))
                {
                    Console.WriteLine($"    username:             {ctx.Username}");
                }

                if (!string.IsNullOrWhiteSpace(ctx.Password))
                {
                    Console.WriteLine($"    password:             ****");
                }

                if (!string.IsNullOrWhiteSpace(ctx.ApiToken))
                {
                    Console.WriteLine($"    api-token:            {ctx.ApiToken[..Math.Min(20, ctx.ApiToken.Length)]}...");
                }

                Console.WriteLine($"    validate-certificate: {ctx.ValidateCertificate}");
                if (ctx.Timeout.HasValue)
                {
                    Console.WriteLine($"    timeout:              {ctx.Timeout}s");
                }
            }
        });
    }
}
