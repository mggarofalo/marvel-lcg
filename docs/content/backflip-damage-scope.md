# Backflip attack-damage eligibility — MARVEL-791

Base: `3684923b` (Black Widow candidate, engine replay 27). This narrow correction
uses printed `datasets/cards/cards.json` card 01003: “When you would take any
amount of damage from an attack, prevent all of that damage.”

The engine captures attack-damage provenance on the exact imminent occurrence,
reconstructed during replay (`JsonIgnore`). `damageFromAttack: "trigger"` is an
engine-chosen condition; it does not inspect `World.Attack`, ability labels, source
names or event-description strings. Explicit attack damage requests, indirect
attack assignments and overkill retain the fact. Direct boost/ability damage,
nested direct damage and damage costs cannot inherit it from an active attack.

Overkill has an exact optional prevention assignment after forced replacement,
retaining attack source/recipient accounting. RR `rr:overkill.2` calls this damage
from an attack; actual ally defeat still gates its creation. No defeat-replacement
or attack-chain semantics change. RR `rr:defend-defense.4.8` still permits generic
defense-labelled prevention outside an attack.

The audit covered every existing `WhenDamageWouldBeDealt` row: Backflip, Cosmic
Flight, Armored Rhino Suit, Crossbones' Armor, Shield Block, Wiggle Room, Energy
Barrier and Defensive Stance. Only Backflip adds an attack-source restriction.
All 645 ability rows remain; the other 644 raw rows are unchanged.

Focused evidence in `BackflipDamageScopeTests` covers actual Shocker and
Concussive Blast reveals, actual Concussive Blast boost damage before enemy
attack damage, actual attack/overkill Backflip, Energy Barrier and Defensive
Stance direct-damage controls while an attack exists, a suspended nested damage
window, a taking-damage cost and Energy Barrier overkill with unchanged attack
`DamageDealt`. Existing Spider-Man, Core damage-prevention, Shield Block and pinned
Core defeat-branch transcript checks also run unchanged in their assertions.
The synthetic remaining-damage fixture explicitly marks its Attack request's
provenance instead of deriving it from its description string.

A compiled distinguishing mutant replaces the occurrence fact with
`context.World.Attack is not null`. Four of ten Backflip scope cases fail,
including direct prevention controls, actual boost damage and suspended nested
damage. A `finally` restores the source before the passing candidate rebuild.
Logs are outside the repository: `C:/Users/mggar/Source/backflip-expanded.log`,
`backflip-mutant.log`, `backflip-restored.log` and `backflip-commit.log`.
This is focused engine evidence; broad/native candidate verification belongs to
the integrated wave run.

Independent review of `a9d871ae` against `3684923b` found no correctness blocker
in the changed occurrence ownership or optional overkill prevention. Review
traced explicit attack requests, forced replacement occurrences, the separate
taking-damage window, nested direct damage, and the attack's dealt/taken
accounting. The current RR 1.8 defeat requirement governs overkill; the Loki
replacement controls remain unchanged.

`BackflipOverkillReviewTests` adds zero-excess and Tough-before-optional-window
controls, plus a helping ally's non-first controller preventing its own spill.
Both identities' health and the recorded ally defender/target are checked.
These three cases, the ten original Backflip scope cases and eight unchanged
RR 1.8 Loki cases pass (21 total).

`OverkillPreventionReplayReviewTests` adds four cases: actual Backflip or Energy
Barrier, each taken or declined at actual overkill optional prevention. Fresh
factories and hosts reconstruct strict-JSON archives at that prompt, after
Barrier's suspended reflection choice, and after completion. Each continuation
compares events, canonical digest, all 624 RNG words, RNG index/count and durable
prompt records with the uninterrupted game; archives remain byte-identical on
restart. Full prevention leaves the original two damage dealt and ally defender
intact, while declined spill places one on its controller's identity. This uses
the existing durable factory seam and a synthetic action to request the real
attack; it is not public pack-admission or native-product evidence. No historical
fixture or architecture baseline changes are part of the review.

Review logs: `C:/Users/mggar/Source/backflip-review-content.log`,
`backflip-review-replay.log`, `backflip-review-server.log` and
`backflip-review-commit.log`.
