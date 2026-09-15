#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "$0")/.." && pwd)
. "$(dirname "$0")/godot-smoke-diagnostics.sh"
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
smoke_log=$(mktemp)
cleanup() { rm -f "$smoke_log"; }
trap cleanup EXIT
run_visual_smoke() {
  set +e
  local status
  if [[ "$use_xvfb" == true ]]; then
    xvfb-run -a "$godot_bin" --audio-driver Dummy --rendering-method gl_compatibility \
      --resolution "$MARVEL_SMOKE_VIEWPORT" --path "$repo_root/src/Marvel.Godot" \
      --script res://smoke/local_game_smoke.gd 2>&1 | tee "$smoke_log"
    status=${PIPESTATUS[0]}
  else
    "$godot_bin" --audio-driver Dummy --rendering-method gl_compatibility \
      --resolution "$MARVEL_SMOKE_VIEWPORT" --path "$repo_root/src/Marvel.Godot" \
      --script res://smoke/local_game_smoke.gd 2>&1 | tee "$smoke_log"
    status=${PIPESTATUS[0]}
  fi
  set -e
  if [[ $status -ne 0 ]] || godot_smoke_has_error "$smoke_log"; then
    echo "Godot visual smoke reported an unexpected failure diagnostic." >&2
    exit 1
  fi
}
for viewport in 1920x1080; do
  for motion in enabled disabled; do
    MARVEL_UI_SCALE=compact MARVEL_SMOKE_VIEWPORT="$viewport" \
      MARVEL_SMOKE_MOTION="$motion" MARVEL_SMOKE_CAPTURE_DIR="$capture_dir" \
      run_visual_smoke
  done
done

echo "Rendered visual checkpoints: $capture_dir"
