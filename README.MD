# cv4pve-cli

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-cli.svg)](LICENSE.md) [![AppVeyor branch](https://img.shields.io/appveyor/ci/franklupo/cv4pve-cli/master.svg)](https://ci.appveyor.com/project/franklupo/cv4pve-cli)

ProxmoxVE Client API .Net

[ProxmoxVE Api](https://pve.proxmox.com/pve-docs/api-viewer/)

```text
    ______                _                      __
   / ____/___  __________(_)___ _   _____  _____/ /_
  / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
 / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
 \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

 Corsinvest for Proxmox VE Cli       (Made in Italy)

Usage: cv4pve-cli [options] [command]

Options:
  -?|-h|--help      Show help information
  --version         Show version information
  --host            The host name host[:port],host1[:port],host2[:port]
  --username        User name <username>@<realm>
  --password        The password. Specify 'file:path_file' to store password in file.

Commands:
  app-check-update  Check update application
  app-upgrade       Upgrade application
  create            Create (POST) from resource
  delete            Delete (DELETE) from resource
  get               Get (GET) from resource
  ls                List child objects on <api_path>.
  set               Set (PUT) from resource
  sh                Interactive shell
  usage             Usage resource

Run 'cv4pve-cli [command] --help' for more information about a command.

cv4pve-cli is a part of suite cv4pve-tools.
For more information visit https://www.cv4pve-tools.com
```

## Copyright and License

Copyright: Corsinvest Srl
For licensing details please visit [LICENSE.md](LICENSE.md)

## Commercial Support

This software is part of a suite of tools called cv4pve-tools. If you want commercial support, visit the [site](https://www.cv4pve-tools.com)

## Introduction

Command line for Proxmox VE.

this software aims to simplify the management of Proxmox VE single node or cluster using command line. The reasons are:

* do not use graphical interface (GUI)
* access to simplified operations
* ability to automate processes through APIs

## Main features

* Completely written in C#
* Use native api REST Proxmox VE (library C#)
* Independent os (Windows, Linux, Macosx)
* Installation unzip file extract binary
* Not require installation in Proxmox VE
* Execute out side Proxmox VE
* API
  * get/set/create/delete command
  * usage command with complete documentation
  * ls like directory filesystem
  * multiple format return text,json,json pretty,png
* Alias command
* Documentation generated at runtime from server
* Interactive shell
* Support multiple host for HA in --host parameter es. host[:port],host1[:port],host2[:port]
* Check-Update and Upgrade application

## Configuration and use

E.g. install on linux 64

Download last package e.g. Debian cv4pve-cli-linux-x64.zip, on your os and install:

```sh
root@debian:~# unzip cv4pve-cli-linux-x64.zip
```

This tool need basically no configuration.

## API

APIs are used to manage data directly.

```text
Commands:
  create        Create (POST) from resource
  delete        Delete (DELETE) from resource
  get           Get (GET) from resource
  ls            List child objects on <api_path>.
  set           Set (PUT) from resource
  usage         Usage resource
```

For any commands

```text
Arguments:
  resource      Resource api request
  parameters    Parameter for resource format key:value (Multiple)

Options:
  -v|--verbose  Show verbose output
  -o|--output   Type output (default: text) Text,Json,JsonPretty,Png
  --wait        Wait for task finish  
  -?|-h|--help  Show help information
```

### Get / Set / Create / Delete

Retry version node

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano get /version
```

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

Retry version json

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano get--output json
```

```text
{"repoid":"aee6f0ec","release":"13","version":"5.4","keyboard":"it"}
```

Retry version json pretty

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano get --output jsonpretty
```

```text
{
  "repoid": "aee6f0ec",
  "release": "13",
  "version": "5.4",
  "keyboard": "it"
}
```

Get status nodes

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano get /nodes
```

```text
┌──────┬────────┬───────┬───────┬────────┬───────────┬───────────┬─────────────────────────────────────────────────────────────────────────────────────────────────┬─────────────────┐
│ node │ status │ cpu   │ level │ maxcpu │ maxmem    │ mem       │ ssl_fingerprint                                                                                 │ uptime          │
├──────┼────────┼───────┼───────┼────────┼───────────┼───────────┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────┤
│ pve1 │ online │ 4,58% │ c     │ 8      │ 31,39 GiB │ 17,03 GiB │ 81:94:9C:7A:20:38:44:E3:5A:6C:96:E4:44:F2:FC:2E:61:14:CF:E1:13:61:D3:29:DB:18:DF:83:20:76:B9:04 │ 12d 19h 12m 55s │
├──────┼────────┼───────┼───────┼────────┼───────────┼───────────┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────┤
│ pve2 │ online │ 4,92% │ c     │ 8      │ 31,39 GiB │ 16,77 GiB │ 70:41:5B:47:21:F4:5C:4D:4F:FD:D8:53:3E:AD:73:44:5E:F6:A2:75:F1:42:94:07:81:94:E2:D8:1C:0D:FF:0E │ 13d 23h 34m 25s │
├──────┼────────┼───────┼───────┼────────┼───────────┼───────────┼─────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────────┤
│ pve3 │ online │ 0,88% │       │ 4      │ 11,72 GiB │ 7,46 GiB  │ 4A:9B:2D:2B:3B:05:D1:9B:03:36:1F:71:44:40:CE:19:55:AE:36:69:35:F9:BB:26:8E:9F:01:F9:A9:D4:00:D7 │ 19d 3h 33m 08s  │
└──────┴────────┴───────┴───────┴────────┴───────────┴───────────┴─────────────────────────────────────────────────────────────────────────────────────────────────┴─────────────────┘
```

For request parameter specify in format key:value

Get resource in cluster only node

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano get /cluster/resources type:node
```

```text
┌───────────┬──────┬───────┬───────────┬─────────┬───────┬────────┬────────────┬───────────┬───────────┬──────┬──────┬────────┬─────────┬─────────────────┐
│ id        │ type │ cpu   │ disk      │ hastate │ level │ maxcpu │ maxdisk    │ maxmem    │ mem       │ node │ pool │ status │ storage │ uptime          │
├───────────┼──────┼───────┼───────────┼─────────┼───────┼────────┼────────────┼───────────┼───────────┼──────┼──────┼────────┼─────────┼─────────────────┤
│ node/pve1 │ node │ 6,47% │ 14,65 GiB │         │ c     │ 8      │ 27,19 GiB  │ 31,39 GiB │ 17,38 GiB │ pve1 │      │ online │         │ 12d 19h 22m 45s │
├───────────┼──────┼───────┼───────────┼─────────┼───────┼────────┼────────────┼───────────┼───────────┼──────┼──────┼────────┼─────────┼─────────────────┤
│ node/pve2 │ node │ 4,38% │ 12,45 GiB │         │ c     │ 8      │ 27,19 GiB  │ 31,39 GiB │ 16,75 GiB │ pve2 │      │ online │         │ 13d 23h 44m 05s │
├───────────┼──────┼───────┼───────────┼─────────┼───────┼────────┼────────────┼───────────┼───────────┼──────┼──────┼────────┼─────────┼─────────────────┤
│ node/pve3 │ node │ 0,93% │ 10,08 GiB │         │       │ 4      │ 397,55 GiB │ 11,72 GiB │ 7,46 GiB  │ pve3 │      │ online │         │ 19d 3h 42m 48s  │
└───────────┴──────┴───────┴───────────┴─────────┴───────┴────────┴────────────┴───────────┴───────────┴──────┴──────┴────────┴─────────┴─────────────────┘

```

For know documentation of command use usage

### Usage

Usage create documentation at runtime from API.

```text
Arguments:
  resource      Resource api request

Options:
  -v|--verbose  Show verbose output
  -c|--command  API command Get,Set,Create,Delete
  -r|--returns  Including schema for returned data.
  -?|-h|--help  Show help information
```

Get usage command

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano usage /cluster/resources
```

```text
USAGE: get /cluster/resources [OPTIONS]
```

If you want know options use -v|--verbose

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano usage /cluster/resources -v
```

```text
USAGE: get /cluster/resources [OPTIONS]

  Resources index (cluster wide).
┌───────┬─────────────────┬─────────────┐
│ param │ type            │ description │
├───────┼─────────────────┼─────────────┤
│ type  │ vm,storage,node │             │
└───────┴─────────────────┴─────────────┘
```

If you want know return use -r|--returns

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano usage /cluster/resources -v -r
```

```text
USAGE: get /cluster/resources [OPTIONS]

  Resources index (cluster wide).
┌───────┬─────────────────┬─────────────┐
│ param │ type            │ description │
├───────┼─────────────────┼─────────────┤
│ type  │ vm,storage,node │             │
└───────┴─────────────────┴─────────────┘
RETURNS:
┌─────────┬────────────────────────┬─────────────────────────────────────────────────────────────┐
│ param   │ type                   │ description                                                 │
├─────────┼────────────────────────┼─────────────────────────────────────────────────────────────┤
│ cpu     │ number                 │ CPU utilization (when type in node,qemu,lxc).               │
│ disk    │ string                 │ Used disk space in bytes (when type in storage),            │
│         │                        │ used root image spave for VMs (type in qemu,lxc).           │
│ hastate │ string                 │ HA service status (for HA managed VMs).                     │
│ id      │ string                 │                                                             │
│ level   │ string                 │ Support level (when type == node).                          │
│ maxcpu  │ number                 │ Number of available CPUs (when type in node,qemu,lxc).      │
│ maxdisk │ integer                │ Storage size in bytes (when type in storage),               │
│         │                        │ root image size for VMs (type in qemu,lxc).                 │
│ maxmem  │ integer                │ Number of available memory in bytes (when type              │
│         │                        │ in node,qemu,lxc).                                          │
│ mem     │ string                 │ Used memory in bytes (when type in node,qemu,lxc).          │
│ node    │ string                 │ The cluster node name (when type in node,storage,qemu,lxc). │
│ pool    │ string                 │ The pool name (when type in pool,qemu,lxc).                 │
│ status  │ string                 │ Resource type dependent status.                             │
│ storage │ string                 │ The storage identifier (when type == storage).              │
│ type    │ node,storage,pool,qemu │ Resource type.                                              │
│         │ lxc,openvz             │                                                             │
│ uptime  │ integer                │ Node uptime in seconds (when type in node,qemu,lxc).        │
└─────────┴────────────────────────┴─────────────────────────────────────────────────────────────┘
```

If "usage" shows more commands you can specify a single command to display using -c | --command

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano usage /nodes/pve1/status
```

```text
USAGE: get /nodes/pve1/status
USAGE: create /nodes/pve1/status command:<string>
```

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano usage /nodes/pve1/status -c create
```

```text
USAGE: create /nodes/pve1/status command:<string>
```

### Ls

Ls command list filesystem modal.

List root

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano ls /
```

```text
Dr---        access
Dr---        cluster
Dr---        nodes
Dr--c        pools
Dr--c        storage
-r---        version
```

List nodes

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano ls /nodes
```

```text
Dr---        pve1
Dr---        pve2
Dr---        pve3
```

List **Qemu** on **pve1** node

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano ls /nodes/pve1/qemu
```

```text
Dr---        100
Dr---        101
Dr---        102
Dr---        118
Dr---        134
Dr---        135
Dr---        205
```

## Sh

The interactive shell allows executing API commands in continuous mode, and with completion of the commands.

```text
Options:
  -s|--script       Script file name
  -r|--only-result  Only result
  -?|-h|--help      Show help information
```

```sh
root@debian:~# cv4pve-cli --host=192.168.0.100 --username=root@pam --password=fagiano sh
```

```text
Corsinvest Interactive Shell for Proxmox VE (martedì 27 agosto 2019)
Type '<TAB>' for completion word
Type 'help', 'quit' to close the application.
Initialization metadata... 111ms
>>>
```

Help command:

```txt
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

For specific help command using -h|--help.

```txt
>>> usage -h

Corsinvest Interactive Shell API for Proxmox VE

Usage resource

Usage:  usage [options] <resource>

Arguments:
  resource      Resource api request

Options:
  -v|--verbose  Show verbose output
  -c|--command  API command Get,Set,Create,Delete
  -r|--returns  Including schema for returned data.
  -?|-h|--help  Show help information
  ```

### Alias

Alias command il like alias shell.

```txt
Options:
  -c|--create   Create new
  -r|--remove   Delete
  -v|--verbose  Show verbose output
  -?|-h|--help  Show help information
```

```txt
>>> alias
┌──────────────────────────────┬───────────────────────────────────────────────────────┬─────┐
│ name                         │ description                                           │ sys │
├──────────────────────────────┼───────────────────────────────────────────────────────┼─────┤
│ cluster-backup,cbck          │ Cluster list vzdump backup schedule                   │ X   │
│ cluster-backup-info,cbckinf  │ Cluster info backup schedule                          │ X   │
│ cluster-replication,crep     │ Cluster replication                                   │ X   │
│ cluster-status,csts          │ Cluster status                                        │ X   │
│ cluster-top,ct,top           │ Cluster top                                           │ X   │
│ cluster-top-node,ctn,topn    │ Cluster top for node                                  │ X   │
│ cluster-top-storage,cts,tops │ Cluster top for storage                               │ X   │
│ cluster-top-vm,ctv,topv      │ Cluster top for VM/CT                                 │ X   │
│ lcx-config,lcfg              │ LXC config vm                                         │ X   │
│ lcx-list,llst                │ LXC list vm                                           │ X   │
│ lcx-snap-config,lscfg        │ LXC snapshot vm delete                                │ X   │
│ lcx-snap-create,lscrt        │ LXC snapshot vm create                                │ X   │
│ lcx-snap-delete,lsdel        │ LXC snapshot vm delete                                │ X   │
│ lcx-snap-list,lslst          │ LXC snapshot vm list                                  │ X   │
│ lcx-start,lstr               │ LXC start vm                                          │ X   │
│ lcx-status,lsts              │ LXC current status vm                                 │ X   │
│ lcx-stop,lsto                │ LXC stop vm                                           │ X   │
│ lxc-migrate,lmig             │ LXC migrate vm other node                             │ X   │
│ lxc-shutdown,lsdwn           │ LXC shutdown vm                                       │ X   │
│ lxc-snap-rollback,lsrbck     │ LXC snapshot vm rollback                              │ X   │
│ lxc-vzdump-restore,lvrst     │ LXC restore vzdump                                    │ X   │
│ node-disks-list,ndlst        │ Node discks list                                      │ X   │
│ node-report,nrpt             │ Node report                                           │ X   │
│ node-services,nsvc           │ Node services                                         │ X   │
│ node-shutdown,nreb           │ Node reboot or shutdown                               │ X   │
│ nodes-list,nlst              │ Node services                                         │ X   │
│ node-status,nsts             │ Node status                                           │ X   │
│ node-storage,nsto            │ Node storage info                                     │ X   │
│ node-storage-content,nstoc   │ Node storage content                                  │ X   │
│ node-tasks-active,ntact      │ Node tasks active                                     │ X   │
│ node-tasks-error,nterr       │ Node tasks errors                                     │ X   │
│ node-version,nver            │ Node version                                          │ X   │
│ node-vzdump-config,nvcfg     │ Node Extract configuration from vzdump backup archive │ X   │
│ qemu-config,qcfg             │ Qemu config vm                                        │ X   │
│ qemu-exec,qexe               │ Qemu exec command vm                                  │ X   │
│ qemu-list,qlst               │ Qemu list vm                                          │ X   │
│ qemu-migrate,qmig            │ Qemu migrate vm other node                            │ X   │
│ qemu-shutdown,qsdwn          │ Qemu shutdown vm                                      │ X   │
│ qemu-snap-config,qscfg       │ Qemu snapshot vm delete                               │ X   │
│ qemu-snap-create,qscrt       │ Qemu snapshot vm create                               │ X   │
│ qemu-snap-delete,qsdel       │ Qemu snapshot vm delete                               │ X   │
│ qemu-snap-list,qslst         │ Qemu snapshot vm list                                 │ X   │
│ qemu-snap-rollback,qsrbck    │ Qemu snapshot vm rollback                             │ X   │
│ qemu-start,qstr              │ Qemu start vm                                         │ X   │
│ qemu-status,qsts             │ Qemu current status vm                                │ X   │
│ qemu-stop,qsto               │ Qemu stop vm                                          │ X   │
│ qemu-vzdump-restore,qvrst    │ Qemu restore vzdump                                   │ X   │
└──────────────────────────────┴───────────────────────────────────────────────────────┴─────┘
```

If you want show alias command using -h option in specific command or "alias -v"

```txt
>>> alias -v
┌──────────────────────────────┬───────────────────────────────────────────────────────┬─────────────────────────────────────────────────────────────────────────────────────────┬────────────────────────────────┬─────┐
│ name                         │ description                                           │ command                                                                                 │ args                           │ sys │
├──────────────────────────────┼───────────────────────────────────────────────────────┼─────────────────────────────────────────────────────────────────────────────────────────┼────────────────────────────────┼─────┤
│ cluster-backup,cbck          │ Cluster list vzdump backup schedule                   │ get /cluster/backup                                                                     │                                │ X   │
│ cluster-backup-info,cbckinf  │ Cluster info backup schedule                          │ get /cluster/backup/{backup}                                                            │ backup                         │ X   │
│ cluster-replication,crep     │ Cluster replication                                   │ get /cluster/replication                                                                │                                │ X   │
│ cluster-status,csts          │ Cluster status                                        │ get /cluster/ha/status/current                                                          │                                │ X   │
│ cluster-top,ct,top           │ Cluster top                                           │ get /cluster/resources                                                                  │                                │ X   │
│ cluster-top-node,ctn,topn    │ Cluster top for node                                  │ get /cluster/resources type:node                                                        │                                │ X   │
│ cluster-top-storage,cts,tops │ Cluster top for storage                               │ get /cluster/resources type:storage                                                     │                                │ X   │
│ cluster-top-vm,ctv           │ Cluster top for VM/CT                                 │ get /cluster/resources type:vm                                                          │                                │ X   │
│ lcx-config,lcfg              │ LXC config vm                                         │ get /nodes/{node}/lcx/{vmid}/config                                                     │ node,vmid                      │ X   │
....
```

It is possible to create alias using the alias -c command.

```txt
>>> alias -c
Create alias (using comma to more name)
Name: mytop
Description: Top Cluster
Command: get /cluster/resources
```

The alias can have multiple names using the comma.

Using parameter -s for execute command from script file.
