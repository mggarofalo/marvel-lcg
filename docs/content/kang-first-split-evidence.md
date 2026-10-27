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

The prepared boundary order is phase-end interrupts, a fail-closed preflight,
`PhaseEnd` lasting-effect expiry, prepared Kang child choices, then the retained
phase response window. RR `rr:lasting-effects.5`
expires effects lasting until the phase ends; `rr:delayed-effect.1` requires
delayed work before responses; 11002–11005 instruct joining at phase end.
Ordering this new controller relative to competing delayed effects is not an
accepted general policy. A matching registered condition therefore raises before
phase-end expiry, temporary-card discard, delayed-effect consumption or pending
departure removal. Matching uses the same activation and awaiting-activation
predicate as `ContinuousEffects.Occur`; another condition, another activation or
an instruction awaiting activation does not block the join. Simultaneous Kang
departures explicitly raise before phase-end mutation; their ordering and
integration with other delayed work remain for the complete scenario controller.
Last-area/solo central reunion also explicitly raises.
The first player records the prepared destination/unique-copy choices; a public
interaction and complete agreement/arbitration contract remain unimplemented.
Destination affordances deliberately use an unspecified anchor rather than
misrepresent game-area IDs as physical Area IDs; the future public contract must
provide the proper structured relationship.

Four prepared tests cover both first-player orders and both phase boundaries,
required singleton destination, declined-choice rejection, original frame and
occurrence retention, deterministic two-seat capture, a departed/reentered Kang
whose later incarnation cannot rewrite the capture, expiry before prompting,
retention of an unrelated delayed registration, two successive unique discards,
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

## Phase-boundary review repairs

The review probes at `c8e9fcfe84e74bec292e1dc7790686588ce7018b` exposed two
unsupported paths: an added source member was omitted from the captured move,
and a matching delayed unique discard was consumed before the Kang choice.
The controller compares complete captured/source membership before phase-end
mutation and again before every prompt, discard answer and whole-group join.
Added members therefore fail closed just as missing members do.

The four original phase/first-player variants retain their required destination,
two real unique discards, captured incarnation, exact owner/frame, whole-group
movement and exact-once assertions. Their co-due independent discard fixture is
now registered for `AttackEnds`, and asserts that both the card and registration
remain. This is an explicit scope change: accepting the old discard-first
expectation would encode the unsupported ordering that this repair rejects.
Dedicated phase/round cases instead assert rejection with unchanged card digest,
registered effects, target placement and pending departure, no emitted events,
and the original owner still outstanding. Additional controls cover mismatched
conditions/activation, awaiting activation, and source membership changes during
both destination and unique-copy choices.

Windows repair evidence is recorded in `C:/Users/mggar/Source/` under
`kang-phase-repair-*.log`. The combined focused Kang/uniqueness run passes 52
cases. Two compiling mutants are killed: removing the complete-membership count
check fails all three membership probes; suppressing the co-due match fails all
three phase/round ordering probes. Both mutations are restored. Final restored
build and focused results accompany the repair commit; independent acceptance
of these repairs remains the reviewer's responsibility. No full managed, Linux,
native, admission, printed defeat hook or public replay evidence is claimed.


## Actual stage-I defeat and first split

Implementation base `d707d848fa8608e791a2aa9ad8d38a477093fb43`.
The nonpublic two-seat slice now connects actual Standard/Expert Kang I defeat
to the next actual phase-ending boundary. `captureKangStageOneDefeat: "this"`
executes inside the normal authored When Defeated tier. Another forced interrupt
on the same defeat shares the first-player ordering prompt in either order;
Kang remains in play until that step-seven batch completes. Scenario finalization
removes Kang and his attachments/statuses without sequential promotion or victory.

`Agenda.KangSplit` retains the physical defeated incarnation. At the next
player/villain phase end it preflights composition, expires the phase normally,
and schedules typed child work before the retained phase owner's responses.
Other due delayed effects and simultaneous prepared joins raise explicitly;
there is no accepted ordering policy for those combinations. Only two living
seats are accepted by this slice. Stage-II printed defeat, simultaneous joins,
central reunion, main-scheme completion, complete admission and UI remain open.
No digest, protocol or session-schema fields are added.

The 2A overlay uses `splitKangStageTwo: "this"`; each actual 3A overlay uses
`createKangGameArea: "this"`. These narrow scenario-owned instructions are
necessary because generic advancement assumes a sequential main-scheme deck,
while the existing DSL has neither game-area creation nor an ordered random
permutation which individually reveals each selected physical scheme. Ordinary
`MainScheme.Advance` also flips its new card synchronously, so it cannot own this
multi-reveal continuation. The typed controller executes the actual 2A and each
3A through the normal interpreter and waits behind their child execution before
flipping their B sides. Unused stages are removed after both local resolutions,
and central 2B flips last. Existing entry rules supply toughness and printed
starting threat; printed II HP is flat 18/22 through existing card facts.

