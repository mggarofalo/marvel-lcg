#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "$0")/.." && pwd)
godot_bin=${GODOT_BIN:-${1:-}}
smoke_port=${MARVEL_HOSTED_SMOKE_PORT:-41924}
external_server=${MARVEL_HOSTED_SMOKE_EXTERNAL_SERVER:-false}

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
server_log=$(mktemp)
smoke_log=$(mktemp)
save_root=$(mktemp -d)
server_pid=
cleanup() {
  if [[ -n "$server_pid" ]] && kill -0 "$server_pid" 2>/dev/null; then
    kill -TERM "$server_pid" 2>/dev/null || true
    wait "$server_pid" 2>/dev/null || true
  fi
  rm -f "$server_log" "$smoke_log"
  rm -rf "$save_root"
}
trap cleanup EXIT

if [[ "$external_server" != true ]]; then
  dotnet run --no-build --project "$repo_root/src/Marvel.Server/Marvel.Server.csproj" -- \
    --listen 127.0.0.1 \
    --port "$smoke_port" \
    --data-root "$repo_root" \
    --save-root "$save_root" \
    --visibility restricted \
    --seat 0 \
    2>"$server_log" &
  server_pid=$!

  for _ in {1..300}; do
    if grep -q '"event_id":"server.listener.started"' "$server_log"; then
      break
    fi
    if ! kill -0 "$server_pid" 2>/dev/null; then
      cat "$server_log" >&2
      exit 1
    fi
    sleep 0.1
  done
  if ! grep -q '"event_id":"server.listener.started"' "$server_log"; then
    cat "$server_log" >&2
    echo "restricted hosted smoke server did not become ready" >&2
    exit 1
  fi
fi

smoke_command=("$godot_bin")
if [[ $(uname -s) == Linux ]]; then
  if ! command -v xvfb-run >/dev/null 2>&1; then
    echo "xvfb-run is required for the hosted clipboard smoke on Linux." >&2
    exit 2
  fi
  smoke_command=(xvfb-run -a "$godot_bin")
fi

set +e
MARVEL_ENGINE_ENDPOINT="tcp://127.0.0.1:$smoke_port" \
  MARVEL_UI_SCALE=compact \
  "${smoke_command[@]}" \
  --path "$repo_root/src/Marvel.Godot" \
  --script res://smoke/hosted_multiplayer_smoke.gd \
  2>&1 | tee "$smoke_log"
smoke_status=${PIPESTATUS[0]}
set -e
if [[ $smoke_status -ne 0 ]] || ! grep -q "HOSTED_MULTIPLAYER_SMOKE_OK" "$smoke_log"; then
  exit 1
fi
