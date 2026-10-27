# Independent simultaneous power review (MARVEL-827)

Reviewed source `2461f134d4f23859977c9885f8e71edfaf2879bb` against base
`fb6f6a1dc2b54b174214a3a3ea078c4d504996f7`. Review branch
`review/simultaneous-power` contains only independent tests and this record.

Verdict: the bounded simultaneous mechanism passes its exercised controls, but
actual Magic Blast has an ordinary villain-stage promotion blocker. Neither
complete 09004 coverage nor Doctor Strange pack readiness is established.

## Blocking reproduction

`SimultaneousPowerReviewTests.ActualMagicBlastFinishesSeparateDamageAndStatusesOnTheSameTitleNextStage`
uses the test-local actual Magic Blast row, a wild discarded card, actual Rhino I
with 10 damage, and actual Rhino II in the villain deck. Both selected orders
raise at the enemy-selection answer:

> villain stage '01095' has an unprojected When Revealed ability before a labelled-power continuation reads the board

The same fixture with undamaged Rhino I passes both orders: 7 damage, stunned,
confused, Rhino II remains in its deck. The blocker is an explicit unsupported
continuation, not a missing offer due to invalid payment or target preparation.
The red tests remain assertions of the required behavior, not exception controls.

Current RR 1.8 `rr:villain-defeat.2` forbids excess first-instance damage carrying
over. However, `.3.1` considers a same-title successor the same character for card
abilities. Rhino II must therefore receive the separate conditional 2 and both
wild status branches after its reveal. General leave-play no-memory rules do not
override this specific villain rule. The review corrected its initial stage-tail
expectation after reading this clause; the observed unsupported-stage failure is
unchanged.

## Passing independent evidence

- Actual Norman Osborn / Criminal Enterprise, five infamy, wild Magic Blast:
  both orders flip the same physical villain to Green Goblin, finish the actual
  reveal allocation, then deal the separate 2. One attack start and end remain.
- Serialize all continuation decisions to JSON, finish the original execution,
  create a fresh deterministic Norman board, reconstruct the pending order and
  replay the restored decisions. Both orders reproduce the full typed event
  trace, final digest, RNG word count, and completed agenda.
- Synthetic adversarial departure/reentry of the selected minion during the
  pending order: both orders retain incarnation provenance and apply neither
  attack damage nor statuses to the returned physical card.
- Existing actual resource-presence, wild, reset, modifier, causal metadata and
  bounded unsupported-tail tests pass in the focused combined run.

The replay proof exercises serialized engine decision reconstruction. It does
not claim hosted session-save admission for the unadmitted Doctor Strange pack.
Actual Astral Projection cost integration remains separately owned and untested.

## Authority and repair boundary

Generated 09004 and 09003 texts, current `rr:and`, `rr:damage`, `rr:event`,
`rr:leaves-play`, and `rr:villain-defeat` were read directly. Historical ruling
`e4ea1bf855e41aac` is pre-1.5 but its separate-instance Norman answer remains
consistent with current step-3 replacement and placement. Historical fixtures
are unchanged.

`AbilityCardReference.Resolve` already follows same-title villain successors and
rejects a changed incarnation. The current failure originates from
`AbilityPowerVillainProjection.StageEntryReadFailure`, then unconditional
`PowerPaths` consumption in `AbilityPowerSuspension.PowerSequenceChildren`.
Reviewed Hammer Throw tests cover actual Rhino reveal/search and reject
nonterminal state-dependent continuations. `AbilityStageEntryDependencies`
admits only a proven stable upgrade return; it does not justify broadly ignoring
stage reveals for conditional attack damage and status tails. Repair needs a
specific dependency analysis retaining those negative controls and current
same-title/different-title semantics.

## Run evidence

Logs outside the repository under `C:/Users/mggar/AppData/Local/Temp/`:

- `simultaneous-review-first.log`: 4 pass, 2 stage failures.
- `simultaneous-review-reentry-final.log`: 2 reentry cases pass.
- `simultaneous-review-final.log`: 27 pass, 2 actual stage blockers fail;
  includes source actual Magic, simultaneous boundary and rule-citation tests.
- `simultaneous-review-corrected.log`: repeats the final focused suite with
  corrected same-title successor assertions; see its summary for final counts.
- `simultaneous-review-commit.log`: normal commit hooks, without bypass.

No production source/book changes or new compiled mutants were made in review.
The source packet's previously recorded three mutant kills are author evidence.
Full Content and broader integration lanes were not run because this review
preserves a concrete blocker for the implementation owner.

## Stage-continuation repair candidate

