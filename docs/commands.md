# Command reference

Core commands of `cv4pve-cli`. For the 320+ operation shortcuts, see [aliases.md](aliases.md).

The command tree is: **config**, **completion**, **api**, plus all the built-in and user **aliases** (registered as first-class subcommands). Run any command with `--help` for flags and examples.

---

## Global options

Available on every command:

| Option | Description |
|--------|-------------|
| `--output` / `-o` | Output format: `text` (default), `json`, `jsonpretty`, `html`, `markdown` |
| `--dry-run` | Print what would be sent without executing |
| `--debug` | Verbose debug logging (hidden) |
| `--log-level <level>` | Log level: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical` (hidden) |

---

## Exit codes

Commands return a semantic exit code so scripts and CI can react to the failure kind. Error messages go to **stderr**; normal output stays on **stdout**.

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | Generic / unclassified error |
| `2` | Authentication or configuration error |
| `3` | Resource not found |
| `4` | API / server error (unreachable, HTTP 5xx) |
| `5` | Async task failed |
| `6` | Input validation error |

```bash
cv4pve-cli config verify prod
case $? in
  0) echo "ok" ;;
  2) echo "bad credentials" ;;
  3) echo "context not found" ;;
  4) echo "server unreachable" ;;
esac
```

---

## config — connection profiles (contexts)

A context stores host, credentials and port. All configuration lives under `~/.cv4pve/cli/` (`%USERPROFILE%\.cv4pve\cli\` on Windows) in editable YAML.

```
cv4pve-cli config add <name> --host <h> [--username u@realm --password p | --api-token 'u@realm!id=uuid']
                             [--port 8006] [--validate-certificate true|false] [--timeout 30]
cv4pve-cli config use <name>            # switch active context
cv4pve-cli config current               # print active context name
cv4pve-cli config list                  # list contexts (* = active)
cv4pve-cli config view                  # full dump (password masked, token truncated)
cv4pve-cli config set <name> [--host ... | --password ... | ...]   # update only given fields
cv4pve-cli config rename <old> <new>
cv4pve-cli config delete <name>
cv4pve-cli config verify [<name>]       # test connectivity/auth
```

**Auth:** token wins over username/password. Token format `USER@REALM!TOKENID=UUID`.

---

## api — direct Proxmox VE REST API

Full API access. Placeholders in the path are filled from the API schema; parameters passed as `--key value`.

```
cv4pve-cli api get    <resource> [--key value ...]
cv4pve-cli api set    <resource> [--key value ...]
cv4pve-cli api create <resource> [--key value ...]
cv4pve-cli api delete <resource>
cv4pve-cli api ls     <resource>
cv4pve-cli api usage  <resource> [method] [--returns] [--output <format>]
```

Examples:

```bash
cv4pve-cli api get /cluster/resources --type vm
cv4pve-cli api set /nodes/pve1/qemu/100/config --memory 4096 --cores 2
cv4pve-cli api create /nodes/pve1/qemu/100/snapshot --snapname before-update --wait
cv4pve-cli api delete /nodes/pve1/qemu/100/snapshot/before-update
cv4pve-cli api usage /nodes/{node}/qemu/{vmid}/config get --returns --output json
```

`--wait` waits for the async task (UPID) to finish before returning.

---

## alias — shortcuts for API commands

Aliases turn a full API path into a short command. Placeholders like `{node}`, `{vmid}` are filled positionally at runtime. 320+ read-oriented aliases ship built in — see [aliases.md](aliases.md).

```
cv4pve-cli alias list [--verbose] [--search <kw>] [--output <format>]
cv4pve-cli alias add <name> --command "<api command>" --description "<desc>"
cv4pve-cli alias remove <name>
```

```bash
cv4pve-cli alias add vm-net \
    --command "get /nodes/{node}/qemu/{vmid}/config" \
    --description "Show VM network config"
cv4pve-cli vm-net pve1 100
```

Built-in aliases cannot be modified or removed. User aliases live in `~/.cv4pve/cli/alias`.

### Waiting for async tasks (`--wait`)

Operations that start an async Proxmox task (start, stop, snapshot, backup, migrate, clone, restore, …) return a task id (UPID) immediately. Add **`--wait`** to any alias to block until the task finishes and reflect its result in the exit code:

```bash
cv4pve-cli do start vm --guest 100 --wait        # returns only when the VM has started
cv4pve-cli do backup guest --guest 100 --storage local --wait
```

Without `--wait` the command is fire-and-forget (prints the UPID and returns). The same flag is available on `api create/set/delete`.

### Guest auto-resolution (`--guest`)

Aliases whose path targets a specific guest accept `--guest <id|name>` and resolve `{node}`, `{vmid}` and `{vmtype}` automatically from `/cluster/resources`.

```bash
cv4pve-cli do start vm --guest 100         # by id
cv4pve-cli do start guest --guest myvm     # by name, VM or CT
cv4pve-cli create guest snapshot --guest myct snap1 "before update"
```

The three special placeholders:

| Placeholder | Filled with |
|-------------|-------------|
| `{node}` | Node where the guest runs |
| `{vmid}` | Numeric guest ID |
| `{vmtype}` | `qemu` or `lxc` |

---

## completion — shell tab completion

Registered automatically on first run for bash, zsh and PowerShell; queries the live API for node names, VM IDs, parameter names.

```
cv4pve-cli completion reset      # force re-registration
```

Reload the profile to activate (`. $PROFILE` / `source ~/.bashrc` / `source ~/.zshrc`).
