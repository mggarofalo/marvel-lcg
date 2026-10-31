# Full healing costs â€” MARVEL-804

Candidate: `fix/full-healing-cost`, based on `c1d692a3`. Momentum Shift (09016)
remains test-local; no ability-book admission, setup or client changes are included.

The generated card requires healing two damage before its arrow. Rules Reference
1.8 initiating-abilities step 5 aborts a cost that cannot be paid; cost clause 5
requires simultaneous costs. Ruling `ecabf270719f2990` (pre-1.5, Michael Boggs)
confirms the full two-damage cost and is consistent with the current clauses.
Ordinary healing effects continue to remove only the damage present.

Offer discovery and exact payment validation require the full heal amount. A
concrete plan can use one damage-paying resource ability plus hand resources,
including a non-event spend-and-heal ability. The projection uses the live damage
cost modifier policy, forced replacement projection, Tough and every active
prevention effect. A partially prevented taking cost cannot supply damage. Optional
prevention can be declined; actual continuation and full-heal commitment remain
authoritative if intervening choices change the result.

The capability deliberately raises before payment when the missing healing would
require multiple selected resource abilities, another state-changing generator
cost, an unknown replacement, lethal generator damage, or an unknown damage replacement. It does not infer a plan by adding independently payable
generators. A currently fully payable heal does not need that future projection.
These limits are not generic payment-planning coverage.

Focused evidence (logs outside the repository under `C:/Users/mggar/Source/`):

- `full-healing-expanded.log`: full-heal tests, existing healing commitment tests
  and rule-citation gate pass (19 tests).
- `full-healing-mutation-results.json`, `full-healing-mutant-partial.log`, and
  `full-healing-mutant-prevention.log`: compiled partial-heal and ignored-prevention
  mutants fail distinguishing full-heal tests.
- `full-healing-restored-build.log`, `full-healing-restored-tests.log`: restored
  sources receive a nonincremental Release build and all 15 full-heal cases pass.
- `full-healing-regressions.log`: 90 focused tests pass, including existing damage
  cost, Backflip, Core prevention, nonattack prevention and attack-scoped prevention.
- Normal pre-commit results are recorded in the delivery handoff, with the resulting
  commit identifying the reviewed candidate.

Tests include generated Momentum Shift with zero, one and two damage, exact
selected versus merely available resources, Tough, stacked active prevention,
partial taking costs, non-event payment, ordinary healing, second-seat authority,
unsupported-projection purity, and repeated queries versus an unprobed execution
control. No native behavior or pack readiness is claimed.

## Independent engine review — 2026-10-26

Reviewed `7487dab4` against `c1d692a3`, then combined actual content commit
`4953b4804da99cce354d5474624a7aa7bd61b24f` and its independent review
`de7310c99e3b0b80c23eeaefe4b70650bb6e41b2`. The combined revision is
`e2de97e53d6f51051da47e083ba6cd59757590bf`, in isolated worktree
`C:/Users/mggar/Source/marvel-full-healing-review`. The content cherry-pick
automatically resolved the constant-card inventory test; inspection confirms
that its prior granted-Mystic review method is retained, the old three-face
slice remains bounded, and the total/Doctor Strange inventory is 658/13.

Current generated 09016, RR 1.8 cost clauses 5/11/12 and initiating-abilities
steps 3–5 agree with the full heal-two requirement. Historical ruling
`ecabf270719f2990` is consistent with those current clauses. The damage
prevention extraction retains live registration order, `Use` calls and damage
receipt semantics; the preview now applies every active prevention. The single
damaging-generator projection distinguishes dealing from taking costs and uses
the live event-damage modifier policy. Its documented unsupported projection
limits do not establish a general payment planner.

**Verdict: engine acceptance withheld.** `IndependentFullHealingCostTests`
demonstrates that `seq:[seq:[heal 1],seq:[heal 1]]` incorrectly receives an
offer with only one damage. The equivalent flat sequence correctly refuses it
and successfully pays with two damage. `Payable` passes `completeCost` to a
direct healing component, but `ExpandedCostPayable`/`SequencePayable` recurse
with the nested sequence as the complete cost. Both nested components therefore
count the same damage. Payment preparation retains the outer cost and later
rejects it, so this finding is an invalid offer rather than evidence of partial
cost commitment. Preserve the complete outer cost through nested admission (or
normalize the cost once), then rerun the unchanged reproducer. No production
code is changed by this review.

