# Independent elimination reset review

MARVEL-830 engine follow-up, reviewed on Windows in the isolated
`review/elimination-reset` tree. Base `be3bc36961a13764719a2c43f21cf3d2b953080a`
contains the support candidate and independently retained empty-deck regression.
`acda3ca1` is the equivalent cherry-pick of root repair `a6bb7ddd`; integrate the
root repair once, separately from this review's tests and evidence.

## Verdict

Accept the engine repair. The owner-based guard precedes both player-deck reset
and Invocation refresh. It preserves the actual step-4 discard and step-5 removal
events, and still allows an active owner's attachment to reset that owner's empty
deck during another seat's elimination. No new production defect was found.

Authority: `rr:player-elimination.step.4`, `.step.5`, and `.6` define elimination
and continuing participation; `rr:ownership-and-control.7.2` determines the
departing card's destination; `rr:player-deck.4` governs the active owner's reset.
The existing discarded foreign-owner Clea cases exercise the actual authored row
and declined defeat interrupt, rather than a synthetic ability substitute.

Conditional support closure: this review clears the identified elimination/reset
blocker for the support candidate on the reviewed base. It does not admit the
Doctor Strange pack, establish full content completion, or replace outstanding
integration gates and other separately required reviews.

## Independent evidence

All builds, tests, mutation and hooks ran serially within this worktree. Logs are
outside the repository under `C:/Users/mggar/Source/evidence/`.

- `elimination-recheck-focused.log`: 11 passing cases, including the retained
  empty-player-deck red regression, owner/destination controls and two new probes.
- `elimination-recheck-invocation-mutant.log`: compiled mutant bypasses the guard
  only for bound Invocation cards. New hosted-Invocation elimination test fails
  on forbidden Reset events; active-owner attachment control passes (one failure,
  one pass). The hosted test also pins RNG state/consumption, detachment, final
  removal and absence of reorder/flip events across the five physical cards.
- `elimination-recheck-restored-build.log`: restored production source rebuilt
  with `--no-incremental`, zero warnings/errors.
- `elimination-recheck-rules.log`: all 1,184 Rules cases pass after restoration,
  including rule-citation validation, Invocation lifecycle and elimination cases.
- `elimination-recheck-content.log`: 22 cases pass across the complete
  `DoctorStrangeSupportPrerequisiteTests`, `DoctorStrangeCleaCardTests`,
  `DoctorStrangeFinalSupportInventoryTests`, `DoctorStrangeFinalSupportReviewTests`
  classes and citation validation. Includes actual foreign-owned Clea with
  eliminated owner and declined interrupt, plus active-owner empty-deck controls.

No full Content suite, native UI acceptance, or second-platform run was claimed.
The two new probes are synthetic engine boundary tests, not whole-game play.
