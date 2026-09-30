# Changelog

---

## [Unreleased]

### Fixes
- **No key of the answer is lost**: `api get …/config`, `show vm` and `-o json` now list numbered keys such as `net0`, `scsi0`, `ide2`, `usb0`. Lists show the columns the API schema describes, as pvesh does, and name the others on standard error; the new `--all-columns` (`-A`) shows them all, and `-o json` always has every column.
- **Sizes, percentages, durations and dates are readable again** (`251.51 GiB`, `4.44%`, `3d 2h 5m 1s`, `2026-09-30 14:22:23`), as in pvesh: the cached API schema had lost the information. `--human-readable false` (or `0`) prints the values as the API returns them; `-o json` always does. The cache in `~/.cv4pve/cli/cache` is rebuilt once.
- **Tab completion after `--guest`** proposes the parameters and their values at once: the value of `--guest` (or of `-o`, or of a `--key`) was read as the node, and the completion waited 30 seconds for the answer of a node that does not exist.
- **Logs go to standard error**: with `--debug` or `--log-level`, and for a call that fails before an answer, the log no longer mixes with the output (`-o json`, completions).
- **Tables are aligned and escaped**: numbers aligned right in text, Markdown and Html output; `|` in Markdown and `<`, `&` in Html no longer break the table; a value on more lines stays inside its cell.
- **`--wait` waits until the task ends**, prints its exit status and exits with 5 when it fails; `--wait-timeout <seconds>` sets a limit. It used to stop after 30 seconds and ignore the result.
- **`--yes` is enforced.** Aliases that change the cluster the most (delete, stop, reboot, rollback, restore…) ran without `--yes`, and failed with it because `--yes` was sent to Proxmox VE as a parameter. They now stop with exit code 6 without `--yes`, and `--yes` is no longer sent.
- **`--dry-run` works**: `api set/create/delete/get` and aliases print the method, the path and the parameters, and send nothing. Aliases that need `--yes` do not need it with `--dry-run`.
- **API errors exit with a non-zero code** and print on stderr: 6 for a rejected parameter (HTTP 400), 2 for a missing privilege (401/403), 3 for a path or object that does not exist, 4 for other server errors. They used to exit with 0 on stdout.
- **API errors say what went wrong**: the call that was sent, what the alias runs, whether the refused parameter is written in the alias itself (a bug to report) or by the user, and, for a rejected parameter, the `api usage` command that lists the accepted ones.
- `api ls` and `api usage` on a path the API does not have print `no such resource` on stderr with exit code 3, instead of exit 0.
- **Parameters are read in the order written.** A `--key` without a value in the middle no longer shifts the values of the keys after it, `--key=value` is accepted, a repeated parameter or a value without `--key` is an error (exit 6).
- An alias argument equal to the value of a parameter (`get vm status pve01 100 --timeout 100`) is no longer lost.
- `--debug`, `--log-level` and `--dry-run` before an alias no longer change how it runs (`--guest` was not resolved).
- `--guest` with a guest that does not exist prints `Error: Guest '…' not found.` (exit 3) instead of crashing; connection errors are reported as they are instead of "no context configured".
- An alias called without all its arguments prints the error on stderr with exit code 6.
- `--host` accepts a port per node (`pve1:8007`) and IPv6 addresses without brackets; `--port` applies to every node written without a port, not only the last one.
- `config view` no longer shows part of the API token secret.
- The "no context" error suggested `config add-context`, which does not exist: now `config add`.
- A path turned into a Windows path by Git Bash (`C:/Program Files/Git/version`) is refused with a hint about `MSYS_NO_PATHCONV=1`, instead of a stack trace.
- Tab completion uses the API schema of the newest Proxmox VE version in the cache (the file names sorted `8.4.9` after `8.4.21`).
- `get vms` description: it lists VMs and containers, like `get guests`.

### Aliases
- Fixed aliases that always failed: `get node packages` (wrong path), `get node rrddata` (now takes the timeframe: `get node rrddata <node> <hour|day|week|month|year>`), `create security token` (token ID is part of the path), `set node hosts` (POST, not PUT), `create cluster replication job` (`--type local` is fixed, no longer an argument).
- Removed aliases that never worked and have no API call to point to: `get vm tags`, `get ct tags`, `get guest tags`, `get ct network`, `get cts`, and `create/set/delete cluster replication` (use `create/set/delete cluster replication job`).
- `delete vm/ct/guest unused-disk` (deletes the disk), `create vm/ct/guest template` (cannot be undone) and `do stop node task` now need `--yes`.
- New aliases:
  - performance history: `get vm/ct/guest rrddata`, `get node storage rrddata` (timeframe as argument);
  - `get node versions` (like `pveversion -v`), `show node service`, `do reload node service`, `do renew node certificate`;
  - `get vm migrate-check` (can the VM migrate, and where), `get vm/ct/guest firewall log`;
  - `get cluster corosync nodes`, `get cluster qdevice`, `get cluster firewall groups`;
  - cluster firewall IPSets and aliases: `get/show/create/delete cluster firewall ipset(s)`, `create/delete cluster firewall ipset-entry`, `get/create/delete cluster firewall alias(es)`;
  - `do apply cluster sdn` (needs `--yes`), `do vm agent fsfreeze` (needs `--yes`), `do vm agent fsthaw`, `do vm agent fsfreeze-status`.
