# Independent review of the villain-phase beginning prerequisite

MARVEL-854. Reviewed implementation `1f916ffd1c58d2545f8cf8fd21d1e05937d84720`
against `b753c463a6baa3756a954d08db3ae40336c190d3` on 2026-10-27.
Canonical delivery scope and implementation evidence remain in
[the prerequisite record](ronan-phase-start-prerequisite.md).

Verdict: no blocking engine finding in the reviewed scope. The new beginning
occurrence precedes step-one threat, retains the scheduler's forced and optional
tiers, and has neither a player nor card subject. The existing threat frame owns
the observation markers across suspension; subsequent phase frames start fresh.
Completion observation follows the entire beginning window, including its
suspended instructions. Adapters forward presence detection without inventing a
resolving player. No production ability row, setup, admission, engagement or
Agenda implementation change is included in this review.

The source tests cover actual test-local Ronan movement, both first-player orders,
optional tiers and declines, exactly-once resumption, observer introduction and
the unchanged ordinary transcript. The independent lifetime class adds five cases:

- A synthetic completion observer chooses a hero, damages it, then discards itself
  during the start interrupt or response. Both variants reach a real threat
  interrupt with no completion frame queued. This challenges observation after
  a suspended start effect removes its source.
- Two separate step-one frames, in different rounds, each resolve their beginning
  and completion abilities exactly once and fully drain their agenda. This is an
  isolated scheduler test, not a complete two-round game.
- With two simultaneous start interrupts, the first player chooses either source.
  That source's own damage is observed before its nested choice; the other source
  remains untouched until that choice finishes. These assertions distinguish the
  selected order and complete-before-next-initiation rule (`rr:forced.5`, `.6`).

Independent evidence lives in `C:/Users/mggar/Source/evidence/`:

- `ronan-phase-review-final-focused.log`: 30 passed. Whole
  `VillainPhaseBeginningTests`, `VillainPhaseBeginningLifetimeTests`,
  `RonanPhaseBeginningTests`, `WreckingPhaseBoundaryTests`, `SetupAbilityTests`
  and `RuleCitationTests`, using Content `dotnet test -c Release --no-restore`.
- `ronan-phase-review-architecture.log`: all 129 Architecture tests passed.
- `ronan-phase-review-session.log`: all 84 Session tests passed, including the
  existing genuine archive compatibility and rejection controls. Archives and
  replay comparisons were not changed.
- `ronan-phase-review-genuine-baseline.log`: the original Sequence source from
  genuine `b753c463` passes the complete event/remaining-step signature test,
  confirming its expected hash independently of the candidate scheduler. This
  substitutes that historical procedure only; it is not a full baseline build.
- `ronan-phase-review-early-observation-mutant.log`: a compiling mutant observes
  completion before the beginning windows; four cases fail and one passes. The
  new departure cases detect a stale completion frame; the implementation's
  introduction cases detect missing response damage. These are behavior failures,
  not compilation or progress-guard failures.
- `ronan-phase-review-restored-build.log`: exact source restored, forced
  non-incremental Content build passed with zero warnings/errors. The restored
  focused run passes before adding the two independent ordering cases; the final
  30-case run includes those cases on the restored source.

The independent review did not repeat the already-recorded full Content/Rules
lanes, public setup/save journeys, native acceptance or whole-set reconciliation.
This result accepts the bounded engine prerequisite, not public Ronan admission.
