/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 *
 * Reads the built-in alias catalog shipped inside cv4pve-cli
 * (src/Corsinvest.ProxmoxVE.Cli/Resources/builtin-aliases.yaml) at build time, so the
 * documentation lists exactly the aliases of the code it is built with.
 *
 * The file has a fixed shape (`- name:`, `description:`, `command:`, optional `confirm: true`)
 * so a line reader is enough and the site needs no YAML dependency.
 */
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

export interface Alias {
  name: string;
  description: string;
  /** API command, e.g. `get /nodes/{node}/qemu/{vmid}/config`. */
  command: string;
  /** Needs `--yes` to run. */
  confirm: boolean;
  /** Placeholders filled positionally, in order (same rule as ApiExplorerHelper.GetArgumentTags). */
  args: string[];
  /** Accepts `--guest <id|name>` (same rule as GuestResolutionEngine.Detect). */
  guest: boolean;
}

// The site is built from docs/, the catalog lives in the .NET project next to it.
const CATALOG = resolve(process.cwd(), '../src/Corsinvest.ProxmoxVE.Cli/Resources/builtin-aliases.yaml');

const unquote = (value: string) => value.trim().replace(/^"(.*)"$/, '$1').replace(/^'(.*)'$/, '$1');

function guestResolution(command: string): boolean {
  const path = command.split(' ').filter(Boolean)[1];
  if (!path) return false;
  const segs = path.toLowerCase().split('/').filter(Boolean);
  for (let i = 0; i < segs.length - 2; i++) {
    if (segs[i] !== 'nodes' || segs[i + 1] !== '{node}') continue;
    if (i + 3 >= segs.length || segs[i + 3] !== '{vmid}') return false;
    return ['qemu', 'lxc', '{vmtype}'].includes(segs[i + 2]);
  }
  return false;
}

let cache: Alias[] | undefined;

export function loadAliases(): Alias[] {
  if (cache) return cache;
  const aliases: Alias[] = [];
  let current: Partial<Alias> | undefined;
  for (const line of readFileSync(CATALOG, 'utf8').split(/\r?\n/)) {
    const m = line.match(/^\s*(-\s+)?(name|description|command|confirm):\s*(.*)$/);
    if (!m) continue;
    const [, dash, key, raw] = m;
    if (dash) {
      current = { confirm: false };
      aliases.push(current as Alias);
    }
    if (!current) continue;
    const value = unquote(raw);
    if (key === 'confirm') current.confirm = value === 'true';
    else current[key as 'name' | 'description' | 'command'] = value;
  }
  for (const a of aliases) {
    a.args = [...a.command.matchAll(/{\s*(.+?)\s*}/g)].map((x) => x[1]);
    a.guest = guestResolution(a.command);
  }
  cache = aliases.sort((a, b) => a.name.localeCompare(b.name));
  return cache;
}
