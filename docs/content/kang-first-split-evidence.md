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
Independent review remains required.
