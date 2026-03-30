# cv4pve-cli

```
   ______                _                      __
  / ____/___  __________(_)___ _   _____  _____/ /_
 / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
/ /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
\____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

Command Line Interface for Proxmox VE (Made in Italy)
```

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-cli.svg?style=flat-square)](LICENSE.md)
[![Release](https://img.shields.io/github/release/Corsinvest/cv4pve-cli.svg?style=flat-square)](https://github.com/Corsinvest/cv4pve-cli/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Corsinvest/cv4pve-cli/total.svg?style=flat-square&logo=download)](https://github.com/Corsinvest/cv4pve-cli/releases)

cv4pve-cli is to Proxmox VE what `kubectl` is to Kubernetes — a remote-first CLI with context switching, direct API access, and aliases for common operations. Think of it as [`pvesh`](https://pve.proxmox.com/pve-docs/pvesh.1.html), but running from your workstation, supporting multiple clusters, and with tab completion that queries the live API.

| | [pvesh](https://pve.proxmox.com/pve-docs/pvesh.1.html) | kubectl | cv4pve-cli |
|---|---|---|---|
| Direct API access | yes | yes | yes |
| Runs remotely (off-node) | no | yes | yes |
| Multiple clusters / contexts | no | yes | yes |
| Tab completion (live) | yes (local only) | yes | yes |
| Aliases / shortcuts | no | yes | yes |

---

## Quick Start

```bash
# 1. Add a context (connection profile)
cv4pve-cli config add homelab --host 192.168.1.100 --username root@pam --password secret

# 2. Use it
cv4pve-cli config use homelab

# 3. Run commands
cv4pve-cli api get /nodes
cv4pve-cli top
```

---

## Installation

### Windows

```powershell
winget install Corsinvest.cv4pve.cli
```

### Linux / macOS

Download the binary for your platform from the [releases page](https://github.com/Corsinvest/cv4pve-cli/releases/latest), then:

```bash
unzip cv4pve-cli-linux-x64.zip
chmod +x cv4pve-cli
sudo mv cv4pve-cli /usr/local/bin/
```

Packages for deb, rpm and AUR are also available on the releases page.

---

## Configuration

All configuration is managed through the `config` subcommand. Contexts are connection profiles — each one stores a host, credentials, and port.

### File locations

| Platform | Base path |
|----------|-----------|
| Linux / macOS | `~/.cv4pve/cli/` |
| Windows | `%USERPROFILE%\.cv4pve\cli\` |

All files are in YAML format and can be edited directly with any text editor.

| File | Content |
|------|---------|
| `config` | Contexts (connection profiles) |
| `alias` | User-defined aliases |
| `cache/<version>.json` | API schema cache (auto-generated) |

### Add a context

```bash
# Username/password
cv4pve-cli config add homelab --host 192.168.1.100 --username root@pam --password secret

# API token (recommended for production)
cv4pve-cli config add prod --host pve.company.com --api-token root@pam!mytoken=uuid-here

# Custom port, skip certificate validation
cv4pve-cli config add dev --host 10.0.0.1 --port 8007 --username root@pam --password secret --validate-certificate false

# Custom timeout (default: 30s)
cv4pve-cli config add slow --host 10.0.0.1 --username root@pam --password secret --timeout 60
```

### Switch context

```bash
cv4pve-cli config use prod
cv4pve-cli config current               # Show which context is active
```

### List and inspect contexts

```bash
cv4pve-cli config list                  # List all contexts (* = active)
cv4pve-cli config view                  # Full config dump (passwords hidden)
```

### Update a context

Only the fields you specify are changed:

```bash
cv4pve-cli config set homelab --password newpass
cv4pve-cli config set homelab --host 192.168.1.200 --port 8006
cv4pve-cli config set homelab --timeout 60
```

### Rename and delete

```bash
cv4pve-cli config rename homelab lab
cv4pve-cli config delete lab
```

### Verify connection

```bash
cv4pve-cli config verify                # Test the current context
cv4pve-cli config verify homelab        # Test a specific context
```

---

## API Commands

All Proxmox VE REST API calls go through the `api` subcommand.

```
cv4pve-cli api get    <resource> [--key value ...]
cv4pve-cli api set    <resource> [--key value ...]
cv4pve-cli api create <resource> [--key value ...]
cv4pve-cli api delete <resource>
cv4pve-cli api ls     <resource>
cv4pve-cli api usage  <resource> [method] [--returns] [--output <format>]
```

Parameters use `--key value` format and can be repeated:

```bash
cv4pve-cli api get /cluster/resources --type vm
cv4pve-cli api set /nodes/pve1/qemu/100/config --memory 4096 --cores 2
cv4pve-cli api create /nodes/pve1/qemu/100/snapshot --snapname before-update
cv4pve-cli api delete /nodes/pve1/qemu/100/snapshot/before-update
```

### Output formats

Use `--output` (or `-o`) to control the output format:

```bash
cv4pve-cli api get /nodes --output text        # default, table
cv4pve-cli api get /nodes --output json
cv4pve-cli api get /nodes --output jsonpretty
cv4pve-cli api get /nodes --output html
cv4pve-cli api get /nodes --output markdown
```

### Browse and explore the API

```bash
# List children of a path
cv4pve-cli api ls /
cv4pve-cli api ls /nodes
cv4pve-cli api ls /nodes/pve1/qemu

# Show parameters and schema for a resource
cv4pve-cli api usage /cluster/resources
cv4pve-cli api usage /nodes/{node}/qemu/{vmid}/config get
cv4pve-cli api usage /nodes/{node}/qemu/{vmid}/config get --returns --output json
```

---

## Aliases

Aliases are shortcuts for frequently used API commands. A command can contain named placeholders in curly braces (e.g. `{node}`, `{vmid}`). When you run the alias, you supply values for each placeholder positionally — the first argument fills the first placeholder, the second fills the second, and so on.

### Built-in aliases

A set of read-only aliases is available out of the box, organized by verb and resource. They cannot be modified or removed. A few examples:

| Alias | Arguments | Description |
|-------|-----------|-------------|
| `top` | — | Cluster resource overview |
| `get vms` | — | List all VMs (cluster-wide) |
| `show vm` | `<node> <vmid>` | Show VM config |
| `do start vm` | `<node> <vmid>` | Start a VM |
| `do snapshot vm` | `<node> <vmid> <snapname>` | Create a VM snapshot |
| `get cluster not-backed-up` | — | List guests with no backup job |

Aliases prefixed with `guest` work for both VMs (`qemu`) and containers (`lxc`) — `{vmtype}` is resolved automatically from the cluster:

| Alias | Arguments | Description |
|-------|-----------|-------------|
| `get guests` | — | List all VMs and containers (cluster-wide) |
| `show guest` | `<node> <vmtype> <vmid>` | Show guest config (VM or CT) |
| `get guest status` | `<node> <vmtype> <vmid>` | Show guest runtime status |
| `do start guest` | `<node> <vmtype> <vmid>` | Start a guest |
| `do stop guest` | `<node> <vmtype> <vmid>` | Stop a guest |
| `do shutdown guest` | `<node> <vmtype> <vmid>` | Shutdown a guest |
| `do reboot guest` | `<node> <vmtype> <vmid>` | Reboot a guest |
| `do migrate guest` | `<node> <vmtype> <vmid> <target_node>` | Migrate a guest to another node |
| `get guest snapshots` | `<node> <vmtype> <vmid>` | List snapshots |
| `create guest snapshot` | `<node> <vmtype> <vmid> <snapname> <descr>` | Create a snapshot |
| `do rollback guest` | `<node> <vmtype> <vmid> <snapname>` | Rollback to a snapshot |
| `delete guest` | `<node> <vmtype> <vmid>` | Destroy a guest (permanent!) |

With `--guest` you can skip `<node>`, `<vmtype>`, and `<vmid>` entirely — the CLI resolves them from a VM/CT name or ID:

```bash
cv4pve-cli get guest status --guest myvm
cv4pve-cli do start guest --guest 100
cv4pve-cli do shutdown guest --guest myct
cv4pve-cli create guest snapshot --guest myvm snap1 "before update"
```

The same operation is available at all three levels — `guest` (both), `vm` (qemu only), `ct` (lxc only):

```bash
cv4pve-cli create guest snapshot --guest myvm  snap1 "before update"  # VM or CT — by name
cv4pve-cli create guest snapshot --guest 100   snap1 "before update"  # VM or CT — by id
cv4pve-cli create vm snapshot pve1 100 snap1 "before update"           # VM only
cv4pve-cli create ct snapshot pve1 200 snap1 "before update"           # CT only
```

See all built-in and user aliases:

```bash
cv4pve-cli alias list
cv4pve-cli alias list --verbose        # includes command and placeholder args
cv4pve-cli alias list --search snap    # filter by keyword
```

### Run an alias

```bash
# No arguments
cv4pve-cli top
cv4pve-cli get vms

# With arguments — placeholders filled positionally
#   "show vm" command: "get /nodes/{node}/qemu/{vmid}/config"
#                                          ↑          ↑
cv4pve-cli show vm pve1 100            #  node=pve1  vmid=100
cv4pve-cli do start vm pve1 100
cv4pve-cli get vm snapshots pve1 100

# Options: --output, --verbose, --wait (passed through to the underlying api call)
cv4pve-cli get nodes --output json
cv4pve-cli show vm pve1 100 --output jsonpretty
cv4pve-cli do start vm pve1 100 --wait
```

If you omit a required placeholder, the CLI prints an error with the expected usage:

```
Error: missing arguments: {vmid}
Usage: show vm <node> <vmid>
```

### Guest auto-resolution (`--guest`)

Aliases whose API path targets a specific guest — i.e. the path contains `/nodes/{node}/qemu/{vmid}`, `/nodes/{node}/lxc/{vmid}`, or `/nodes/{node}/{vmtype}/{vmid}` — accept an optional `--guest` (or `-g`) option. Instead of supplying the node and vmid separately, you can pass a VM/container **ID or name** and the CLI resolves node, vmid (and vmtype where needed) automatically.

```bash
# Old style — explicit positional arguments (still works)
cv4pve-cli get vm status pve1 100
cv4pve-cli show vm pve1 100
cv4pve-cli do start vm pve1 100

# New style — let the CLI find the node and vmid for you
cv4pve-cli get vm status --guest 100       # by numeric vmid
cv4pve-cli get vm status --guest myvm      # by VM name
cv4pve-cli show vm --guest myvm
cv4pve-cli do start vm --guest myvm

# Works for containers too (aliases pointing to /lxc/{vmid})
cv4pve-cli get ct status --guest myct
```

The `--guest` option is available on any alias whose command path requires node/vmid resolution. Tab completion on `--guest <TAB>` shows all VM and container IDs and names live from the cluster.

To check whether an alias supports `--guest`, run it with `--help` — if the description contains the tip below, `--guest` is available:

```
Tip: use --guest <id|name> to resolve guest info automatically
```

```bash
cv4pve-cli get guest status --help
cv4pve-cli get vm status --help
```

> When `--guest` is used, the remaining positional arguments (node, vmid, vmtype) must not be supplied — they are filled automatically from the resolved guest info.

### User aliases

You can create your own aliases using any API call as the command. Use `{name}` placeholders in the path wherever you want the caller to supply a value at runtime.

```bash
# Add an alias without placeholders
cv4pve-cli alias add cluster-res \
    --command "get /cluster/resources" \
    --description "All cluster resources"

# Add an alias with placeholders
cv4pve-cli alias add vm-net \
    --command "get /nodes/{node}/qemu/{vmid}/config" \
    --description "Show VM network config"

# Use them
cv4pve-cli cluster-res
cv4pve-cli vm-net pve1 100

# Remove a user alias (built-in aliases cannot be removed)
cv4pve-cli alias remove vm-net
```

> Built-in aliases cannot be added, modified, or removed. Attempting to create an alias with the same name as a built-in will return an error.

#### Special placeholders

Most placeholders (e.g. `{snapname}`, `{storage}`) are generic: the caller supplies their values positionally at runtime.

Three placeholders have a **special meaning** and enable guest auto-resolution via `--guest`:

| Placeholder | Filled with | Example value |
|-------------|-------------|---------------|
| `{node}` | The cluster node where the guest runs | `pve1` |
| `{vmid}` | The numeric ID of the guest | `100` |
| `{vmtype}` | The guest type | `qemu` or `lxc` |

When a command path contains `/nodes/{node}/qemu/{vmid}` or `/nodes/{node}/lxc/{vmid}`, the CLI automatically offers `--guest` and resolves `{node}` and `{vmid}` from the cluster. When the path uses `/{vmtype}/{vmid}` instead of a fixed type, `{vmtype}` is also resolved automatically (either `qemu` or `lxc` depending on the guest).

```bash
# These two aliases both get --guest support automatically:
cv4pve-cli alias add vm-status \
    --command "get /nodes/{node}/qemu/{vmid}/status/current" \
    --description "VM status"

cv4pve-cli alias add guest-status \
    --command "get /nodes/{node}/{vmtype}/{vmid}/status/current" \
    --description "VM or container status"

# Usage
cv4pve-cli vm-status --guest myvm           # {node} and {vmid} resolved automatically
cv4pve-cli guest-status --guest myct        # {node}, {vmtype} and {vmid} resolved automatically
```

---

## Tab Completion

Tab completion is registered automatically on first run. It works across all commands, API paths, parameters, contexts, and alias arguments. Open a new terminal session after the first run to activate it.

### What completes

| Typing | TAB suggests |
|--------|-------------|
| `cv4pve-cli <TAB>` | `api`, `config`, `alias`, `top`, `get`, `show`, `do`, ... |
| `cv4pve-cli get <TAB>` | `vms`, `nodes`, `storages`, `cluster`, `vm`, `ct`, ... |
| `cv4pve-cli do <TAB>` | `start`, `stop`, `shutdown`, `migrate`, `clone`, ... |
| `cv4pve-cli config <TAB>` | `add`, `use`, `list`, `set`, `delete`, `rename`, `verify`, `view` |
| `cv4pve-cli config use <TAB>` | context names (e.g. `homelab`, `prod`) |
| `cv4pve-cli alias remove <TAB>` | user alias names only |
| `cv4pve-cli api get /nodes/<TAB>` | node names live from API (e.g. `pve1`, `pve2`) |
| `cv4pve-cli api get /nodes/pve1/qemu/<TAB>` | VM IDs live from API (e.g. `100`, `101`) |
| `cv4pve-cli api get /nodes/pve1/qemu/100/config <TAB>` | parameter names (e.g. `--cores`, `--memory`) |
| `cv4pve-cli show vm <TAB>` | node names live from API |
| `cv4pve-cli show vm pve1 <TAB>` | VM IDs live from API |

Completion for API paths and alias arguments queries the live Proxmox VE API, so results reflect the actual state of your cluster.

### Setup

Tab completion shims are written to `~/.cv4pve/cli/` (`completion.ps1` on Windows, `completion.bash` / `completion.zsh` on Linux/macOS) and sourced automatically into your shell profile on first run.

**After the first run, open a new terminal session** to activate completion. Alternatively, reload your profile in the current session:

**PowerShell**
```powershell
. $PROFILE
```

**Bash**
```bash
source ~/.bashrc
```

**Zsh**
```bash
source ~/.zshrc
```

To force re-registration (e.g. after reinstalling or upgrading), run the following and then open a new terminal:

```bash
cv4pve-cli completion reset
```

---

## API Metadata Cache

The Proxmox VE API schema is downloaded once from the connected host and cached locally under `~/.cv4pve/cli/cache/`. The cache is keyed by PVE version, so it is refreshed automatically when the host is upgraded.

---

## Links

- [Proxmox VE API reference](https://pve.proxmox.com/pve-docs/api-viewer/index.html)
- [Proxmox VE API token documentation](https://pve.proxmox.com/pve-docs/pveum-plain.html)

---

## Support

Professional support available through [Corsinvest](https://www.corsinvest.it/cv4pve).

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
