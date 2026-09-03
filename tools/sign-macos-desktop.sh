#!/usr/bin/env bash
set -euo pipefail

usage() {
    echo "usage: tools/sign-macos-desktop.sh --input DIR --output DIR" >&2
    exit 2
}

input=""
output=""
while [[ $# -gt 0 ]]; do
    case "$1" in
        --input) [[ $# -ge 2 ]] || usage; input=$2; shift 2 ;;
        --output) [[ $# -ge 2 ]] || usage; output=$2; shift 2 ;;
        *) usage ;;
    esac
done

[[ $(uname -s) == Darwin ]] || { echo "macOS signing requires macOS" >&2; exit 2; }
[[ -n "$input" && -n "$output" ]] || usage
input=$(cd "$input" && pwd)
output_parent=$(cd "$(dirname "$output")" && pwd)
output="$output_parent/$(basename "$output")"
app="$input/Marvel Champions.app"
manifest="$app/Contents/Resources/release-manifest.json"
[[ -d "$app" && -f "$manifest" ]] || { echo "unsigned macOS input is incomplete" >&2; exit 2; }
[[ ! -e "$output" ]] || { echo "output already exists" >&2; exit 2; }
: "${MARVEL_MACOS_SIGNING_IDENTITY:?MARVEL_MACOS_SIGNING_IDENTITY is required}"
: "${MARVEL_APPLE_API_ISSUER:?MARVEL_APPLE_API_ISSUER is required}"
: "${MARVEL_APPLE_API_KEY_ID:?MARVEL_APPLE_API_KEY_ID is required}"
: "${MARVEL_APPLE_API_KEY_FILE:?MARVEL_APPLE_API_KEY_FILE is required}"
[[ -f "$MARVEL_APPLE_API_KEY_FILE" ]] || { echo "notarization API key file was not found" >&2; exit 2; }

version=$(jq -er '.product_version' "$manifest")
channel=$(jq -er '.channel' "$manifest")
[[ "$channel" == preview || "$channel" == stable ]] || {
    echo "only preview or stable inputs may enter protected signing" >&2
    exit 2
}
unsigned_archive="$input/MarvelChampions-${version}-macos-unsigned.zip"
[[ -f "$unsigned_archive" ]] || { echo "unsigned macOS archive was not found" >&2; exit 2; }
unsigned_hash=$(shasum -a 256 "$unsigned_archive" | awk '{ print $1 }')
repository=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
scratch=$(mktemp -d "${TMPDIR:-/tmp}/marvel-macos-sign.XXXXXX")
cleanup() { rm -rf "$scratch"; }
trap cleanup EXIT
cp -R "$app" "$scratch/Marvel Champions.app"
signed_app="$scratch/Marvel Champions.app"

# Sign Mach-O code from the leaves toward the outer application. Apple warns
# that --deep is verification shorthand, not a correct signing strategy.
while IFS= read -r code; do
    codesign --force --timestamp --options runtime \
        --sign "$MARVEL_MACOS_SIGNING_IDENTITY" "$code"
done < <(
    find "$signed_app/Contents" -type f -print0 \
        | xargs -0 file \
        | awk -F: '/Mach-O/ { print $1 }' \
        | awk '{ print length, $0 }' \
        | sort -rn \
        | cut -d' ' -f2-
)
while IFS= read -r framework; do
    codesign --force --timestamp --options runtime \
        --sign "$MARVEL_MACOS_SIGNING_IDENTITY" "$framework"
done < <(find "$signed_app/Contents" -type d -name '*.framework' -print | sort -r)
codesign --force --timestamp --options runtime \
    --entitlements "$repository/tools/macos-entitlements.plist" \
    --sign "$MARVEL_MACOS_SIGNING_IDENTITY" "$signed_app"
codesign --verify --strict --verbose=2 "$signed_app"

submission="$scratch/notarization.zip"
ditto -c -k --keepParent "$signed_app" "$submission"
result="$scratch/notarization-result.json"
xcrun notarytool submit "$submission" \
    --key "$MARVEL_APPLE_API_KEY_FILE" \
    --key-id "$MARVEL_APPLE_API_KEY_ID" \
    --issuer "$MARVEL_APPLE_API_ISSUER" \
    --wait --output-format json > "$result"
[[ $(jq -er '.status' "$result") == Accepted ]] || {
    echo "Apple notarization did not accept the application" >&2
    exit 2
}
xcrun stapler staple "$signed_app"
xcrun stapler validate "$signed_app"
spctl --assess --type execute --verbose=2 "$signed_app"

mkdir -p "$output"
archive="$output/MarvelChampions-${version}-macos.zip"
ditto -c -k --keepParent "$signed_app" "$archive"
shasum -a 256 "$archive" > "$archive.sha256"
signed_hash=$(shasum -a 256 "$archive" | awk '{ print $1 }')
jq -n \
    --arg product_version "$version" \
    --arg commit "$(jq -er '.commit' "$manifest")" \
    --arg unsigned_input_sha256 "$unsigned_hash" \
    --arg signed_artifact_sha256 "$signed_hash" \
    '{
        format: "marvel-desktop-signing",
        schema: 1,
        product_version: $product_version,
        commit: $commit,
        unsigned_input_sha256: $unsigned_input_sha256,
        signed_artifact_sha256: $signed_artifact_sha256
    }' > "$archive.provenance.json"
echo "$archive"
