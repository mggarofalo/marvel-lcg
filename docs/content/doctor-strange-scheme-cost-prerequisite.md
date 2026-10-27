# Doctor Strange scheme-choice cost prerequisite

MARVEL-826; candidate capability on base `fb6f6a1d`, without production 09003
admission. The generated 09003 text says “Choose a scheme → remove 3 threat
from that scheme and look at the top card of the encounter deck.” Choosing is
before the arrow, so RR 1.8 `rr:cost.1`, `rr:cost-arrow-icon` and
`rr:initiating-abilities.step.5` govern. The current vendored MarvelCDB corpus
queries 09003 and contains no entry for it. RR 1.8 `rr:confuse-confused.5`
requires costs even when confusion replaces the thwart attempt.

The new standalone `chooseScheme: "chosen"` cost uses the engine scheme query,
one mandatory activation target and existing chosen area/incarnation provenance.
Lowering admits only a single labelled Action thwart targeting that binding.
The cost has no physical mutation, but its complete answer is validated and
captured before printed event payment. Existing continuation storage owns the
binding after payment suspension; there is no recipient fallback or new choice.

The named initial red from DoctorStrangeRemainingEventGapTests was preserved at
`%LOCALAPPDATA%/Temp/AstralOriginalProbe.cs`; it demonstrates that an outer effect
choice can be skipped by confusion. AstralSchemeCostTests owns the corrected
bounded cost probes. Invalid missing/duplicate/non-scheme/departed choices
preserve digest, RNG and agenda; a same-price complete answer pays the event
and removes confusion. Normal selection removes threat from the selected side
scheme, preserving the main scheme. Query tests assert mandatory cardinality and
cost explanation. Lowering tests reject a compound cost and a non-thwart body.

Evidence: `%LOCALAPPDATA%/Temp/astral-scheme-cost-focused.log` passed 52 tests
across AstralSchemeCostTests, SafeHouseCostChoiceTests,
EventRecipientCommitmentTests and AbilityCostLoweringTests before mutation.
`astral-mutant-validation.log`: compiled validation bypass killed all four
invalid-answer probes, including an accepted non-scheme with payment.
`astral-mutant-binding.log`: removes the pre-payment binding and is checked by
the normal selected-side removal probe (one failure, seven passing controls). Restored focused evidence passed 64 tests, adding SafeHouseCostSuspensionTests,
RecipientCostSuspensionTests and RuleCitationTests. Restored sources receive a forced
nonincremental build before `astral-scheme-cost-restored.log`.

The complete printed simultaneous removal/private inspection/bonus body is
explicitly dependent on the separate labelled-power simultaneous continuation
prerequisite. These tests do not establish its behavior, both AND orders,
private inspection resumption, production setup or complete Doctor Strange pack
admission. Existing boost-inspection proof lives in Marvel.View.Tests
EncounterInspectionBoostTests and EncounterInspectionBoostReviewTests.

## Independent bounded cost review

Reviewed source `7f44364342701b62ad2fa622d24d5fa8decbc9f6` against
`fb6f6a1d`. Verdict: changes required for suspended payment target lifetime.
No production ability rows or engine files were changed by this review.

Generated 09003 and current RR 1.8 cost, cost-arrow, initiating-abilities,
confuse, thwart, ability and leaves-play clauses support the mandatory
pre-arrow choice and its exact incarnation. The existing eight cost probes
cover invalid missing, duplicate, non-scheme and already-departed choices,
same-price valid retries, confusion and a pure mandatory target description.
The independent Release run passed all 64 tests in AstralSchemeCostTests,
SafeHouseCostChoiceTests, SafeHouseCostSuspensionTests,
RecipientCostSuspensionTests, EventRecipientCommitmentTests,
AbilityCostLoweringTests and RuleCitationTests.

New AstralSchemeCostReviewTests uses the same synthetic bounded 09003 body
and a two-resource generator whose lethal cost opens a genuine optional defeat
interrupt. The unchanged selected side scheme passes through that suspension;
the confused control also pays and completes without removing threat. When the
selected scheme is moved out of play during the interrupt, resumption throws
`card ... is not a scheme Spider-Man can thwart`. If the same physical card
returns to play, with a new incarnation asserted, resumption throws
`'09003' cannot find the target of its Thwart`. Both failures occur after
payment, through AbilityRecipientCommitment.Resume's unconditional execution
of the scheme-cost body. The paid event must complete without replacing its
chosen scheme or affecting the reentered copy. These are saved red regression
tests, not an assertion that recovery is implemented.

