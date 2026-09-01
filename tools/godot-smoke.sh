#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "$0")/.." && pwd)
godot_bin=${GODOT_BIN:-${1:-}}

if [[ -z "$godot_bin" ]]; then
  for candidate in godot-mono godot; do
    if command -v "$candidate" >/dev/null 2>&1; then
      godot_bin=$(command -v "$candidate")
      break
    fi
  done
fi

if [[ -z "$godot_bin" || ! -x "$godot_bin" ]]; then
  echo "Set GODOT_BIN to the Godot 4.7 .NET executable." >&2
  exit 2
fi

version=$($godot_bin --version)
if [[ "$version" != 4.7.* ]]; then
  echo "Godot 4.7 .NET is required; found: $version" >&2
  exit 2
fi

dotnet build "$repo_root/src/Marvel.Godot/Marvel.Godot.csproj" --nologo
"$godot_bin" --headless --path "$repo_root/src/Marvel.Godot" \
  --script res://smoke/local_game_smoke.gd
