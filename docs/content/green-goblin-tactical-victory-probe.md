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