Release evidence, with full logs outside the repository:

- `full-healing-review-combined.log` and
  `full-healing-review-results/full-healing-review-combined.trx`: 97 passed,
  none failed/skipped, at the exact combined revision above. Filter:
  `DoctorStrange|AbilityData|FullHealingCostTests|RuleCitationTests` using
  `FullyQualifiedName~` for each term. Actual Momentum Shift's unchanged
  zero/one/two theory, all response/Foiled classes and both independent event
  tests run without exclusions. This resolves the earlier actual-row 14/15
  result for that content; it does not waive the new engine finding.
- `full-healing-review-adversarial.log`: the new two-case nested/flat theory
  compiles; one passes and the nested case fails at `Assert.DoesNotContain`.
  The initial raw-string compile error was corrected before this behavioral run.
- `full-healing-review-adjacent.log`: 73 passed, none failed/skipped, covering
  damage costs, Backflip, Core/nonattack/attack-scoped prevention and healing
  payment commitment. No full-suite, native or complete-pack acceptance is claimed.

Log paths above are relative to `C:/Users/mggar/Source/`. The review commit
contains the intentionally failing reproducer; acceptance requires its passing
on the corrected candidate, alongside the unexcluded combined content checks.

## Nested cost context correction — 2026-10-26

Candidate branch `fix/nested-healing-cost-context` starts at `cea71a19`, which
includes the independent reproducer and the combined engine/content review.
`AbilitySequenceCostAvailability` flattens nested sequence grouping for cost
admission after the existing Invocation expansion. It assesses the combined
resource and physical costs once, then passes that same complete cost to every
healing component. The extraction keeps `AbilityPaymentRules` below the
300-line class threshold. Selected healing validation already retains the
complete original cost through payment preparation and remains authoritative.
No Invocation, event-pricing, ability-book, content-admission or client logic is
changed; the bounded generator projection and its unsupported cases remain.

The unchanged independent nested/flat healing reproducer now passes both cases:
one damage yields no offer, and two damage pays both healing components. Added
synthetic tests establish shared resource/counter pools, repeated-query and
rejected-payment purity (including RNG and outstanding agenda work), valid
same-cost controls, and explicit refusal of unsupported combined direct damage
and insufficient healing across either grouping.

Evidence under `C:/Users/mggar/Source/`:

- `nested-healing-combined.log` and `nested-healing-results/nested-healing-combined.trx`:
  105 passed, none failed/skipped. The unexcluded filter is
  `DoctorStrange|AbilityData|FullHealingCostTests|NestedCostContextTests|RuleCitationTests`,
  using `FullyQualifiedName~` for each term. This includes actual Momentum Shift
  zero/one/two, the independent review tests, and all combined MARVEL-803 classes.
- `nested-healing-adjacent.log` and its matching TRX: 87 passed, none failed/skipped,
  covering damage costs, Backflip, Core/nonattack/attack-scoped prevention,
  healing payment commitment, distinct resources, deck/random costs,
  Spell Mastery and generic resource costs.
- `nested-healing-mutant.log` and its matching TRX: replacing the complete cost
  with the individual component compiles and fails both unchanged independent
  cases at the forbidden-offer assertion (2 failed, none passed).
- `nested-healing-restored-build.log`: exact source restoration followed by a
  nonincremental Release content-test build, zero warnings/errors.
  `nested-healing-restored.log` and its matching TRX then repeat all 105
  combined checks successfully.

The source correction awaits independent review before integration. These are
focused engine/content results; no full-suite, native or complete-pack acceptance
is claimed.

## Independent nested-cost review — 2026-10-26

Reviewed `c866c596899efd06d1f62886e2ae527a99847eeb` against `cea71a19`
in `C:/Users/mggar/Source/marvel-nested-healing-review`, branch
`review/nested-healing-cost`. The normalization preserves depth-first component
order and receives Invocation-expanded costs. Aggregate resources, counters and
healing share the normalized context; selected payment preparation retains its
original complete cost and remains authoritative. No production code is changed
by this review.

