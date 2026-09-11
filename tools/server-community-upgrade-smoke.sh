#!/usr/bin/env bash
set -euo pipefail

current_image=${1:-}
current_version=${2:-}
godot_bin=${3:-}
if [[ -z "$current_image" || -z "$current_version" || ! -x "$godot_bin" ]]; then
  echo 'usage: server-community-upgrade-smoke.sh IMAGE@DIGEST VERSION GODOT_BIN' >&2
  exit 2
fi

repo_root=$(cd "$(dirname "$0")/.." && pwd)
prefix=marvel-upgrade-${GITHUB_RUN_ID:-$$}
previous_image=$prefix-previous:0.0.0
sessions=$prefix-sessions
restored=$prefix-restored
interrupted=$prefix-interrupted
rollback=$prefix-rollback-volume
downgrade=$prefix-downgrade
diagnostics=$prefix-diagnostics
backup=$(mktemp)
diagnostic_record=$(mktemp)
schema_two_copy=$(mktemp -d)
containers=()
cleanup() {
  for container in "${containers[@]}"; do
    docker rm --force "$container" >/dev/null 2>&1 || true
  done
  docker volume rm "$sessions" "$restored" "$interrupted" "$rollback" \
    "$downgrade" "$diagnostics" \
    >/dev/null 2>&1 || true
  docker image rm "$previous_image" >/dev/null 2>&1 || true
  rm -f "$backup" "$diagnostic_record"
  rm -rf "$schema_two_copy"
}
trap cleanup EXIT

docker pull --quiet "$current_image" >/dev/null
docker build --quiet \
  --file "$repo_root/src/Marvel.Server/Dockerfile" \
  --build-arg MARVEL_PRODUCT_VERSION=0.0.0 \
  --build-arg MARVEL_COMMIT=0000000000000000000000000000000000000000 \
  --build-arg MARVEL_VERSION_MAJOR=0 \
  --build-arg MARVEL_VERSION_MINOR=0 \
  --build-arg MARVEL_VERSION_PATCH=0 \
  --tag "$previous_image" "$repo_root" >/dev/null

for volume in "$sessions" "$restored" "$interrupted" "$rollback" \
    "$downgrade" "$diagnostics"; do
  docker volume create "$volume" >/dev/null
done

