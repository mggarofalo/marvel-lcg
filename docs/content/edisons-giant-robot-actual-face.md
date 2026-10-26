# Edison’s Giant Robot actual face (MARVEL-734)

Base: `619992df` (reviewed blanking prerequisite integrated from `25c9b453`).
Actual row `05028` is appended to the book: 573 unique authored faces, 31 of 34
Ms. Marvel faces. The complete prior book prefix remains byte-identical through
its final row; only its closing list delimiter is extended. Explicit authored-face
inventory adds only `05028`. No generated facts, engine or public admission change.

Printed authority is `datasets/cards/cards.json`: damage cannot be taken; Hero
Action spends one mental resource to blank the printed text box until phase end.
Vendored `ruling:d1087a7e030d2fd9` confirms blanking applies to all players.
`preventDamageWhile` expresses the unconditional prohibition; the independently
reviewed standalone `blankPrintedTextUntilEndOfPhase` expresses its action.
The interpreter’s true condition and opcode names are engine representation choices.

`PrintedTextBlankingTests` reads the unmodified full ability book. Both seats can
pay for the Robot engaged with seat zero. Damage is actually prevented before
payment, accepted from the other seat after payment, then prevented after expiry.
Both seats lose the action while blanked. ROBOT, ATK 2, SCH 1 and HP 8 remain.
The blank survives turn end; phase end restores immunity and action offers.
A separate `Game.Resolve` path commits the actual action, ends the turn, resolves
the phase discard decision and proves scheduler restoration with a live next prompt.
Alter-ego cannot use the action. Wrong energy payment preserves both payment cards,
card digest, pending agenda, registered and active effects, and RNG after repeated
offer queries. Actual-row reentry tests retain the existing independent lasting-effect
control. Synthetic variant bodies in lifecycle and preflight review tests remain
synthetic and retain their separate controls; none append a duplicate actual row.

Verification, Windows Release:

- Initial Cards + blanking + citation filter: 179 pass, before the added scheduler case.
- Actual-face class with scheduler case: 7 pass.
- Data mutant changing only actual `05028` cost from mental B to energy Y: 4 fail,
  3 pass. Both seat payment paths reject the wrong mutant cost; the energy-negative
  test fails because energy becomes accepted. The scheduler payment also fails.
- The original data is restored byte-for-byte, then the Content project is rebuilt
  nonincrementally. Final Cards + all blanking + citation filter: 180 pass, zero fail.
- Full Content and combined integration lanes remain lead-owned and were not run here.

External logs under `C:/Users/mggar/Source/`: `robot-actual-focused.log`,
`robot-actual-scheduler.log`, `robot-actual-mutant.log`,
`robot-actual-restored-build.log` and `robot-actual-restored.log`.
Independent actual-row content review remains required. Ms. Marvel public admission
stays closed; the remaining unauthored faces are 05014, 05018 and 05029.

## Independent actual-face review

Reviewed `daff354e98eb64052298e2268f0e156cd4e77386` in the isolated
`marvel-robot-05028-review` worktree. Raw generated `05028` text and vendored
`ruling:d1087a7e030d2fd9` agree with the authored damage prohibition, mental
cost, Hero Action form restriction, whole-box blanking, shared effect and phase
expiry. Existing tests distinguish both seats, energy refusal, actual damage
prevention, phase expiry and leaving/reentering play. The independent
`RobotActualReviewTests` adds a wild-resource payment through `Game.Resolve`;
it checks payment discard, enabled damage and removal of the blanked action.

Production finding: the actual `Game.Pending` action has a null `Description`.
The engine does not explain blanking or its phase duration. An executable probe
requiring the words `blank` and `phase` fails on null; the other ten focused
checks pass. `AbilityAffordanceDescription` delegates to
`AbilityEffectDescription.Summary` and `AbilitySourceActionDescription.Caption`,
which have no case for the lowered blanking grant. View's `AffordancePresenter`
passes the description through. The generic persistent-effect formatter also
represents that grant using the internal `printedTextBlanked` field; it is not
a readable description of the operation. Add the semantic explanation at the
engine owner and verify its projection before claiming player-facing readiness.
No production fix is included in this review.

The failing probe and its source are preserved outside the repository at
`C:/Users/mggar/Source/robot-actual-independent-probe.log` and
`C:/Users/mggar/Source/robot-actual-independent-probe.cs`. The committed regressions retain the passing wild payment and the failing semantic expectations for the owning correction. Restored focused verification is recorded
in `C:/Users/mggar/Source/robot-actual-independent-restored.log`.

Verdict: printed behavior content is accepted for this bounded face. Player-facing
semantic proof remains blocked on the concrete missing description. No full
suite, native UI, public Ms. Marvel admission or hosted persistence claim is made.

Request changes: the committed offer regression requires a readable blanking description and phase duration, plus action and commitment captions. The committed persistent-action regression rejects disclosure of the internal numeric field while requiring a nonempty action body. These are intentionally red review evidence pending the lead-owned production correction.

## Semantic correction (protocol 39)

The engine recognizes the checked self-only, phase-bound blanking body and supplies
an offer description, action caption and commitment caption: blank this card's
printed text box until the end of the phase. The persistent description emits
`BlankPrintedText`, source target and phase expiry, with neither an internal field
nor numeric amount. View projects that supplied meaning; Godot renders the operation
and duration alongside the existing hero restriction and mental resource cost.
Protocol 39 explicitly rejects protocol 38 peers; save/replay identities are unchanged.
No gameplay, ability data, or public admission changed.

Windows Release focused evidence in `C:/Users/mggar/Source/robot-semantics-*.log`:

- `content-restored`: actual Robot and blanking regressions, 23 pass.
- `view-restored`: actual projection, ordinary Grant control and Core persistent facts, 11 pass.
- `godot`: actual projected summary, ordinary source summaries and client predecessor rejection, 6 pass.
- `server`: both transports preserve actual projected meaning, both reject protocol 38,
  and existing table/encounter/version contracts remain green, 20 pass.
- `mutant`: inverted persistent blanking matcher compiles and fails both new tests:
  actual blanking falls back to Grant; ordinary retaliate is incorrectly classified
  as blanking. Original source restored, Cards rebuilt nonincrementally with zero
  warnings/errors, then the same View tests plus Content regressions pass.

These are bounded semantic and regression checks. Independent review and combined
integration gates remain lead-owned; no new native interaction or full-product
acceptance claim is made.