# Behavioral contracts for architectural decomposition

These finite examples protect the boundaries being changed by the architectural
decomposition module. They supplement the authority-derived Core corpus. They
do not claim that running a game, binding an obligation, or recording a digest
independently proves every rule encountered along the way.

## Entry points and observations

| Boundary | Executable example | Entry point and distinguishing observation |
|---|---|---|
| Initiation and payment | `GameBoundaryTests.PaidEventWaitsForItsTargetThenReportsHealingAndDiscard` | `Game.Resolve` accepts First Aid with an explicit payment. The resource is discarded while healing and event disposal wait for the target answer. |
| Suspension and resumption | The same First Aid example | A second `Game.Resolve` answers the pending target, heals Peter Parker from 8 to 10 HP, discards First Aid, and returns to the root turn menu. |
| Choice, limit and source re-entry | `GameBoundaryTests.PaidChoiceConsumesItsLimitAndReentryStartsANewInstance` | `Game.Resolve` pays Vision's Action and answers THW. A second energy resource remains available, so the missing repeat Action distinguishes the limit from inability to pay. Vision attacks, dies from consequential damage, and returns through Make the Call without the old bonus or limit. |
| Interrupt ordering | `CardsInWindowsTests.TheWholeGameStopsInTheVillainPhaseToAskAboutSpiderSense` | The public game loop enters the villain phase. Charge has granted overkill before the cancellable Spider-Sense interrupt is offered. |
| Component tier order | `AbilityWindowTests.AForcedInterruptGoesBeforeAnOptionalOneWhoeverControlsIt` | `AbilityWindow.Tiers` returns forced before optional irrespective of controller. This narrower test observes the ordering of the returned collection. |
| Player/phase boundary | `GameBoundaryTests.BothPlayerTurnsFinishBeforeEndPhaseDiscardBegins` | Separate `Game.Resolve` decisions give the second player a turn before the first end-phase discard question appears. |
| Terminal resolution | `GameBoundaryTests.FinalVillainDefeatEndsThePublicDecisionLoop` | Spider-Man's basic attack defeats the final Rhino stage through `Game.Resolve`; the result is a win, both pending prompts are absent, and further decisions are refused. |

The new examples use `CanonicalCoreScene` to arrange complete supported decks
before `Game.Begin`. Every subsequent transition uses public game decisions.
The card expectations come from `datasets/cards/`; Vision's new-instance
expectation also follows the vendored FAQ for `01068`. General behavior has
narrow `[Rule]` citations with readable clauses in the tests.

Existing Core transcripts remain useful at their actual boundaries. For example,
the draw binding calls `Draw.Cards`, while card Action bindings schedule
`PlayerAction` and run `Sequence.Work`. Those prove component and agenda
behavior; they do not exercise the root-menu routing in `Game.Resolve`.

The First Aid example also checks semantic event payloads: complete source and
destination area references, moved-card identities and destination positions,
the healed character and exact HP values, and healing before event disposal.
The acceptance invariant that a changed digest needs at least one event cannot
distinguish incorrect values inside a nonempty event list. Its contrapositive
does not add that missing evidence either.

## Executed mutation checks

The following experiments were run on test revision
`61a765bfe33266e2de293bc246a6b1f1ea75c5a3`, based on engine revision
`9bd4aa7d127b8a6f3e3e0fc17830b299af3b635a`. Each experiment changed only the named
expression, rebuilt the selected test project, and restored the source before
the next experiment. All eight mutations compiled. No mutation remains in the
production source.

For each row, run this command after applying its stated edit. `PROJECT` is
`Marvel.Content.Tests` except M5's component check, which uses
`Marvel.Rules.Tests`. `TEST` is the method named in the table above.

```bash
dotnet test tests/PROJECT/PROJECT.csproj -c Release \
  --filter FullyQualifiedName~TEST --logger 'console;verbosity=normal'
```

