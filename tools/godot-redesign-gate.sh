#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "$0")/.." && pwd)
. "$(dirname "$0")/godot-smoke-diagnostics.sh"
godot_bin=${GODOT_BIN:-${1:-}}
capture_root=${MARVEL_SMOKE_CAPTURE_DIR:-$(mktemp -d)}

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
mkdir -p "$capture_root"

run_redesign_smoke() {
  local scale=$1
  local seats=$2
  local capture_dir="$capture_root/$seats-$scale"
  mkdir -p "$capture_dir"
  local smoke_log
  smoke_log=$(mktemp)
  local -a multiplayer=()
  if [[ "$seats" == two-player ]]; then
    multiplayer=(MARVEL_SMOKE_TWO_PLAYER=true)
  fi
  set +e
  env MARVEL_REDESIGN_GATE=true MARVEL_UI_SCALE="$scale" \
    MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
    MARVEL_SMOKE_CAPTURE_DIR="$capture_dir" "${multiplayer[@]}" \
    "$godot_bin" --audio-driver Dummy --rendering-method gl_compatibility \
    --resolution 1920x1080 --path "$repo_root/src/Marvel.Godot" \
    --script res://smoke/local_game_smoke.gd 2>&1 | tee "$smoke_log"
  local status=${PIPESTATUS[0]}
  set -e
  if [[ $status -ne 0 ]] || godot_smoke_has_error "$smoke_log"; then
    rm -f "$smoke_log"
    echo "Godot redesign gate failed for $seats at $scale%." >&2
    return 1
  fi
  rm -f "$smoke_log"
}

failed=0
for seats in single-player two-player; do
  for scale in 100 150; do
    if ! run_redesign_smoke "$scale" "$seats"; then
      failed=$((failed + 1))
    fi
  done
done

if [[ $failed -ne 0 ]]; then
  echo "Godot redesign gate failed in $failed matrix case(s)." >&2
  exit 1
fi

echo "Rendered redesign checkpoints: $capture_root"
