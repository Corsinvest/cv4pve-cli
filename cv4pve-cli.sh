#!/usr/bin/env bash
# SPDX-FileCopyrightText: Copyright Corsinvest Srl
# SPDX-License-Identifier: MIT
# Development wrapper - calls dotnet run so changes are picked up immediately
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec dotnet run --project "$SCRIPT_DIR/src/Corsinvest.ProxmoxVE.Cli" -- "$@"
