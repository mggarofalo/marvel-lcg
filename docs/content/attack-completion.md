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
Its interpretation is tracked by MARVEL-640 under Angel-pack MARVEL-599; this ledger does not assert
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

## Checkpoint evidence and remaining acceptance

Checkpoint `951e3e110191a55dcdb90f8478755842792745d7` passed 2,698 Content tests,
1,000 Rules tests, 129 architecture tests and commit hooks. This is evidence on
the isolated Captain branch, not the final combined delivery.

Thirteen compiled mutations were rejected by distinguishing assertions:

| Mutation | Failing cases |
|---|---:|
| Merge native and authored spent identity | 1 |
| Merge native and authored resolution identity | 1 |
| Demote native forced-response priority | 4 |
| Ignore basic-only ranged scope | 1 |
| Omit generic selected-recipient capture | 3 |
| Resume enclosing effects before attack responses | 2 |
| Retain an expired end-of-attack grant | 1 |
| Accept another execution's owner | 1 |
| Accept an unstarted owner | 1 |
| Defer completed event discard past responses | 9 |
| Omit retaliation when attack damage is zero | 1 |
| Retain duplicate physical attack recipients | 8 |
| Bind indirect retaliation to the hero despite an ally defending | 1 |

The zero-damage case has an actual admitted predecessor reproduction. Original
runtime `91098022d37ce739dd2880e8964b40ee3675eeb3` generated Black Panther against
standard Rhino with game seed 700 and policy seed 13. Its 30-decision journal
replays without divergence on that original runtime. At decision 9, the identical
Defense answer produces 18 old events versus 19 new events: the corrected
completion path deals 1 retaliate damage after `AttackCompleted`, although the
defended enemy attack dealt zero damage. The board digest consequently changes.
The predecessor journal's SHA-256 is
`f9107037687344f2cfde00005e3acc2a5494894b1f905c0ef8e94aae1b0e7be1`.

The integration candidate is based on PR-434 candidate `9097da07`. Replay v21
admits explicit frozen-content v19 and v20 histories only after complete original
verification, and retains the same-contract additive-content policy. Genuine v20
host artifacts distinguish the unchanged played campaign from the affected Black
Panther session; see [replay evidence](../../tests/Marvel.Server.Tests/Fixtures/replay-v21-evidence.md).
Final combined regression and independent review remain acceptance requirements.
Default serialization omission does not establish replay compatibility.

Multi-villain stage-lineage integration belongs to MARVEL-638 and must verify
that a departed recipient cannot bind to another group's identically titled
villain. Captain America remains unadmitted.

The v21 transition has five additional compiled mutation controls: remove source 19
admission (one failure), remove source 20 admission (one), remove additive 21
admission (one), accept an unapproved destination (three), and skip original
session-history verification (both genuine predecessor Black Panther cases fail).
The combined candidate on `9097da07` passes the complete 4,240-test unit solution,
including 2,747 Content and 1,003 Rules tests. Final integration and delivery evidence
is tracked in MARVEL-635; these checks do not admit Captain America content.