**Verdict: combined MARVEL-804 plus MARVEL-803 acceptance remains withheld.**
The nested-context delta resolves the unchanged independent reproducer, but the
combined engine regresses an existing supported cost. The unmodified
`ActionAbilityOrderedPaymentThreatRemovalTests.OrderedPaymentPreservesHealingResultsAndAuthoredPrimitiveOrder`
starts with one damage, pays `dealDamage 2` then `heal 2`, and asserts one
remaining damage plus a healed-result counter of two. It passes at pre-804
`c1d692a3`, and fails at both `cea71a19` and `c866c596` because
`AbilityHealingCostAvailability.CanPay` raises for combined damage/heal before
payment. Proving that failure predates the nesting delta does not absolve the
overall MARVEL-804 implementation. Its unsupported projection policy must not
remove this already supported cost. Repair the engine and rerun this unchanged
test; do not adjust its starting damage, expected results or inclusion.

Added six synthetic independent controls in `IndependentNestedCostReviewTests`:
flat/nested resource costs reject an insufficient selected payment even when the
full pool is available, preserving state, RNG and pending work; flat/nested
damage/heal costs in both orders remain payable when their healing was already
fully payable. Those controls distinguish ordinary costs from a missing-damage
projection, but do not excuse the regression above. The existing nested/flat
one-damage rejection and two-damage payment control remains unchanged.

Evidence under `C:/Users/mggar/Source/`:

- `nested-healing-independent-combined.log` and matching TRX under
  `nested-healing-independent-results/`: unchanged combined selection, 105 passed.
- `nested-healing-independent-final.log`: combined selection plus six new
  controls, 111 passed, none failed/skipped. The first attempt to compile the new
  controls raised CA1822 on a helper; marking that helper static resolved it.
- `nested-healing-independent-adjacent.log`: expanded selection runs every test
  in the 20 classes represented in the source adjacency evidence: 117 passed,
  one failed, none skipped. The failure is the unmodified cost-order test above.
- `nested-healing-independent-exact-adjacent.log`: the exact original method
  selection passes all 87 cases. That narrower selection does not cover the
  regression and cannot establish combined acceptance.
- `nested-healing-independent-parent-boundary.log` and
  `nested-healing-independent-pre804.log`, with matching TRX files: the exact
  cost-order test fails at `cea71a19` and passes at `c1d692a3`, respectively.
- `nested-healing-independent-restored-build.log`: nonincremental Release build
  after restoring `c866c596`; `nested-healing-independent-restored.log` repeats
  all 111 combined cases successfully, and
  `nested-healing-independent-restored-adjacent.log` repeats the exact 87 cases.

These are focused Windows engine/content checks. No full-suite, native or
complete-pack acceptance is claimed. The test expectations for the blocking
regression are preserved without exclusions or weakening.


## Ordered damage and healing repair — 2026-10-26

Candidate `fix/mixed-damage-healing-cost` starts at `92e58193`. The unchanged
`OrderedPaymentPreservesHealingResultsAndAuthoredPrimitiveOrder` remains an
acceptance requirement. Direct damage and healing costs are supported together;
they are not an unsupported projection boundary.

`AbilityHealingCostProjection` follows the payment executor's shared flattened
cost order: taken damage first, resource generation, then the remaining physical
components in authored order. Every healing component must find its full amount
at that position; the same damage cannot fund two heals. Damage uses the existing
live event modifier policy, forced replacement preview, `DamageAssignment` and
`DamagePrevention` arithmetic. A read-only snapshot of registered prevention uses
preserves remaining counts and duplicate registrations across damage instances;
Tough is consumed once. This is a projection of one cost's health components,
not an execution of arbitrary resource abilities or their effects. Live execution
still owns optional prevention, partial-taking aborts, full-heal commitment and
defeat settlement. Nested selected-payment validation uses the same flattened
complete cost, retaining the exact selected resource validation.

Synthetic controls cover both authored orders, taking-before-healing, flat and
nested damage/healing, Tough, stacked prevention, repeated instances, bounded and
already-consumed prevention, and a selected damaging generator plus direct damage.
The original regression and six independent selected-payment/already-payable
controls are unchanged. No ability data, admission or UI changes are included.

Evidence under `C:/Users/mggar/Source/`:

- `mixed-healing-combined.log` / `mixed-healing-results/mixed-healing-combined.trx`:
  1,046 passed with no failures/skips before the final three controls were added.
- `mixed-healing-mutant.log` / matching TRX: the compiled partial-healing mutant
  fails the reverse-order forbidden-offer assertion (1 failed, 4 passed).
- `mixed-healing-restored-build.log`: restored sources rebuilt nonincrementally
  in Release, zero warnings/errors.
