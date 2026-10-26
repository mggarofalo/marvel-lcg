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

Independent policy/authority review and integrated CI remain required. Further
work should improve legal play or use a reviewed legal custom deck, retaining
exact replay and each unproven mode's victory requirement.