start_server() {
  local name=$1 image=$2 volume=$3 port=$4
  containers+=("$name")
  docker run --detach --name "$name" \
    --stop-timeout 40 \
    --publish "127.0.0.1:$port:41923" \
    --volume "$volume:/var/lib/marvel/sessions" \
    --volume "$diagnostics:/var/lib/marvel/diagnostics" \
    --read-only --cap-drop ALL --security-opt no-new-privileges \
    --memory 512m --cpus 1 --pids-limit 128 \
    "$image" --visibility restricted --seat 0 >/dev/null
  for _ in {1..300}; do
    [[ $(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{end}}' "$name") == healthy ]] && return
    [[ $(docker inspect --format '{{.State.Running}}' "$name") == true ]] || {
      docker logs "$name" >&2
      return 1
    }
    sleep 0.1
  done
  docker logs "$name" >&2
  return 1
}

stop_server() {
  docker stop "$1" >/dev/null
  [[ $(docker inspect --format '{{.State.ExitCode}}' "$1") == 0 ]]
}

run_game() {
  MARVEL_HOSTED_SMOKE_EXTERNAL_SERVER=true \
    MARVEL_HOSTED_SMOKE_PORT="$1" \
    bash "$repo_root/tools/godot-hosted-multiplayer-smoke.sh" "$godot_bin"
}

# Start from empty storage under a lower compatible product and commit a save.
start_server "$prefix-previous" "$previous_image" "$sessions" 41924
run_game 41924
stop_server "$prefix-previous"

# The lower product uses the current protocol so the packaged client can drive
# it, then its stopped generation is converted to the exact schema 2 prompt
# shape that the release reader promises to migrate.
docker cp "$prefix-previous:/var/lib/marvel/sessions/." "$schema_two_copy"
mapfile -t predecessor_saves < <(find "$schema_two_copy" -type f -name '*.session.json')
[[ ${#predecessor_saves[@]} == 1 ]] || {
  echo 'expected exactly one predecessor session save' >&2
  exit 2
}
predecessor_save=${predecessor_saves[0]}
jq '
  def schema_two_prompt:
    .affordances |= map(
      (if .targets != null then
        .targets.is_grouped = ((.targets.groups // []) | length > 0)
      else . end)
      | .costs |= map(
        .has_alternative = ((.or_cost | length) > 0)
        | .generators = (.sources // [])
        | .variable_requests = (.variables // [])
        | .resource_costs = (.components // [{cost: .cost, rule: .rule, printed: false}])));
  .schema = 2
  | (if .current_prompt != null then .current_prompt |= schema_two_prompt else . end)
  | .units |= map(.decisions |= map(.prompt |= schema_two_prompt))
' "$predecessor_save" > "$predecessor_save.tmp"
mv "$predecessor_save.tmp" "$predecessor_save"
relative_save=${predecessor_save#"$schema_two_copy"/}
docker cp "$predecessor_save" \
  "$prefix-previous:/var/lib/marvel/sessions/$relative_save"
jq -e '.schema == 2' "$predecessor_save" >/dev/null

docker run --rm --volume "$diagnostics:/diagnostics:ro" --entrypoint sh \
  "$current_image" -c 'head -n 1 /diagnostics/operational.jsonl' \
  > "$diagnostic_record"
[[ -s "$diagnostic_record" ]] || { echo 'pre-upgrade diagnostic record is absent' >&2; exit 2; }

# Preserve a complete stopped-volume backup before attempting the upgrade.
docker run --rm --volume "$sessions:/source:ro" --volume "$(dirname "$backup"):/backup" \
  alpine:3.23.3 tar -C /source -czf "/backup/$(basename "$backup")" .

# Interrupt a valid candidate after it has opened and restored the copied save.
docker run --rm --volume "$sessions:/source:ro" --volume "$interrupted:/target" \
  alpine:3.23.3 sh -c 'cd /source && tar -cf - . | tar -C /target -xf -'
start_server "$prefix-interrupted" "$current_image" "$interrupted" 41924
docker kill --signal KILL "$prefix-interrupted" >/dev/null
[[ $(docker inspect --format '{{.State.ExitCode}}' "$prefix-interrupted") != 0 ]]
start_server "$prefix-interrupted-recovery" "$current_image" "$interrupted" 41925
docker logs "$prefix-interrupted-recovery" 2>&1 | grep -q 'session.restore.completed'
stop_server "$prefix-interrupted-recovery"

# The pre-upgrade backup remains a runnable rollback unit with the prior image.
docker run --rm --volume "$rollback:/target" --volume "$(dirname "$backup"):/backup:ro" \
  alpine:3.23.3 tar -C /target -xzf "/backup/$(basename "$backup")"
start_server "$prefix-rollback" "$previous_image" "$rollback" 41924
docker logs "$prefix-rollback" 2>&1 | grep -q 'session.restore.completed'
stop_server "$prefix-rollback"

# The exact release image restores the existing save and retains diagnostics.
start_server "$prefix-current" "$current_image" "$sessions" 41924
current_logs=$(docker logs "$prefix-current" 2>&1)
grep -q 'session.restore.completed' <<< "$current_logs"
grep -q '"stage":"migration"' <<< "$current_logs"
grep -q '"save_committed":true' <<< "$current_logs"
docker run --rm --volume "$diagnostics:/diagnostics:ro" --entrypoint sh \
  "$current_image" -c 'cat /diagnostics/operational.jsonl' > "$backup.diagnostics"
grep -F -x -f "$diagnostic_record" "$backup.diagnostics" >/dev/null
grep -F '"product_version":"'"$current_version"'"' "$backup.diagnostics" >/dev/null
rm -f "$backup.diagnostics"
stop_server "$prefix-current"

# A backup restores into a fresh volume and remains runnable under the release image.
docker run --rm --volume "$restored:/target" --volume "$(dirname "$backup"):/backup:ro" \
  alpine:3.23.3 tar -C /target -xzf "/backup/$(basename "$backup")"
start_server "$prefix-restored" "$current_image" "$restored" 41924
docker logs "$prefix-restored" 2>&1 | grep -q 'session.restore.completed'
stop_server "$prefix-restored"

# A save written by the release cannot be handed to the lower product as rollback.
start_server "$prefix-writer" "$current_image" "$downgrade" 41924
run_game 41924
stop_server "$prefix-writer"
start_server "$prefix-downgraded" "$previous_image" "$downgrade" 41924
docker logs "$prefix-downgraded" 2>&1 | grep -q 'unsupported_downgrade'
stop_server "$prefix-downgraded"

docker run --rm --entrypoint dotnet "$current_image" Marvel.Server.dll --version |
  grep -F "v$current_version · engine engine-replay-v2 · protocol 14 · save 3"
echo 'SERVER_COMMUNITY_UPGRADE_SMOKE_OK'