- `mixed-healing-restored.log` / matching TRX: 1,049 passed, no failures/skips.
  The final broad selection covers whole
  cost/payment/prevention/Backflip classes, all Doctor Strange and ability-data
  classes, all healing/nested independent reviews, and rule citations. Exact
  membership is retained in the TRX. The unchanged original regression is included.

The filter uses `FullyQualifiedName~` for each of
`DoctorStrange|AbilityData|Healing|NestedCost|Payment|Cost|Prevention|Backflip|RuleCitation`.
These are focused Windows engine/content checks, not full-suite, native or pack
acceptance. Independent review is required before integration.

## Independent ordered-cost review — 2026-10-26

Reviewed `1df61febabe2c61c7b8bfa70256c99edc712fce2` against `92e58193`
and the complete MARVEL-804 chain from `c1d692a3`, in isolated worktree
`C:/Users/mggar/Source/marvel-mixed-healing-review`, branch
`review/mixed-healing-final`. No production code or ability data is changed.
The shared flattening preserves authored dealing/healing order and the existing
taking-before-resource policy. The prevention overlay follows registered use
counts, duplicate registration and Tough consumption. Actual Momentum Shift
zero/one/two cases, all combined MARVEL-803 content classes and the unchanged
`OrderedPaymentPreservesHealingResultsAndAuthoredPrimitiveOrder` pass.

**Verdict: combined engine acceptance remains withheld.** The independent
synthetic `LaterDamageRespectsProtectionEnabledByEarlierCostDamage` uses an ally
with a constant damage prohibition while it has at least one damage. Its cost is
`dealDamage(this,2), dealDamage(this,2), heal(this,3)`. The first hit enables the
prohibition, so the second supplies no damage and the full heal-three cost cannot
be paid. The candidate nevertheless offers the action: the test fails at the
offer assertion, expected false and actual true. The equivalent heal-two control
executes successfully, heals all damage and draws exactly one card. This is an
invalid offer, not proof of partial commitment: the separate forged selected
payment test rejects the heal-three case and preserves digest, RNG and pending
work. Tough plus one-use prevention consumption and stale-offer rejection after
new prevention also pass.

`AbilityHealingCostProjection.ApplyDamage` evaluates
`AbilityProgramQueries.CanTakeDamage` against the initial live world for each
instance, while only damage/Tough/registered prevention receive an overlay.
Conditional protection enabled by earlier cost damage therefore remains invisible
to the projection. RR 1.8 `rr:ability.9` makes conditional constants active whenever
their condition holds; `rr:initiating-abilities.step.5` requires aborting an
unpayable cost without payment. Correct admission must trace this changing
protection or explicitly raise for an unimplemented projection, rather than
publish a payment plan that its own validation rejects. The current documented
unknown-replacement and multiple-generator boundaries do not address this case.

Evidence under `C:/Users/mggar/Source/`:

- `mixed-healing-independent.log` and matching TRX under
  `mixed-healing-independent-results/`: fresh Release build and all 1,049
  existing checks pass, none failed/skipped. The unexcluded whole-class filter is
  `DoctorStrange|AbilityData|Healing|NestedCost|Payment|Cost|Prevention|Backflip|RuleCitation`,
  using `FullyQualifiedName~` for each term. TRX confirms the original ordering
  regression and every actual Momentum Shift zero/one/two case passed.
- `mixed-healing-independent-controls.log` and matching TRX: the five new
  independent cases compile, four pass and the conditional-protection offer case
  fails as described. Initial raw-string and unknown fixture-card errors were
  corrected before this behavioral result; the fixture uses catalogued ally 01066.
- `mixed-healing-independent-final.log` and matching TRX: the same whole-class
  filter includes all five new cases. The conditional-protection offer failure is
  retained without weakening or excluding it.

These are focused Windows engine/content results. No full-suite, cross-platform,
native or complete-pack acceptance is claimed. The review tests preserve both
the distinguishing failing offer and valid same-cost control for a separate repair.


## Evolving health-cost protection repair — 2026-10-26

Candidate `fix/healing-evolving-protection` starts at independent review
`56b70ad77aa30b856572a2637261afcabf9d9ef7`. The unchanged invalid-offer
reproducer and heal-two execution control are preserved. The engine's existing
constant evaluation now receives an immutable quantity overlay for each step of
payment. Both self protection and explicitly targeted protection share the same
expression and selector evaluators. Damage and remaining-health numeric reads,
and remaining-health ranking, use those quantities. Forced damage-replacement
window admission receives the same overlay, including its conditional eligibility;
a newly eligible unsupported replacement raises before payment.