The authored data book is unchanged. Separate future row authoring must include
11001/11034 When Defeated, 11008a When Revealed and 11009a-11012a When Revealed
using these three instructions, alongside all other printed abilities. Tests
explicitly overlay only reached abilities and empty unreached B-side reveal
lists. They do not establish complete printed-face coverage or public setup.

The engine chooses one seeded Choice from remaining 3A codes in printed-code
order for each seat in player order, removing each selection before the next.
Seed 42 assigns Rama-Tut then Chronopolis and consumes three MT words, including
the rejected second-draw candidate. Seeds 0-5 reach all four scheme/villain
pairs and starting-threat values 0/1/1/2. The shared encounter deck/discard stay
shared; existing engaged minions follow their seats into the new topology.

Eight prepared tests cover lethal-damage entry, Standard/Expert and both
first-player orders; both competing defeat-interrupt orders; all four local
pairings; and deterministic reconstruction in two independent worlds. The latter
compares canonical card digest, complete concrete event JSON, game-area membership,
all RNG state words/index/consumption, drained pending work and exact-once agenda
completion. This is engine reconstruction evidence, not public save/host replay.

Windows logs outside the repository use `C:/Users/mggar/Source/kang-stage-one-*`.
Focused: 8 passed. Final nonincremental Release content build and combined Kang,
uniqueness, WhenDefeated and DefeatStepSeven run are recorded in
`kang-stage-one-final-build.log` and `kang-stage-one-final-combined.log`.
Four earlier compiling mutants (ordinary final-villain victory, numeric seat
allocation, premature central flip, omitted villain toughness) each failed the
focused tests and were restored. A fifth compiling mutant moved unused-stage
removal before local resolution; the added causal-order assertion kills it.
The final sources are force rebuilt after restoration. No complete managed,
Linux, native, whole-game or public replay result is claimed. Independent review
is still required for this new slice.


The normal hook found and rejected two source-policy issues before the first
commit: the immediate dispatcher reached 303 lines, and a static array was
mutable shared state. The cohesive Kang dispatch now lives in
`AbilityKangImmediateExecution`; the scenario mapping is an immutable array.
No baseline or gate was changed. The affected combined run after extraction
passes 87 cases (`kang-stage-one-extracted-combined.log`).

## Independent stage-I review on integrated candidate

Reviewed integrated `b92fa81dffd38baa0b8e4814460200d3051b452c` (source packet
`3bbcc838568478037772977764186d48f4ccabef`) in a fresh detached worktree.
Authority checked against the generated printed rows 11001/11034, 11008a/b,
11009a/b–11012a/b and the pinned mc11 `areas`, `game-areas`,
`rules-clarifications`, `adjustable-difficulty` and `setup` records. The
implementation follows the printed two-seat first split: own When Defeated
capture in the normal interrupt tier, phase-end delay, distinct seeded choices
in player order, matching flat-HP/tough villains, full local 3A resolution before
its B side, and removal of unused 3s before the final central flip. Selection
order and bounded RNG consumption are explicitly engine choices. No extra player
selection or bookkeeping prompt is introduced. These observations do not accept
later Kang behavior or public admission.

**Review finding: fail-closed local stage-3 completion is missing.** Once this
slice reaches a local 3B, scheduled placement to its printed target runs the
ordinary `MainSchemeCompletion.Commit` path and ends the game with
`Outcome.VillainWins`. Printed 11009b–11012b instead require a facedown Dominion
under 4A and a delayed removal/join. Deferring that implementation is within the
packet boundary, but reaching it must raise `RulesNotImplementedException`.
`MainSchemeCompletionAbilities` checks literal When Completed text; the local
cards print Forced Response / After this stage is completed, so that guard does
not cover them. This finding blocks acceptance until the newly reached
unsupported consequence fails closed. The review does not implement the repair.

`KangFirstSplitReviewTests` adds five prepared cases. Four pass: acceleration
carry-over from 1B combines with two side schemes; all side schemes and their
hosted attachment depart; Kang I attachments/statuses do not transfer; and
one/three/four-player paths raise before phase-end card mutation, events or RNG
consumption. The fifth is an intentionally failing regression for the real
scheduled local-completion path after an actual first split. Expected result is
unfinished plus the explicit unsupported exception; observed result is villain
victory without that exception. All fixtures remain nonpublic and use narrow
actual-card overlays, including empty B-side rows for the still-unimplemented
abilities. No product or whole-game claim is made from them.

Windows commands and raw logs outside the repository:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~KangFirstSplit'
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~Kang|FullyQualifiedName~Uniqueness|FullyQualifiedName~WhenDefeated|FullyQualifiedName~DefeatStepSeven'
```

`C:/Users/mggar/Source/kang-stage-one-independent.log`: 12 passed, one failed
(the initial direct completion probe). The subsequent scheduled-path probe is
preserved in `kang-stage-one-independent-combined.log`: 92 passed, one failed,
93 total. An intermediate scheduled-probe attempt used the wrong enum member
`CardEffect` and failed compilation; `CardAbility` is the corrected fixture.
No source mutation, Linux, native, full managed or public replay evidence is
claimed by this review. Review tests and this finding are preserved for the
production repair and its independent retest.
