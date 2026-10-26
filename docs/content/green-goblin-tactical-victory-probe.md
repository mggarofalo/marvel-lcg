# Risky Business Standard victory and remaining Goblin proof

On merged PR #450 revision d649560f, the admitted Captain America starter wins
Risky Business Standard with seed 1 in 13 rounds and 139 decisions. Every answer
uses a legal production offer through DatasetGameFactory and Game.Resolve.
A separately created game receives each exact answer; canonical state, RNG
consumption, prompt, outcome, phase, round, emitted events and information
match after every decision. No world/RNG mutation or seed search is used.
The retained test requires PlayersWin, GamePhase.Over and no replay prompt.

The strategy builds during the first three Norman rounds, uses basic attacks
before large attack events, favors useful allies/supports, spends weak cards,
defends with allies and recovers earlier. These are test-policy choices, not
rules. Final proof log: C:/Users/mggar/Source/goblin-victory-main-standard.log.
There were two recoveries and three ally defenses in the winning game.

Expert seed 1 still loses to the main scheme at round 17; both Mutagen seed 3
probes also lose. Other-mode winning-game obligations remain open. The test
covers only Risky Standard, not a relabeled defeat or completion of the pack.
Failed all-mode source is preserved externally in goblin-victory-all-mode-probe.cs;
raw earlier experiments are goblin-victory-{first,second,trace,captain,refined,
norman-basic,cap-ready-priority,build-norman,recovery,recovery-mutagen}.log.
Only Risky seed 1 and Mutagen seed 3 were explored. Earlier strategies and earlier
engine revisions are not evidence for this final passing candidate.

Independent policy review below covers the Standard victory; integrated CI remains required. Further
work should improve legal play or use a reviewed legal custom deck, retaining
exact replay and each unproven mode's victory requirement.

## Independent review after the ally-entry correction

Reviewed `6264976199305c6682e747e51b7ddb9a2c263814` in the isolated
`marvel-lcg-goblin-victory-review` tree. Source revision `a1fe492d` applies the
production changes from `d174876e`: ally entry responses open once when an
ally-limit choice suspends entry. The cherry-pick retains the compatible existing
`DiscardAndAllyLimitTests` update; its added `DiscardPlayDecisionTests` file is
absent from the victory base and was omitted from this isolated review, rather
than importing its unrelated prerequisite implementation.

Both the original proof and the strengthened proof pass on this corrected source:
`dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --filter FullyQualifiedName~GreenGoblinVictoryTests --logger 'console;verbosity=detailed'`.
The result remains `PlayersWin`, round 13, 139 decisions, two recoveries and three
ally defenses. Logs are outside the repository at
`C:/Users/mggar/Source/goblin-victory-independent-review.log` and
`C:/Users/mggar/Source/goblin-victory-independent-reviewed-test.log`.

The reviewed setup uses `DatasetGameFactory.Load` and the ordinary admitted
`GameSpecification`, with null custom decks and null modular overrides. The
strengthened test checks public discovery, the admitted 40-card Captain America
starter's deck assessment, Standard mode, the starting identity/villain/main
scheme, and observation of both faces of stages I and II before victory. It also
compares canonical card state, RNG consumption, outcome, round, phase and prompt
immediately before and after each policy answer, then retains the existing exact
answer replay and resolution-event/information comparison. This guards against
policy-side observable state changes; it is not a complete serialization of every
internal pending continuation.

Policy review found no hidden-information strategy or fixture bypass. Decisions
read the answering player's hand, public board/form/health/threat and offered
choice, payment and target data. `CoreGamePolicy` and `PolicyTargetSelection`
fallbacks use the same categories. Payment feasibility delegates to
`ResourcePayment.Allocate`; targets come from the offered constraints. No policy
code reads encounter/player deck order, advances RNG, constructs a replacement
world, or changes gameplay state. The use of full engine objects in a test policy
is not evidence for client visibility boundaries. Priorities and round thresholds
are strategy choices; this review adds no engine legality rules.