| Mutation | Exact edit | Observed failure |
|---|---|---|
| M1: lose payment | In `Game.TriggerAction`, replace `[.. input.Spent]` with `[]` in the `PlayerAction` constructor. | First Aid fails at the initial resolve: the cost is 1 but the payment generates an empty resource string. |
| M2: lose continuation routing | In `Game.Resolve`, add `&& Phase != GamePhase.PlayerTurn` to the branch guarded by `asking == Asker.Sequence`. | First Aid's target answer throws because `Choose_Option` reaches the root verb dispatcher. |
| M3: wrong event payload | In `Damage.Heal`, replace the `FieldSet` constructor's final `after` argument with `before`. | First Aid's HP event assertion expects 10 and receives 8, even though the character was healed. |
| M4: skip a turn | In `Game.FinishTurn`, change `offset < world.Players` to `offset < world.Players - 1`. | The two-player test expects `PlayerTurn` and receives `EndPhase` after only the first player passes. |
| M5: reverse tiers | In `AbilityWindow.Tiers`, change `byTier.Select` to `byTier.Reverse().Select`. | The component tier test expects `[ForcedInterrupt, Interrupt]` and receives the reverse order. |
| M6: wrong terminal outcome | In `Defeat`'s final villain branch, replace `world.Finish(Outcome.PlayersWin)` with `world.Finish(Outcome.VillainWins)`. | The public terminal test expects `PlayersWin` and receives `VillainWins`. |
| M7: retain the old bonus | In `ContinuousEffects.CardLeftPlay`, change `entry.Effect.Affects == card.ObjectId` to `!=`. | Vision's post-return THW assertion expects 1 and receives 3. |
| M8: skip forced resolution | In `Offering.Work`, change `if (forced.Count == 1)` to `if (forced.Count == 1 && forced[0].Type != AbilityType.ForcedInterrupt)`. | The public interrupt test finds no overkill effect when Spider-Sense is offered. |

M5 initially **survived the public interrupt test**. `Offering.Forced` still
selects the mandatory ability before optional abilities are offered, regardless
of that particular two-tier list's order. The existing component test kills M5;
M8 separately verifies the public forced-resolution boundary. M5 is therefore
not globally equivalent, and the public scenario alone is not credited with
testing the returned tier order.

These results establish sensitivity to the named mistakes at the recorded
revision. They are not a mutation score or evidence that every catalog rationale
has been executed. After a relevant implementation changes, choose and run a
mutation against its new decision point rather than treating these historical
edits as current proof.

## Decision and journal boundary checks

The engine assessment and journal capture changes have the following executable
mutation evidence. Each mutation was applied individually, compiled successfully,
failed the named behavioral test, and was restored before the clean suite ran.

| Decision changed | Mutation | Test that rejected it |
|---|---|---|
| Admission before mutation | Restrict rejection to an impossible offered handle, bypassing ordinary malformed answers | `GameDecisionAdmissionTests.InvalidAnswersLeaveTheCurrentDecisionAndWorldUntouched` |
| Target-specific prices | Treat every nonnegative cost target as applicable | `DecisionAssessmentTests.APriceForAnUnselectedTargetCannotPayForTheChosenTarget` |
| Off-turn ownership | Reverse the comparison with the engine-selected acting seat | `DecisionAssessmentTests.OffTurnActionsBelongToTheirOfferedOwner` |
| Alternative variables | Reverse the selected cost's variable validation | `DecisionAssessmentTests.AlternativeCostsValidateTheirOwnVariables` |
| Shared empty answer values | Expose a mutable shared empty dictionary | `DecisionAssessmentTests.EmptyAnswerValuesCannotBeMutatedAcrossGames` |
| Nested journal snapshots | Copy the outer group collection but retain each mutable inner group | `JournalCaptureIsolationTests.CapturedPromptsRetainTargetsCostsAndNestedGroupsAfterSourceMutation` |

The admission test also submits the valid answer after rejecting all its malformed
answers, checking that the pending decision can still progress.
The cross-layer `DecisionAuthorityTests` exercise the same alternative price through
draft composition, durable capture and replay with a freshly issued offer handle.

## Session authority, transport and client recovery checks

MARVEL537's following six mutations each compiled and failed the named behavioral
regression. Each source was restored immediately, its owning assembly rebuilt,
and the clean focused Server tests (10) and client recovery tests (7) passed.
These are targeted sensitivity checks, not a coverage percentage.

