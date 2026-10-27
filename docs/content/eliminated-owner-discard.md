# Eliminated owner's foreign-controlled cards

MARVEL-830. Engine prerequisite for the separately tracked Doctor Strange support
candidate; this packet does not admit content or establish pack completion.

Candidate base: `22d678049b5c6b646c7c69453b2d8fe80d57207b`.
Owned changes are `Discard.cs`, `EliminatedOwnerDiscardTests.cs`, and this record.

## Authority and behavior

The vendored Rules Reference 1.8, `rr:ownership-and-control.7.2`, sends a card
leaving play to its owner's equivalent out-of-play area, “or removed from the
game if that player is no longer in the game.” `rr:player-elimination.step.5`
removes that owner's discard pile and other areas. Ordinary discard therefore
moves a foreign-controlled card owned by an eliminated seat directly to
`RemovedArea`, preserving departure cleanup and movement/detachment events.
Reset processing stops for a removed destination.

An active owner still receives the card in their own discard pile under
`rr:discard.1`. If their deck and discard were empty, this first discard resets
their deck and deals them an encounter card under `rr:player-deck.4`. The
controller's deck and encounter queue are not the destination or reset subject.

Elimination marks the seat eliminated before executing its numbered steps.
Cards still in that seat's own play area must retain the step 4 transition into
its discard pile before step 5 removes its areas. The new destination branch is
therefore limited to cards outside their owner's play area. The explicit
step-4/step-5 regression checks both movement events, not only the final removed
state.

## Evidence

Windows, Release, isolated `fix/eliminated-owner-discard` worktree. Commands,
mutations, restoration, and hooks ran serially.

- New synthetic engine class: eight cases covering direct discard and an actual
  ally-limit decision, eliminated owner versus a different eliminated seat,
  active owner with a nonempty or empty deck, attachment departure to a third
  active owner, and the ordered elimination transitions. Assertions cover the
  selected destination, movement events, reset/shuffle events, encounter deal,
  unchanged controller deck and full RNG state, and drained agenda.
- Compiled mutant changed the elimination lookup from `card.Owner` to controller
  seat 0. Three of seven then-current cases failed with expected `RemovedArea`
  versus actual `PlayerDeck`; four active-owner controls passed. Log:
  `C:/Users/mggar/Source/eliminated-discard-mutant.log`.
- Restored sources were rebuilt with `dotnet build
  tests/Marvel.Rules.Tests/Marvel.Rules.Tests.csproj -c Release --no-incremental`:
  zero warnings/errors. The affected complete classes and citation check passed
  105 cases. Logs: `eliminated-discard-restored-build.log` and
  `eliminated-discard-restored-tests.log` beside the mutant log.
- After adding the explicit elimination-transition control, `dotnet test
  tests/Marvel.Rules.Tests/Marvel.Rules.Tests.csproj -c Release` passed all
  1,181 cases, including all eight new cases and citation validation. Log:
  `C:/Users/mggar/Source/eliminated-discard-all-rules.log`.

The removed-destination reset guard is observationally redundant for ordinary
cards on a completed elimination board: that owner's discard pile is empty, so
`PlayerDeck.Reset` has no cards to recycle. The destination mutant proves the
actual resurrection defect. No separate killed mutant is claimed for that guard.

Independent review and integration with the support candidate, including the
actual Clea declined-defeat-interrupt regression, remain the lead's next steps.
These engine tests do not depend on the support ability book or its Runner.

## Elimination reset follow-up

Independent support review retained a failing own-area elimination probe:
`EliminatingAnEmptyDeckSeatDoesNotResetOrConsumeAnEncounterCard`. With an empty
player deck, step 4 discarded an owned ally, rebuilt that eliminated player's
deck and consumed an encounter card before step 5 removed the areas.

The reset guard now ignores every eliminated owner, preserving the explicit
step-4 discard followed by step-5 removal. It also prevents separate Invocation
refresh during those departures. Active-owner attachment cleanup/reset remains
unchanged. Authority: rr:player-elimination.step.5 and .6.

The exact independent probe was copied unchanged into this isolated repair tree
for verification: compiled predecessor failed (encounter deck [6,7,8] became
[6,7]); repaired nonincremental build and the full Rules suite pass 1,182 cases.
The test belongs to the separate reviewer commit and is not duplicated here.
Logs: `Source/evidence/elimination-reset-{red,build,rules}.log`. The red build is
the guard-removal mutant; compilation succeeded and the observed deck assertion
failed. Independent repair review remains required before integration.