Health changes to every cost target are traced together in payment order. This
closes a reachable cross-target error: earlier damage to the identity can enable
protection on the ally whose later damage would otherwise fund healing. Each
target retains its own Tough and registered prevention consumption. A selected
resource generator is included even when its damage target differs from the ally
being healed. Selected payment cannot bypass this trace merely because the cost
was payable with another resource plan. The hand-payment control remains valid.
No live card quantities, RNG, pending work or effect registrations are mutated by
these queries. No ability-book, admission boundary, action execution or UI change
is included.

Nine new synthetic cases cover damage/remaining-health conditions, self/titled
protection, activation followed by healing deactivation, a newly eligible unknown
forced replacement, cross-target prohibition with a valid threshold control, and
selected-generator rejection with a valid hand-payment control. An already-payable
healing cost retains support for multiple exhaustion-only generators; selected
damaging generators always enter the health projection. Existing bounded
resource projection limits remain: arbitrary replacement side effects, arbitrary
resource effects and general payment planning are not executed by this health
trace. The replacement overlay supplies health facts to existing admission; it
is not a simulated replacement resolution. No general guarantee about effects
outside these ordered health components is claimed.

Evidence under `C:/Users/mggar/Source/`:

- `evidence/healing-final-fixture-closeout.log`:
  all 14 independent plus new targeted cases pass, none failed/skipped.
- `evolving-healing-mutant.log` and matching TRX: removing projected quantities
  from the damage-permission call compiles and fails the unchanged independent
  invalid offer plus the new cross-target invalid offer (2 failed, 2 passed).
- `evolving-healing-restored-build.log`: restored source receives a
  nonincremental Release content-test build, zero warnings/errors.
- `evidence/healing-final-root-broad.log`: all 1,063 cases pass, none
  failed/skipped. The whole-class filter remains
  `DoctorStrange|AbilityData|Healing|NestedCost|Payment|Cost|Prevention|Backflip|RuleCitation`
  using `FullyQualifiedName~` per term, preserving every prior independent and
  actual-card check and the original ordered-payment regression.
- Normal pre-commit hook output is retained in `evolving-healing-commit.log`.

Initial new-fixture compilation required the existing Timing namespace, and the
protection fixture was corrected to use supported self/titled selector spellings
before final behavioral verification. These are focused Windows engine/content
results. Independent review remains required; no complete-pack, full-suite,
cross-platform or native acceptance is claimed.

Lead closeout corrects a missing JSON brace in the final multiple-generator test
fixture. The repaired fixture and all existing/new selected classes pass at the
final source tree: 14 targeted and 1,063 broad cases, no failures or skips.
Earlier worker logs precede this fixture correction and must not be cited for
those final counts. Worker validation and amendment briefly overlapped before
closeout; both completed before this final serialized edit, build, verification
and normal-hook amendment. Final hook log:
`Source/evidence/healing-final-root-amend.log`.

## Independent evolving-projection review — 2026-10-26

Reviewed `eb02527e` against `56b70ad77aa30b856572a2637261afcabf9d9ef7`
in `C:/Users/mggar/Source/marvel-healing-projection-review`, branch
`review/healing-projection`, including the complete healing-cost behavior and
actual Momentum Shift row. This review changes tests and this record only.
The immutable damage overlay reaches the existing damage/remaining-health
expressions, remaining-health ranking, explicit target protection and forced
replacement eligibility. Ordered damage on different targets shares one trace.
The previous conditional-damage reproducer, actual Momentum Shift zero/one/two
cases and unchanged original ordered damage/healing regression all pass.

**Verdict: combined engine acceptance remains withheld.** A concrete supported
cost interaction still publishes an invalid offer. The synthetic
`IndependentHealingProjectionReviewTests.EarlierCounterPaymentChangesDamageProtection`
gives ally 01066 one `test` counter and the cost
`removeCounters(this,test,1), dealDamage(this,2), heal(this,2)`. Its constant
`preventDamageWhile(this, not(countersOn(this,test) >= 1))` prohibits damage
after the last counter is removed. The heal therefore cannot be paid. Admission
nevertheless offers the action: expected false, actual true. With two initial
counters, the identical cost executes, leaves one counter, heals the full two
damage and draws exactly one card. The separate one-counter selected-payment
test rejects and preserves digest, complete RNG state and pending agenda work.
This proves an invalid offer; it does not establish partial cost commitment.

