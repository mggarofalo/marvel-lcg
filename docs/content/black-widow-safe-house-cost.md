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

Independent engine review of source `2180d7b7962dd3ce21d406c5730e14111de3e773`
found no correctness defect in this bounded prerequisite. Printed 08005 and
`rr:cost.1`, `.5`, `.8` place the own-discard choice before effect resolution.
The reviewed lowering admits only the complete exhaustion/choice/recovery shape;
selection validation runs before payment, and continuation state captures the
exact selected card, area and incarnation rather than selecting again.

The review adds both-seat controller/alter-ego controls and synthetic suspended
payment probes for selected-card departure to hand, discard/hand/discard without
an in-play crossing, source departure/reentry, and a distinct accepted recovery
action while the first receipt remains pending. These synthetic tests establish
receipt isolation and continuation behavior, not a claim that another turn
Action can legally interrupt payment. Entering play creates a new incarnation;
movement only between out-of-play areas retains the physical selection.

The review's focused lane passed 39 tests (`SafeHouseCost`,
`RecipientCommitment`, `RuleCitationTests`), with log at
`C:/Users/mggar/Source/bw-safe-house-review-expanded-tests.log`.
An initial fixture incorrectly expected departure itself to increment the source
incarnation; the fixture now asserts the engine's documented entry generation
and distinguishes departure from reentry. No engine source or assertions about
the resolved recovery were weakened.

The stale-world diagnostic is not a demonstrated valid-play gap: this source has
no resource-generation stage that could legally change the discard between a
current turn offer and submission, and Game owns state changes between prompts.
The probe deliberately bypasses that owner. Ordinary malformed submissions and
the accepted payment continuation have their separate rejection/identity tests.

A durable Safe House save/restart journey remains unverified. Content tests have
no durable serializer for opaque runtime continuations; the production session
journey requires public content admission that this prerequisite intentionally
does not change. No new admission bypass or serialization mechanism is added for
this review. The integrated pack must supply that journey after full admission.
