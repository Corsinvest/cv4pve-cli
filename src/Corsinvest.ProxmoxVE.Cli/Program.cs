using System;
using Corsinvest.ProxmoxVE.Api.Extension.Utils.Shell;

namespace Corsinvest.ProxmoxVE.Cli
{
    class Program
    {
        public static int Main(string[] args)
        {
            var app = ShellHelper.CreateConsoleApp("cv4pve-cli", "Command line for Proxmox VE");
            ShellCommands.Commands(app);
            return app.ExecuteConsoleApp(Console.Out, args);
        }
    }
}