`AbilityHealingCostProjection.Apply` ignores `RemoveCounters`; its later
`WithDamage` retains the initial token map. Both
`AbilityTargetDamageProtection` and the expression evaluator therefore see the
counter that payment will already have removed. `AbilitySequenceCostAvailability`
checks that the counter exists but does not evolve its count. These are existing
supported cost and constant primitives, with a successful same-cost control,
rather than arbitrary replacement execution or a request for a general simulator.
RR 1.8 `rr:ability.9` activates constants whenever their condition holds;
`rr:initiating-abilities.step.5` requires aborting an unpayable cost without
payment. The documented non-health projection limitation does not justify a
known false offer. Trace this dependency or refuse the unimplemented interaction
before publishing the offer, while retaining existing supported legal payments.

The next repair should audit the supported payment instructions against the
facts consumed by protection/replacement admission: counter removal changes
token quantities; exhaustion variants change readiness and selected-cost facts;
discard, return and campaign removal change active sources and area membership;
resource generation and hand/deck costs can change those same dependencies.
Only the counter interaction above is a demonstrated additional blocker in this
review. This dependency list is a bounded audit guide, not a claim that every
listed interaction fails or must be executed speculatively. Any projection must
remain read-only and preserve exact selected-payment validation.

Evidence under `C:/Users/mggar/Source/`:

- `healing-projection-independent-controls.log` and matching TRX under
  `healing-projection-independent-results/`: fresh Release build, two passing
  cases and the one-counter forbidden-offer failure; none skipped.
- `healing-projection-independent-broad.log` and matching TRX: 1,065 passed,
  one failed, none skipped. The unexcluded whole-class filter uses
  `FullyQualifiedName~` for each of
  `DoctorStrange|AbilityData|Healing|NestedCost|Payment|Cost|Prevention|Backflip|RuleCitation`.
  The TRX records all three actual Momentum Shift cases and the original
  `OrderedPaymentPreservesHealingResultsAndAuthoredPrimitiveOrder` as passed.

The failing reproducer is retained without exclusions or weakened expectations.
These are focused Windows engine/content checks; no full-suite, cross-platform,
native or complete-pack acceptance is claimed.


## Counter and readiness dependency repair — 2026-10-26

Candidate `fix/healing-cost-state` starts at retained independent blocker
`9c038e3a0ab5a22c9dad2fc9bf341fd0de1a66d5`. The independent counter
reproducer, its same-cost two-counter execution control and atomic selected
rejection remain unchanged. The payment trace now evolves typed counter pools
and readiness in its immutable quantity input. Live payment and projection
share counter-pool selection, including ambiguous all-purpose-pool rejection.
Counter reads in protection conditions and forced replacement eligibility use
these quantities through the existing expression evaluator.

The bounded audit also finds a reachable readiness dependency: a forced damage
interrupt can have an exhaust-source cost. An earlier outer exhaust makes that
cost unpayable, so the interrupt must not become a projected replacement.
Source and titled exhaustion update quantities; a shared exhaustion affordability
query serves ordinary payment and projected window admission. Both source and
titled exhaustion controls execute, leave the source exhausted, fully heal the
cost damage and draw exactly one card. No rule text is reinterpreted by a second
condition or selector evaluator.

Counter removal can have lifecycle effects. The live Uses-departure predicate is
shared with this trace; native counter-operation ownership remains in its native
rule owner. Reaching the last Uses counter or a native counter transition before
a later health component raises `RulesNotImplementedException` with that precise
boundary. A Uses card retaining one counter and a native operation retaining one
infamy counter both execute successfully. Counter removal after all health
components needs no later health projection. The health trace does not mutate
cards, areas, effect registrations, pending work or RNG.

The dependency audit is deliberately bounded to this payment path:

