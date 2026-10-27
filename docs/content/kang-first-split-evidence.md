# Kang first split: prerequisite and implementation checkpoint

MARVEL-858 under MARVEL-568. Baseline `343da12048fc2d8e0feaf66a6108d43bbc35f637`.
This packet implements a query needed by reunion decisions; it does **not**
implement Kang I defeat, seeded splitting, phase-end reunion or public admission.
The accepted area-routing prerequisite remains intact. No ability book, setup,
public admission, scheduler, digest, protocol or UI changes are included.

## Implemented prerequisite

`Uniqueness.ConflictsAfterJoin` reports matching physical in-play card pairs in
the proposed source/destination union. It includes all players already grouped
in either area, reuses `Uniqueness.Matches`, excludes detached central cards,
other game areas and out-of-play cards, and orders pairs by physical object ID.
It validates world ownership and treats joining an area to itself as no change.
The result is a snapshot of conflicts, not a survivor/discard selection or a
legal join offer. A future controller must re-query after each actual discard.

Authority: `pack:mc11:rules-clarifications` says players combining game areas
must discard matching uniques until only one remains; the first player decides
when players cannot agree. `pack:mc11:game-areas` moves the whole group, including
side schemes and engaged minions. Pair order and validation are engine choices.
The query does not mutate topology, consume RNG or choose a card for the player.

Prepared nonpublic tests use actual Spider-Woman/Shuri faces. The four-seat
fixture is a synthetic topology capability state; duplicate identity fixtures
are moved out of play to isolate the ally reconciliation assertion. It is not a
played Kang scenario or legal public setup. Three checks cover every physical
pair in a multiple-player group, outsider/central/hand exclusion, reversed
source/destination symmetry, same-area no-op, foreign-world rejection, unchanged
card digest, topology, pending agenda frames, full RNG state and word consumption.

## Concrete execution boundary

The split/reunion controller must integrate the following existing seams rather
than calling a topology helper directly from a renderer or phase timer:

1. `Defeat.Character` records the exact defeat, invokes
   `CardAbilityEntries.ExecuteDefeat` and waits for step-seven work before
   `Defeat.FinalizeCharacter`. `DefeatedCardAbilityResolution` supplies own
   When Defeated handles; `DefeatProcedure`, `DefeatWindowEligibility` and
   `DefeatWindowResumption` own the applicable ordering and resumption. An unauthored printed When Defeated currently raises here in
   the interpreter. Authoring the own ability is deliberately outside this
   packet. Calling a bespoke hook before that tier would bypass ordering.
2. `Defeat.FinalizeCharacter` currently chooses Wrecking Crew topology or
   `VictoryDisplay.VillainStage`. Neither is a Kang controller: the latter
   promotes the sequential villain deck, inherits same-title state, or declares
   victory when that deck is empty. Kang I/II require explicit scenario-owned
   departure/pending-transition policy; do not reuse this sequential promotion.
3. `PhaseEnd.End` expires lasting effects, then calls `DelayedEffects.Occur`
   before responses. `DelayedEffects` currently executes fixed synchronous
   operations. A join can require destination and unique-discard prompts, so
   it needs typed owned work, not a synchronous callback. `PhaseTransitionProcedure`
   returns no prompt today; new work must retain its exact originating phase
   frame. `Agenda.ContinueBeforeOwner` is the existing continuation seam.
   `AbilityDelayedExecution` demonstrates retained typed ability captures after
   activation, but is **not** a generic end-of-phase controller.
4. `World.Join` moves one play area and emits one topology event. Whole-group
   reunion must capture and deterministically order every source play area
   before moving any; GameArea membership is a HashSet. Side schemes/minions in
   those play areas follow without physical card moves. Use the conflict query
   above to construct real survivor/discard choices; do not auto-discard by ID.
