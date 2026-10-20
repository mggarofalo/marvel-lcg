# Attack completion authority and acceptance ledger

MARVEL-635 is a prerequisite of Captain America (MARVEL-560), not pack admission.
MARVEL-639 records the duration and delayed-effect review findings.

## Authority reconciliation

The runtime's general authority is Rules Reference 1.8, harvested 2026-08-24.
`rr:attack-player-ability-type.step.7` places forced after-attack abilities,
including retaliate, in a shared ordering window. `rr:retaliate-x.1` treats the
keyword as a Forced Response. `rr:delayed-effect.1` instead gives delayed effects
priority before responses; clause 1.1 gives them constant-effect priority.
`rr:lasting-effects.5` expires a duration at its named timing point.

These are three distinct responsibilities: expiration, delayed resolution, and
forced-response ordering. A native response does not retain an expired modifier.

The vendored May 2025 observation `ruling:0b5a58ddd0b04465` describes Black Widow's
A.I.M. Commando preparation delayed effect **after the attack**, and allows its
owner to order it with retaliate. It does not discuss Charge's **at the end of
this attack** instruction. It has no modifying relationship in
`datasets/rules-graph.json`. Under `docs/rules-provenance.md`, it does not replace
the general RR 1.8 delayed-effect priority. This implementation preserves that
priority; it does not claim implementation of the unadmitted preparation effect.

A recursive inventory of every object and array in the authored ability book
finds five `delayUntil` producers:

| Face | Card | Timing |
|---|---|---|
| 01099 | Charge | At attack end, discard itself |
| 01106 | Stampede | Damage dealt during this attack, stun damaged character |
| 01168 | Sweeping Swoop | Damage dealt during this activation, stun damaged character |
| 01084 | Nick Fury | At round end, discard himself |
| 04013 | Goliath | At player-phase end, discard himself |

Charge is the sole admitted attack-completion delayed producer. Its actual
authored discard must precede Black Panther's native retaliate response.
The generic synthetic duration controls distinguish an explicit **until end of
attack** grant, an **until end of phase** grant, and a persistent grant.

Aerial Agility 42004 instead says **for this attack** and grants retaliate in
Archangel form. `ruling:ef77ad713fff71d9` (October 2023) confirms it creates a
lasting effect but does not independently specify its expiration boundary.
Neither that card nor a `for this attack` retaliate duration is admitted here.
Its interpretation belongs to Angel-pack acceptance; this ledger does not assert
equivalence to the explicit `Until: EndOfAttack` representation.

## Containing abilities and event discard

`rr:event` and `rr:initiating-abilities.step.7` discard an event after its effects
resolve. A finished selection wrapper has no remaining effect. A terminal
attack event therefore completes its discard before its after-attack response
window. The Uppercut/Superhuman Strength canonical transcript retains its exact
discard-order assertion.

When another effect remains in an enclosing ability, that continuation waits
for the nested attack's response window. The engine retains the exact owner
frame and occurrence, and the continuation cannot run while that owner remains.
It rejects foreign, stale, unstarted, wrong-owner and same-origin/window aliases.
No exemption bypasses ordinary invocation child-work or window prerequisites.

The native and authored abilities have distinct engine identities even when
they originate from the same physical card. Their affordance handles are
assigned together, and both resolutions remain separately recorded.

## Evidence still required before acceptance

The work remains unaccepted until focused controls, independent review, compiled
mutations, complete regression suites and dedicated admitted retaliate replay
comparisons are recorded against a committed candidate. New response order and
native resolution facts require explicit replay-version assessment; default
serialization omission alone does not establish compatibility.

Multi-villain stage-lineage integration belongs to MARVEL-638 and must verify
that a departed recipient cannot bind to another group's identically titled
villain. Captain America remains unadmitted.
