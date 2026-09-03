#!/usr/bin/env bash
#
# Prove every presentation project-reference allowlist rejects an engine
# dependency. The product solution is the positive case; these isolated
# projects make each negative Error condition run on every supported OS.
set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
probes="$root/tests/presentation-wall"
log="$(mktemp)"
trap 'rm -f "$log"' EXIT

failures=0

check() {
  local role="$1" expect="$2" project="$3"
  if dotnet build "$project" --nologo --verbosity quiet >"$log" 2>&1; then
    echo "  FAIL  $role succeeded, but $expect had to stop it."
    failures=$((failures + 1))
    return
  fi

  if grep -q "error $expect:" "$log"; then
    echo "  ok    $role fails with $expect"
    return
  fi

  echo "  FAIL  $role failed, but not with $expect."
  sed 's/^/        /' "$log"
  failures=$((failures + 1))
}

echo "The presentation project wall:"

check "View's forbidden engine reference" MARVELPRESENTATION \
  "$probes/Marvel.PresentationProbe.View/Marvel.PresentationProbe.View.csproj"
check "Decisions' forbidden engine reference" MARVELPRESENTATION \
  "$probes/Marvel.PresentationProbe.Decisions/Marvel.PresentationProbe.Decisions.csproj"
check "Client's forbidden engine reference" MARVELPRESENTATION \
  "$probes/Marvel.PresentationProbe.Client/Marvel.PresentationProbe.Client.csproj"
check "Godot's forbidden engine reference" MARVELPRESENTATION \
  "$probes/Marvel.PresentationProbe.Godot/Marvel.PresentationProbe.Godot.csproj"
check "a presentation project cannot expose transitive references" \
  MARVELPRESENTATIONCONFIG \
  "$probes/Marvel.PresentationProbe.Transitive/Marvel.PresentationProbe.Transitive.csproj"

if [ "$failures" -ne 0 ]; then
  echo
  echo "$failures presentation project gates did not behave as specified."
  exit 1
fi

echo
echo "Every forbidden presentation dependency and configuration was rejected."
