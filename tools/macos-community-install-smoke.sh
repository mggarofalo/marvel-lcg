#!/usr/bin/env bash
set -euo pipefail

archive=${1:-}
if [[ -z "$archive" || ! -f "$archive" || ! -f "$archive.sha256" \
    || -z ${MARVEL_ENGINE_ENDPOINT:-} ]]; then
  echo 'usage: macos-community-install-smoke.sh APPLICATION.zip' >&2
  echo 'MARVEL_ENGINE_ENDPOINT must name the disposable test server.' >&2
  exit 2
fi

archive=$(cd "$(dirname "$archive")" && pwd)/$(basename "$archive")
expected=$(awk 'NR == 1 { print $1 }' "$archive.sha256")
named=$(awk 'NR == 1 { print $2 }' "$archive.sha256" | sed 's/^\*//')
[[ $expected =~ ^[0-9a-f]{64}$ && $named == "$(basename "$archive")" ]] || {
  echo 'macOS artifact hash file is malformed or names another file' >&2
  exit 2
}
actual=$(shasum -a 256 "$archive" | awk '{ print $1 }')
[[ $actual == "$expected" ]] || {
  echo 'macOS artifact hash mismatch' >&2
  exit 2
}

install_root=$(mktemp -d)
log=$(mktemp)
cleanup() {
  rm -rf "$install_root"
  rm -f "$log"
}
trap cleanup EXIT

ditto -x -k "$archive" "$install_root"
app="$install_root/Marvel Champions.app"
executable="$app/Contents/MacOS/Marvel Champions"
[[ -x "$executable" ]] || { echo 'macOS application executable is absent' >&2; exit 2; }
codesign --verify --deep --strict "$app"

# This deliberately models a downloaded community artifact. An ad-hoc
# signature supplies integrity, not Apple publisher trust or notarization.
xattr -w com.apple.quarantine '0081;00000000;Marvel Champions;' "$app"
if spctl --assess --type execute "$app" >"$log" 2>&1; then
  echo 'ad-hoc macOS artifact unexpectedly passed public Gatekeeper assessment' >&2
  exit 2
fi
grep -Eiq 'rejected|not accepted|unnotarized|source=' "$log" || {
  echo 'Gatekeeper did not return the expected first-launch rejection' >&2
  exit 2
}

# The documented override is an explicit user choice, not a trust claim.
xattr -dr com.apple.quarantine "$app"
if xattr -p com.apple.quarantine "$app" >/dev/null 2>&1; then
  echo 'the user-controlled quarantine override did not clear the application' >&2
  exit 2
fi

"$executable" --script res://smoke/hosted_multiplayer_smoke.gd \
  >"$log" 2>&1
grep -q 'HOSTED_MULTIPLAYER_SMOKE_OK' "$log" || {
  cat "$log" >&2
  echo 'the extracted macOS application did not complete its hosted game smoke' >&2
  exit 1
}

rm -rf "$install_root"
[[ ! -e "$install_root" ]] || { echo 'macOS application removal failed' >&2; exit 2; }
echo 'MACOS_COMMUNITY_INSTALL_SMOKE_OK'
