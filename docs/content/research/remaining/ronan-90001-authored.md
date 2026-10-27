# Ronan 90001 authored face

MARVEL-567. Bounded implementation on `feat/ronan-completion`, based on
`abdca907ee1f5fc8e228a3e5ce147da61ceea4b9` (the complete 718-row Hulk book
plus the accepted Ronan prerequisites). This packet adds only actual `90001`;
independent review follows. It does not establish the whole modular set or public
setup, save/restart, or native acceptance.

Printed authority is the joined `datasets/cards/cards.json` 90001 row. The
[original PDF pin](ronan-source-pin.md) records the seven physical cards and its
local inspected source. Current RR 1.8 governs Toughness, first-player ties,
engagement and the absolute cannot restriction. The FAQ acquisition inventory
contains 90001; no applicable card ruling changes this body.

The 719th row uses a constant `cannot_be_stunned` grant, the accepted
game-subject phase-beginning interrupt and the existing selected-identity /
ordered `eachPlayer` engagement composition. Its Boost ability uses ordinary
`putIntoPlay` engaged with the activation's resolving player. Toughness remains
the printed keyword's ordinary entry lifecycle. No engine operation, setup
recipe, or public admission policy changes. The preceding 718 row bytes remain
exactly unchanged; the book is explicit UTF-8 with LF line endings.

`RonanPhaseBeginningTests` now executes the production row in both first-player
orders and observes movement before step-one threat and activation against the
new player. `RonanSelectedEngagementTests` executes that row at the actual phase
occurrence for ties, current engagement, no heroes, alternate forms, stale chosen
identities and preserved transfer state. Its Vulture/Thor entry-hook cases retain
an explicitly synthetic body. Other synthetic scheduler fixtures retain their
own books.

`RonanActualCardTests` proves ordinary entry Toughness, stun rejection with a
stunnable mercenary control and a confusable Ronan, plus complete attack and
scheme boost activations. Both target seat 1 while seat 0 is first player. Ronan
enters the recipient's engagement, gains exactly one Toughness status, remains
in play through cleanup, and the activation completes without game termination.
The exact authored Ronan inventory is only 90001; the incomplete set remains
absent from public playable faces and rejected by public dealing. Older global
negative controls now use catalogued, still-unauthored Kang 11001. Exact book
counts become 719; the final Hulk slice remains exactly its two faces.

Evidence is under `C:/Users/mggar/Source/evidence/`:

- `ronan-actual-restored-focused.log`: 99 passed, zero failed/skipped. This covers
  all Ronan, ability-data, citation, beginning/lifetime, Thor engagement and
  Wrecking phase classes plus the exact inventory methods affected by the append.
  The exact filter is `ronan-final-filter.txt`; command is Content `dotnet test
  -c Release --no-restore --no-build --filter <that filter>`.
- `ronan-actual-architecture.log`: all 129 architecture checks passed.
- `ronan-actual-stun-mutant.log`: the compiling/lowered wrong-status constant
  (`cannot_be_confused` in place of `cannot_be_stunned`) fails the actual entry
  test at expected-null/actual-stunned-card. Exact production bytes are restored;
  `ronan-actual-restored-build.log` is the forced non-incremental Release build
  with zero warnings/errors before the final focused run.
- `ronan-actual-final-focused.log`: broader pre-fix evidence, 923 passed and three
  fixture assertions failed across 926 cases. The exact Hulk tail was unbounded;
  two Toughness event assertions expected `Give_Status` instead of the established
  `Enter_Play` / `toughness` marker. Those assertions are corrected without
  weakening status, engagement or completed-activation observations, and their
  complete classes pass in the restored 99-case run. The 926-case run is not
  claimed green and is not repeated after those narrow assertion corrections.

Remaining: independent review of this face, actual `90002–90005`, exact seven-copy
set composition, complete public admission and setup/save/restart evidence.
