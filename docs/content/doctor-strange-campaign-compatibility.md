# Doctor Strange candidate: Expert predecessor rejection

The unchanged `expert-campaign-91098022.zip` is rejected at the first changed
historical decision: `unit 25 decision 0 events diverged: expected 3, got 6`.
This is a diagnostic-test correction, with no runtime, compatibility route,
protocol, replay identity, schema, dataset or archive change.

The archive SHA-256 remains
`1A7B953709EE3E390863BFD28A6C920EE00B53F7F24853B197013AA72903538B`.
Its genuine predecessor production and independent verification are documented
in [the fixture provenance](../../tests/Marvel.Server.Tests/Fixtures/README.md).
The test uses the frozen content book supplied by `WithAddedScenario`, including
its unscoped Backflip; it does not substitute the candidate's authored content.

The deciding proof is journal entry 4, Crossbones attempt 1. The prefix helper
verifies initial events, RNG and digest, then every preceding prompt, decision,
event, RNG count, fingerprint and result. At unit 25 decision 0, Spider-Man holds
confusion card 99 and plays For Justice! (01060, object 25), paying with Energy
(01088, object 42), without selecting a scheme yet.

Both executions produce exactly the same first three events: reveal For
Justice!, discard Energy, and complete the payment activity. The predecessor
then asks for the effect's scheme target. The current execution instead removes
confusion, detaches its status card, and discards For Justice!, returning to the
turn menu. Threat on every card and RNG consumption remain unchanged.

The Rules Reference says “Status card abilities have timing priority over all
conflicting triggered abilities” (`rr:status-cards.2`), and the confused thwart
attempt still pays its costs (`rr:confuse-confused.5`). The replaced character
has not thwarted (`rr:confuse-confused.7`). Settling this replacement before
requesting an effect target is the engine's chosen interaction order.
`AbilityEventStatusPriority` recognizes the thwart power inside the frozen
target-choice envelope. No historical target answer is forced through the
replacement to reach a later diagnostic.

The causal control reconstructs the same verified prefix separately, removes
only confusion, then applies the same recorded action. Its three events and
next target prompt exactly match the archive. This altered board is explicitly
synthetic: it identifies the cause and is not an accepted predecessor replay.

`ExpertCampaignReplayUpgradeTests` continues to verify rejection through two
restore attempts, byte-identical source generations, unchanged canonical saves,
no published campaign or game, and no imported copy. Its unaffected initial
Absorbing Man attempt still upgrades with all original facts intact and resolves
both recovery choices. Whole-campaign acceptance is not claimed.

The later journal-13 Great Responsibility/Backflip diagnostic cannot establish
the aggregate's first divergence. Direct-damage coverage remains in
`PlayedCampaignReplayUpgradeTests.AuthenticPlayedTaskmasterAttemptRejectsDirectDamageWindowWithoutChangingHistory`,
`CoreThreatAbilityTests.GreatResponsibilityReplacesThreatWithDamage`,
`BackflipDamageScopeTests`, `DirectDamageEventProvenanceTests`, and
`SimultaneousDirectDamageTests`. The current printed Backflip scope and the
frozen compatibility-book probe have distinct purposes.

## Independent diagnostic review

Accepted at `fc140a284bf29e2835963a8935eb51c7ca067450` on 2026-10-26 for the
diagnostic correction and retained compatibility behavior. There are no blocking
findings in the two changed files. This verdict does not admit the whole Expert
campaign or establish complete Doctor Strange product acceptance.

Independent archive inspection confirms journal entry 4 is Crossbones attempt 1,
and unit 25 decision 0 records For Justice! object 25, Energy object 42, no
targets, and exactly the three payment/reveal events described above. The next
recorded prompt offers schemes 50, 77 and 63. The ZIP's SHA-256 matches the pinned
value above. The commit changes neither that archive nor any runtime source.

The current result follows `rr:confuse-confused.5` and `.7`, with status priority
from `rr:status-cards.2`. `rr:cancel.2` preserves initiation and paid costs;
`rr:cancel.3` requires a canceled event to be discarded. The payment followed by
cancellation is also consistent with `rr:initiating-abilities.step.5` through
`.step.7`. Those clauses justify the result; the exact timing of the client-facing
target prompt remains the documented engine choice.

The prefix helper verifies the original initial events, RNG and digest, then
every preceding prompt, recorded decision resolution, event list, RNG count,
state fingerprint and result. It verifies the divergence prompt too. The causal
control independently reconstructs that prefix and moves only confusion out of
its status area. It compares the complete resulting event list and next prompt
to the historical records without answering that prompt or advancing an invalid
historical step. This contrast distinguishes the cancellation cause adequately;
an additional test duplicating those assertions is unnecessary.

The independent Release run passed 13/13 cases: all four Expert cases, all seven
Played campaign cases, and the two citation checks. Evidence is retained outside
the repository at
`C:/Users/mggar/Source/strange-campaign-evidence/independent-compatibility-review.trx`.
The unchanged rejection, two restore attempts, source-generation hashes,
canonical-save equality, import refusal and both Absorbing Man recovery choices
remain asserted. The Played cases retain genuine predecessor Taskmaster
direct-damage prevention-window and current printed Backflip-scope checks.

The supplied evidence additionally records 55/55 compatibility cases, 52/52
focused current-content cases and 8/8 direct-damage rules cases; these were
inspected rather than rerun as part of this review. Its initial broader content
selection also records a failure in
`CoreThreatAbilityTests.ConfusionCancelsForJusticeAfterItsCostsArePaid` from a
stale staged-target fixture. That separate fixture correction is in `4b7d42cf`;
it is outside this diagnostic commit and is not hidden by this acceptance.