5. Stage 2B is detached central state in the accepted representation. Legal
   destinations need an explicit central option only after all other local
   game areas are gone; do not create an empty group as a side effect of querying.
   MainSchemeCompletion.Commit also needs a Kang policy before a stage-3
   completion can be allowed to follow its ordinary sequential/loss behavior.

## Next bounded implementation

Implement a nonpublic scenario-owned pending phase-transition model and its
scheduler operation, plus focused tests that a retained phase frame suspends
and resumes exactly once across a required destination decision. Keep actual
Kang row authoring serialized with ability-book ownership. The controller needs
captured defeated incarnation, phase boundary, departing group and pending
operation; keep these game-owned and replay-reconstructible, not new card-token
or digest fields. Prove first-player ordering with both seat orders.

Then connect the I defeat policy and stage-2A resolution: side-scheme count and
discard, seeded distinct 3A selection in player order, flat 18/22 II HP,
toughness, printed starting threat and encounter dealing; flip 2A only after
both complete. Existing random Choice2 consumes no RNG when selecting every
item, so four-seat assignment cannot use unordered Choice2-all as a random
permutation. Specify selection order/consumption as an engine choice and pin it.

Finally connect II defeat, required legal destination choice, conflict discards,
whole-group join, simultaneous departures and central/stage-4 continuation.
Compare topology, pending work, RNG, events and next decisions independently
across reconstruction; digest v3 cannot observe game-area topology. Public
session replay waits for complete admission and cannot be replaced with an
admission bypass. No new schema/digest compatibility decision is made here.

## Evidence

Windows focused commands, with raw logs outside the repository in
`C:/Users/mggar/Source/kang-join-*.log` and
`kang-first-split-prerequisite.log`:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter FullyQualifiedName~KangJoinConflictTests
dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~KangJoinConflictTests|FullyQualifiedName~KangAreaRoutingTests|FullyQualifiedName~KangRoutingIndependentReviewTests|FullyQualifiedName~Uniqueness'
```

Initial prerequisite run: 3 passed. Restored combined focused run: 32 passed.
Three compiling mutants were killed: omit destination membership (2 failures),
accept foreign-world areas (1 failure), and report conflicts on a same-area
no-op (1 failure). The initial same-area mutant survived because its source
contained no conflict; the fixture now includes a matching physical pair and
the repeated mutant fails. A forced nonincremental restored build passed.
The final test-only extension also compares every RNG state word and index. No full
managed, Linux, native, save/replay or played split/reunion evidence is claimed.
Independent review is recorded below.

## Independent join-query review

Reviewed production revision `2f7317e758350788d1d9634bb7485cd73d3d97a1`
in a separate worktree. Accepted only the pure conflict-query prerequisite:
no production correctness finding was identified. This verdict does not accept
a split/reunion controller, discard choice, public admission or played Kang.

The vendored `pack:mc11:rules-clarifications` requires unique-copy reconciliation
inside the combined game area. RR 1.8 `rr:unique-icon.1.1` and `.1.2` define
matching; reusing `Uniqueness.Matches` preserves the distinction between bare
titles, subtitles and alter-ego names. Sorting physical object IDs determines
report order without selecting a survivor. Membership uses actual placement,
including every player's play area in both groups. Detached central cards,
other groups and out-of-play cards are outside this union. Source inspection
confirms that the query performs no state, RNG or agenda mutation; the existing
purity test observes those baselines before its first query.

`KangJoinMatchingReviewTests` adds five passing actual-card cases: Spider-Man's
two-face identity matches the Peter Parker ally in both forms and both join
directions; the Miles Morales ally does not match merely by hero title; physical
placement governs a differently owned ally; distinct Nick Fury printings match;
and querying after a real discard removes that pair. These are prepared
nonpublic states, not public setup or played scenario evidence.

Independent Windows command (30 passed):

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~KangJoin|FullyQualifiedName~KangAreaRoutingTests|FullyQualifiedName~KangRoutingIndependentReviewTests'
```

