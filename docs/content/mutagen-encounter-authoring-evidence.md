# Mutagen encounter authoring checkpoint

Base: `9087b5689625325ed70afe37a0f04c4a9dcd721f`. Worktree:
`C:/Users/mggar/Source/marvel-lcg-mutagen-encounter`.
Seven research rows 02019–02025 are authored. This makes 50/57 distinct Green
Goblin faces authored; 02026–02032 remain fail-closed and public admission remains
closed. No engine, setup, generated printed-card or public admission change.

Authority: `datasets/cards/cards.json`; raw quantities remain in the pinned
`datasets/marvelsdb/pack/gob_encounter.json`. Glider/Bombs printed text, numeric
attributes and star annotations match 02033/02034. Exact offline FAQ lookup for
02019–02025 found no entries. Relevant vendored ruling ids are recorded in
`datasets/abilities/UPSTREAM.md`.

Focused Release Windows selection passed 64 tests, zero failed:
MutagenEncounter, MutagenHysteria, GoblinGimmicksAttachment, AbilityData,
ABoostAbilityNobody and RuleCitationTests. Log:
`%TEMP%/mutagen-encounter-focused.log`. Initial test output accidentally used the
project default TRX directory; that sole generated TRX was moved to
`%TEMP%/mutagen-encounter-initial.trx` and the empty directory removed.

Actual authored-card tests exercise full reveal entry, exact-copy/player-one
boost entry, deferred exact-source shuffle after attack, Knight discarded Goblin
recruitment across exhaustion, non-Goblin/old-discard controls, Soldier defeat,
Monster stunned branches, Hysteria attack and scheme extra boosts with two copies,
and reprint attachment ranking/surge, removal costs and form restriction.
Hysteria uses separate printed attack/scheme trigger clauses because ordinary
attack occurrences name the enemy actor, while scheme initiation names it as
subject. Both paths pass without engine changes.

The first content commit `b5d9fe31` passed normal hooks: 66 architecture checks,
complexity and file length. The continuation adds actual 02021 completed-attack
response with cross-player defense and stunned cancellation; actual 02024 boost
entry followed by Guard legality and departure restoration; and actual 02022/02025
scheme boosts with exact-source shuffle and expected threat.

Final focused restored selection: 89 passed, zero failed, Release Windows.
Log: `%TEMP%/mutagen-encounter-final-proof.log`. This includes ability-data and
citations. A compiled actual 02022 mutant changes the discard count from one to
two. The non-reset recruitment case fails because two encounter cards remain
instead of one; the reset case passes. The same restored cases are included in
the 89 passing tests. Mutant log: `%TEMP%/mutagen-encounter-mutant.log`.

## Queued activation proof

The corrected actual-card probe uses Hysteria to reveal Hydra Jet-Trooper and
Knight/Monster during one activation. At the target player's second defense
prompt, exactly one attack has completed and the exact Knight/Monster source is
already back in the encounter deck; its other physical copy remains discarded.
Both completed attacks target player one. This proves the source shuffle belongs
to the containing activation and precedes the queued activation.

An earlier probe counted defense prompts from both players as separate attacks;
its reported timing gap was a fixture mistake, not an established engine defect.
The corrected probe is executable in `MutagenEncounterTests`. The final restored
selection passes 91 tests, zero failed. Log:
`%TEMP%/mutagen-encounter-corrected-proof.log`.

Public admission stays closed. No whole-game, replay/native or product readiness
is claimed. Remaining 02026–02032 authoring and whole-product proof are separate.