Verdict: the admitted Risky Business Standard winning transcript survives the
corrected ally-entry behavior with unchanged policy. This is focused acceptance
evidence, not a complete Green Goblin authority audit, final integrated CI result,
native UI acceptance, or winning proof for the other three modes.

## Bounded Expert strategy investigation (2026-10-26)

Base `67446424` was probed in the isolated `marvel-lcg-goblin-expert`
worktree. Seed 1 and the recommended Goblin Gimmicks modular were retained
throughout. No production code, card data, world state or RNG was changed.
Each probe retained exact-answer replay, every-observation comparison and the
policy query-purity observation. These failed attempts do not discharge the
Risky Business Expert winning proof. The reviewed Standard policy and
winning proof remain unchanged.

| Probe | Expert ending | Standard ending |
| --- | --- | --- |
| Reviewed policy baseline | Main-scheme loss, round 17 / 160 decisions | Win, round 13 / 139 decisions |
| Defend without Shield Block; return to hero at 7 health | Main-scheme loss, round 22 / 233 decisions | Win, round 18 / 180 decisions |
| Above, plus earlier readying and main-scheme threat control at 3 | HP loss, round 17 / 164 decisions | Win, round 13 / 125 decisions |
| Above, plus draw/setup and permanent cards before attack events | HP loss, round 13 / 125 decisions | Win, round 16 / 168 decisions |
| Above, with six Norman building rounds and return at 9 health | Main-scheme loss, round 17 / 171 decisions | Win, round 23 / 244 decisions |
| Reviewed policy with legal custom Justice deck in Expert only | Main-scheme loss, round 14 / 127 decisions | Original starter proof retained |

These are strategy experiments, not engine-rule changes. The baseline trace
shows nine recoveries after large undefended attacks. Defending more often
reduces damage but can leave Captain America exhausted; aggressive threat
control also consumes attacks needed to remove Norman's infamy counters.
The permanent-first experiment repeatedly flipped Norman while injured and
then needed another alter-ego recovery. Longer building failed to make progress
against stage II before scheme loss. These observations justify stopping this
threshold investigation rather than accepting a defeat as a victory.

The custom deck used the 15 admitted Captain America signature cards plus
Daredevil, Jessica Jones, Mockingbird, Nick Fury, Avengers Mansion, Helicarrier,
three For Justice!, three Surveillance Team, two The Power of Justice, Heroic
Intuition, three First Aid, three Emergency, Energy, Genius, Strength and
Interrogation Room. The public `AssessDeck` accepted it before both games were
created with the public `GameSpecification.PlayerDecks` API. The reason for
trying Justice was to free basic attacks by controlling threat with cards;
this one legal construction did not establish a winning strategy.

Command for each probe:
`dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --filter FullyQualifiedName~GreenGoblinVictoryTests --logger 'console;verbosity=detailed'`.
Logs are outside the repository at `C:/Users/mggar/Source/`, named
`goblin-expert-{baseline,defense,ready-threat,invest,build,justice}.log`.
The final failed custom-deck proof source is preserved there as
`goblin-expert-justice-probe.cs`; it retains the victory assertion and must not
be mistaken for an admitted passing test. No exploratory policy change is
retained. Expert victory is unproven by this investigation; Mutagen was not
probed here. This note does not reinterpret the pack acceptance criteria.


## Bounded Mutagen Formula Standard strategy probe

A separate isolated probe on base `67446424` used `mutagen_formula`, Captain
America and the fixed seed 3. It did **not** establish a victory. No new winning
acceptance test is admitted from this probe, and the reviewed Risky Business
policy is unchanged. The existing Standard/Expert defeat evidence remains valid;
this record does not claim pack completion. The canonical obligation is victory
and defeat for each scenario plus explicit difficulty coverage, rather than a
winning transcript for each difficulty.