| Decision changed | Exact mutation | Regression and observed failure |
|---|---|---|
| Cached committed history | `AuthorizedSessionProjector.History`: change `session.History ??= Inspect(session.Save)` to unconditional assignment | `SessionConcurrencyTests.RepeatedSynchronizationReusesCommittedVerifiedHistory`: factory calls increase from 4 to 7 during three read-only syncs. |
| Durable revocation | `EngineHostEntryRecovery.RecoverEntry`: replace the `access.Revoked` guard with `request.Version < 0` | `EntryRecoveryTests.LostAttachReplyRecoversOnlyTheOriginalClaimAndRevocationIsDurable`: revoked recovery succeeds instead of returning `session_not_found`. |
| Independent sessions | `EngineHost.HandleSerialized`: replace `lock (sessionGate)` with `lock (entryGates[0])` | `SessionConcurrencyTests.CommittingOneGameDoesNotBlockAnotherAndRacingAnswersCommitOnce`: another game's synchronization times out while the first commit is held. |
| Independent socket frames | `SocketEngineServer.RunAsync`: await `ServeConnectionAsync` directly instead of adding its task to the active set | `ConcurrentSocketFramingTests`: a complete second request times out behind an incomplete peer. |
| Typed failure meaning | `LocalGameMutationClient.InterpretMutationError`: treat `Rejected` as `SessionUnavailable` | `TypedFailureRecoveryTests`: ordinary rejection loses authority and explicit unavailability retains it. |
| Retained uncertain entry | `LocalGameEntryClient.DefinitiveFailure`: compare disposition against `SessionUnavailable` instead of `CommitUncertain` | `ClientEntryRecoveryTests.LostOpenResponseRetainsAnExactRetryAttemptInsteadOfCreatingAnotherGame` (`uncertain` case): no retry attempt is retained after a possibly committed entry. |

The full Server suite passed 255 tests before these experiments. After restoration,
the full Godot/client suite passed 803 tests. The same regression families also
cover lost replies across host restart, unchanged seat authority, altered-intent
rejection, discarded malformed response envelopes, scoped invitation recovery,
and retaining the embedded host connection until entry recovery completes.

## Content, setup and delayed-copy checks

The MARVEL-537 working tree based on `05dd0b88` was checked on Windows with
Release builds. The focused Content suite passed 89 tests covering deck
admission, scenario setup suspension, delayed discard lifetime and the migrated
player-decision fixtures. `ExplicitDeckSessionTests` separately passed its
protocol-to-durable-save-to-restarted-host ordered-deck reconstruction test.

Each following mutation was compiled, rejected by its focused behavioral suite,
restored, and followed by a clean build. These are individual executed probes,
not a whole-engine mutation score.

| Decision changed | Applied mutation | Rejecting suite |
|---|---|---|
| Full deck size | Permit 51 cards instead of at most 50 | `PlayerDeckAdmissionTests` |
| Signature membership | Skip the remaining required signature-card check | `PlayerDeckAdmissionTests` |
| One selected aspect | Permit two aspects | `PlayerDeckAdmissionTests` |
| Printed copy limit | Increase the permitted copy count by one | `PlayerDeckAdmissionTests` |
| Supported player count | Permit five players | `PlayerDeckAdmissionTests` |
| Starting identity form | Return the raw face order | `SetupAdmissionTests` |
| Identity shape | Bypass the two-face/one-Hero requirement | `SetupAdmissionTests` |
| Nemesis association | Derive the nemesis icon by appending a suffix to the identity icon | `SetupAdmissionTests` |
| Strict mode data | Default malformed/missing expert flags to false | `SetupAdmissionTests` |
| Authored set roles | Return true for every modular-set query | `SetupAdmissionTests` |
| Scenario completeness | Bypass the required main-scheme/villain check | `SetupAdmissionTests` |
| Setup suspension | Continue setup despite a pending decision | `ScenarioSetupDecisionTests` |
| Event-sink boundary | Interpret the caller's earlier events as new setup events | `ScenarioSetupDecisionTests` |
| Delayed target lifetime | Ignore target incarnation | `DelayedDiscardLifetimeTests` |
| Delayed target location | Ignore whether the target remains in play | `DelayedDiscardLifetimeTests` |

