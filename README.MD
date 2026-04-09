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
[![WinGet](https://img.shields.io/winget/v/Corsinvest.cv4pve.cli?style=flat-square&logo=windows)](https://winstall.app/apps/Corsinvest.cv4pve.cli)
[![AUR](https://img.shields.io/aur/version/cv4pve-cli?style=flat-square&logo=archlinux)](https://aur.archlinux.org/packages/cv4pve-cli)

> cv4pve-cli is to Proxmox VE what `kubectl` is to Kubernetes — a remote-first CLI with context switching, direct API access, and aliases for common operations. Think of it as [`pvesh`](https://pve.proxmox.com/pve-docs/pvesh.1.html), but running from your workstation, supporting multiple clusters, and with tab completion that queries the live API.

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

| Platform | Command |
|----------|---------|
| **Windows** | `winget install Corsinvest.cv4pve.cli` |
| **Arch Linux** | `yay -S cv4pve-cli` |
| **Debian/Ubuntu** | `sudo dpkg -i cv4pve-cli-VERSION-ARCH.deb` |
| **RHEL/Fedora** | `sudo rpm -i cv4pve-cli-VERSION-ARCH.rpm` |
| **macOS** | `brew tap Corsinvest/homebrew-tap && brew install cv4pve-cli` |
| **Linux / manual** | Download zip from [Releases](https://github.com/Corsinvest/cv4pve-cli/releases/latest), `chmod +x cv4pve-cli`, `sudo mv cv4pve-cli /usr/local/bin/` |

---

## Features

- **Self-contained binary** — no runtime to install, copy and run
- **Cross-platform** — Windows, Linux, macOS
- **Multiple clusters** — save and switch between connection profiles (contexts)
- **Direct API access** — full Proxmox VE REST API via `api get/set/create/delete`
- **[300+ built-in aliases](#built-in-aliases)** — shortcuts for common operations (start, stop, snapshot, migrate, …)
- **Guest auto-resolution** — use `--guest <name|id>` instead of typing node, vmtype and vmid
- **Tab completion** — bash, zsh and PowerShell, queries the live API
- **API schema cached locally** — refreshed automatically on PVE upgrade

| | [pvesh](https://pve.proxmox.com/pve-docs/pvesh.1.html) | kubectl | cv4pve-cli |
|---|---|---|---|
| Direct API access | yes | yes | yes |
| Runs remotely (off-node) | no | yes | yes |
| Multiple clusters / contexts | no | yes | yes |
| Tab completion (live) | yes (local only) | yes | yes |
| Aliases / shortcuts | no | yes | yes |

---

## Configuration

All configuration is managed through the `config` subcommand. Contexts are connection profiles — each stores a host, credentials, and port.

### File locations

| Platform | Base path |
|----------|-----------|
| Linux / macOS | `~/.cv4pve/cli/` |
| Windows | `%USERPROFILE%\.cv4pve\cli\` |

Files are in YAML format and can be edited directly.

| File | Content |
|------|---------|
| `config` | Contexts (connection profiles) |
| `alias` | User-defined aliases |
| `cache/<version>.json` | API schema cache (auto-generated) |

<details>
<summary><strong>Context commands</strong></summary>

```bash
# Add
cv4pve-cli config add homelab --host 192.168.1.100 --username root@pam --password secret
cv4pve-cli config add prod --host pve.company.com --api-token root@pam!mytoken=uuid-here
cv4pve-cli config add dev --host 10.0.0.1 --port 8007 --username root@pam --password secret --validate-certificate false
cv4pve-cli config add slow --host 10.0.0.1 --username root@pam --password secret --timeout 60

# Switch / inspect
cv4pve-cli config use prod
cv4pve-cli config current
cv4pve-cli config list                  # * = active
cv4pve-cli config view                  # full dump (passwords hidden)

# Update (only specified fields change)
cv4pve-cli config set homelab --password newpass
cv4pve-cli config set homelab --host 192.168.1.200

# Rename / delete / verify
cv4pve-cli config rename homelab lab
cv4pve-cli config delete lab
cv4pve-cli config verify
cv4pve-cli config verify homelab
```

</details>

---

## API Commands

```
cv4pve-cli api get    <resource> [--key value ...]
cv4pve-cli api set    <resource> [--key value ...]
cv4pve-cli api create <resource> [--key value ...]
cv4pve-cli api delete <resource>
cv4pve-cli api ls     <resource>
cv4pve-cli api usage  <resource> [method] [--returns] [--output <format>]
```

```bash
cv4pve-cli api get /cluster/resources --type vm
cv4pve-cli api set /nodes/pve1/qemu/100/config --memory 4096 --cores 2
cv4pve-cli api create /nodes/pve1/qemu/100/snapshot --snapname before-update
cv4pve-cli api delete /nodes/pve1/qemu/100/snapshot/before-update
```

### Output formats

`--output` / `-o`: `text` (default), `json`, `jsonpretty`, `html`, `markdown`

```bash
cv4pve-cli api get /nodes --output json
cv4pve-cli api usage /nodes/{node}/qemu/{vmid}/config get --returns --output json
```

---

## Aliases

Aliases are shortcuts for API commands. Placeholders like `{node}`, `{vmid}` are filled positionally at runtime.

### Built-in aliases

300+ read-only aliases are available out of the box — see the [full list](src/Corsinvest.ProxmoxVE.Cli/Resources/builtin-aliases.yaml) or run `cv4pve-cli alias list`. A few examples:

| Alias | Arguments | Description |
|-------|-----------|-------------|
| `top` | — | Cluster resource overview |
| `get vms` | — | List all VMs (cluster-wide) |
| `show vm` | `<node> <vmid>` | Show VM config |
| `do start vm` | `<node> <vmid>` | Start a VM |
| `create vm snapshot` | `<node> <vmid> <snapname> <descr>` | Create a VM snapshot |
| `get cluster not-backed-up` | — | List guests with no backup job |

Aliases prefixed with `guest` work for both VMs and containers — `{vmtype}` is resolved automatically:

| Alias | Arguments | Description |
|-------|-----------|-------------|
| `get guests` | — | List all VMs and containers |
| `show guest` | `<node> <vmtype> <vmid>` | Show guest config |
| `do start guest` | `<node> <vmtype> <vmid>` | Start a guest |
| `do stop guest` | `<node> <vmtype> <vmid>` | Stop a guest |
| `create guest snapshot` | `<node> <vmtype> <vmid> <snapname> <descr>` | Create a snapshot |
| `do rollback guest` | `<node> <vmtype> <vmid> <snapname>` | Rollback to a snapshot |
| `delete guest` | `<node> <vmtype> <vmid>` | Destroy a guest (permanent!) |

The same operation is available at three levels — `guest` (both VM and CT), `vm` (qemu only), `ct` (lxc only):

```bash
cv4pve-cli create guest snapshot --guest myvm  snap1 "before update"  # VM or CT — by name
cv4pve-cli create guest snapshot --guest 100   snap1 "before update"  # VM or CT — by id
cv4pve-cli create vm snapshot pve1 100 snap1 "before update"           # VM only
cv4pve-cli create ct snapshot pve1 200 snap1 "before update"           # CT only
```

```bash
cv4pve-cli alias list
cv4pve-cli alias list --verbose        # includes command and placeholder args
cv4pve-cli alias list --search snap    # filter by keyword
```

### Guest auto-resolution (`--guest`)

Aliases whose path targets a specific guest accept `--guest <id|name>`. The CLI resolves node, vmid and vmtype automatically from the cluster.

```bash
# Without --guest (explicit)
cv4pve-cli do start vm pve1 100

# With --guest (auto-resolve)
cv4pve-cli do start vm --guest 100
cv4pve-cli do start vm --guest myvm
cv4pve-cli get ct status --guest myct
cv4pve-cli create guest snapshot --guest myvm snap1 "before update"
```

Tab completion on `--guest <TAB>` shows all VM and container names and IDs live from the cluster.

To check if an alias supports `--guest`, run it with `--help` — the description will contain:
```
Tip: use --guest <id|name> to resolve guest info automatically
```

### User aliases

```bash
# Add
cv4pve-cli alias add vm-net \
    --command "get /nodes/{node}/qemu/{vmid}/config" \
    --description "Show VM network config"

# Use
cv4pve-cli vm-net pve1 100

# Remove
cv4pve-cli alias remove vm-net
```

Built-in aliases cannot be modified or removed.

<details>
<summary><strong>Special placeholders</strong></summary>

Most placeholders (e.g. `{snapname}`, `{storage}`) are filled positionally at runtime.

Three have special meaning and enable `--guest` auto-resolution:

| Placeholder | Filled with | Example |
|-------------|-------------|---------|
| `{node}` | Node where the guest runs | `pve1` |
| `{vmid}` | Numeric guest ID | `100` |
| `{vmtype}` | Guest type | `qemu` or `lxc` |

```bash
cv4pve-cli alias add guest-status \
    --command "get /nodes/{node}/{vmtype}/{vmid}/status/current" \
    --description "VM or container status"

cv4pve-cli guest-status --guest myct
```

</details>

---

## Tab Completion

Tab completion is registered automatically on first run. Open a new terminal to activate it, or reload your profile:

| Shell | Command |
|-------|---------|
| PowerShell | `. $PROFILE` |
| Bash | `source ~/.bashrc` |
| Zsh | `source ~/.zshrc` |

To force re-registration: `cv4pve-cli completion reset`

### What completes

| Typing | TAB suggests |
|--------|-------------|
| `cv4pve-cli <TAB>` | `api`, `config`, `alias`, `top`, `get`, `do`, ... |
| `cv4pve-cli api get /nodes/<TAB>` | node names live from API |
| `cv4pve-cli api get /nodes/pve1/qemu/<TAB>` | VM IDs live from API |
| `cv4pve-cli api get /nodes/pve1/qemu/100/config <TAB>` | parameter names |
| `cv4pve-cli show vm pve1 <TAB>` | VM IDs live from API |
| `cv4pve-cli config use <TAB>` | context names |

---

## Support

Professional support available through [Corsinvest](https://www.corsinvest.it/cv4pve).

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
