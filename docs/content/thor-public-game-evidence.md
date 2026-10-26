# Thor public-game evidence checkpoint

Candidate: `96b14f94` plus setup candidate `c823a4b1` (isolated cherry-pick
`994518c7`), with the acceptance tests and tactics in this commit. This is
candidate evidence, not a merged or accepted Thor product. Setup independent
review, native/restart evidence and engine fix review are separate work packets.

## Scope and method

`ThorPublicGameTests` uses `DatasetGameFactory.Load` and its ordinary admitted
`Create(GameSpecification)` path. Every game uses the published Thor starter:
40 player cards; no custom replacement, rearranged draw, synthetic board,
research factory, damage shortcut or direct rules mutation. The test assesses
deck legality and verifies physical starter quantities after scenario setup,
including the physical player card used by Ultron's initial drone.

The authored test strategy uses the answering player's hand, public board and
engine offers. It prioritizes Worthy, Mjolnir, Asgard, God of Thunder, Defender
of the Nine Realms, Hammer Throw and Lightning Strike, pays through the existing
engine resource allocator, takes Thor's offered engagement response and selects
targets through the shared offered-target helper. These are test tactics rather
than gameplay rules. The strategy does not compensate for the known engine gap.

Each accepted decision is applied independently to a fresh second game from the
same public specification. The assertion compares canonical card state, RNG
words, outcome, phase, round, complete next prompt, events and information.
Policy evaluation also preserves that observation. The test asserts a terminal
outcome, completed game phase, absent replay prompt, paid actions, attacks and
actual Mjolnir play. It checks the engagement response adds exactly two hand
cards, and checks AERIAL whenever Thor is in hero form with Mjolnir in play.
This supplements focused authority-derived card tests; it does not replace them.

## Command and artifacts

Windows, Release, .NET 8:

```powershell
dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --filter FullyQualifiedName~ThorPublicGameTests --logger 'console;verbosity=detailed'
```

Raw final output: `C:/Users/mggar/Source/thor-public-games-final.log` (outside
the repository). The detailed output captures every answer with its ordinal,
round, answering seat, source face, prompt label and pre-decision state SHA256.
Final summaries capture the exact expanded starter list, outcome, rounds, ended
turns, decisions, payments, attacks, observed villain stages, RNG words, cards
played, selected abilities, engagement draws, God of Thunder resource uses,
Mjolnir installations, final state SHA256 and complete decision/result trace
SHA256. Rerunning the command regenerates those diagnostics without writing to
the repository.

Earlier diagnostic logs `thor-public-games-first.log`,
`thor-public-games-tactical.log` and `thor-public-games-expanded.log` are
investigation records. They are not substituted for final-source verification.

## Unresolved Hammer Throw promotion

Two ordinary public Rhino games reach the same production failure:

| Seed | Decision | Round | Source and target |
|---|---:|---:|---|
| 1 | 28 | 4 | Hammer Throw `06005`, choose Rhino I `01094` (object 49) |
| 8 | 15 | 2 | Hammer Throw `06005`, choose Rhino I `01094` (object 49), with minion `01103` also offered |

Both raise `RulesNotImplementedException`: `villain stage '01095' has an
unprojected When Revealed ability before a labelled-power continuation reads
the board`. The stack originates at `AbilityPowerSuspension.SuspendsPowerEffect`
and `AbilityStructuralPowerExecution.Power` during Hammer Throw's targeted
resolution, before its Mjolnir-return continuation finishes. The exact answers
and all preceding public decisions are retained in the final log.

`HammerThrowPromotionCompletesWithExactDecisionReplay` retains seed 1 as a red
regression. The main completion theory retains seed 8 as another red regression.
Neither case is skipped, converted to a passing expected-exception test or
counted as a complete game. Production repair is tracked separately as
MARVEL-759; this packet owns no production changes.

## Remaining acceptance

Eight representative games completed as policy losses before the final
diagnostic assertions were added: Rhino solo seeds 7/9/10, Klaw 2, Ultron 3,
Crossbones 4, Absorbing Man 5, and Rhino with Thor/Captain Marvel 6. The final
run results and hashes follow below. Crossbones reaches actual Lightning Strike,
God of Thunder, Defender and Mjolnir; the other games broaden card and scenario
coverage. Two-player seats use distinct identities and both receive turns.

No victory is proven by this checkpoint. Required next work is to repair and
independently review the promotion continuation, rerun the original two blocked
seeds, establish at least one public victory, and integrate native/restart and
broader required gates on the resulting coherent candidate. These games do not
establish difficulty-wide win rates, expert-mode coverage, campaign continuation,
all-player-count gameplay, every Thor card's execution or UI product acceptance.

