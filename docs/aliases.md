# Alias reference

Built-in aliases are shortcuts for [`api`](commands.md#api--direct-proxmox-ve-rest-api) commands. Each maps a short verb phrase (e.g. `do start vm`) to a full API path with `{placeholders}` filled at runtime.

- Aliases starting with **`guest`** work for both VMs and containers (`{vmtype}` auto-resolved); `vm` = qemu only, `ct` = lxc only.
- Aliases targeting a specific guest accept **`--guest <id|name>`** to resolve `{node}`/`{vmid}`/`{vmtype}` automatically — see [Guest auto-resolution](commands.md#guest-auto-resolution---guest).
- Destructive aliases require **`--yes`** to confirm.

> This page is generated from the built-in alias catalog. To regenerate:
> ```bash
> cv4pve-cli alias list --output markdown > docs/aliases.md
> ```
> (then re-add this header). For the full command + placeholder args of each alias, use `cv4pve-cli alias list --verbose` or `cv4pve-cli <alias> --help`.

---

## All aliases

| name                               | description                                                                           |
|------------------------------------|---------------------------------------------------------------------------------------|
| create cluster backup-job          | Create a new scheduled backup job                                                     |
| create cluster firewall-rule       | Add a new firewall rule to the cluster                                                |
| create cluster ha group            | Create a new HA group                                                                 |
| create cluster ha resource         | Add a VM or CT to HA management                                                       |
| create cluster mapping pci         | Create a new PCI hardware mapping                                                     |
| create cluster mapping usb         | Create a new USB hardware mapping                                                     |
| create cluster metrics-server      | Add a new metric server target (InfluxDB/Graphite)                                    |
| create cluster replication         | Create a new ZFS replication job                                                      |
| create cluster replication job     | Create a new replication job (cluster-level)                                          |
| create ct firewall-rule            | Add a new firewall rule to a container                                                |
| create ct snapshot                 | Create a new snapshot for a container                                                 |
| create ct template                 | Convert the container into a template                                                 |
| create guest firewall-rule         | Add a new firewall rule to a guest (VM or CT)                                         |
| create guest snapshot              | Create a new snapshot for a guest (VM or CT)                                          |
| create guest template              | Convert a guest into a template (VM or CT)                                            |
| create node firewall-rule          | Add a new firewall rule to a node                                                     |
| create pool                        | Create a new resource pool                                                            |
| create security domain             | Create a new authentication domain (LDAP, AD, OpenID, …)                              |
| create security group              | Create a new user group                                                               |
| create security role               | Create a new custom role with specific privileges                                     |
| create security tfa                | Add a TFA entry (TOTP, WebAuthn, recovery keys) for a user                            |
| create security token              | Generate a new API token for a user                                                   |
| create security user               | Create a new user account                                                             |
| create storage                     | Add a new storage definition to the cluster                                           |
| create vm firewall-rule            | Add a new firewall rule to a VM                                                       |
| create vm snapshot                 | Create a new snapshot for a VM                                                        |
| create vm template                 | Convert the VM into a read-only template                                              |
| delete cluster backup-job          | Remove a backup job schedule                                                          |
| delete cluster firewall-rule       | Remove a cluster firewall rule by position                                            |
| delete cluster ha group            | Delete a HA group configuration                                                       |
| delete cluster ha resource         | Remove a resource from HA management                                                  |
| delete cluster mapping pci         | Remove a PCI hardware mapping                                                         |
| delete cluster mapping usb         | Remove a USB hardware mapping                                                         |
| delete cluster metrics-server      | Remove a metric server configuration                                                  |
| delete cluster replication         | Remove a replication job                                                              |
| delete cluster replication job     | Remove a cluster replication job                                                      |
| delete ct                          | Destroy a container (Permanent!)                                                      |
| delete ct firewall-rule            | Remove a firewall rule from a container                                               |
| delete ct snapshot                 | Delete a specific container snapshot                                                  |
| delete ct tags                     | Remove all tags from a container                                                      |
| delete ct unused-disk              | Remove an unattached disk from a CT                                                   |
| delete guest                       | Destroy a guest VM or CT (Permanent!)                                                 |
| delete guest backup                | Permanently delete a backup file from storage                                         |
| delete guest firewall-rule         | Remove a firewall rule from a guest (VM or CT)                                        |
| delete guest snapshot              | Delete a specific guest snapshot                                                      |
| delete guest tags                  | Remove all tags from a guest (VM or CT)                                               |
| delete guest unused-disk           | Remove an unattached disk from a guest (VM or CT)                                     |
| delete node firewall-rule          | Remove a firewall rule from a node                                                    |
| delete pool                        | Remove a resource pool                                                                |
| delete security domain             | Remove an authentication domain                                                       |
| delete security group              | Remove a user group                                                                   |
| delete security role               | Remove a custom role                                                                  |
| delete security tfa                | Remove a TFA entry for a user                                                         |
| delete security token              | Remove an API token                                                                   |
| delete security user               | Remove a user account                                                                 |
| delete storage config              | Remove a storage definition from the cluster                                          |
| delete vm                          | Destroy a VM (Permanent!)                                                             |
| delete vm firewall-rule            | Remove a firewall rule from a VM                                                      |
| delete vm snapshot                 | Delete a specific VM snapshot                                                         |
| delete vm tags                     | Remove all tags from a VM                                                             |
| delete vm unused-disk              | Remove an unattached disk from a VM                                                   |
| do backup guest                    | Start a backup job immediately for a VM or CT                                         |
| do clone ct                        | Clone a container                                                                     |
| do clone vm                        | Clone a VM                                                                            |
| do download storage                | Download an ISO, template or image from a URL to a storage                            |
| do exec vm agent                   | Execute a command inside the VM via QEMU guest agent                                  |
| do migrate cluster ha resource     | Request online migration of a HA resource to another node                             |
| do migrate ct                      | Migrate a container to another node                                                   |
| do migrate guest                   | Migrate a guest to another node (VM or CT)                                            |
| do migrate vm                      | Live migrate a VM to another node                                                     |
| do migrateall node                 | Migrate all guests from a node to other nodes                                         |
| do monitor vm                      | Execute a QEMU monitor (HMP) command on a VM                                          |
| do move ct disk                    | Move a CT volume to a different storage                                               |
| do move vm disk                    | Move a VM disk to a different storage                                                 |
| do prune node backups              | Prune old backups on a storage according to retention settings                        |
| do reboot ct                       | Reboot a container                                                                    |
| do reboot guest                    | Reboot a guest (VM or CT)                                                             |
| do reboot node                     | Reboot a node                                                                         |
| do reboot vm                       | Reboot a VM                                                                           |
| do relocate cluster ha resource    | Relocate a HA resource to another node (stop and restart)                             |
| do reset vm                        | Reset a VM                                                                            |
| do resize ct disk                  | Expand a CT disk                                                                      |
| do resize guest disk               | Expand a guest disk (VM or CT)                                                        |
| do resize vm disk                  | Expand a VM disk                                                                      |
| do restart node service            | Restart a service on a node                                                           |
| do restore ct                      | Restore a container from a vzdump backup archive (creates/overwrites the target vmid) |
| do restore vm                      | Restore a VM from a vzdump backup archive (creates/overwrites the target vmid)        |
| do resume ct                       | Resume a suspended container                                                          |
| do resume guest                    | Resume a suspended guest (VM or CT)                                                   |
| do resume vm                       | Resume a suspended VM                                                                 |
| do rollback ct                     | Rollback container state to a specific snapshot                                       |
| do rollback guest                  | Rollback guest state to a specific snapshot (VM or CT)                                |
| do rollback vm                     | Rollback VM state to a specific snapshot                                              |
| do shutdown ct                     | Shutdown a container                                                                  |
| do shutdown guest                  | Shutdown a guest (VM or CT)                                                           |
| do shutdown node                   | Shutdown a node                                                                       |
| do shutdown vm                     | Shutdown a VM                                                                         |
| do start ct                        | Start a container                                                                     |
| do start guest                     | Start a guest (VM or CT)                                                              |
| do start node service              | Start a service on a node                                                             |
| do start vm                        | Start a VM                                                                            |
| do startall node                   | Start all VMs and containers on a node (onboot=1 by default)                          |
| do stop ct                         | Stop a container                                                                      |
| do stop guest                      | Stop a guest (VM or CT)                                                               |
| do stop node service               | Stop a service on a node                                                              |
| do stop node task                  | Stop/Kill a running task                                                              |
| do stop vm                         | Stop a VM                                                                             |
| do stopall node                    | Stop all VMs and containers on a node                                                 |
| do suspend vm                      | Suspend a VM                                                                          |
| do suspendall node                 | Suspend all guests on a node                                                          |
| do sync cluster replication        | Force an immediate replication sync                                                   |
| do sync security domain            | Sync users/groups from an external authentication domain                              |
| do test cluster notification       | Send a test notification to a specific target                                         |
| do update node packages            | Trigger an apt update (refresh package cache) on a node                               |
| do vm agent fstrim                 | Run fstrim inside the guest via QEMU guest agent                                      |
| do vm agent ping                   | Ping the QEMU guest agent to verify it is running                                     |
| do wakeup node                     | Send Wake-on-LAN magic packet to a node                                               |
| get ceph flags                     | List global Ceph flags (noout, norebalance, ...)                                      |
| get ceph status                    | Cluster-wide Ceph status                                                              |
| get cluster backup volumes         | List volumes included in a specific backup job                                        |
| get cluster backup-jobs            | List all scheduled backup jobs                                                        |
| get cluster firewall               | List cluster-wide firewall rules                                                      |
| get cluster firewall options       | Show cluster firewall options                                                         |
| get cluster ha                     | Show HA manager status                                                                |
| get cluster ha groups              | List all HA groups                                                                    |
| get cluster ha manager-status      | Show full HA manager status including LRM                                             |
| get cluster ha resources           | List all HA resources                                                                 |
| get cluster log                    | Show recent cluster-wide logs                                                         |
| get cluster mappings pci           | List all PCI hardware mappings                                                        |
| get cluster mappings usb           | List all USB hardware mappings                                                        |
| get cluster metrics-servers        | List all configured metric server targets                                             |
| get cluster nextid                 | Get the next free VM/CT ID in the cluster                                             |
| get cluster not-backed-up          | List all guests not covered by any backup job                                         |
| get cluster notification-endpoints | List all configured notification endpoints                                            |
| get cluster notification-matchers  | List all notification matchers                                                        |
| get cluster notification-targets   | List all available notification targets                                               |
| get cluster options                | Show cluster-wide options                                                             |
| get cluster replications           | List all replication jobs on a node                                                   |
| get cluster replications all       | List all replication jobs (cluster-wide)                                              |
| get cluster schedule-analyze       | Show when a schedule expression will trigger next                                     |
| get cluster sdn controllers        | List all SDN controllers                                                              |
| get cluster sdn vnets              | List all SDN virtual networks                                                         |
| get cluster sdn zones              | List all SDN zones                                                                    |
| get cluster status                 | Show health and quorum status of the cluster                                          |
| get cluster tasks                  | Show all active tasks in the cluster                                                  |
| get ct firewall                    | List firewall rules for a container                                                   |
| get ct firewall options            | Show firewall options for a container                                                 |
| get ct interfaces                  | Show runtime network interfaces of a running container (via guest agent)              |
| get ct network                     | Show CT network interfaces                                                            |
| get ct pending                     | Show CT configuration including pending (unapplied) changes                           |
| get ct snapshots                   | List container snapshots                                                              |
| get ct status                      | Show container runtime status                                                         |
| get ct tags                        | Get current tags for a container                                                      |
| get ct templates                   | List CT templates on storage                                                          |
| get cts                            | List all containers (cluster-wide)                                                    |
| get guest backup config            | Extract VM/CT configuration from a vzdump backup archive                              |
| get guest backups                  | List all backups for a guest on a storage                                             |
| get guest feature                  | Check if a feature is available for a guest (VM or CT)                                |
| get guest firewall                 | List firewall rules for a guest (VM or CT)                                            |
| get guest firewall options         | Show firewall options for a guest (VM or CT)                                          |
| get guest pending                  | Show guest configuration including pending (unapplied) changes                        |
| get guest snapshots                | List snapshots for a guest (VM or CT)                                                 |
| get guest status                   | Show guest runtime status (VM or CT)                                                  |
| get guest tags                     | Get current tags for a guest (VM or CT)                                               |
| get guests                         | List all VMs and containers (cluster-wide)                                            |
| get node apt changelog             | Show changelog of a package available on a node                                       |
| get node apt repos                 | Show APT repository configuration on a node                                           |
| get node backup defaults           | Show the currently configured vzdump default settings                                 |
| get node backups                   | List backup files on a storage                                                        |
| get node ceph config               | Show the Ceph configuration file                                                      |
| get node ceph fs                   | List CephFS filesystems on a node                                                     |
| get node ceph mgrs                 | List Ceph managers on a node                                                          |
| get node ceph mons                 | List Ceph monitors on a node                                                          |
| get node ceph osds                 | List Ceph OSDs (tree) on a node                                                       |
| get node ceph pools                | List Ceph pools on a node                                                             |
| get node ceph status               | Ceph status as seen from a specific node                                              |
| get node certificates              | List certificates on a node                                                           |
| get node config                    | Show node configuration (NTP, DNS, notify, etc.)                                      |
| get node cpu-models                | List available CPU models on a node                                                   |
| get node disk lvm                  | List LVM volume groups                                                                |
| get node disk smart                | Show S.M.A.R.T. status for a disk                                                     |
| get node disk zfs                  | List ZFS pools on a node                                                              |
| get node disks                     | List physical disks on a node                                                         |
| get node dns                       | Show DNS settings for a node                                                          |
| get node firewall                  | List firewall rules for a node                                                        |
| get node firewall log              | Show firewall log for a node                                                          |
| get node firewall options          | Show firewall options for a node                                                      |
| get node hardware pci              | List local PCI devices on a node                                                      |
| get node hardware usb              | List local USB devices on a node                                                      |
| get node hosts                     | Show /etc/hosts content of a node                                                     |
| get node iso                       | List ISO images on storage                                                            |
| get node journal                   | Read the systemd journal of a node                                                    |
| get node machine-types             | List available QEMU machine types on a node                                           |
| get node netstat                   | Show network statistics for a node                                                    |
| get node network                   | List network interfaces on a node                                                     |
| get node packages                  | List available package updates on a node                                              |
| get node prune-info                | Preview which backups would be removed by a prune operation                           |
| get node replication log           | Show log of a replication job on a specific node                                      |
| get node replication status        | Show status of a replication job on a specific node                                   |
| get node report                    | Generate a full diagnostic report for a node                                          |
| get node rrddata                   | Show node performance data (RRD)                                                      |
| get node scan cifs                 | Scan a remote host for CIFS/SMB shares accessible from a node                         |
| get node scan iscsi                | Scan for iSCSI targets accessible from a node                                         |
| get node scan lvm                  | List LVM volume groups accessible from a node                                         |
| get node scan nfs                  | Scan a remote host for NFS shares accessible from a node                              |
| get node scan pbs                  | Scan a Proxmox Backup Server accessible from a node                                   |
| get node scan zfs                  | Scan for ZFS pools accessible from a node                                             |
| get node services                  | List all services on a node                                                           |
| get node storages                  | List storages available on a node                                                     |
| get node subscription              | Show subscription status of a node                                                    |
| get node syslog                    | Read the system log (syslog) of a node                                                |
| get node tasks                     | List recent tasks on node                                                             |
| get node time                      | Show current time and timezone of a node                                              |
| get node version                   | Show PVE version installed on a node                                                  |
| get nodes                          | List all nodes                                                                        |
| get pools                          | List all resource pools                                                               |
| get resources                      | List all cluster resources                                                            |
| get security acls                  | List all access control rules                                                         |
| get security domains               | List all authentication domains/realms                                                |
| get security groups                | List all user groups                                                                  |
| get security privileges            | List all available privileges in the system                                           |
| get security roles                 | List all defined roles and their privileges                                           |
| get security tfa                   | List TFA configurations for all users                                                 |
| get security tokens                | List all API tokens for a user                                                        |
| get security user tfa              | List TFA entries for a specific user                                                  |
| get security users                 | List all users and their status                                                       |
| get storages                       | List all storages (cluster-wide)                                                      |
| get storages config                | List all configured storages (cluster-wide config)                                    |
| get version                        | Show API version                                                                      |
| get vm agent exec-status           | Get the result/status of a command executed via guest agent                           |
| get vm agent fsinfo                | Show filesystem info of the guest via QEMU guest agent                                |
| get vm agent hostname              | Show hostname of the guest OS via QEMU guest agent                                    |
| get vm agent info                  | Show guest OS info via QEMU guest agent                                               |
| get vm agent memory                | Show memory blocks info of the guest via QEMU guest agent                             |
| get vm agent network               | Show guest network interfaces via QEMU guest agent                                    |
| get vm agent osinfo                | Show guest OS information via QEMU guest agent                                        |
| get vm agent time                  | Show system time of the guest via QEMU guest agent                                    |
| get vm agent timezone              | Show timezone of the guest OS via QEMU guest agent                                    |
| get vm agent users                 | List users logged into the guest via QEMU guest agent                                 |
| get vm agent vcpus                 | Show virtual CPU info of the guest via QEMU guest agent                               |
| get vm cloudinit                   | Dump the cloud-init configuration for a VM (user/network/meta)                        |
| get vm firewall                    | List firewall rules for a VM                                                          |
| get vm firewall options            | Show firewall options for a VM                                                        |
| get vm pending                     | Show VM configuration including pending (unapplied) changes                           |
| get vm snapshots                   | List VM snapshots                                                                     |
| get vm status                      | Show VM runtime status                                                                |
| get vm tags                        | Get current tags for a VM                                                             |
| get vms                            | List all VMs (cluster-wide)                                                           |
| set cluster backup-job             | Update an existing backup job                                                         |
| set cluster firewall options       | Update cluster firewall options                                                       |
| set cluster firewall-rule          | Update a specific cluster firewall rule                                               |
| set cluster ha group               | Update a HA group configuration                                                       |
| set cluster ha resource            | Update a HA resource configuration                                                    |
| set cluster mapping pci            | Update a PCI hardware mapping                                                         |
| set cluster mapping usb            | Update a USB hardware mapping                                                         |
| set cluster metrics-server         | Update a metric server configuration                                                  |
| set cluster options                | Update cluster-wide options                                                           |
| set cluster replication            | Update a replication job                                                              |
| set cluster replication job        | Update a cluster replication job                                                      |
| set ct config                      | Update CT configuration (RAM, Cores, etc.)                                            |
| set ct firewall                    | Enable/Disable container firewall                                                     |
| set ct firewall options            | Update firewall options for a container                                               |
| set ct firewall-rule               | Update a CT firewall rule                                                             |
| set ct snapshot                    | Update CT snapshot description                                                        |
| set ct tags                        | Set or overwrite tags for a container                                                 |
| set guest config                   | Update guest configuration (VM or CT)                                                 |
| set guest firewall                 | Enable/Disable guest firewall (VM or CT)                                              |
| set guest firewall options         | Update firewall options for a guest (VM or CT)                                        |
| set guest firewall-rule            | Update a guest firewall rule (VM or CT)                                               |
| set guest snapshot                 | Update guest snapshot description                                                     |
| set guest tags                     | Set or overwrite tags for a guest (VM or CT)                                          |
| set node config                    | Update node configuration options                                                     |
| set node dns                       | Set DNS servers for a node                                                            |
| set node firewall                  | Enable/Disable node firewall                                                          |
| set node firewall options          | Update firewall options for a node                                                    |
| set node firewall-rule             | Update a specific node firewall rule                                                  |
| set node hosts                     | Write /etc/hosts content of a node                                                    |
| set node time                      | Set the timezone of a node                                                            |
| set pool                           | Update pool configuration                                                             |
| set security acl                   | Update permissions for a path/user/group                                              |
| set security domain                | Update an authentication domain                                                       |
| set security group                 | Update a user group                                                                   |
| set security password              | Change the password of a user                                                         |
| set security role                  | Update privileges for an existing custom role                                         |
| set security user                  | Update user account settings                                                          |
| set storage config                 | Update an existing storage configuration                                              |
| set vm config                      | Update VM configuration (RAM, Cores, etc.)                                            |
| set vm firewall                    | Enable/Disable VM firewall                                                            |
| set vm firewall options            | Update firewall options for a VM                                                      |
| set vm firewall-rule               | Update a VM firewall rule                                                             |
| set vm snapshot                    | Update VM snapshot description                                                        |
| set vm tags                        | Set or overwrite tags for a VM                                                        |
| show cluster backup-job            | Show details of a specific backup job                                                 |
| show cluster firewall-rule         | Show details of a specific cluster firewall rule                                      |
| show cluster ha group              | Show configuration of a specific HA group                                             |
| show cluster ha resource           | Show configuration of a specific HA resource                                          |
| show cluster mapping pci           | Show a specific PCI hardware mapping                                                  |
| show cluster mapping usb           | Show a specific USB hardware mapping                                                  |
| show cluster metrics-server        | Show configuration of a specific metric server                                        |
| show cluster replication           | Show status of a specific replication job                                             |
| show cluster sdn vnet              | Show configuration of a specific SDN vnet                                             |
| show cluster sdn zone              | Show configuration of a specific SDN zone                                             |
| show ct                            | Show container config                                                                 |
| show ct firewall-rule              | Show details of a specific CT firewall rule                                           |
| show ct snapshot                   | Show configuration of a specific container snapshot                                   |
| show guest                         | Show guest config (VM or CT)                                                          |
| show guest backup                  | Show details of a specific backup file                                                |
| show guest firewall-rule           | Show details of a specific guest firewall rule                                        |
| show guest snapshot                | Show configuration of a specific guest snapshot                                       |
| show node                          | Show node status                                                                      |
| show node disk zfs                 | Show detailed status of a ZFS pool                                                    |
| show node firewall-rule            | Show details of a specific node firewall rule                                         |
| show node storage                  | Show configuration of a specific storage on a node                                    |
| show node task                     | Show log of a specific task                                                           |
| show node task status              | Show status of a specific task on a node                                              |
| show pool                          | Show details of a specific pool                                                       |
| show security domain               | Show configuration of an authentication domain                                        |
| show security group                | Show details of a group                                                               |
| show security role                 | Show privileges assigned to a specific role                                           |
| show security token                | Show details of a specific API token                                                  |
| show security user                 | Show details of a specific user                                                       |
| show storage                       | Show storage status/usage                                                             |
| show storage config                | Show configuration of a specific storage                                              |
| show vm                            | Show VM config                                                                        |
| show vm firewall-rule              | Show details of a specific VM firewall rule                                           |
| show vm snapshot                   | Show configuration of a specific VM snapshot                                          |
| top                                | Show all cluster resources (quick overview)                                           |