The repair based on review revision `7e754ebc3b243c13fd427219d32a13b314fc4bec`
adds a bounded suspension proof for a simultaneous chosen-recipient constant
attack-damage / own-top-card-discard pair followed only by constant chosen
attack damage, chosen status, or conditions over the retained discard-resource
receipt. Both resource-condition branches must have that same closed shape.
These instructions execute after live damage settlement; admission does not
predict the stage's search, reveal, hidden deck or subsequent board state.
The existing damage procedure owns any reveal suspension, and
`AbilityCardReference.Resolve` owns incarnation and same-title stage resolution.

The opaque stage-entry and Hammer Throw dependency guards remain in place.
A board-dependent condition followed by otherwise supported chosen attack damage
is explicitly rejected before cost, alongside the existing draw-tail control.
The production ability book, runner and scheduler are unchanged.

The actual Rhino II proof now includes Breakin' & Takin' in the encounter deck,
asserts its revealed three threat precedes the successor's separate damage,
and retains one attack identity. Both selected orders finish with two damage,
stunned and confused on Rhino II. The serialized-decision proof additionally
rebuilds this deterministic promotion board from scratch and compares every
typed event, final digest, RNG word count and completed agenda for both orders.
Existing Norman flip/reveal and minion departure/reentry controls remain covered.

Repair evidence is outside the repository under
`C:/Users/mggar/AppData/Local/Temp/`, with the `magic-stage-` prefix:

- `mutant-gate-build.log` / `mutant-gate.log`: removing the live receipt-tail
  recognition compiles and fails four Rhino promotion/reconstruction cases.
- `mutant-condition-build.log` / `mutant-condition.log`: admitting arbitrary
  tail conditions compiles and fails the board-read attack-tail negative control.
- `restored-build.log` / `restored-final.log`: non-incremental restored build
  and the complete affected Magic Blast, simultaneous-power, Hammer Throw and
  rule-citation classes: 85 passed, zero failed. Focused Lizard CCN-10 and
  `git diff --check` pass.

This is a repair candidate for independent review. It establishes neither
production 09004 admission nor complete Doctor Strange product readiness.

## Independent stage-repair review

Reviewed repair `231670c7c223867c61689894372677a54b2c7e2a`, including source
`2461f134d4f23859977c9885f8e71edfaf2879bb` and review
`7e754ebc3b243c13fd427219d32a13b314fc4bec`. Verdict: accept the bounded engine
prerequisite and stage-continuation repair. This supersedes the ordinary Rhino
promotion blocker above; it does not establish production card or pack admission.

The new suspension recognition accepts the same already bounded damage/discard
pair followed only by fixed chosen damage/status instructions and Boolean
combinations of discarded-resource tests. Both conditional branches are checked.
It does not predict the unknown reveal or permit a later board/deck-dependent
condition. The existing stage-entry dependency guards and incarnation-bound,
same-title villain reference resolution remain unchanged. The retained discard
receipt determines only the live tail, so this repair adds no hidden future
identity or pile-order facts to admission or prompt descriptions.

`MagicBlastRevealBoundaryReviewTests` independently adds the missing actual
choice boundary: Norman with five infamy, the actual printed Goblin reveal,
and an actual Spider-Woman ally make indirect-damage allocation require an
answer. With only the hero that allocation can finish automatically. In both
structural orders, the pending allocation has no later damage/status tail and
no attack end. Repeated queries preserve the prompt, digest, complete RNG state,
agenda and typed events. An incomplete allocation is rejected without changing
that snapshot; a valid allocation then applies exactly the separate 2 damage,
stun and confuse and ends the original single attack once.

Independent logs are outside the repository in
`C:/Users/mggar/AppData/Local/Temp/`, prefixed `magic-independent-`:

- `boundary.log`: both actual allocation-boundary cases pass.
- `mutant-build.log` and `mutant.log`: suppressing incomplete damage-procedure
  suspension compiles, then both new cases fail because unfinished recipients
  prevent continuation completion. This is a killed execution-boundary mutant.
- `restored-build.log` and `restored.log`: sources restored and rebuilt
  nonincrementally, followed by complete Magic Blast, simultaneous-power,
  Hammer Throw, labelled defeat continuation, stage-entry continuation and
  rule-citation classes: 96 passed, zero failed or skipped.
- `commit.log`: normal commit hooks, without bypass.

The candidate's actual Rhino search/reveal chronology, same-title successor
damage/statuses, serialized fresh-board replay, different minion incarnation,
and unsupported arbitrary-tail controls are included in that restored run.
Full Content, hosted save admission, complete Astral cost integration and pack
acceptance remain outside this independent engine review.

Integrated with reveal-origin authority and the accepted Black Widow baseline:
all 237 selected Strange, Magic Blast, simultaneous-power, Hammer Throw,
reveal-origin, ability-data and citation cases pass without failures or skips.
Log: `Source/evidence/strange-magic-reveal-integration.log`. Sources `2461f134`,
`7e754ebc`, `231670c7` and independent repair review `4d54f799` are preserved.
Actual production event rows and complete pack proof remain open.