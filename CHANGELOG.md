# Changelog

---

## [2.2.1] — 2026-04-09

### Bug fixes

- Parameters like `--limit 100` were ignored — now passed correctly to the API.
- Alias arguments with spaces (e.g. `"before update"`) were cut at the first word — now handled correctly.
- The CLI now authenticates once per session instead of logging in on every command.
- Commands that return a list of text lines (e.g. `get node journal`) now display correctly instead of crashing.

---

## [2.2.0] — 2026-03-30

### Aliases

- Expanded built-in aliases to **316 total** — now covering firewall, SDN, metrics, node scan, apt, replication, QEMU agent and more.
- Added `get/set firewall options` for guest, vm, ct, node and cluster.
- Added `do migrateall node` and `do suspendall node`.
- Added `get node scan nfs/cifs/pbs/iscsi/zfs/lvm` to discover storage targets from a node.
- Added `get vm agent hostname/timezone/vcpus/time/memory` and `do vm agent ping/fstrim`.
- Added `get/show/create/set/delete cluster metrics-server` for InfluxDB/Graphite targets.
- Added `get/show cluster sdn zones/vnets/controllers`.

---

## [2.1.0] — 2026-03-17

### Guest auto-resolution

- New `--guest <name|id>` option on all aliases that target a specific VM or container. Instead of typing node, vmtype and vmid separately, you can just pass the VM name or ID and the CLI finds it automatically.

```bash
cv4pve-cli do start vm --guest myvm
cv4pve-cli create guest snapshot --guest 100 snap1 "before update"
```

- New `guest` aliases that work for both VMs and containers (start, stop, reboot, shutdown, migrate, snapshot, rollback, firewall, config, tags, resize, delete, …).

---

## [2.0.0] — 2026-03-13

- Complete rewrite with a new command structure: `api`, `config`, `alias`, `completion`.
- Context management: save multiple cluster connections and switch between them with `config use`.
- Built-in and user-defined aliases with `{placeholder}` support.
- Tab completion for bash, zsh and PowerShell — queries the live API.
- API schema cached locally, refreshed automatically on PVE upgrade.
