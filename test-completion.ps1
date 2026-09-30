#!/usr/bin/env pwsh
# SPDX-FileCopyrightText: Copyright Corsinvest Srl
# SPDX-License-Identifier: MIT
#
# test-completion.ps1: checks the tab completion of cv4pve-cli against a real cluster.
# Usage: .\test-completion.ps1 [-Cli <path>]
#
# Needs a current context. Nodes, VMs and containers are read from the cluster (cv4pve-cli top),
# so the script holds no names of its own.

param(
    [string]$Cli = "cv4pve-cli"
)

$pass = 0
$fail = 0

$resources = & $Cli top -o json | ConvertFrom-Json
$nodes = @($resources | Where-Object type -eq 'node' | ForEach-Object node)
$vms = @($resources | Where-Object type -eq 'qemu')
$cts = @($resources | Where-Object type -eq 'lxc')
if ($nodes.Count -eq 0) { Write-Host "No nodes found: is a context configured?" -ForegroundColor Red; exit 1 }

$node = $nodes[0]
$vmsOnNode = @($vms | Where-Object node -eq $node | ForEach-Object { "$($_.vmid)" })
$ctsOnNode = @($cts | Where-Object node -eq $node | ForEach-Object { "$($_.vmid)" })
$allVmIds = @($vms | ForEach-Object { "$($_.vmid)" })
$allCtIds = @($cts | ForEach-Object { "$($_.vmid)" })
$vmNames = @($vms | Where-Object name | ForEach-Object name)
$ctNames = @($cts | Where-Object name | ForEach-Object name)
$contexts = @(& $Cli config list | ForEach-Object { ($_.Substring(2) -split '\s+')[0] })

function Complete($line) {
    & $Cli complete -- "cv4pve-cli $line" 2>$null
}

function Report($desc, $line, [bool]$ok, $detail) {
    if ($ok) {
        Write-Host "  PASS  $desc" -ForegroundColor Green
        $script:pass++
    } else {
        Write-Host "  FAIL  $desc" -ForegroundColor Red
        Write-Host "        line : cv4pve-cli $line"
        Write-Host "        $detail"
        $script:fail++
    }
}

function Test-Contains($desc, $line, [string[]]$expected) {
    if ($expected.Count -eq 0) { Write-Host "  SKIP  $desc (nothing to expect on this cluster)" -ForegroundColor Yellow; return }
    $results = @(Complete $line)
    $missing = $expected | Where-Object { $_ -notin $results }
    Report $desc $line ($missing.Count -eq 0) "missing: $($missing -join ', ')  got: $($results -join ', ')"
}

function Test-NotContains($desc, $line, [string[]]$notExpected) {
    if ($notExpected.Count -eq 0) { Write-Host "  SKIP  $desc (nothing to check on this cluster)" -ForegroundColor Yellow; return }
    $results = @(Complete $line)
    $found = $notExpected | Where-Object { $_ -in $results }
    Report $desc $line ($found.Count -eq 0) "should not have: $($found -join ', ')"
}

function Test-Exact($desc, $line, [string[]]$expected) {
    $results = @(Complete $line)
    $missing = $expected | Where-Object { $_ -notin $results }
    $extra = $results | Where-Object { $_ -notin $expected }
    Report $desc $line ($missing.Count -eq 0 -and $extra.Count -eq 0) "missing: $($missing -join ', ')  extra: $($extra -join ', ')"
}

Write-Host ""
Write-Host "=== Top-level commands ===" -ForegroundColor Cyan
Test-Contains "root"      ""       @("get", "show", "do", "api", "config", "alias", "task", "top")
Test-Contains "get"       "get "   @("guests", "vms", "nodes", "vm", "ct", "guest", "cluster")
Test-Contains "do"        "do "    @("start", "stop", "reboot", "shutdown", "migrate")
Test-Contains "show"      "show "  @("vm", "ct", "guest", "node")

Write-Host ""
Write-Host "=== get guest status - positional ===" -ForegroundColor Cyan
Test-Contains    "slot 1: nodes"                "get guest status "                $nodes
Test-Exact       "slot 2: only vmtype"          "get guest status $node "          @("qemu", "lxc")
Test-NotContains "slot 2: no nodes"             "get guest status $node "          $nodes
Test-Contains    "slot 3 qemu: VM IDs"          "get guest status $node qemu "     $vmsOnNode
Test-NotContains "slot 3 qemu: no vmtype"       "get guest status $node qemu "     @("qemu", "lxc")
Test-Contains    "slot 3 lxc: CT IDs"           "get guest status $node lxc "      $ctsOnNode
Test-NotContains "slot 3 lxc: no VM IDs"        "get guest status $node lxc "      $vmsOnNode

Write-Host ""
Write-Host "=== --guest ===" -ForegroundColor Cyan
Test-Contains    "guest: VM and CT IDs"         "get guest status --guest "        ($allVmIds + $allCtIds)
Test-Contains    "guest: VM and CT names"       "get guest status --guest "        ($vmNames + $ctNames)
Test-NotContains "guest: no nodes"              "get guest status --guest "        $nodes
Test-Contains    "vm: VM IDs and names"         "get vm status --guest "           ($allVmIds + $vmNames)
Test-NotContains "vm: no nodes"                 "get vm status --guest "           $nodes
Test-Contains    "do start guest"               "do start guest --guest "          ($allVmIds + $allCtIds)
if ($allVmIds.Count -gt 0) {
    # The --guest value is not an argument: parameters and their values follow at once (it used to wait 30 s).
    Test-Exact "vm: parameter after --guest"  "set vm config --guest $($allVmIds[0]) --onb"      @("--onboot")
    Test-Exact "vm: value after --guest"      "set vm config --guest $($allVmIds[0]) --onboot "  @("0", "1")
}

Write-Host ""
Write-Host "=== get vm status - positional ===" -ForegroundColor Cyan
Test-Contains    "slot 1: nodes"                "get vm status "                   $nodes
Test-Contains    "slot 1 after -o json: nodes"  "get vm status -o json "           $nodes
Test-Contains    "slot 2: VM IDs"               "get vm status $node "             $vmsOnNode
Test-NotContains "slot 2: no CT IDs"            "get vm status $node "             $ctsOnNode

Write-Host ""
Write-Host "=== Config ===" -ForegroundColor Cyan
Test-Contains "config"            "config "      @("add", "use", "list", "set", "delete", "rename", "verify", "view")
Test-Contains "config use"        "config use "  $contexts

Write-Host ""
Write-Host "=== API path ===" -ForegroundColor Cyan
Test-Contains "api get /"         "api get /"          @("/nodes", "/cluster", "/version")
Test-Contains "api get /nodes/"   "api get /nodes/"    @($nodes | ForEach-Object { "/nodes/$_" })

Write-Host ""
$color = if ($fail -eq 0) { "Green" } else { "Red" }
Write-Host "==============================" -ForegroundColor $color
Write-Host "  PASS: $pass   FAIL: $fail"   -ForegroundColor $color
Write-Host "==============================" -ForegroundColor $color
if ($fail -gt 0) { exit 1 }
