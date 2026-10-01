---
name: cv4pve-cli
description: "Read and operate a Proxmox VE cluster through its API with cv4pve-cli: list nodes, VMs, containers and storage, read configurations, and run changes such as start, stop, snapshot or migrate. Use it for any question or task about the Proxmox VE cluster instead of SSH to a node."
---

# cv4pve-cli

`cv4pve-cli` calls the Proxmox VE API from the command line. A context (host and API token) is already
configured and current: every command goes to that cluster with that token's privileges.

## Rules

- Do not add, change, switch or delete contexts (`cv4pve-cli config …`), unless the user asks.
- Read with `-o json` and parse the JSON.
- Before any command that changes the cluster (`do`, `create`, `set`, `delete` aliases, `api set/create/delete`):
  run it first with `--dry-run`, show the user the call it prints, and run it for real only after the user agrees.
- Some aliases (delete, stop, reboot, rollback, restore, …) refuse to run without `--yes`. Add `--yes` only
  after the user has agreed to that exact change.
- An exit code other than 0 means the command failed; the reason is on standard error.
- On Windows in Git Bash, prefix commands with `MSYS_NO_PATHCONV=1`, or API paths such as `/version` are
  rewritten into Windows paths.

## Find guests

`--guest <id|name>` finds the node and the type of a guest by VM ID or name. `vm` aliases look only among
VMs, `ct` aliases only among containers, `guest` aliases among both: when you do not know whether an ID is a
VM or a container, use the `guest` aliases.

```bash
cv4pve-cli top -o json                                  # every node, VM, container, storage, pool
cv4pve-cli get guests -o json                           # VMs and containers
cv4pve-cli show guest --guest <id|name> -o json         # configuration of a VM or container
cv4pve-cli get guest status --guest <id|name> -o json   # running or stopped, CPU, memory, uptime
```

## Explore the API

```bash
cv4pve-cli alias list --search <word>                   # aliases whose name, description or call contains <word>
cv4pve-cli api ls <path>                                # what is under a path, e.g. /nodes/<node>
cv4pve-cli api usage <path> [get|set|create|delete] -v  # parameters a call accepts
cv4pve-cli api get <path> -o json                       # any read
```

Parameters are written `--key value` or `--key=value`, after the arguments of an alias.

## Change the cluster

```bash
cv4pve-cli do start vm --guest <id|name> --dry-run                         # 1. show the call
cv4pve-cli do start vm --guest <id|name> --wait                            # 2. run it after the user agrees
cv4pve-cli create vm snapshot --guest <id|name> <snapname> "<description>" --wait
cv4pve-cli delete vm snapshot --guest <id|name> <snapname> --yes --wait
```

`--wait` waits for the task the call starts and prints its exit status: exit code 0 if it succeeded, 5 if it
failed. Without `--wait` the command prints the task ID (UPID) and returns at once; follow it with
`cv4pve-cli task log <upid> --follow`.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 2 | Authentication or permission: the token lacks a privilege |
| 3 | Not found: path, guest, node |
| 4 | Node unreachable or server error |
| 5 | Task failed, or `--wait` timed out |
| 6 | Wrong input: parameter refused, argument missing, `--yes` missing |

Documentation: https://corsinvest.github.io/cv4pve-cli/
