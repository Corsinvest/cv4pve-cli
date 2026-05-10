# Using cv4pve-cli with AI coding assistants

`cv4pve-cli` works well as a tool for AI coding assistants (Claude Code, Codex, …) that need to interact with a Proxmox VE cluster. This page collects configuration tips for the most common sandboxes, plus a minimal `SKILL.md` template you can drop into a project.

> Credit: most of the findings below come from [issue #10](https://github.com/Corsinvest/cv4pve-cli/issues/10) — thanks to [@cheriot](https://github.com/cheriot).

---

## Claude Code (macOS sandbox)

If your Proxmox host uses a **self-signed certificate**, disabling certificate validation in `cv4pve-cli` (`--validate-certificate false` on the context) is not enough on macOS: the Claude Code sandbox blocks the Mach lookup that macOS performs internally for **every** HTTPS connection, even when the app skips verification. The connection will fail with an opaque error.

Add the following to your `.claude/settings.json`:

```json
{
  "permissions": {
    "allow": [
      "WebFetch(domain:$PROXMOX_IP)"
    ]
  },
  "sandbox": {
    "network": {
      "allowMachLookup": ["com.apple.trustd*"]
    },
    "filesystem": {
      "allowWrite": ["~/.cv4pve/"]
    }
  }
}
```

What each entry does:

| Entry | Purpose |
|-------|---------|
| `WebFetch(domain:$PROXMOX_IP)` | Allow outbound calls to the Proxmox host |
| `allowMachLookup: ["com.apple.trustd*"]` | Allow macOS trust daemon lookup (required for HTTPS, even with cert validation off) |
| `allowWrite: ["~/.cv4pve/"]` | Let cv4pve-cli persist contexts, aliases and API cache |

---

## Codex

Codex needs an explicit allow rule for the binary. Add to `~/.codex/rules/default.rules`:

```
prefix_rule(pattern=["cv4pve-cli"], decision="allow")
```

Tighten the rule further if you want to restrict it to a specific context (e.g. a read-only auditor token).

---

## Suggested `SKILL.md` for a project

A short `SKILL.md` describing the tool and a couple of canonical examples is often enough to make agents productive. Minimal template:

```markdown
# cv4pve-cli

Remote-first CLI for Proxmox VE. Use it for any read or change against the
Proxmox API instead of SSH-ing into a node.

## Active context
A context named `homelab` is already configured. Do **not** add or switch contexts.

## Common commands
- `cv4pve-cli top`                          — cluster resource overview
- `cv4pve-cli get vms`                      — list all VMs
- `cv4pve-cli show vm --guest <name|id>`    — show VM config
- `cv4pve-cli api get <path>`               — raw API read
- `cv4pve-cli api usage <path> [method]`    — discover parameters of an endpoint

## Conventions
- Prefer `--guest <name|id>` over typing `<node> <vmtype> <vmid>` by hand.
- Use `--output json` when the result will be parsed.
- Destructive aliases live under `do …` / `delete …` — confirm with the user first.
```

Adapt the active-context section to whatever token / role you provisioned (e.g. an auditor token for read-only work).

---

## Tips

- **Read-only by default:** create a dedicated Proxmox API token with the `PVEAuditor` role and use it as the agent's context. The agent can still inspect everything but cannot modify state.
- **Cache lives under `~/.cv4pve/`:** make sure the sandbox can write there, otherwise the API schema is re-fetched on every call.
- **Tab completion is not useful inside an agent** — agents don't drive a TTY. Skip `completion reset` in the sandbox.