The lifetime tests retain a real owned Nick Fury card but register the delayed
effect directly, isolating the shared Rules contract. A separate full game used
Ultron Standard, Spider-Man/Captain Marvel/She-Hulk/Black Panther, game seed 266,
policy seed 266 and decision limit 1200. It finished after 126 decisions and four
rounds with a player-loss result and no simulation failure. This is regression
evidence for that game; the isolated tests distinguish discarded, removed,
replayed and continuously present copies under `rr:leaves-play.1`.

The source metadata review adds explicit dispositions for seven encounter-set
roles and five nemesis-set icons. Their underlying card inventories remain the
same. The reviewed catalog contains 2,608 authority sources and 4,368 obligations;
`Marvel.Behavior.Index write` followed by `check` passes. The Iron Man JCS vector
is pinned independently in `BehaviorAuthorityTests` and the behavioral
specification document.

Malformed protocol values were checked separately after the structural review:

| Decision changed | Exact mutation | Regression and observed failure |
|---|---|---|
| Constructor validation remains a frame error | In `EngineJson.ReadRequest`, restrict the `ArgumentException` catch to the unreachable parameter name `unreachable` | `ProtocolRecoveryContractTests.InvalidDeckConstructorValuesProduceProtocolErrors`: null/blank deck entries throw constructor exceptions instead of `JsonException`. |
| Version value kind is checked before numeric access | In `EngineJson.ReadResponseVersion`, remove the `JsonValueKind.Number` guard | `ProtocolRecoveryContractTests.InvalidVersionValuesProduceProtocolErrors`: null, Boolean and object versions throw `InvalidOperationException` instead of `JsonException`. |

Both mutations compiled, were killed, and were restored. The clean codec/framing
suite passed 13 cases, including
`ConcurrentSocketFramingTests.InvalidDeckValuesRejectOnlyTheirFrameAndKeepTheListenerAvailable`:
an invalid recipe receives `invalid_frame`, and the same listener subsequently
answers a healthy peer. The seven presentation-wall dependency probes also
rejected their forbidden references with the intended build diagnostic.

## Interleaved compiled-program isolation

`CompiledProgramInterleavingTests.SharedProgramKeepsChoicesQueriesRandomnessAndDelayedEffectsLocal`
uses one compiled Core ability program and runner for two independent worlds.
It interleaves Ancestral Knowledge's suspended choices and read-only queries,
selects different ordered cards, and completes Sonic Boom's delayed boost effects
in both possible game orders. Each checkpoint matches a separately compiled,
isolated control's full prompt/event serialization, offers, concealed-candidate
metadata, RNG words consumed and canonical state. These are Core component
fixtures, not a complete played-game transcript.

The executed mutant added one static `SharedEffects` dictionary to
`AbilityGameRuntime` and initialized every instance's `activationEffects` field
from that dictionary. It compiled successfully. Both completion-order cases failed
because the second game's Sonic Boom completion lacked its expected exhaustion
`FieldSet` event: the first game's completion had consumed the shared pending
work. Production source was restored byte-for-byte, the Cards assembly rebuilt,
and both clean cases passed again. This checks an actual cross-game state leak,
not merely the runtime objects' reference identities.

## Cancellation after socket dispatch

`SocketCancellationRecoveryTests.CancelledCommittedResponseRecoversWithoutRepeatingTheOperation`
synchronizes cancellation with a real peer after the host has committed the
request. A cancelled open retains its exact entry claim and recovers the same
session; a cancelled resolve performs one sync and observes revision 1 without
resubmitting the decision. Replacing the response read's cancellation token with
`CancellationToken.None` compiled and killed both cases by exceeding their
recovery deadline.

`ExchangeDeadlineBoundsAnUnresponsivePeerWithoutCallerCancellation` uses an
internal one-second test deadline; production exchanges use 30 seconds. Removing
`CancelAfter` compiled and failed this case because the unanswered exchange never
completed. Both mutants were restored; all three clean real-socket cases and
11 existing cancellation-boundary cases passed. These deadlines bound waiting;
they do not promise rollback of dispatched engine work.


