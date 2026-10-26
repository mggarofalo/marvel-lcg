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

Incomplete checkpoint: mutation proof and normal hooks are still required.
Deferred shuffle nested-activation and scheme controls, explicit Thrall Guard
blocking/unblocking after boost entry, and actual 02021 completed-attack indirect
damage deserve focused additions before independent acceptance. No whole-game,
replay/native or product readiness is claimed. The worker stops at its bounded
checkpoint and leaves the exact remaining scope to the lead.