Every attempted game was created by `DatasetGameFactory.Load` and the ordinary
public `GameSpecification`. Each custom deck was checked through
`factory.AssessDeck("captain_america", cards).IsValid` before creation. The
strategy read only the answering player's hand, public board/form/health/threat
and current engine offers. It used offered targets, `TargetRequest.Allows` and
`ResourcePayment.Allocate`; there was no hidden deck-order read, world mutation,
RNG mutation, setup bypass or seed search. Each decision was resolved against an
independently created replay game and compared canonical state, RNG words,
outcome, phase, round, prompt, events and information. The policy-answer step
also retained the pre/post observable-state equality assertion. This is not a
serialization of every internal pending continuation.

The first probe used the admitted Captain America starter. It prioritized
minions, ally defense and recovery, and ended `VillainWins` at round 11 after
111 decisions. Its raw first-failure trace is preserved as
`C:/Users/mggar/Source/mutagen-victory-probe-evidence/mutagen-victory-first.log`.
Subsequent public-board strategy changes preserved the fixed seed: protect the
Shield from payment, build Serum, use Hawkeye/Quinjet responses, ready Captain
before spending his hand, favor enemy removal and select more Shield Toss
hand discards for a visible swarm.

Two legal 40-card custom-deck approaches were also attempted. Leadership used
Maria Hill/Nick Fury, additional economy and healing instead of the starter's
expensive Avengers Assemble/Quinjet package; Aggression used Relentless Assault,
Uppercut, Chase Them Down and Tac Team to address Mutagen's minion pressure.
Neither established a win. These were strategy choices, not new legality rules.
The latest Aggression attempt ended `PlayersLose`, round 8, 94 decisions. Its
`PlayersWin` assertion failed; the assertion was not weakened to admit the loss.
The last logged attack sequence left Captain at 9 damage, followed by the
engaged Goblin Knight taking him to 11 damage. Earlier Leadership attempts lost
to threat after an alter-ego recovery turn while multiple minions remained.

Failed probe sources, including the complete last custom deck, and raw run logs
are preserved outside the repository at
`C:/Users/mggar/Source/mutagen-victory-probe-evidence/`. The final source is
`MutagenVictoryTests.cs`, `MutagenTacticalPolicy.cs` and
`MutagenPolicyPayment.cs`; `mutagen-leadership-failed-test.cs` preserves the
Leadership deck. The first raw log is retained independently; the first policy
source was iterated and is not represented as an exact retained source snapshot.
The focused command was:

```powershell
dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --filter FullyQualifiedName~MutagenVictoryTests --logger 'console;verbosity=detailed'
```

The temporary failing tests/helpers were removed from the candidate rather than
committed as passing coverage. No engine defect is established by these losses.
A next bounded attempt should address the visible causal failure: preserve enough
resources for minion removal while keeping Captain active and preventing an
alter-ego turn from handing a live minion board enough scheme activations to end
the game. Mutagen Standard victory with exact replay remains open.


The first starter trace identifies a concrete committed tactical error. Decision
8 uses Shield Toss, discards Heroic Strike and returns Captain America's Shield
to hand. Decision 12 then uses I Can Do This All Day, discarding that sole hand
card (object 20). This trades away the Shield's DEF/retaliate benefit and Shield
Block enabler until it is drawn again. Later strategies explicitly preserve and
replay the Shield; that correction alone is not winning evidence.

The same first trace's terminal sequence is also concrete: decision 104 changes
to alter-ego at 8 damage with 10 main-scheme threat. Steve's Apartment heals to
7 damage; an ally thwart reduces main threat to 8. End Phase decision 111
resolves to `VillainWins` before another prompt. Mutagen Cloud's printed target
is 11 per player. The terminal cause is main-scheme completion during the
following villain phase, rather than identity damage or deck exhaustion. The
text trace does not itemize each automatic threat placement in that resolution,
so it does not attribute the final three threat to a specific boost or minion.
