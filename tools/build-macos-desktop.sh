#!/usr/bin/env bash
set -euo pipefail

usage() {
    echo "usage: tools/build-macos-desktop.sh --godot PATH --version VERSION --commit SHA --output DIR" >&2
    exit 2
}

godot_bin=""
version=""
commit=""
output=""
while [[ $# -gt 0 ]]; do
    case "$1" in
        --godot) [[ $# -ge 2 ]] || usage; godot_bin=$2; shift 2 ;;
        --version) [[ $# -ge 2 ]] || usage; version=$2; shift 2 ;;
        --commit) [[ $# -ge 2 ]] || usage; commit=$2; shift 2 ;;
        --output) [[ $# -ge 2 ]] || usage; output=$2; shift 2 ;;
        *) usage ;;
    esac
done

[[ $(uname -s) == Darwin ]] || { echo "macOS desktop export requires macOS" >&2; exit 2; }
[[ -x "$godot_bin" ]] || { echo "Godot editor executable was not found" >&2; exit 2; }
[[ -n "$version" && -n "$commit" && -n "$output" ]] || usage
[[ "$commit" =~ ^[0-9a-f]{40}$ ]] || { echo "commit must be 40 lowercase hexadecimal characters" >&2; exit 2; }

repository=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
[[ $(git -C "$repository" rev-parse HEAD) == "$commit" ]] || {
    echo "commit does not identify the checked-out source" >&2
    exit 2
}
[[ -z $(git -C "$repository" status --porcelain --untracked-files=all) ]] || {
    echo "desktop artifacts require a clean checkout" >&2
    exit 2
}
output_parent=$(cd "$(dirname "$output")" && pwd)
output="$output_parent/$(basename "$output")"
[[ ! -e "$output" ]] || { echo "output already exists" >&2; exit 2; }

base_version=${version%%-*}
IFS=. read -r version_major version_minor version_patch extra <<< "$base_version"
[[ -n "$version_major" && -n "$version_minor" && -n "$version_patch" && -z "${extra:-}" ]] || usage

scratch=$(mktemp -d "${TMPDIR:-/tmp}/marvel-macos-desktop.XXXXXX")
cleanup() { rm -rf "$scratch"; }
trap cleanup EXIT
source_root="$scratch/source"
stage="$scratch/stage"
mkdir -p "$source_root/src" "$source_root/tools" "$stage"
# macOS exposes /var through the /private/var physical path. Roslyn records the
# physical path, so give PathMap that same spelling for byte-identical builds.
source_root=$(cd "$source_root" && pwd -P)

for file in Directory.Build.props Directory.Build.targets Directory.Packages.props global.json; do
    cp "$repository/$file" "$source_root/$file"
done
rsync -a --exclude bin --exclude obj --exclude .godot "$repository/src/" "$source_root/src/"
rsync -a --exclude bin --exclude obj "$repository/tools/Marvel.Release/" "$source_root/tools/Marvel.Release/"
/usr/bin/sed -i '' \
    "s|config/version=\"[^\"]*\"|config/version=\"$base_version\"|" \
    "$source_root/src/Marvel.Godot/project.godot"

export MarvelProductVersion="$version"
export MarvelCommit="$commit"
export MarvelVersionMajor="$version_major"
export MarvelVersionMinor="$version_minor"
export MarvelVersionPatch="$version_patch"
export MarvelSourceRoot="$source_root"

godot_project="$source_root/src/Marvel.Godot"
# Godot 4.7's .NET export plugin still requires a legacy solution named after
# the project. Generate it only in the isolated source copy; Marvel.slnx
# remains the repository and CI solution.
dotnet new sln --name Marvel.Godot --format sln --output "$godot_project"
dotnet sln "$godot_project/Marvel.Godot.sln" add "$godot_project/Marvel.Godot.csproj"
dotnet restore "$godot_project/Marvel.Godot.csproj"
dotnet restore "$source_root/tools/Marvel.Release/Marvel.Release.csproj"
app="$stage/Marvel Champions.app"
"$godot_bin" --headless \
    --path "$godot_project" \
    --export-release macOS "$app"
[[ -x "$app/Contents/MacOS/Marvel Champions" ]] || {
    echo "Godot did not produce an executable macOS application" >&2
    exit 2
}
find "$app/Contents/Resources" -type f -name 'Marvel.Godot.dll' -print -quit \
    | grep -q . || {
        echo "Godot export omitted the managed application" >&2
        exit 2
    }

resources="$app/Contents/Resources"
mkdir -p "$resources/datasets/cards" "$resources/datasets/setup" "$resources/datasets/abilities"
cp "$repository/datasets/cards/cards.json" "$resources/datasets/cards/cards.json"
cp "$repository/datasets/setup/setup.json" "$resources/datasets/setup/setup.json"
cp "$repository/datasets/abilities/abilities.json" "$resources/datasets/abilities/abilities.json"

dotnet run --project "$source_root/tools/Marvel.Release/Marvel.Release.csproj" \
    --configuration Release --no-restore \
    -p:MarvelProductVersion="$version" \
    -p:MarvelCommit="$commit" \
    -p:MarvelVersionMajor="$version_major" \
    -p:MarvelVersionMinor="$version_minor" \
    -p:MarvelVersionPatch="$version_patch" \
    -- manifest \
    --version "$version" \
    --commit "$commit" \
    --data-root "$repository" \
    --output "$resources/release-manifest.json"

mkdir -p "$output"
mv "$app" "$output/Marvel Champions.app"
commit_epoch=$(git -C "$repository" show -s --format=%ct "$commit")
timestamp=$(date -r "$commit_epoch" +%Y%m%d%H%M.%S)
find "$output/Marvel Champions.app" -exec touch -h -t "$timestamp" {} +
archive="$output/MarvelChampions-${version}-macos-unsigned.zip"
(
    cd "$output"
    find "Marvel Champions.app" -print | LC_ALL=C sort | zip -X -y -q "$archive" -@
)
(
    cd "$output"
    archive_name=$(basename "$archive")
    shasum -a 256 "$archive_name" > "$archive_name.sha256"
)
echo "$archive"