## Final-source result

Final run: 10 cases, 8 completed and replay-identical losses, 2 blocked red regressions; 38.346 seconds. The added engagement-draw and hero-form AERIAL assertions pass in the eight completed games.

| Scenario | Heroes | Seed | Outcome | Round / ended turns / decisions | Engagement draws / God of Thunder uses / Mjolnir installs |
|---|---|---:|---|---|---|
| rhino | thor | 7 | VillainWins | 6 / 6 / 47 | 1 / 0 / 1 |
| absorbing_man | thor | 5 | PlayersLose | 4 / 4 / 30 | 1 / 0 / 2 |
| rhino | thor,captain_marvel | 6 | PlayersLose | 5 / 8 / 74 | 3 / 2 / 4 |
| crossbones | thor | 4 | PlayersLose | 4 / 4 / 44 | 2 / 0 / 1 |
| rhino | thor | 10 | PlayersLose | 4 / 4 / 31 | 1 / 0 / 1 |
| klaw | thor | 2 | PlayersLose | 3 / 3 / 27 | 1 / 2 / 1 |
| ultron | thor | 3 | VillainWins | 5 / 5 / 49 | 0 / 1 / 1 |
| rhino | thor | 9 | PlayersLose | 4 / 4 / 37 | 3 / 0 / 2 |

| Scenario / seed | Final canonical state SHA256 | Decision/result trace SHA256 |
|---|---|---|
| rhino / 7 | 4AF6547DE989B9D82A71FD9238A127545223716B2DEDCFE0BC67B57865EDBD5A | 8A2B90923ED4AABBE5BA63B40AE9E2543A8952648CBD2B3DAF7313062FE1C087 |
| absorbing_man / 5 | 922D3A53274011EB73DB55427D7A1DC1A1C05B5F9B09155FAD5D7392995C5E16 | 1ABD0BDE447CD352494CA2C25FAE3329AD51F1FDF46344AF90D6CCBD19743E7E |
| rhino / 6 | 83CEF9D7F21877E782787251AB2BFE5523DBA15BA061B3778B5B5CF56B5E7E9F | D6A24E485CE49002D1A68DF99666F97E3020B07A80A1DF9A6BB8D125ADAEFEF5 |
| crossbones / 4 | C06DBEE63DC938DD281C91CF6224658219B18D28663D4523446891F1D68A702A | 236377D54850E6997545DC07C49858B5BEA209CED259DAB4F168638F94AD5681 |
| rhino / 10 | A815E7B92868C69F4B2C6E1344BD47F572369B36C37F6BB6D98D184DC0FF4A69 | 165BE52CDAD0D312AE81BCD93FC24682ADEE69580BB03DC456BFD30474B943E3 |
| klaw / 2 | B6FA3C0E89096832ECF9567DDDDAEC9036A9F984080236EAF38B11EBB9811014 | 216EF2ECB9458A2FF7BB20827E6C152E67C7E40D1F3E74FADCD9493B12799259 |
| ultron / 3 | 5860C54E4773A520A80209ACF34A29DBBC725AB492526DCE5D9D08FBD6FA12EB | 1A6EAEEBD7F0B8E2B3584430E6ED701D016EE40DE638BAFA9DA4C73C0DBFE20E |
| rhino / 9 | 6B522C8BD35726A7ED3815003AE867482D2DF2B04277B0FBC122343E6E88FC93 | 7BB6AF830FFD365430E3F48E71322E2FFBB1E121429EFF28635DA85803C2E768 |

Final raw output SHA256: 2B04BCA4AF85EDFEA3214395745D93A81359E35CF8BDD9B3314AA6E36E827C90.

Normal pre-commit architecture, complexity and file-length hooks remain enabled. No full-suite pass or product acceptance is claimed by this checkpoint.


## Cohesive test decomposition verification

The normal complexity hook initially rejected three new test methods. Evidence capture now belongs to ThorGameEvidence, while recovery, defense, response selection and basic powers have separate test-policy methods. No tactics or assertions changed. A fresh focused run of the decomposed final sources again completed eight games with exactly the eight canonical-state and decision/result trace hash pairs above; the same two Hammer Throw promotion seeds remain red. Raw rerun output: C:/Users/mggar/Source/thor-public-games-refactored.log. SHA256: 02EE9070172755FE721FCDE8DB43A474ABD35D0B94A03AF76E03F98D3B6087F3. The focused complexity check passes at the unchanged maximum CCN of 10. Full normal hooks are run for the commit.