Raw log: `C:/Users/mggar/Source/kang-join-independent-review-restored.log`.
An earlier review attempt hit a fixture-only missing `Seat.Discard` compile
error; the earlier review log retains four failures from constructing a one-face
identity. The corrected fixture constructs `01001a,01001b`; production sources
were unchanged. Normal commit hooks passed, including 66 architecture checks,
complexity and file length.
No additional mutation, full-suite, Linux, native or replay result is claimed.

## Prepared phase-transition continuation

Continuation based on accepted review `5092072f0bd541268e3e15ebf5b2c0e80db5fc82`.
`Agenda.KangTransitions` owns pending stage-II departure captures between phase
frames. Each capture retains the physical defeated card and incarnation, round,
player/villain phase boundary, source game area and a read-only whole-group
snapshot sorted by play-area seat. `KangJoinOperation` retains the exact original
phase frame and its occurrence through a required destination choice and repeated
unique-copy discard choices. Every answer schedules a typed continuation and
advances its exact child frame; occurrence identity alone cannot distinguish it
from the phase owner. The prepared controller joins all captured play areas only
after re-querying uniqueness following each real discard.

This is a nonpublic prepared capability, **not** connection of printed Kang
defeat/When Defeated abilities, stage-3 completion, stage-I splitting or public
admission. The future defeat hook must capture the incarnation/group while the
proper When Defeated tier resolves, suppress ordinary sequential promotion and
perform the printed scheme removal. Its phase boundary must be supplied from
the actual phase, not inferred from whichever villain is current later.

The current boundary order is phase-end interrupts, `PhaseEnd` lasting-effect
expiry, existing synchronous delayed-effect resolution, prepared Kang child
choices, then the retained phase response window. RR `rr:lasting-effects.5`
expires effects lasting until the phase ends; `rr:delayed-effect.1` requires
delayed work before responses; 11002–11005 instruct joining at phase end.
Ordering this new controller relative to competing delayed effects is not an
accepted general policy. The prepared test's independent synchronous discard
establishes the present seam only. Simultaneous Kang departures explicitly raise;
their ordering and integration with other delayed work remain for the complete
scenario controller. Last-area/solo central reunion also explicitly raises.
The first player records the prepared destination/unique-copy choices; a public
interaction and complete agreement/arbitration contract remain unimplemented.
Destination affordances deliberately use an unspecified anchor rather than
misrepresent game-area IDs as physical Area IDs; the future public contract must
provide the proper structured relationship.

Four prepared tests cover both first-player orders and both phase boundaries,
required singleton destination, declined-choice rejection, original frame and
occurrence retention, deterministic two-seat capture, a departed/reentered Kang
whose later incarnation cannot rewrite the capture, expiry before prompting,
synchronous delayed discard before prompting, two successive unique discards,
one whole-group join, unchanged RNG consumption and no repeated join after the
agenda drains. An identical later phase frame distinguishes exact frame identity
from matching phase coordinates. Topology and agenda are asserted directly;
digest v3 is unchanged and cannot establish topology restoration.

Windows evidence outside the repository:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter FullyQualifiedName~KangPhaseTransitionTests
dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~Kang|FullyQualifiedName~Uniqueness'
```

Focused: 4 passed. Forced restored build: passed. Combined restored focused:
41 passed. Compiling mutants removing child identity advancement and truncating
the captured group to one play area each failed all four phase tests; both
mutants were restored before the forced rebuild. Logs:
`C:/Users/mggar/Source/kang-phase-focused.log`,
`kang-phase-mutant-identity.log`, `kang-phase-mutant-group.log`,
`kang-phase-restored-build.log` and `kang-phase-restored-focused.log`.
Earlier fixture iterations exposed incorrect API assumptions, attempted removed
card reentry, and empty player-deck encounter exhaustion; the final fixture has
nonempty player decks and models reentry through the encounter discard instead.
No full managed, Linux, native, public replay or actual Kang split/reunion
acceptance is claimed. Independent review remains required.
