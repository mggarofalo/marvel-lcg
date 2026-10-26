# Safe House #29 cost prerequisite (MARVEL-769)

Bounded nonpublic engine prerequisite based on generated printed card 08005 in
`datasets/cards/cards.json`: exhaust Safe House and choose a Preparation in your
own discard pile before the arrow, then add that exact card to hand.

The typed `chooseFromDiscard` cost uses the existing exact target request and
payment validation. Its only admitted composition is source exhaustion plus
one trait-filtered selection, followed by `addToHand: chosen` in an unlabelled
Action. The choice captures the existing card/area/incarnation binding before
payment. Existing payment continuations retain it; an absent or reentered copy
is never replaced. Exhaustion settles before recovery.

Evidence in `SafeHouseCostChoiceTests` and `SafeHouseCostSuspensionTests` covers
both seats, nonfirst eligible selection, no eligible card, exhaustion, trait and
ownership filtering, missing/invalid/foreign/duplicate answers, and synthetic
externally mutated stale state. Rejected answers preserve prompt, digest, full
RNG and pending agenda, then permit a valid retry. Queries are compared with an
unprobed execution. Suspended exhaustion proves post-arrow ordering and exact
physical-incarnation retention.

The synthetic stale-state probe deliberately mutates the world after issuing a
turn prompt; the existing Game action admission then raises the specific
`no action this player can trigger` diagnostic. This is separate from ordinary
malformed answer rejection and does not claim a new stale wire-input contract.

Verification: 165 focused Content tests passed, including recipient and cost
lowering regression controls and rule citations; 129 architecture tests passed.
A compiled mutant selecting the first eligible discard instead of the supplied
selection failed both nonfirst-selection cases. Restored source received a
nonincremental rebuild and the same 165-test focused lane passed.

Full logs are external to the repository under
`C:/Users/mggar/Source/bw-safe-house-{tests,architecture,mutant,restored-build,restored-tests}.log`.
The initial mutation attempt was rejected by CA1826 before execution; only the
subsequent compiled mutant above counts as mutation evidence.

No production ability rows, setup, public pack admission, UI or replay format
changes are included. Broad integrated-wave verification and independent review
remain with the delivery lead.