Final bounded run: 68 tests, 66 passed and those two lifetime cases failed.
Evidence is outside the repository in
`%LOCALAPPDATA%/Temp/astral-scheme-review-focused.log`,
`astral-scheme-review-bounded.log` and
`astral-scheme-review/bounded-review.trx`. No review mutant was necessary to
establish the actual defect; the author's documented mutation results were not
independently rerun. Historical replay fixtures remain untouched.

Next action: fix the target-lifetime handling in a separate implementation
packet, then rerun the entire bounded class group with both saved failures.
Nested selections, non-first-seat composition, current public contract/client
semantics and the complete printed AND/private-inspection/boost-tail path are
not certified by this review. The complete path still requires the separate
labelled simultaneous-power prerequisite and combined proof before 09003
production authoring or admission.

## Suspended cost target lifetime repair

The bounded repair preserves the captured scheme area/incarnation through the
existing payment and power continuations. The owning scheme-cost binding check
matches the original authored node, so it does not change ordinary recipient or
discard-cost behavior. If the chosen copy departs, the normal labelled ability
still begins after costs (`rr:labeled-ability.1`, `.4`) and its existing
scheme-less thwart continuation runs independent effects. Chosen threat removal
applies nothing (`rr:ability.1`, `rr:leaves-play.1`); the paid cost never chooses
another scheme or follows the reentered copy. Confusion uses normal whole-ability
cancellation, removing the status after payment and suppressing the independent
draw as well (`rr:labeled-ability.6`).

The new chooseScheme lowering boundary now explicitly admits constant threat
removal from chosen, independent draws for you, and sequences of those effects.
Other bodies, including discarding chosen, fail during lowering before payment.
Existing DSL shapes outside this new cost are unchanged. AND and encounter
inspection require the separate simultaneous-power integration and its combined
proof; this repair does not certify the complete Astral Projection body.

Focused Release evidence passed 75 tests across the eight classes listed in the
independent cost review, including all ten suspended-payment cases. Added cases
prove independent drawing for unchanged, departed and reentered choices, and
confusion after departure cancels both effects while retaining the paid event and
resource-generator costs. The original two red lifetime tests now pass. A
compiled mutant omitting the incarnation comparison failed both nonconfused
reentry cases (2 failed, 8 passing controls). Restored sources received a forced
nonincremental Release build, then the same 75-test group passed again.
Evidence: `%LOCALAPPDATA%/Temp/astral-cost-departure-focused.log`,
`astral-cost-departure-mutant.log`, `astral-cost-departure-restored-build.log`,
`astral-cost-departure-restored.log`, and
`astral-cost-departure/departure-restored.trx`.

## Complete printed body continuation candidate

The combined candidate builds on `a341a29b`, containing the scheme-cost source,
independent cost review, target-lifetime repair and labelled-power simultaneous
source. Lowering additionally recognizes only the complete 09003 inspection
shape: chosen removal of 3 and private inspection of 1 encounter card into
`boostIconsLookedAt`, followed by chosen removal of that result. Either authored
AND pair order is accepted. The pre-existing exact chosen-discard refusal and
simple-body boundary remain. No runtime shared helper, production ability row,
public admission, Runner or agenda code changes in this extension.

AstralFullCostContinuationTests uses the generated 09003 printed body with
synthetic resource and optional-defeat-interrupt rows. Its nine suspended-payment
cases cover both AND orders for unchanged, departed and reentered selected
schemes, plus confusion for all three lifetimes. Before acknowledging the private
look, the tests distinguish inspection-first from removal-first. After payment
and complete resumption they verify the numeric bonus, independent inspection
after departure, no removal from the reentered copy, preservation of another
scheme and the main scheme, unchanged concealed encounter order, paid event and
generator cleanup, no replacement choice and a drained agenda. Confusion removes
the status after costs and suppresses both the AND and result tail. Two immediate
payment controls complete the printed body in either order. Three lowering
controls refuse an inspection of two cards, another result and a discard tail.

