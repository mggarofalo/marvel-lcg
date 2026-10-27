# Ronan phase-start timing prerequisite

MARVEL-854 (engine prerequisite under Ronan MARVEL-567). Implementation baseline: `b753c463a6baa3756a954d08db3ae40336c190d3`.
This packet adds the real `WhenVillainPhaseBegins` occurrence needed by 90001.
It changes no production ability row, setup, admission or engagement operation.
The test-local actual body uses the existing composition demonstrated by
`4086973d2384753f8cdb9d360f7b0c7e42f5fad3`.

The genuine baseline `Sequence` emits a start event before threat placement, but
only opens a threat-placement occurrence. Recompiling that original procedure
against the new explicit `subject: game` abilities reproduces missing choice,
ordering and actual Ronan movement failures. See
`C:/Users/mggar/Source/evidence/ronan-phase-original-sequence.log`.
The explicit game subject matters: the DSL's omitted subject means `this`, which
cannot match a phase beginning without a card subject.

`VillainPhaseBeginning` observes the board when step one is actually reached,
after preceding phase-end work. In-play authored observers cause a typed
`BeginVillainPhase` transition to be inserted ahead of threat placement. The
existing scheduler opens interrupt and response windows, so forced ordering,
optional opportunities, continuation ownership and completion use their ordinary
paths. The start occurrence has no player or card subject. Ronan's explicit
first-player chooser and scoped `eachPlayer` body supply only the contexts their
instructions need. No substitute resolving player is invented.

Markers on the existing threat frame record the beginning and completion-observer
checks exactly once across suspension. The start event keeps its established position. Completion of villain
step one remains a separate later occurrence. Its consumers are observed after the phase-start windows finish and before threat
placement, so an observer introduced by a start effect participates. The internal start coordinate is step one's index
-1, an engine representation choice; no public wire field is introduced.

Unobserved phases allocate no start frame. The regression test pins the complete
raw event stream and remaining scheduler coordinates through the first defense
against the genuine baseline procedure (SHA-256
`3EA267491A3FFBCE51FCC0E2D30E4874E002D1FCA80F3AE88AA7B4E8D563F732`).
No existing production row observes this new condition, so replay identity,
protocol, RNG and digest formats remain unchanged. Existing archives and their
comparisons are not edited or waived.

`VillainPhaseBeginningTests` exercises suspension/resumption, exactly one start,
non-first-player encounter source, first-player ordering of two simultaneous
forced abilities in either order, forced-before-optional priority, optional
interrupt/response tiers, and the baseline transcript. `RonanPhaseBeginningTests`
executes the actual test-local 90001 forced interrupt and existing ranked-hero /
scoped engagement body on both first-player orders. It verifies preserved Ronan
state/incarnation, movement before threat events and activation against the new
player. The separately reviewed body packet covers ties, no heroes, current-seat
no-op, form filters and stale selected identities.

This is engine prerequisite evidence, not whole-set, public-setup, save/restart
or native-client acceptance.

The closure probe proves a reachable synthetic start effect can put a minion
with a step-one-completion response into play. The initial helper queried that
observer before the start effect: its compiling test failed with expected damage
1, actual 0 (`ronan-phase-new-observer-before-fix.log`). Both forced interrupt and
forced response variants pass after the narrow scheduling repair. No Agenda,
ability-book, setup, admission or engagement implementation is changed.

Final-source verification (logs in `C:/Users/mggar/Source/evidence/`):

- `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental --no-restore`:
  passed, zero warnings/errors; `ronan-phase-closure-restored-build.log`.
- Focused Content filter covering `VillainPhaseBeginningTests`,
  `RonanPhaseBeginningTests`, and `WreckingPhaseBoundaryTests`: 17 passed;
  `ronan-phase-closure-restored-focused.log`.
- Full Content: 7,792 passed (6m08s); `ronan-phase-closure-content-full.log`.
- Full Rules: 1,187 passed; `ronan-phase-closure-rules-full.log`.
- Full Session: 84 passed; `ronan-phase-closure-session-full.log`.
- Full Architecture: 129 passed; `ronan-phase-closure-architecture-full.log`.

Each full lane uses its project under `tests/` with `dotnet test -c Release
--no-restore`; Content additionally uses `--no-build` after the forced rebuild.
A subsequent behavior-preserving consolidation shares the empty beginning and
step-one-completion switch body. The exact consolidated source passes the focused
17 tests and all 129 Architecture tests again; see
`ronan-phase-closure-consolidated-focused.log` and
`ronan-phase-closure-consolidated-architecture.log`. The full Content/Rules/Session
runs above precede only that no-op case consolidation.
The source commit and normal commit-hook outcome are in the delivery handoff.
`ronan-phase-*-full.log` without `closure` predate the completion-observer repair
and are not final-candidate evidence.

Compiling negative controls, followed by exact UTF-8 source restoration and the
forced non-incremental build above:

- Disabled start observation: 10 failed, 1 passed;
  `ronan-phase-closure-missing-observer-mutant.log`. The start choice disappears
  and the first question is defense instead.
- Delayed boundary (`Then` instead of `Now`, returning normally to process
  threat): 10 failed, 1 passed;
  `ronan-phase-closure-delayed-boundary-mutant.log`. Choices observe threat 2
  instead of 0, and actual Ronan movement follows threat placement.
- The prior duplicate-start mutant was killed by the suspension test;
  `ronan-phase-mutant-duplicate.log`. The earlier disabled-observer compile
  failure and progress-guard-only delayed-boundary failure are not evidence.