## Runtime frames and committed completion

`ActualAbilityCompletionTests` exercises synthetic DSL contracts over legal Core
boards: nested sibling choices, partial healing, valid multi-target ready/exhaust, and
threat prevention determine `then`/`otherwise` from committed results.
`DelayedAbilityChoiceTests` suspends inside a delayed body and verifies that its
original ability suffix and activation callback each execute once.
`AbilityChoiceProgressionTests` includes the reachable Rhino Expert seed 24
Spider-Tracer/Interrogation Room ordering and the Core Sonic Boom state with no
payable resource option and no ready character.

The following temporary mutations compiled and were killed by behavioral assertions.
Each source was restored byte-for-byte after its run.

| Decision changed | Mutation | Killing evidence |
|---|---|---|
| Stable agenda frame identity | Compare a captured frame by record equality after a Surge payload update | `AgendaFrameIdentityTests` cannot independently advance the original and identical nested frame. |
| Aggregate all predecessor children | Keep only the first completed child outcome | `ActualAbilityCompletionTests` incorrectly draws after a partially completed second heal. |
| Use committed threat | Complete the receipt with assigned rather than placed threat | Three prevention/dependency cases draw on the wrong branch. |
| Finish a triggered choice before its owner | Insert the choice after its owning window | Rhino Expert seed 24 reaches an unresolvable Interrogation Room prompt. |
| Validate the complete ordered cursor | Remove the total cursor-length check | `InvalidOrderedContinuationIsRejectedBeforeRunningAnySibling` accepts a duplicate completed index. |
| Consume delayed work once | Read activation work without removing it | `DelayedAbilityChoiceTests` receives duplicate pending choices. |
| Complete an unavailable mandatory choice | Publish an empty payment choice instead of completing it | Sonic Boom leaves an unanswered agenda item. |
| Exclude invalid readiness targets | Count already exhausted characters in the requested exhaustion amount | The new readiness cases and existing `ThenIgnoresCharactersThatAreNotValidExhaustTargets` lose their legal dependent draw. |
| Report defender completion | Report no completion after committing the defender | `CompletedDefenderDeclarationResolvesItsDependentDraw` loses its dependent draw. |
| Bound delayed resumption to its body | Remove the delayed-frame traversal boundary | The delayed choice attempts to resume outside its detached body. |
| Serialize authored reveal abilities | Execute a later ability while the earlier ability is suspended | Both choice and power Surge ordering cases fail. |
| Bind Surge to its reveal | Ignore the continuation's exact reveal occurrence | `AgendaSurgeOwnershipTests` marks an unrelated reveal of the same physical card. |
| Bind indirect-damage receipts to their procedure | Match numeric occurrence ids instead of exact owners | `AgendaProcedureOwnershipTests` overwrites the outer procedure's committed amount with nested damage. |
| Move the scheduled batch, not an index slice | Select children by position after the owner | `AgendaFrameIdentityTests.RevealTailStaysBehindAnAlreadySuspendedChild` moves unrelated later work and loses the proper reveal tail order. |

After restoration, all 869 Rules tests passed. The original two-player Sonic
Boom failure was also rerun as a whole game: Klaw Standard, Iron Man and
Captain Marvel, game/policy seed 267, decision limit 1200. It completed with a
player-loss ending after 72 decisions and six rounds, with no simulation failure.
The strengthened Core Swarm Attack transcript checks the named first and second
Drone, damage after each attack, and the treachery remaining revealed until both
activations finish. Mark V Armor's elimination transcript retains its original
event digest after the exact-owner scheduling repair.

### Nested effect ownership mutation

`NestedEffectOccurrenceTests` exercises a printed Interrogation Room response
through `Sequence`: Spider-Man defeats a Hydra Mercenary, then the support
removes the final threat from a side scheme. It observes distinct occurrences,
the exact defeat retained by each, support attribution, both discards, and an
empty agenda after resolution. This is a component timing contract; the acting
Rhino Expert seed 26 regression separately exercises the public game loop.

