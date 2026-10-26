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
