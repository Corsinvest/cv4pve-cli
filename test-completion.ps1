#!/usr/bin/env pwsh
# test-completion.ps1 — verifica la completion di cv4pve-cli
# Uso: .\test-completion.ps1 [-Cli <path>]
#
# Cluster reale:
#   Nodi   : cc01, cc02
#   VM (qemu) cc01: 1000 opnsense-cc01, 1006 DomainControllerCV, 1007 DomainControllerCV2,
#                   1010 sodeos-vps, 1011 phenometa1, 1013 phenometaNG, 1020 mavert-danea2,
#                   1030 GitLab-Mecatrovision, 1104 GitLab-Corsinvest, 1106 SRV16-DieselCalcinato,
#                   1107 srvImpresa24
#   VM (qemu) cc02: 102 opnsense-cc02, 106 DanielPDCM, 1012 Mailstore, 203 testdebian,
#                   204 testdebian2, 999 vmware
#   CT (lxc)  cc01: 1014 docker-cc01.local, 105 test
#   CT (lxc)  cc02: 100 pbs01.local, 101 docker-cc02.public, 103 docker-cc02.local,
#                   104 docker-cc02.frank

param(
    [string]$Cli = "cv4pve-cli"
)

$pass = 0
$fail = 0

function Complete($line) {
    & $Cli complete -- "cv4pve-cli $line" 2>$null
}

function Test-Contains {
    param($desc, $line, [string[]]$expected)
    $results = @(Complete $line)
    $missing = $expected | Where-Object { $_ -notin $results }
    if ($missing.Count -eq 0) {
        Write-Host "  PASS  $desc" -ForegroundColor Green
        $script:pass++
    } else {
        Write-Host "  FAIL  $desc" -ForegroundColor Red
        Write-Host "        line    : cv4pve-cli $line"
        Write-Host "        missing : $($missing -join ', ')"
        Write-Host "        got     : $($results -join ', ')"
        $script:fail++
    }
}

function Test-NotContains {
    param($desc, $line, [string[]]$notExpected)
    $results = @(Complete $line)
    $found = $notExpected | Where-Object { $_ -in $results }
    if ($found.Count -eq 0) {
        Write-Host "  PASS  $desc" -ForegroundColor Green
        $script:pass++
    } else {
        Write-Host "  FAIL  $desc" -ForegroundColor Red
        Write-Host "        line             : cv4pve-cli $line"
        Write-Host "        should NOT have  : $($found -join ', ')"
        Write-Host "        got              : $($results -join ', ')"
        $script:fail++
    }
}

