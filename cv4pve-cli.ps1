#!/usr/bin/env pwsh
# SPDX-FileCopyrightText: Copyright Corsinvest Srl
# SPDX-License-Identifier: MIT
# Development wrapper - calls dotnet run so changes are picked up immediately
$project = Join-Path $PSScriptRoot "src\Corsinvest.ProxmoxVE.Cli"
dotnet run --project $project -- @args
