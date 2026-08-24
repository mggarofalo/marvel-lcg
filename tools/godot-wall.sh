#!/usr/bin/env bash
#
# MARVEL-162 — prove the wall.
#
# `Directory.Build.targets` refuses to build any assembly below `Marvel.Godot`
# that can see `GodotSharp`, and refuses to build one that targets a framework
# Godot cannot reference. Both are `<Error>` conditions, and an `<Error>`
# condition nobody has watched evaluate to true is a claim about a build rather
# than a property of one — the gate could be misspelled, batching over the wrong
# item, or hung off a target that never runs.
#
# So this builds four throwaway projects under tests/godot-wall/ and checks the
# verdicts. It needs no network: nothing here downloads the real `GodotSharp`.
#
# Run it from the repository root. CI runs exactly this.
set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
probes="$root/tests/godot-wall"
log="$(mktemp)"
trap 'rm -f "$log"' EXIT

failures=0

# Builds one project and asserts the outcome. `expect` is a build verdict —
# `pass`, or the MSBuild error code the build is required to stop on.
check() {
  local what="$1" expect="$2" project="$3"
  shift 3

  if dotnet build "$project" --nologo --verbosity quiet "$@" >"$log" 2>&1; then
    if [ "$expect" = "pass" ]; then
      echo "  ok    $what"
      return
    fi
    echo "  FAIL  $what: the build succeeded, but $expect had to stop it."
    failures=$((failures + 1))
    return
  fi

  if [ "$expect" = "pass" ]; then
    echo "  FAIL  $what: the build failed and should not have."
    sed 's/^/        /' "$log"
    failures=$((failures + 1))
    return
  fi

  # A failure is only the right failure if it is *this* gate's. Anything else —
  # a missing SDK, a syntax error in the probe — would otherwise read as proof.
  if grep -q "error $expect" "$log"; then
    echo "  ok    $what"
    return
  fi

  echo "  FAIL  $what: the build failed, but not with $expect."
  sed 's/^/        /' "$log"
  failures=$((failures + 1))
}

echo "The Godot wall:"

# The case that a .csproj scan would miss. Marvel.WallProbe names Godot
# nowhere; it reaches it through Marvel.WallProbe.Middle.
check "a transitive GodotSharp reference stops the build" \
  MARVELWALL "$probes/Marvel.WallProbe/Marvel.WallProbe.csproj"

# And the escape hatch, without which Marvel.Godot could not build at all.
check "the presentation layer may opt out" \
  pass "$probes/Marvel.WallProbe.Allowed/Marvel.WallProbe.Allowed.csproj"

echo "The runtime floor:"

# Raising one project above Godot's floor is the accident that will actually
# happen: a new .csproj, whatever TFM the SDK offers that year.
check "a framework above the floor stops the build" \
  MARVELTFM "$probes/Marvel.WallProbe.Future/Marvel.WallProbe.Future.csproj"

if [ "$failures" -ne 0 ]; then
  echo
  echo "$failures of the wall's gates did not behave as specified."
  exit 1
fi

echo
echo "The wall holds, and both gates were watched firing."