| Cost family | Facts and admission path | Evidence or limit |
|---|---|---|
| Damage, take-damage and heal | Damage, remaining health, Tough, registered prevention, replacement eligibility | Existing ordered and cross-target controls remain; taking retains its shared taking-before-resources order. |
| Remove counters | Typed/all-purpose quantities; zero Uses can depart; native counter operations can change faces | Retained independent blocker plus repeated typed/all-purpose cycles and newly enabled unknown replacement; explicit lifecycle boundaries with legal controls. |
| Exhaust source or titled card | Readiness affects replacement arrow-cost affordability | Two distinguishing execution controls and compiled readiness mutant. |
| Exhaust chosen | Selected identities, current readiness, basic-use receipt and exhausted-for-cost facts | Ordinary affordability shares the readiness query. The health trace has no exact outer chosen-payment input and does not project its selected receipt; general chosen-exhaustion dependencies remain unproved. There is no direct Ready condition in the current expression DSL. |
| Discard source/identity, return source/titled card, consume campaign card | Active constants, area membership, incarnations, hosted cleanup and registered effects can change | Direct return-before-identity-damage/heal is a successful synthetic execution control. This does not establish conditional protection or replacement behavior after departure. No generic lifecycle/area trace or guarantee is added; those interactions remain an explicit unproved limit. |
| Spend/resources and SpendX | Hand discards, generator costs/effects, allocation and local receipts | Existing selected damaging-generator and hand-payment controls remain. Supported source exhaustion is now traced inside the selected damaging generator. The existing one-damaging-generator boundary remains; arbitrary generator effects/counter/departure costs and general payment planning are not implemented by this health trace. |
| DiscardFromHand, RandomDiscardFromHand, DiscardTop, DiscardUntilMinionEngaged, DealEncounterToYou | Hand/deck/discard membership, hidden outcomes, deck exhaustion and possible entry/reveal effects | Existing physical/selected-payment validators own these costs. General dependencies from their area changes to later protection/replacement are unproved here; no hidden outcomes are sampled. |
| ChooseFromDiscard and InvocationTop | Selection binding or invocation expansion | Existing binding/expansion owns these; this repair does not execute selection effects or change invocation admission. |

Unproved combinations in the table are audit limits, not claims that every such
combination fails, nor claims that this patch installs a universal rejection for
them. Only concrete counter/readiness dependencies and the identified counter
lifecycle boundary are changed. Existing simple return and ordinary counter
payments remain supported. This packet does not establish general area-changing
cost projection, arbitrary replacement execution, full-suite, native,
cross-platform or complete-pack acceptance. Independent review remains required.

Evidence under `C:/Users/mggar/Source/`:

- `healing-cost-state-controls-final.log` and matching TRX: 17 passed, no
  failures/skips, including unchanged independent controls and rule citations.
- `healing-cost-state-mutant-counter.log` and matching TRX: compiled unchanged
  projected-token mutant fails six cases, including the retained forbidden offer,
  repeated typed/all-purpose cases, forced replacement and lifecycle boundaries.
- `healing-cost-state-mutant-ready.log`: compiled ready-instead-of-exhausted
  mutant fails both source/titled replacement-cost controls (2 failed, 13 passed).
- `healing-cost-state-mutant-lifecycle.log`: compiled lifecycle-guard mutant
  fails both last-Uses and native-transition cases (2 failed, 13 passed).
- `healing-cost-state-restored-build.log`: restored sources receive a
  nonincremental Release build, zero warnings/errors.

Initial fixture compilation corrected a raw string and the Uses rule citation.
Native controls require explicit any-player permission and both physical faces;
the titled exhaustion fixture names catalogued Hawkeye (01066). Earlier failing
fixture runs are retained separately from final behavioral evidence.

Final restored broad evidence: `healing-cost-state-broad.log` and
`healing-cost-state-results/cost-state-broad.trx` record 1,262 passed, no
failures/skips. The unexcluded whole-class filter uses `FullyQualifiedName~`
for each of `DoctorStrange|AbilityData|Healing|NestedCost|Payment|Cost|Prevention|
Backflip|RuleCitation|RiskyBusiness|GreenGoblin`; the final two terms exercise the
native owner whose counter-operation predicate is shared. TRX confirms all three
actual Momentum Shift zero/one/two cases and unchanged
`OrderedPaymentPreservesHealingResultsAndAuthoredPrimitiveOrder` passed.
Normal hook evidence: `healing-cost-state-commit.log`.

The first normal commit hook passed all 66 architecture checks, then correctly
rejected two methods at CCN 13/11. Quantity-cost updates and health evaluation
now have separate responsibilities; window arrow/event affordability is grouped
in its own method. No complexity baseline or behavior assertion is weakened.
`healing-cost-state-final-build.log` records the post-decomposition nonincremental
Release build; `healing-cost-state-final-broad.log` and
`healing-cost-state-results/cost-state-final-broad.trx` record the final 1,262
passing cases, none failed/skipped. The normal retry hook log is
`healing-cost-state-commit-final.log`.