function Test-Exact {
    param($desc, $line, [string[]]$expected)
    $results = @(Complete $line)
    $missing = $expected | Where-Object { $_ -notin $results }
    $extra   = $results  | Where-Object { $_ -notin $expected }
    if ($missing.Count -eq 0 -and $extra.Count -eq 0) {
        Write-Host "  PASS  $desc" -ForegroundColor Green
        $script:pass++
    } else {
        Write-Host "  FAIL  $desc" -ForegroundColor Red
        Write-Host "        line    : cv4pve-cli $line"
        if ($missing.Count -gt 0) { Write-Host "        missing : $($missing -join ', ')" }
        if ($extra.Count   -gt 0) { Write-Host "        extra   : $($extra -join ', ')" }
        $script:fail++
    }
}

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Top-level commands ===" -ForegroundColor Cyan
Test-Contains "root TAB mostra verbi e alias"  ""       @("get", "show", "do", "api", "config", "alias", "top")
Test-Contains "get TAB"                        "get "   @("guests", "vms", "nodes", "vm", "ct", "guest", "cluster")
Test-Contains "do TAB"                         "do "    @("start", "stop", "reboot", "shutdown", "migrate")
Test-Contains "show TAB"                       "show "  @("vm", "ct", "guest", "node")

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== get guest status - positional ===" -ForegroundColor Cyan
Test-Contains    "slot 1 → nodi"              "get guest status "           @("cc01", "cc02")
Test-Contains    "slot 1 prefisso parziale"   "get guest status cc0"        @("cc01", "cc02")
Test-Exact       "slot 2 → solo vmtype"       "get guest status cc01 "      @("qemu", "lxc")
Test-NotContains "slot 2 NON mostra nodi"     "get guest status cc01 "      @("cc01", "cc02")
Test-Contains    "slot 3 cc01 qemu → vmid"    "get guest status cc01 qemu " @("1000", "1006", "1007", "1104", "1106", "1107")
Test-NotContains "slot 3 NON mostra vmtype"   "get guest status cc01 qemu " @("qemu", "lxc")
Test-Contains    "slot 3 cc01 lxc → vmid CT"  "get guest status cc01 lxc "  @("1014", "105")
Test-NotContains "slot 3 lxc NON mostra vmid qemu" "get guest status cc01 lxc " @("1000", "1006")
Test-Contains    "slot 3 cc02 qemu → vmid"    "get guest status cc02 qemu " @("102", "106", "1012", "203", "204", "999")
Test-Contains    "slot 3 cc02 lxc → vmid CT"  "get guest status cc02 lxc "  @("100", "101", "103", "104")

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== get guest status - --guest ===" -ForegroundColor Cyan
Test-Contains    "--guest → vmid numerici"    "get guest status --guest "   @("1000", "1006", "102", "100", "101")
Test-Contains    "--guest → nomi VM"          "get guest status --guest "   @("opnsense-cc01", "DomainControllerCV", "Mailstore")
Test-Contains    "--guest → nomi CT"          "get guest status --guest "   @("pbs01.local", "docker-cc02.public", "docker-cc01.local")
Test-NotContains "--guest NON mostra nodi"    "get guest status --guest "   @("cc01", "cc02")
Test-NotContains "--guest NON mostra vmtype"  "get guest status --guest "   @("qemu", "lxc")

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== get vm status - positional ===" -ForegroundColor Cyan
Test-Contains    "slot 1 → nodi"              "get vm status "              @("cc01", "cc02")
Test-Contains    "slot 1 prefisso parziale"   "get vm status cc0"           @("cc01", "cc02")
Test-Contains    "slot 2 cc01 → vmid qemu"    "get vm status cc01 "         @("1000", "1006", "1007", "1104")
Test-NotContains "slot 2 cc01 NON mostra CT"  "get vm status cc01 "         @("1014", "105")
Test-Contains    "slot 2 cc02 → vmid qemu"    "get vm status cc02 "         @("102", "106", "1012", "203")
Test-NotContains "slot 2 cc02 NON mostra CT"  "get vm status cc02 "         @("100", "101")

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== get vm status - --guest ===" -ForegroundColor Cyan
Test-Contains    "--guest → vmid VM"          "get vm status --guest "      @("1000", "1006", "102", "106")
Test-Contains    "--guest → nomi VM"          "get vm status --guest "      @("opnsense-cc01", "DomainControllerCV", "Mailstore")
Test-NotContains "--guest NON mostra nodi"    "get vm status --guest "      @("cc01", "cc02")

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== show guest - --guest ===" -ForegroundColor Cyan
Test-Contains    "--guest → vmid+nomi"        "show guest --guest "         @("1000", "opnsense-cc01", "100", "pbs01.local")
Test-NotContains "--guest NON mostra nodi"    "show guest --guest "         @("cc01", "cc02")

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== do start/stop/shutdown guest - --guest ===" -ForegroundColor Cyan
Test-Contains    "start --guest → vmid+nomi"    "do start guest --guest "    @("1000", "opnsense-cc01", "100")
Test-Contains    "stop --guest → vmid+nomi"     "do stop guest --guest "     @("1000", "opnsense-cc01", "100")
Test-Contains    "shutdown --guest → vmid+nomi" "do shutdown guest --guest " @("1000", "opnsense-cc01", "100")
Test-Contains    "reboot --guest → vmid+nomi"   "do reboot guest --guest "   @("1000", "opnsense-cc01", "100")

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== get guest snapshots - positional ===" -ForegroundColor Cyan
Test-Contains    "slot 1 → nodi"              "get guest snapshots "           @("cc01", "cc02")
Test-Exact       "slot 2 → solo vmtype"       "get guest snapshots cc01 "      @("qemu", "lxc")
Test-Contains    "slot 3 cc01 qemu → vmid"    "get guest snapshots cc01 qemu " @("1000", "1006")

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== Config ===" -ForegroundColor Cyan
Test-Contains "config TAB"       "config "      @("add", "use", "list", "set", "delete", "rename", "verify", "view")
Test-Contains "config use TAB"   "config use "  @("cc01", "prod")

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "=== API path ===" -ForegroundColor Cyan
Test-Contains "api get / → risorse radice"    "api get /"           @("/nodes", "/cluster", "/version")
Test-Contains "api get /nodes/ → nodi"        "api get /nodes/"     @("/nodes/cc01", "/nodes/cc02")

# ---------------------------------------------------------------------------
Write-Host ""
$color = if ($fail -eq 0) { "Green" } else { "Red" }
Write-Host "==============================" -ForegroundColor $color
Write-Host "  PASS: $pass   FAIL: $fail"   -ForegroundColor $color
Write-Host "==============================" -ForegroundColor $color
if ($fail -gt 0) { exit 1 }
