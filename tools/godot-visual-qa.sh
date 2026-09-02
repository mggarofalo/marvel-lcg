#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "$0")/.." && pwd)
godot_bin=${GODOT_BIN:-${1:-}}
capture_dir=${MARVEL_SMOKE_CAPTURE_DIR:-${2:-}}

if [[ -z "$godot_bin" || ! -x "$godot_bin" ]]; then
  echo "Set GODOT_BIN to the Godot 4.7 .NET executable." >&2
  exit 2
fi
if [[ -z "$capture_dir" ]]; then
  capture_dir=$(mktemp -d)
fi

version=$("$godot_bin" --version)
if [[ "$version" != 4.7.* ]]; then
  echo "Godot 4.7 .NET is required; found: $version" >&2
  exit 2
fi

dotnet build "$repo_root/src/Marvel.Godot/Marvel.Godot.csproj" --nologo
use_xvfb=false
if [[ "$(uname -s)" == Linux ]]; then
  if ! command -v xvfb-run >/dev/null 2>&1; then
    echo "xvfb-run is required for rendered Linux checkpoints." >&2
    exit 2
  fi
  use_xvfb=true
fi

mkdir -p "$capture_dir"
for viewport in 1280x720 1920x1080; do
  for motion in enabled disabled; do
    if [[ "$use_xvfb" == true ]]; then
      MARVEL_UI_SCALE=standard \
        MARVEL_SMOKE_VIEWPORT="$viewport" \
        MARVEL_SMOKE_MOTION="$motion" \
        MARVEL_SMOKE_CAPTURE_DIR="$capture_dir" \
      xvfb-run -a "$godot_bin" \
        --rendering-method gl_compatibility \
        --resolution "$viewport" \
        --path "$repo_root/src/Marvel.Godot" \
          --script res://smoke/local_game_smoke.gd
    else
      MARVEL_UI_SCALE=standard \
        MARVEL_SMOKE_VIEWPORT="$viewport" \
        MARVEL_SMOKE_MOTION="$motion" \
        MARVEL_SMOKE_CAPTURE_DIR="$capture_dir" \
      "$godot_bin" \
        --rendering-method gl_compatibility \
        --resolution "$viewport" \
        --path "$repo_root/src/Marvel.Godot" \
          --script res://smoke/local_game_smoke.gd
    fi
  done
done

echo "Rendered visual checkpoints: $capture_dir"