## Independent counter/readiness review — 2026-10-26

Reviewed `4d193dd6b3f39250f83129a2248ee4e6b9891b95` against retained
blocker `9c038e3a0ab5a22c9dad2fc9bf341fd0de1a66d5`, including the
accumulated full-healing behavior and actual Momentum Shift row, in
`C:/Users/mggar/Source/marvel-healing-cost-state-review`, branch
`review/healing-cost-state`. This review adds tests and this record only.
The prior independent counter reproducer, two-counter execution control and
selected-payment purity assertions are unchanged.

**Verdict: accept the bounded engine repair and accumulated MARVEL-804
full-healing-cost change.** No additional concrete false supported offer or
regression was demonstrated in this review. The retained one-counter offer is
absent; the identical two-counter cost executes; selected rejection preserves
the card digest, complete RNG state and pending agenda work. Counter updates
retain damage/readiness facts through later steps, and health updates retain
the projected counter pools. Typed and all-purpose removals share live pool
selection, including its explicit ambiguous-pool boundary. Source and titled
exhaustion use projected readiness when admitting replacement arrow costs.
The shared exhaustion query preserves the ordinary live affordability checks.

The physical order matches payment preparation: outer taking costs precede
resource generation; the remaining physical components preserve authored order
after flattening. Five added independent cases distinguish this order using
flat/nested costs and typed/all-purpose removal, compare repeated queries with
an execution control, and prove that removing the last Uses counter *after* all
health components still executes its live departure. The new ordering cases
would be rejected if the earlier-authored removal were projected before taking:
its zero-counter constant would prohibit the damage. Existing already-payable
multiple-generator, exact selected-generator, evolving cross-target protection,
prevention, original ordering and actual Momentum Shift controls also pass.

The precise last-Uses/native-transition refusal before later health components
is consistent with the existing unsupported-execution contract; its legal
non-transitioning controls pass. This is an immutable quantity trace through
the existing expression, selector and admission owners, without live payment,
RNG consumption or a second area interpreter. The final dependency table's
unproved general area/deck/resource combinations remain unproved; this verdict
does not claim universal projection or universal preflight rejection for them.
The genuine pre-MARVEL-804 baseline `c1d692a3` checks only positive damage for
healing affordability; an intermediate revision containing the feature is not
used as evidence that a defect is pre-existing. No newly demonstrated defect is
being waived as a projection limit.

Independent evidence under `C:/Users/mggar/Source/`:

- `healing-cost-state-independent-broad.log` and
  `healing-cost-state-independent-results/healing-cost-state-independent-broad.trx`:
  fresh Release build and all 1,262 original affected cases pass, none skipped.
- `healing-cost-state-independent-final.log` and
  `healing-cost-state-independent-results/healing-cost-state-independent-final.trx`:
  final review tree, all 1,267 cases pass, none failed/skipped. The unexcluded
  whole-class filter uses `FullyQualifiedName~` for each of
  `DoctorStrange|AbilityData|Healing|NestedCost|Payment|Cost|Prevention|Backflip|RuleCitation|RiskyBusiness|GreenGoblin`.
  TRX confirms all three actual Momentum Shift damage cases and unchanged
  `OrderedPaymentPreservesHealingResultsAndAuthoredPrimitiveOrder` pass.
- The supplied counter/readiness/lifecycle mutation logs were inspected: their
  compiled runs fail six/two/two distinguishing cases, respectively. They are
  implementation evidence, not newly executed independent mutations.

Normal review-commit hook output is retained in
`healing-cost-state-independent-commit.log`. These are focused Windows
engine/content results; no full-suite, native, cross-platform, complete-pack or
product-owner acceptance is claimed.

## Lead integration with reviewed Invocation support

The complete source/review chain through `4d193dd6` / `0cf90669` is integrated
with Master, Spell Mastery, typed set filtering and the existing Strange rows.
An intermediate cherry-pick exceeded the class-size gate before the reviewed
sequence-helper decomposition was applied. The complete chain was therefore
combined into one final integration commit, with normal hooks required; no gate
was disabled. Invocation costs still expand before sequence affordability.

The combined 1,559-case run passed 1,557 with two inventory-only failures:
the earlier search packet needed a bounded two-row slice, and the event packet
still asserted the now-authored identity was absent. Both were corrected without
removing behavioral assertions; all 20 cases in those classes then passed.
Exact logs and current inventory are in `doctor-strange-delivery-checkpoint.md`.
