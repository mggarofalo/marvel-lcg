# Green Goblin complete-game evidence

This packet tests the production `DatasetGameFactory` at base revision
`1616799b2585c656052890a0164d07af7dd5f933` with the accompanying acceptance tests.
It supplements the canonical pack delivery record; it does not establish pack
acceptance, native client acceptance or final CI success.

Every game uses the admitted scenario, its recommended modular set and the
unmodified supported hero starter. No card, zone, counter, deck order or world
state is changed by the tests. `CoreGamePolicy` submits choices from actual
engine prompts. The tests compare every resolution against a second production
deal receiving the exact same decisions: card digest, RNG words, outcome, phase,
round, complete prompt, events and information receipts. They assert the specific
terminal outcome and observed villain faces, plus actual card play, payment and
basic attacks. This is factory/public-admission evidence, not a transport or
durable-session restart test.

| Scenario | Starter | Seed | Outcome | Round | Decisions | Observed villain faces |
|---|---|---:|---|---:|---:|---|
| Risky Business Standard | Spider-Man | 1 | PlayersLose | 11 | 74 | 02001a/b |
| Risky Business Expert | Spider-Man | 2 | PlayersLose | 2 | 19 | 02002a/b |
| Mutagen Formula Standard | Spider-Man | 3 | PlayersLose | 5 | 47 | 02014, 02015 |
| Mutagen Formula Expert | Spider-Man | 4 | PlayersLose | 5 | 48 | 02015 |
| Risky Business Standard | Captain America | 3 | PlayersLose | 4 | 32 | 02001a/b, 02002a/b |
| Risky Business Expert | Captain America | 10 | PlayersLose | 10 | 79 | 02002a/b, 02003a/b |
| Risky Business Standard | Captain America | 8 | VillainWins | 10 | 81 | 02001a/b |
| Risky Business Expert | Captain America | 12 | VillainWins | 13 | 73 | 02002a/b |

These paths exercise actual terminal player elimination and scheme defeat.
Mutagen Standard promotes I to II; Risky Standard promotes I to II and Expert
promotes II to III. Observing both sides of the same Risky stage is a form flip,
not promotion. None of these games demonstrates a player victory. No campaign
continuation is claimed.

Run the focused lane from the repository root:

```powershell
dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --filter FullyQualifiedName~GreenGoblinGameTests --logger 'console;verbosity=detailed'
```

External Windows log: `C:/Users/mggar/AppData/Local/Temp/goblin-complete-final-eight.log`.
The reviewed source adds only this evidence, the acceptance class and its
production Server project reference. Broader managed, native and platform gates
remain with the integrated candidate.

## Concrete unsupported continuation

A separate characterization sweep found a reproducible public-game failure:
Captain America against Mutagen Standard seed 11, decision 20, and Expert seed
23, decision 37. Heroic Strike (`03004`) offers an Element prompt labelled
`03004: choose a card`, with Goblin Soldier (`02023`) and Goblin Thrall (`02024`).
Choosing the offered Soldier (affordance 58 Standard, 57 Expert), with empty
targets/resources/values/allocations, throws `RulesNotImplementedException`:
`character '02023' is defeated before a labelled-power continuation reads a defeat-triggered ability`.
The stack reaches `AbilityPowerAlternatives.PowerPaths` through
`AbilityStructuralPowerExecution.Power`. This blocks complete playable-pack
closure until independently fixed and replayed.

The exact reproduction source is retained outside the green project at
`C:/Users/mggar/AppData/Local/Temp/GreenGoblinGameTests-labelled-power-reproduction.cs`.
Its full stack, prompt and action are in
`C:/Users/mggar/AppData/Local/Temp/goblin-labelled-power-reproduction.log`.
These two extra rows deliberately fail before reaching the ending assertion;
throwing is not treated as an accepted game outcome.

Characterization searched Captain America seeds 1–24 in each Risky mode,
1–11 Mutagen Standard and 1–23 Expert (stopping on each unsupported continuation),
and Spider-Man seeds 1–200 in all four modes. No player victory was found.
Those exploratory sweeps are not committed tests or substitutes for the pinned
eight complete replays. Raw logs are `goblin-complete-search.log` and
`goblin-spider-search.log` in the same external Temp directory; the Spider-Man
search log has a stale Captain America output label, while its executable policy
used Spider-Man. Victory, Expert Mutagen promotion, multiplayer complete games,
pending-choice restart and native interaction remain unproved by this packet.

