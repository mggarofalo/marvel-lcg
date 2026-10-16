#!/usr/bin/env bash
set -euo pipefail
repo_root=$(cd "$(dirname "$0")/.." && pwd)
godot_bin=${1:-${GODOT_BIN:-}}
profile=${2:---exhaustive}
if [[ "$profile" != --representative && "$profile" != --exhaustive ]]; then
  echo "usage: godot-smoke.sh [GODOT_BIN] [--representative|--exhaustive]" >&2
  exit 2
fi
if [[ -z "$godot_bin" ]]; then
  for candidate in godot-mono godot; do
    if command -v "$candidate" >/dev/null 2>&1; then godot_bin=$(command -v "$candidate"); break; fi
  done
fi
exec pwsh -NoProfile -File "$repo_root/tools/native-cases.ps1" -GodotBin "$godot_bin" -Profile "${profile#--}"
