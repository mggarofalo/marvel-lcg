#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "$0")/.." && pwd)
. "$(dirname "$0")/godot-smoke-diagnostics.sh"
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
pwsh -NoProfile -File "$repo_root/tools/godot-smoke-b1.ps1" -GodotBin "$godot_bin"
pwsh -NoProfile -File "$repo_root/tools/godot-smoke-tableau.ps1" -GodotBin "$godot_bin"
smoke_log=$(mktemp)
cleanup() { rm -f "$smoke_log"; }
trap cleanup EXIT

run_local_smoke() {
  local script=${1:-res://smoke/local_game_smoke.gd}
  set +e
  "$godot_bin" --headless --audio-driver Dummy --path "$repo_root/src/Marvel.Godot" \
    --script "$script" 2>&1 | tee "$smoke_log"
  local status=${PIPESTATUS[0]}
  set -e
  if [[ $status -ne 0 ]] || godot_smoke_has_error "$smoke_log"; then
    echo "Godot local smoke reported an unexpected failure diagnostic." >&2
    exit 1
  fi
}
if [[ "$profile" == --representative ]]; then
  viewports=(1920x1080)
  scales=(100)
else
  viewports=(1920x1080)
  scales=(50 60 70 80 90 100 110 120 130 140 150)
fi

unset MARVEL_SMOKE_TWO_PLAYER
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
  run_local_smoke res://smoke/ultron_inspection_smoke.gd
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  run_local_smoke res://smoke/ultron_inspection_smoke.gd
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
  run_local_smoke res://smoke/minion_order_smoke.gd
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  run_local_smoke res://smoke/minion_order_smoke.gd
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
  run_local_smoke res://smoke/boost_choice_smoke.gd
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  run_local_smoke res://smoke/boost_choice_smoke.gd
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
  run_local_smoke res://smoke/ability_choice_smoke.gd
for window_scale in 100 150; do
  for window_script in ability_window ability_window_pass forced_ability ability_window_long_cause_layout staged_response; do
    MARVEL_UI_SCALE=$window_scale MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
      run_local_smoke "res://smoke/${window_script}_smoke.gd"
  done
done
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  run_local_smoke res://smoke/ability_choice_smoke.gd
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
  run_local_smoke res://smoke/deferred_event_smoke.gd
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  run_local_smoke res://smoke/deferred_event_smoke.gd
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
  run_local_smoke res://smoke/repeated_commit_smoke.gd
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
  run_local_smoke res://smoke/hand_intent_smoke.gd
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
  run_local_smoke res://smoke/discard_preview_smoke.gd
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled MARVEL_RECIPIENT_FACE=01091 MARVEL_RECIPIENT_SEED=1 \
  run_local_smoke res://smoke/recipient_commitment_smoke.gd
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled MARVEL_SMOKE_TWO_PLAYER=true \
  run_local_smoke res://smoke/layout_continuity_smoke.gd
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
  run_local_smoke res://smoke/resource_finalizer_smoke.gd
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  run_local_smoke res://smoke/repeated_commit_smoke.gd
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  run_local_smoke res://smoke/hand_intent_smoke.gd
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  run_local_smoke res://smoke/discard_preview_smoke.gd
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled MARVEL_SMOKE_TWO_PLAYER=true MARVEL_RECIPIENT_FACE=01092 MARVEL_RECIPIENT_SEED=9 \
  run_local_smoke res://smoke/recipient_commitment_smoke.gd
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled MARVEL_SMOKE_TWO_PLAYER=true \
  run_local_smoke res://smoke/layout_continuity_smoke.gd
for scale in 50 80 100 150; do
  MARVEL_UI_SCALE="$scale" MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
    run_local_smoke res://smoke/search_choice_smoke.gd
  MARVEL_UI_SCALE="$scale" MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
    run_local_smoke res://smoke/shuri_search_smoke.gd
  MARVEL_UI_SCALE="$scale" MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
    run_local_smoke res://smoke/ancestral_knowledge_smoke.gd
done

MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
  run_local_smoke res://smoke/result_playback_smoke.gd
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
  run_local_smoke res://smoke/event_activity_smoke.gd
MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
  run_local_smoke res://smoke/timeline_navigation_smoke.gd
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  run_local_smoke res://smoke/result_playback_smoke.gd
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  run_local_smoke res://smoke/event_activity_smoke.gd
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  run_local_smoke res://smoke/timeline_navigation_smoke.gd

for viewport in "${viewports[@]}"; do
  for scale in "${scales[@]}"; do
    MARVEL_UI_SCALE="$scale" MARVEL_SMOKE_VIEWPORT="$viewport" MARVEL_SMOKE_MOTION=enabled \
      run_local_smoke
  done
done

MARVEL_UI_SCALE=100 MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=disabled \
  run_local_smoke

for scale in 100 120 150; do
  MARVEL_UI_SCALE="$scale" MARVEL_SMOKE_VIEWPORT=1920x1080 MARVEL_SMOKE_MOTION=enabled \
    MARVEL_SMOKE_TWO_PLAYER=true run_local_smoke
done
unset MARVEL_SMOKE_TWO_PLAYER
