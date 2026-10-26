# Remaining damage interrupt eligibility (MARVEL736)

Based on 0d2a33ab. Printed 05017 Energy Barrier and raw vendored faq:05017
establish that copies resolve individually and cannot trigger after no damage
remains. rr:prevent.1 and .1.1 preserve the distinction between damage dealt and
damage taken. This packet changes engine eligibility and preflight only; the
05017 interpretation is injected as a test literal, with no ability-book,
generated-data, public-admission or UI changes.

The earlier external repro under `ms-defense-energy-barrier-repro` omitted
`World.Activation`. `AttackCompletion.Over` treats a missing activation as ended,
so that fixture could offer interrupts but could not establish attack placement
or prevention consumption. It is not production defect evidence. The committed
fixture supplies an actual enemy activation and the real DealAttackDamage stage.
With the complete residual guard compiled to return true, this corrected fixture
fails four of six cases while both two-damage positive controls pass. The same
fixture passes with the candidate guard.

Rules binds each prepared replacement to its owning occurrence. Optional card
interrupt discovery and preflight inspect its current amount minus pending
recipient prevention without spending prevention or rewriting the dealt amount.
The existing window and placement procedures retain their normal lifecycles.
Unknown-amount manually constructed test occurrences retain their existing
behavior. The context is transient and rebuilt by replay; no wire shape changes.

`RemainingDamageInterruptTests` covers zero, one and two calculated damage, both
seats, foreign-controller exclusions, exact counters, the next damage instance,
query card/RNG/pending/effect stability, invalid-answer retry and stale preflight
cost protection. Rhino I has printed ATK 2, so the one-damage case distinguishes
the stored reduced amount from printed attack. The actual activation damage
receipt remains the original dealt amount while identity damage is zero.

External logs under `C:/Users/mggar/Source/`:

- `marvel736-baseline-mutant.log`: compiled complete residual guard bypass,
  four expected failures and two passing controls.
- `marvel736-admission-mutant.log`: compiled discovery guard removal,
  four expected failures and two passing controls.
- `marvel736-preflight-mutant.log`: compiled preflight removal, stale answer
  fails its expected rejection; five other cases pass.
- `marvel736-{admission,preflight}-restore-build.log`: forced Rebuild after
  byte-exact source restoration in finally blocks.
- `marvel736-final.log`: 80 passing restored focused tests, including ordinary
  Backflip, Shield Block, Wiggle Room/Morphogenetics, indirect damage, forced
  replacements and rule citations.

No public pack journey, native interaction, save/restart journey or complete-game
acceptance is claimed. Independent engine review and integration-wide managed
verification remain the delivery lead's next steps.

## Independent engine review

Reviewed production revision `2da3094e750c6035ce3e66fb5af364dc6cfdec41`
in the separate `marvel-residual-review` worktree. Printed 05017, raw vendored
faq:05017, rr:prevent.1/.1.1, rr:interrupt.3.1/.4 and
rr:replacement-effect.1 were read directly. No production changes were made.

`RemainingDamageReviewTests` contributes six synthetic engine cases. Partial
recipient prevention leaves an optional healing effect available; another
recipient's prevention cannot suppress it. Full prevention suppresses the
synthetic **would-take** trigger even though its effect is healing rather than
prevention. A mandatory step-one ability still resolves with a full pending
prevention shield. These synthetic rows do not admit any card.

The stale-preflight case records the card digest, complete RNG words/index and
consumption count, pending agenda, stage, open window and registered effects
before rejection, then verifies unchanged values and costs. Declining the same
prompt successfully completes the damage stage and an actual
`AttackCompletionOperation`: `AttackCompleted` is emitted, the active attack and
activation are cleared, and the finished activation retains dealt 1 / placed 0.
This independently distinguishes real completion from an absent activation.

All twelve residual-damage cases passed. Two independent compiled mutations each
failed one of the six review cases: removing the recipient filter wrongly
suppressed the foreign-shield control; removing the preflight guard accepted the
stale optional answer. Sources were restored byte-for-byte in `finally` blocks,
followed by a forced Release Rebuild and fourteen passing residual/rule-citation
cases. Logs outside the repository:

- `C:/Users/mggar/Source/marvel736-review-baseline.log`
- `C:/Users/mggar/Source/marvel736-review-target-mutant.log`
- `C:/Users/mggar/Source/marvel736-review-preflight-mutant.log`
- `C:/Users/mggar/Source/marvel736-review-restore-build.log`
- `C:/Users/mggar/Source/marvel736-review-final.log`

**Independent verdict: request changes.** `Steps.DamageWouldBeDealt` is documented as step-one
**would-be-dealt**, while 05017 is **would-take**. Prevention does not reduce
dealt damage under rr:prevent.1.1. Thus the present broad optional gate is
supported for the would-take prevention cards examined here, including the
synthetic would-take healing control; neither that control nor faq:05017 proves
that a true would-be-dealt optional ability should disappear.

The independent `WouldDealOptionalTriggerStillExistsWhenOnlyTakenDamageIsPrevented`
counterexample uses a synthetic optional **actor:you** would-deal trigger with a
healing effect: the hero deals 1 to a villain whose next taken damage is prevented.
The replacement still reports dealt 1, yet discovery returns no optional ability.
It compiles and fails `Assert.Single` on the empty collection against the reviewed
production revision; see
`C:/Users/mggar/Source/marvel736-review-dealer-counterexample.log`.
The regression is committed as a failing test, without skipping or weakening it.
The fourteen passing cases above preceded this added counterexample and do not
establish a green final candidate.

The dealer-side family is documented as unused by the currently admitted pool;
that limits immediate card exposure, but does not justify altering its contract.
Introduce explicit would-take trigger eligibility while preserving dealt semantics,
or use a narrower prevention-body eligibility guard if the engine owner establishes
that interpretation from authority. Resolve this distinction before approving the
general engine capability; this review does not accept the broad guard merely to
admit Energy Barrier.