Release evidence: 93 tests passed across AstralFullCostContinuationTests,
AstralSchemeCostTests, AstralSchemeCostReviewTests, SafeHouseCostChoiceTests,
SafeHouseCostSuspensionTests, RecipientCostSuspensionTests,
EventRecipientCommitmentTests, AbilityCostLoweringTests,
SimultaneousPowerBoundaryTests and RuleCitationTests. A compiled mutant bypassing
scheme-cost lifetime handling only for result-valued removal failed both
nonconfused reentry orders (2 failed, 12 passing controls). A compiled lowering
mutant admitting inspection count 2 failed its precise negative control (1
failed, 13 passing controls). Restored sources received a nonincremental Release
build with no warnings, followed by the same 93 passing tests.

Evidence is outside the repository under `%LOCALAPPDATA%/Temp/`:
`astral-full-focused.log`, `astral-full-tail-mutant.log`,
`astral-full-gate-mutant.log`, `astral-full-restored-build.log`,
`astral-full-restored.log` and
`astral-full-continuation/astral-full-restored.trx`. This is implementation
evidence awaiting independent combined review; it does not establish production
09003 authoring, setup, public admission or complete Doctor Strange delivery.

## Independent combined continuation review

Accepted the bounded prerequisite at source
`b7b69388214d58b448354140df896947b31de9e8`, including the scheme-cost
source `7f443643`, saved review regressions `a2e7d9a3`, departure repair
`76c5c65a`, and simultaneous-power source cherry-picked as `a341a29b`.
This review changes tests and this evidence record only.

Generated 09003 supplies the mandatory pre-arrow scheme choice, the independent
removal of 3 and private encounter inspection, and the numeric boost-icon tail.
Current RR 1.8 cost-arrow, AND, labeled-ability, side-scheme and leaves-play
clauses support the tested ordering and incarnation semantics. The vendored
MarvelCDB FAQ records 09003 as queried with no entry. Inspection of lowering,
cost selection/validation, pre-payment binding, resumed commitment and both
removal paths found no production correctness defect within this bounded shape.
The existing exact-discard and inspection-count-2 refusals remain intact.

New `AstralFullCostReviewTests` adds twelve independent cases. Both authored
pair orders and both player-selected execution orders preserve inspection when
the first removal defeats the selected side scheme. Departure/reentry during
the private look itself preserves the new copy's threat, including the
inspection-first path where both removals remain pending. The tests also retain
the unrelated side and main schemes, concealed encounter order, paid event and
resource cleanup, no replacement choice and a drained agenda. Four lowering
controls reject a first removal of 4, either removal targeting the main scheme
instead of chosen, and an unrelated result-valued tail.

The combined Release group passed 105 tests across the implementation class
group listed above, the new review class and rule citations. Independently
compiled mutants establish the new checks: ignoring the incarnation comparison
failed all four reentry-during-look cases with eight controls passing; permitting
any constant in the AND removal failed only the removal-of-4 admission control
with eleven controls passing. Both production files were restored, a forced
nonincremental Release build passed, and the complete 105-test group passed again.

Evidence resides outside the repository in `%LOCALAPPDATA%/Temp/`:
`astral-full-independent-baseline.log`,
`astral-full-independent-lifetime-build.log`,
`astral-full-independent-lifetime-mutant.log`,
`astral-full-independent-gate-build.log`,
`astral-full-independent-gate-mutant.log`,
`astral-full-independent-restored-build.log`,
`astral-full-independent-restored.log` and
`astral-full-independent/independent-restored.trx`.
Acceptance covers this engine prerequisite; production 09003 authoring,
public admission, complete pack delivery and native interaction remain later work.
The separate Rhino-stage path is outside this combined review.

## Combined integration

The complete source/review chain (`7f443643`, `a2e7d9a`, `76c5c65a`,
`b7b69388`, `5ed56abe`) is combined with independently reviewed simultaneous
powers, Magic Blast stage continuations and reveal-origin handling. All 1,178
selected Content cases pass, including complete cost/payment classes, Astral,
Strange, Magic Blast, Hammer Throw, reveal-origin, ability-data and citations.
Log: `Source/evidence/strange-astral-magic-cost-integration.log`.
The documentation append conflict retains both reveal-origin and choose-scheme
contracts. No production card row or public admission is added by this packet.