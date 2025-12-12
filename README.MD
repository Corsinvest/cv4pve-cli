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


---

## Quick Start

```bash
# Check available releases at: https://github.com/Corsinvest/cv4pve-cli/releases
# Download specific version (replace VERSION with actual version number)
wget https://github.com/Corsinvest/cv4pve-cli/releases/download/VERSION/cv4pve-cli-linux-x64.zip
unzip cv4pve-cli-linux-x64.zip

# Make executable
chmod +x cv4pve-cli

# Run interactive shell
./cv4pve-cli --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD sh

# Execute API command
./cv4pve-cli --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD get /nodes
```

---

## Table of Contents

<details>
<summary><strong>Click to expand navigation</strong></summary>

- [Features](#-features)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Usage Examples](#-usage-examples)
- [Command Reference](#-command-reference)

</details>

---

## Features

### Core Capabilities

#### **Performance & Reliability**
- **Native C#** implementation
- **Cross-platform** (Windows, Linux, macOS)
- **API-based** operation (no root access required)
- **Cluster support** with automatic failover
- **High availability** with multiple host support

#### **Flexible API Access**
- **Full REST API** coverage (GET/POST/PUT/DELETE)
- **API documentation** generated at runtime
- **Usage command** with complete documentation
- **Multiple output** formats (text, json, table)
- **ls command** like filesystem navigation

#### **Advanced Features**
- **Interactive shell** mode for exploration
- **Script file** execution support
- **API token** support (Proxmox VE 6.2+)
- **SSL validation** options
- **Parameter files** for complex setups
- **Alias commands** for frequent operations

#### **Enterprise Features**
- **Multiple host** support for HA
- **Timeout management** for operations
- **Error resilience** and detailed logging
- **Comprehensive** command documentation
- **Automated scripting** capabilities

---

## Installation

<div align="center">
  <img src="https://img.shields.io/badge/INSTALLATION-GUIDE-green?style=for-the-badge&logo=download" alt="Installation Guide">
</div>

### Linux Installation

```bash
# Check available releases and get the specific version number
# Visit: https://github.com/Corsinvest/cv4pve-cli/releases

# Download specific version (replace VERSION with actual version like v1.9.0)
wget https://github.com/Corsinvest/cv4pve-cli/releases/download/VERSION/cv4pve-cli-linux-x64.zip

# Alternative: Get latest release URL programmatically
LATEST_URL=$(curl -s https://api.github.com/repos/Corsinvest/cv4pve-cli/releases/latest | grep browser_download_url | grep linux-x64 | cut -d '"' -f 4)
wget "$LATEST_URL"

# Extract and make executable
unzip cv4pve-cli-linux-x64.zip
chmod +x cv4pve-cli

# Optional: Move to system path
sudo mv cv4pve-cli /usr/local/bin/
```

### Windows Installation

```powershell
# Check available releases at: https://github.com/Corsinvest/cv4pve-cli/releases
# Download specific version (replace VERSION with actual version)
Invoke-WebRequest -Uri "https://github.com/Corsinvest/cv4pve-cli/releases/download/VERSION/cv4pve-cli.exe-win-x64.zip" -OutFile "cv4pve-cli.zip"

# Extract
Expand-Archive cv4pve-cli.zip -DestinationPath "C:\Tools\cv4pve-cli"

# Add to PATH (optional)
$env:PATH += ";C:\Tools\cv4pve-cli"
```

### macOS Installation

```bash
# Check available releases at: https://github.com/Corsinvest/cv4pve-cli/releases
# Download specific version (replace VERSION with actual version)
wget https://github.com/Corsinvest/cv4pve-cli/releases/download/VERSION/cv4pve-cli-osx-x64.zip
unzip cv4pve-cli-osx-x64.zip
chmod +x cv4pve-cli

# Move to applications
sudo mv cv4pve-cli /usr/local/bin/
```

---

## Configuration

<div align="center">
  <img src="https://img.shields.io/badge/CONFIGURATION-SETUP-blue?style=for-the-badge&logo=settings" alt="Configuration Setup">
</div>

### Authentication Methods

<table>
<tr>
<td width="50%">

#### **Username/Password**
```bash
cv4pve-cli --host=192.168.1.100 --username=root@pam --password=your_password [commands]
```

#### **API Token (Recommended)**
```bash
cv4pve-cli --host=192.168.1.100 --api-token=root@pam!token1=uuid-here [commands]
```

</td>
<td width="50%">

#### **Password from File**
```bash
# Store password in file
echo "your_password" > /etc/cv4pve/password
chmod 600 /etc/cv4pve/password

# Use password file
cv4pve-cli --host=192.168.1.100 --username=root@pam --password=file:/etc/cv4pve/password [commands]
```

</td>
</tr>
</table>

### Connection Options

| Parameter | Description | Example |
|-----------|-------------|---------|
| `--host` | Proxmox host(s) (multiple for HA) | `--host=pve1:8006,pve2:8006` |
| `--username` | Username@realm | `--username=root@pam` |
| `--password` | Password or file | `--password=secret` or `--password=file:/path` |
| `--api-token` | API token | `--api-token=user@realm!token=uuid` |
| `--validate-certificate` | Validate SSL certificate | `--validate-certificate` |

---

## Usage Examples

<div align="center">
  <img src="https://img.shields.io/badge/USAGE-EXAMPLES-orange?style=for-the-badge&logo=terminal" alt="Usage Examples">
</div>

### Interactive Shell

<details>
<summary><strong>Interactive Mode</strong></summary>

#### Start Interactive Shell
```bash
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret sh
```

**Output:**
```text
Corsinvest CLI for Proxmox VE (Tuesday, December 11, 2025)
Type '<TAB>' for completion word
Type 'help', 'quit' to close the application.
Initialization metadata... 111ms

>>>
```

#### Help Command
```text
>>> help

Commands:
  alias         Alias commands
  clear         Clear screen
  create        Create (POST) from resource
  delete        Delete (DELETE) from resource
  get           Get (GET) from resource
  help          Show help information
  history       Show history command
  ls            List child objects on <api_path>.
  quit          Close application
  set           Set (PUT) from resource
  usage         Usage resource
```

#### Interactive Commands
```bash
# Inside shell, explore the API
>>> get /nodes
>>> get /nodes/pve1/qemu
>>> get /cluster/resources type:vm
>>> ls /nodes/pve1
>>> usage /cluster/resources
>>> quit
```

#### Script File Execution
```bash
# Create script file
cat > commands.txt <<EOF
get /nodes
get /cluster/resources
ls /nodes/pve1
EOF

# Execute script
cv4pve-cli --host=pve.local --username=root@pam --password=secret sh --script=commands.txt
```

#### Alias Feature
The shell supports custom aliases for frequently used commands:

```text
>>> alias

System aliases available:
  cluster-backup, cbck           - Cluster list vzdump backup schedule
  cluster-status, csts           - Cluster status
  cluster-top, ct, top           - Cluster top
  qemu-list, qlst                - Qemu list vm
  qemu-status, qsts              - Qemu current status vm
  qemu-start, qstr               - Qemu start vm
  qemu-stop, qsto                - Qemu stop vm
  ... and many more
```

</details>

### API Operations

<details>
<summary><strong>GET - Read Data</strong></summary>

#### Get Version
```bash
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret get /version
```

**Output:**
```text
┌─────────┬──────────┐
│ key     │ value    │
├─────────┼──────────┤
│ release │ 13       │
├─────────┼──────────┤
│ repoid  │ aee6f0ec │
├─────────┼──────────┤
│ version │ 5.4      │
└─────────┴──────────┘
```

#### Get Nodes Status
```bash
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret get /nodes
```

**Output:**
```text
┌──────┬────────┬───────┬───────┬────────┬───────────┬───────────┬──────────────────────────┬─────────────────┐
│ node │ status │ cpu   │ level │ maxcpu │ maxmem    │ mem       │ ssl_fingerprint          │ uptime          │
├──────┼────────┼───────┼───────┼────────┼───────────┼───────────┼──────────────────────────┼─────────────────┤
│ pve1 │ online │ 4,58% │ c     │ 8      │ 31,39 GiB │ 17,03 GiB │ 81:94:9C:7A:20:38:44...  │ 12d 19h 12m 55s │
│ pve2 │ online │ 4,92% │ c     │ 8      │ 31,39 GiB │ 16,77 GiB │ 70:41:5B:47:21:F4:5C...  │ 13d 23h 34m 25s │
└──────┴────────┴───────┴───────┴────────┴───────────┴───────────┴──────────────────────────┴─────────────────┘
```

#### Get Cluster Resources (with filter)
```bash
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret get /cluster/resources type:node
```

**Output:**
```text
┌───────────┬──────┬───────┬───────────┬────────┬────────────┬───────────┬───────────┬──────┬────────┐
│ id        │ type │ cpu   │ disk      │ maxcpu │ maxdisk    │ maxmem    │ mem       │ node │ status │
├───────────┼──────┼───────┼───────────┼────────┼────────────┼───────────┼───────────┼──────┼────────┤
│ node/pve1 │ node │ 6,47% │ 14,65 GiB │ 8      │ 27,19 GiB  │ 31,39 GiB │ 17,38 GiB │ pve1 │ online │
│ node/pve2 │ node │ 4,38% │ 12,45 GiB │ 8      │ 27,19 GiB  │ 31,39 GiB │ 16,75 GiB │ pve2 │ online │
└───────────┴──────┴───────┴───────────┴────────┴────────────┴───────────┴───────────┴──────┴────────┘
```

#### JSON Output
```bash
# Standard JSON
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret get /version --output=json
```

**Output:**
```json
{"repoid":"aee6f0ec","release":"13","version":"5.4","keyboard":"it"}
```

#### Pretty JSON Output
```bash
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret get /version --output=jsonpretty
```

**Output:**
```json
{
  "repoid": "aee6f0ec",
  "release": "13",
  "version": "5.4",
  "keyboard": "it"
}
```

</details>

<details>
<summary><strong>SET - Update Configuration</strong></summary>

```bash
# Update VM memory
cv4pve-cli --host=pve.local --username=root@pam --password=secret set /nodes/pve1/qemu/100/config memory:4096

# Update VM description
cv4pve-cli --host=pve.local --username=root@pam --password=secret set /nodes/pve1/qemu/100/config description:"Updated by cv4pve-cli"

# Update multiple parameters
cv4pve-cli --host=pve.local --username=root@pam --password=secret set /nodes/pve1/qemu/100/config memory:4096 cores:2
```

</details>

<details>
<summary><strong>CREATE - Create Resources</strong></summary>

```bash
# Create snapshot
cv4pve-cli --host=pve.local --username=root@pam --password=secret create /nodes/pve1/qemu/100/snapshot snapname:backup-$(date +%Y%m%d)

# Create user
cv4pve-cli --host=pve.local --username=root@pam --password=secret create /access/users userid:testuser@pve password:secret

# Start VM
cv4pve-cli --host=pve.local --username=root@pam --password=secret create /nodes/pve1/qemu/100/status/start
```

</details>

<details>
<summary><strong>DELETE - Remove Resources</strong></summary>

```bash
# Delete snapshot
cv4pve-cli --host=pve.local --username=root@pam --password=secret delete /nodes/pve1/qemu/100/snapshot/backup-20250211

# Stop VM
cv4pve-cli --host=pve.local --username=root@pam --password=secret create /nodes/pve1/qemu/100/status/stop
```

</details>

### Navigation & Documentation

<details>
<summary><strong>LS - Browse API Like Filesystem</strong></summary>

#### List Root
```bash
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret ls /
```

**Output:**
```text
Dr---        access
Dr---        cluster
Dr---        nodes
Dr--c        pools
Dr--c        storage
-r---        version
```

#### List Nodes
```bash
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret ls /nodes
```

**Output:**
```text
Dr---        pve1
Dr---        pve2
Dr---        pve3
```

#### List VMs on Node
```bash
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret ls /nodes/pve1/qemu
```

**Output:**
```text
Dr---        100
Dr---        101
Dr---        102
Dr---        118
```

</details>

<details>
<summary><strong>USAGE - API Documentation</strong></summary>

#### Basic Usage Command
```bash
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret usage /cluster/resources
```

**Output:**
```text
USAGE: get /cluster/resources [OPTIONS]
```

#### Verbose Usage (with parameters)
```bash
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret usage /cluster/resources --verbose
```

**Output:**
```text
USAGE: get /cluster/resources [OPTIONS]

  Resources index (cluster wide).
┌───────┬─────────────────┬─────────────┐
│ param │ type            │ description │
├───────┼─────────────────┼─────────────┤
│ type  │ vm,storage,node │             │
└───────┴─────────────────┴─────────────┘
```

#### Usage with Returns Schema
```bash
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret usage /cluster/resources --verbose --returns
```

**Output:**
```text
USAGE: get /cluster/resources [OPTIONS]

  Resources index (cluster wide).
┌───────┬─────────────────┬─────────────┐
│ param │ type            │ description │
├───────┼─────────────────┼─────────────┤
│ type  │ vm,storage,node │             │
└───────┴─────────────────┴─────────────┘
RETURNS:
┌─────────┬────────────────────────┬──────────────────────────────────────────┐
│ param   │ type                   │ description                              │
├─────────┼────────────────────────┼──────────────────────────────────────────┤
│ cpu     │ number                 │ CPU utilization                          │
│ disk    │ string                 │ Used disk space in bytes                 │
│ maxcpu  │ number                 │ Number of available CPUs                 │
│ maxmem  │ integer                │ Number of available memory in bytes      │
│ mem     │ string                 │ Used memory in bytes                     │
│ node    │ string                 │ The cluster node name                    │
│ status  │ string                 │ Resource type dependent status           │
│ type    │ node,storage,pool,qemu │ Resource type                            │
└─────────┴────────────────────────┴──────────────────────────────────────────┘
```

#### Specific Command Usage
```bash
# Show all available commands for a resource
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret usage /nodes/pve1/status
```

**Output:**
```text
USAGE: get /nodes/pve1/status
USAGE: create /nodes/pve1/status command:<string>
```

```bash
# Show specific command documentation
cv4pve-cli --host=192.168.0.100 --username=root@pam --password=secret usage /nodes/pve1/status --command=create
```

**Output:**
```text
USAGE: create /nodes/pve1/status command:<string>
```

</details>

### Advanced Usage

<details>
<summary><strong>Parameter Files</strong></summary>

#### Create Parameter File
```bash
# /etc/cv4pve/production.conf
--host=pve-cluster.company.com
--api-token=automation@pve!production=uuid-here
--validate-certificate
```

#### Execute with Parameter File
```bash
cv4pve-cli @/etc/cv4pve/production.conf get /cluster/resources
```

</details>

<details>
<summary><strong>Output Formats</strong></summary>

```bash
# Text output (default)
cv4pve-cli --host=pve.local --username=root@pam --password=secret get /nodes

# JSON output
cv4pve-cli --host=pve.local --username=root@pam --password=secret get /nodes --output=json

# Pretty JSON
cv4pve-cli --host=pve.local --username=root@pam --password=secret get /nodes --output=jsonpretty

# Table output (for supported commands)
cv4pve-cli --host=pve.local --username=root@pam --password=secret get /cluster/resources --output=table
```

</details>

---

## Command Reference

<div align="center">
  <img src="https://img.shields.io/badge/COMMAND-REFERENCE-navy?style=for-the-badge&logo=terminal" alt="Command Reference">
</div>

### Global Options

```bash
cv4pve-cli [global-options] [command] [parameters]
```

| Parameter | Description | Example |
|-----------|-------------|---------|
| `--host` | Host name (host[:port]) | `--host=pve.local:8006` |
| `--username` | Username@realm | `--username=root@pam` |
| `--password` | Password or file | `--password=secret` |
| `--api-token` | API token | `--api-token=user@realm!token=uuid` |
| `--validate-certificate` | Validate SSL certificate | `--validate-certificate` |
| `--version` | Show version | `--version` |
| `-?, -h, --help` | Show help | `--help` |

### Commands

#### API Commands

| Command | Description | Syntax |
|---------|-------------|--------|
| `get` | GET from resource | `get <resource> [parameters]` |
| `set` | PUT to resource | `set <resource> [parameters]` |
| `create` | POST to resource | `create <resource> [parameters]` |
| `delete` | DELETE resource | `delete <resource> [parameters]` |

#### Utility Commands

| Command | Description | Syntax |
|---------|-------------|--------|
| `usage` | Show API usage | `usage <resource> [options]` |
| `ls` | List child objects | `ls <resource>` |
| `sh` | Interactive shell | `sh [--script=file] [--only-result]` |

### Command Options

#### API Command Options
```bash
--output <format>     Output format: text, json, jsonpretty, table
--wait                Wait for task to finish
--verbose             Enable verbose output
```

#### Usage Command Options
```bash
--command, -c <type>  API command type: get, set, create, delete
--returns, -r         Include schema for returned data
--output <format>     Output format
--verbose             Enable verbose output
```

#### Shell Command Options
```bash
--script, -s <file>   Script file to execute
--only-result, -r     Only show result (no prompts)
```

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/cv4pve).

---


Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl