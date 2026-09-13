#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "$0")/.." && pwd)
godot_bin=${GODOT_BIN:-${1:-}}
profile=${2:---exhaustive}

if [[ "$profile" != --representative && "$profile" != --exhaustive ]]; then
  echo "usage: godot-smoke.sh [GODOT_BIN] [--representative|--exhaustive]" >&2
  exit 2
fi

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

version=$("$godot_bin" --version)
if [[ "$version" != 4.7.* ]]; then
  echo "Godot 4.7 .NET is required; found: $version" >&2
  exit 2
fi

dotnet build "$repo_root/src/Marvel.Godot/Marvel.Godot.csproj" --nologo
if [[ "$profile" == --representative ]]; then
  viewports=(1920x1080)
  scales=(100)
else
  viewports=(1920x1080 2560x1440)
  scales=(50 60 70 80 90 100 110 120 130 140 150)
fi

for viewport in "${viewports[@]}"; do
  for scale in "${scales[@]}"; do
    MARVEL_UI_SCALE="$scale" MARVEL_SMOKE_VIEWPORT="$viewport" MARVEL_SMOKE_MOTION=enabled \
      "$godot_bin" --headless \
      --path "$repo_root/src/Marvel.Godot" \
      --script res://smoke/local_game_smoke.gd
  done
done

MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  "$godot_bin" --headless \
  --path "$repo_root/src/Marvel.Godot" \
  --script res://smoke/local_game_smoke.gd
