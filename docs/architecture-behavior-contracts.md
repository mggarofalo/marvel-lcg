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