- Aliases whose API call needs a parameter they do not fill say so in their description (firewall rules: `--action` and `--type`; metrics server, hardware mappings, PBS scan).

### AI coding assistants
- New skill [`skills/cv4pve-cli/SKILL.md`](skills/cv4pve-cli/SKILL.md) for Claude Code, Codex and other assistants: when to use cv4pve-cli, how to read with JSON, find guests, explore the API, run changes with `--dry-run` first and `--yes` only after the user agrees.

### Tests
- New test project `Corsinvest.ProxmoxVE.Cli.Tests`: parameter parsing, alias rewriting, host list, exit codes.
- Every built-in alias is checked against the API of the latest Proxmox VE release (schema of the official API viewer, downloaded by the test): path, method and parameters.
- `test-completion.ps1` reads nodes and guests from the cluster instead of holding their names.

### Documentation
- No em or en dashes in the site, README, CHANGELOG and messages (`config verify` prints `Connected to …, PVE version …`); CHANGELOG headings as `## [x.y.z] - date`; docs theme 2.6.1
- Documentation site at https://corsinvest.github.io/cv4pve-cli/, built from `docs/` and published by the shared cv4pve workflow: contexts, permissions, API calls, aliases, tasks, tab completion, scripting, AI coding assistants, and a reference of every command, alias and file. The alias reference is generated from the built-in catalog at build time.
- `docs/aliases.md`, `docs/commands.md` and `docs/AI-AGENTS.md` moved to the site; README shortened to point to it

### Changed
- Uses Corsinvest.ProxmoxVE.Api.Console 9.2.4: API calls, parameters and aliases go through its new `Shell` classes (`ApiRequest`, `ApiCommandLine`, `ApiSchema`); `api usage`, `api ls` and tab completion no longer use `ApiExplorerHelper`. `api usage` lists the methods in the order get, set, create, delete
- Product icon (Lucide `square-terminal`) and Windows executable icon
- Project metadata, symbols (Source Link, `.snupkg`) and code style aligned with the other cv4pve tools

## [2.3.0] - 2026-07-06

### Commands

- Added a `task` command tree to inspect and follow async Proxmox tasks by UPID: `task list`, `task show`, `task wait`, `task log` (with `--follow` to tail live, polling every 2s) and `task stop`.
- Every alias now honors `--wait` to block until the async Proxmox task finishes (previously the flag was ignored on aliases: start/backup/migrate were fire-and-forget).

### Aliases

- Added `do restore vm` and `do restore ct` to restore a guest from a vzdump backup archive.
- Added Ceph read aliases: `get ceph status/flags` and `get node ceph status/osds/mons/mgrs/pools/fs/config`.

### Scripting

- Commands now return **semantic exit codes** (`0` ok, `1` generic, `2` auth/config, `3` not-found, `4` api/server, `5` task-failed, `6` validation) and print errors to **stderr** instead of stdout. Previously failures printed to stdout and still returned `0`.

### Bug fixes

- Fixed a startup crash (`ArgumentNullException`) that made every command fail: the `--log-level` option was not registered.

### Documentation

- Added `docs/commands.md` (core command reference) and `docs/aliases.md` (full alias list, regenerable via `alias list --output markdown`), linked from the README.
- Added tips for running `cv4pve-cli` inside AI coding assistants (`docs/AI-AGENTS.md`).

---

## [2.2.1] - 2026-04-09

### Bug fixes

- Parameters like `--limit 100` were ignored; now passed correctly to the API.
- Alias arguments with spaces (e.g. `"before update"`) were cut at the first word; now handled correctly.
- The CLI now authenticates once per session instead of logging in on every command.
- Commands that return a list of text lines (e.g. `get node journal`) now display correctly instead of crashing.

---

## [2.2.0] - 2026-03-30

### Aliases

- Expanded built-in aliases to **316 total**, now covering firewall, SDN, metrics, node scan, apt, replication, QEMU agent and more.
- Added `get/set firewall options` for guest, vm, ct, node and cluster.
- Added `do migrateall node` and `do suspendall node`.
- Added `get node scan nfs/cifs/pbs/iscsi/zfs/lvm` to discover storage targets from a node.
- Added `get vm agent hostname/timezone/vcpus/time/memory` and `do vm agent ping/fstrim`.
- Added `get/show/create/set/delete cluster metrics-server` for InfluxDB/Graphite targets.
- Added `get/show cluster sdn zones/vnets/controllers`.

---

## [2.1.0] - 2026-03-17

### Guest auto-resolution

- New `--guest <name|id>` option on all aliases that target a specific VM or container. Instead of typing node, vmtype and vmid separately, you can just pass the VM name or ID and the CLI finds it automatically.

```bash
cv4pve-cli do start vm --guest myvm
cv4pve-cli create guest snapshot --guest 100 snap1 "before update"
```

- New `guest` aliases that work for both VMs and containers (start, stop, reboot, shutdown, migrate, snapshot, rollback, firewall, config, tags, resize, delete, …).

---

## [2.0.0] - 2026-03-13

- Complete rewrite with a new command structure: `api`, `config`, `alias`, `completion`.
- Context management: save multiple cluster connections and switch between them with `config use`.
- Built-in and user-defined aliases with `{placeholder}` support.
- Tab completion for bash, zsh and PowerShell; queries the live API.
- API schema cached locally, refreshed automatically on PVE upgrade.