Disabling `AbilityEffectBoundary.Begin` for all worlds compiled successfully
and killed the component test at its distinct-occurrence assertion. The source
was restored and the clean test passed. This checks ownership, rather than
merely accepting either of two cards from an aggregated defeat list.


### Defeat trigger instances and cancellation ownership

`DefeatTriggerBindingTests` exercises a shared response window through `Sequence`:
two defeated minions produce distinct offered handles, the player chooses the
second instance first, and each suspended choice retains its own defeater. The
resolution ledger records both completed instances. A separate admission case
rejects a response when one player's minion defeat and another player's scheme
defeat would satisfy different clauses only if their facts were mixed.

Five additional mutations compiled and failed behavioral assertions:

| Decision | Mutation | Observable failure |
| --- | --- | --- |
| Spend each triggering instance separately | Omit the defeat from the spent key | The first response consumes both instances. |
| Preserve the selected defeat across suspension | Drop the binding from the captured invocation | The resumed choice cannot identify its defeater. |
| Correlate every condition clause | Evaluate `defeatedByYou` against any defeat in the window | Another player's minion defeat is incorrectly offered. |
| Cancel the exact procedure owner | Include plan frames when selecting the cancellation owner | `AgendaFrameIdentityTests` cannot preserve the suspended ability's cleanup. |
| Retain the damage cause through nested work | Drop `DamageRequest.RecordDefeatOn` during placement | `NestedEffectOccurrenceTests` records the defeat on the ambient child instead of its cause. |

All sources were restored and the focused tests passed. The complete acting-game
acceptance lane passed all 76 cases, including Rhino Expert seeds 24 and 26.
The restored Rules lane passed all 871 cases; the Content lane passed all 2063
cases before the additional stable defeated-kind regression work.

### Identity attribution and captured defeat facts

The public-game Ground Stomp regressions cover one Hydra Mercenary, two
Mercenaries, and an Ultron Drone. Each defeated minion offers its own
Interrogation Room response; resolving one removes threat, exhausts the support,
and prevents another offer. The Drone case also observes its temporary profile
ending when the physical card leaves play. A public-game Mockingbird attack
regression verifies that an ally's minion defeat offers no Interrogation Room
response, while completing the attack and consequential damage.

Four further mutations compiled and failed behavioral assertions:

| Decision | Mutation | Observable failure |
| --- | --- | --- |
| Credit the actual identity performer | Credit an ally attack to its owner's seat | Mockingbird's defeat incorrectly offers Interrogation Room. |
| Credit event abilities to the identity | Return no identity from direct-effect attribution | All three Ground Stomp cases lose their required Room responses. |
| Retain the threat-removal cause through nested work | Drop `ThreatRemovalRequest.RecordDefeatOn` when recording scheme defeat | The defeat is absent from its captured cause in `NestedEffectOccurrenceTests`. |
| Retain the defeated character's classification | Read the physical card's current kind instead of the captured defeat kind | The defeated Drone loses its required Room response after its temporary profile ends. |

Each mutation was restored byte-for-byte. The restored attribution and causal
ownership focus passed all seven cases; the stable-kind mutation was separately
restored and all three Ground Stomp cases passed.

## Native gesture settlement predicate

The exhausted-card smoke now relies on the same completed-return predicate for
cancelled and invalid drags: restored local pose, inactive return motion, strict
transform basis, and less than 0.1 rendered pixel of global origin displacement.
It separately retains exact revision and commitment checks. A second,
coordinate-relative whole-transform comparison contradicted that predicate.

The normal local-game smoke runs a synthetic native fractional-parent probe.
A 0.03125-pixel parent translation distinguishes the two predicates; changed local
position, rotation, and excessive global displacement remain rejected. Replacing
the canonical comparison with the stricter whole-transform comparison produced
`FRACTIONAL_PARENT_PROBE accepted=false`; restored source passed. The corrected
scale-80 full journey passed. The original intermittent failure was not reproduced
in the initial full journey and five bounded focused repeats; this evidence proves
the predicate contradiction, not an independently reproduced production race.
