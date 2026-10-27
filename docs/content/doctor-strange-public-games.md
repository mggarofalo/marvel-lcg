# Doctor Strange public complete games

This record covers the MARVEL-838 public-game packet based on `d887173f0eed622cbabbcd6a466d7744bdccc75d`. The complete candidate contains the actual 685-row ability book, all 40 Doctor Strange playable faces, the pinned mc08 starter and normal five-card Invocation setup. It does not replace the pack's focused card/rules evidence or certify native interaction.

## Method and bounded cases

`DoctorStrangePublicGameTests` uses `DatasetGameFactory.Load`, ordinary `GameSpecification` and `EngineHost.OpenGame`. It neither prepares a world nor creates or moves cards directly. It asserts the public catalog's exact 40-card mc08 starter, assesses the chosen deck through production deck admission, and compares the physical ordinary deck after normal setup. Invocations remain outside that forty-card comparison.

The deterministic test policy prefers payable, legal offered Doctor Strange actions and uses existing engine-constrained payment/target selection and `CoreGamePolicy` otherwise. This is a test-policy choice, not game legality. The focused policy test distinguishes using offered Natural Talent before changing form. The gameplay path has one seeded MT19937 stream and no tactical seed search.

Eight distinct seed/deck cases are the initial bounded candidate set (maximum twelve authorized): Rhino/1, Klaw/2, Ultron/3, Crossbones/4, Absorbing Man/5, Rhino Expert/6, Absorbing Man Expert/7, and Rhino custom/8. The last two are Captain Marvel in seat 0 and Doctor Strange in seat 1. All other games are solo Strange. Recommended modular sets are selected by the normal scenario recipes.

The custom deck is Strange's fifteen signature cards plus the first twenty-two non-signature cards from She-Hulk's admitted starter and three copies of Skilled Strike (09037). Captain Marvel uses her admitted starter. Production assessment must accept this standard-construction 40-card Aggression deck; the opened physical card multiset must match it exactly. Presence in this deck does not imply that every card was played.

For every accepted decision, an independent public host must produce the same current prompt, exact raw event sequence, cumulative RNG count, state fingerprint and terminal result as the direct public-factory game. Policy selection must leave the card digest, RNG, result, phase, round and pending prompt unchanged. Strict session JSON round-trip and `SessionReplay.Verify` then reconstruct the complete terminal trace. The trace hash includes each decision and complete post-decision observation.

## Evidence

The final Release run passed seven tests: the policy control and six complete games. Two games remain blocked by the same genuine runtime failure. No new seed/deck candidates were added. All completed games ended in a loss; hero evidence has no invented victory gate.

Command: `dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --no-build --filter FullyQualifiedName~DoctorStrangePublicGameTests --logger "trx;LogFileName=final-public.trx" --results-directory C:/Users/mggar/AppData/Local/Temp/strange-public-games`.

Result: 9 tests, 7 passed, 2 failed, 0 skipped; 3m54s. Runtime revision: `d887173f0eed622cbabbcd6a466d7744bdccc75d`; the test harness is the proof commit containing this record. A restored nonincremental Release build completed with zero warnings/errors. Normal commit hooks provide the architecture/complexity/layout checks.

The policy control passed. Replacing the 090 prefix priority with a never-matching prefix compiled and failed the intended Natural Talent assertion (expected affordance 4; actual 1). The source was restored before the nonincremental build and final nine-test run.

The initial eight-case run also had a test-only assertion requiring Skilled Strike to be played. That requirement was outside the requested custom-deck proof and incompatible with the policy normally exhausting Strange for Spell Mastery. It was removed; the same seed/deck/policy then completed exact replay. No engine legality, ability, card or deck was changed.

The per-decision trace reports answering seat, source, selected answer, public Invocation top and pre-decision digest hash. Aggregate characteristic counters record accepted Spell Mastery, Natural Talent and Master of the Mystic Arts selections, plus actual physical Invocation discard movements. A source counter is not a claim that every branch or every possible Invocation effect was exercised. Full-game terminal results and accepted-decision replay supplement focused Special implementation tests.

## Scope

These games prove the tested public setup and complete accepted traces. They do not claim a campaign continuation, all possible encounter combinations, all cards played, a victory requirement, hosted multiplayer networking, a new host started mid-payment/target/Master return/recycle, or native Godot/player-comprehension acceptance. Deep fresh-host resumption is a separate packet.

## Exact completed traces

The counters below are accepted source selections; physical discard faces are actual movement events. Rhino/1 and Ultron/3 naturally selected Master of the Mystic Arts. Every completed game selected Spell Mastery and Natural Talent.

```text
absorbing_man_expert, heroes=captain_marvel,doctor_strange, custom=False, seed=7: PlayersLose; round=7, decisions=120; spellMastery=2, naturalTalent=1, master=0; played=01012,01013,01017,01018,01067,01070,01071,01074,01086,09003,09004,09007,09008,09015,09016,09019,09020,09021; abilities=01010a,01012,01013,01018,01070,01071,01086,04084,09001a,09001b,09003,09004,09007,09013,09015,09016,09019,09020,09021; invocationDiscards=09033,09035,09036; rngWords=388; finalSha256=0E446A35874A84BFAF7CFD58B1203363358DA54E5B74AF3C1701A9CD275969A8; traceSha256=D765FF8D7ADD6A608E310BFA8B4F6A20FE1ECFD43EF6DB9449D18E0A63A1F57E
crossbones, heroes=doctor_strange, custom=False, seed=4: PlayersLose; round=5, decisions=37; spellMastery=2, naturalTalent=1, master=0; played=09003,09004,09007,09016,09020,09021; abilities=04072,04149,09001a,09001b,09003,09004,09007,09016,09020,09021; invocationDiscards=09032,09035,09036; rngWords=171; finalSha256=D8F5B5E0A0563BBEFA0E86314EAA7CE592A191800E38B35C4649DE8A70AC1A82; traceSha256=EB499CF43279DDAD452DCCCCC9B4492C23DF4DF27B6732200F2ED58354342F48
klaw, heroes=doctor_strange, custom=False, seed=2: PlayersLose; round=4, decisions=55; spellMastery=5, naturalTalent=1, master=0; played=09003,09004,09006,09009,09010,09015,09019,09020,09021,09026; abilities=09001a,09001b,09003,09004,09006,09009,09015,09019,09020,09021; invocationDiscards=09032,09033,09034,09035,09036; rngWords=207; finalSha256=DC41B812097A49865448405DBE294C4E6632FB8A8E3623D690067AEB0B0C8A10; traceSha256=CD7A32BF5311B16CE1F1D132E0BB19EA8E02B516FA07148C86A7DF6CF74ECCEA
rhino, heroes=captain_marvel,doctor_strange, custom=True, seed=8: PlayersLose; round=4, decisions=61; spellMastery=2, naturalTalent=2, master=1; played=01013,01015,01017,01018,01054,01070,09003,09005,09006; abilities=01010a,01013,01015,01018,01054,01070,09001a,09001b,09003,09005,09006; invocationDiscards=09032,09033,09034,09035; rngWords=207; finalSha256=77CFC156B62F590124CAE9A0EFFB42EE2DFA7749916D62E07CEC450B9DD3C81A; traceSha256=3A1C244E4D82AFCEA17E4B1F5BB9E6E6FFCEBF4A732CAEBF4D06B9ABE1913146
rhino, heroes=doctor_strange, custom=False, seed=1: VillainWins; round=5, decisions=50; spellMastery=5, naturalTalent=2, master=2; played=09003,09004,09005,09008,09009,09015,09021; abilities=09001a,09001b,09003,09004,09005,09008,09009,09015,09021; invocationDiscards=09032,09033,09034,09035,09036; rngWords=150; finalSha256=2D07B24EDC260B12D8FD0C7882FD289EAF42B94EC01398F96D512D69F03E396A; traceSha256=3A58C250954B27885F921BB94A4C01A2CC22F4B601CDE318D49E7A2B88ACF157
ultron, heroes=doctor_strange, custom=False, seed=3: PlayersLose; round=6, decisions=67; spellMastery=2, naturalTalent=2, master=1; played=09002,09004,09005,09010,09012,09015,09016,09020,09021; abilities=01142,09001a,09001b,09002,09004,09005,09012,09015,09016,09020,09021; invocationDiscards=09032,09033,09034,09035,09036; rngWords=181; finalSha256=9012EE716101EEB5D2DA3B8B926131EF8B5AED5CB82E3C54F951C048298726D8; traceSha256=57923DCFCF25C34C92F364259B405C0B75CC63A51143C1F3C5B6AD5A27E2A292
```

Custom Aggression (exact ordered recipe):

```text
09002,09003,09003,09004,09004,09005,09005,09006,09007,09007,09008,09009,09010,09010,09011,01050,01051,01052,01052,01053,01053,01054,01054,01055,01055,01056,01056,01057,01057,01083,01084,01085,01086,01087,01088,01089,01090,09037,09037,09037
```

Published starter (exact ordered recipe):

```text
09002,09003,09003,09004,09004,09005,09005,09006,09007,09007,09008,09009,09010,09010,09011,09012,09013,09014,09015,09015,09015,09016,09016,09016,09017,09017,09018,09018,09019,09020,09020,09020,09021,09021,09021,09022,09023,09024,09025,09026
```

## Known runtime failures

- `absorbing_man, multiplayer=False, custom=False, seed=5, decision=96, round=12`. The engine offers legal/payable Astral Projection (09003), then raises `RulesNotImplementedException: '09003' cannot initiate this action in the current state`. The prior accepted prefix matched the independent public host. This is an uncompleted game and remains a failing test.
- `rhino_expert, multiplayer=False, custom=False, seed=6, decision=12, round=2`. The engine offers legal/payable Astral Projection (09003), then raises `RulesNotImplementedException: '09003' cannot initiate this action in the current state`. The prior accepted prefix matched the independent public host. This is an uncompleted game and remains a failing test.

The failure-only harness writes canonical accepted-prefix session JSON outside the repository. Those archives contain 95 accepted decisions for Absorbing Man/5 and 11 for Rhino Expert/6; the next answer is retained in the per-decision output. Engine repair must make these same candidates complete without skipping the card or altering the ability book.

Retained artifacts: `C:/Users/mggar/Source/evidence/strange-public-games/` contains final TRX/log, control/mutant TRX/log, restored build log and both canonical session prefixes. `C:/Users/mggar/Source/evidence/strange-public-game-astral-seed5.log` is the bounded exact failure trace. The TRX retains every chosen decision and pre-state hash for both passing and failing games.

Exact tested harness SHA-256 values:

- `DoctorStrangeGameEvidence.cs`: `D16A2353F20C287E0FE3295CAAE3F07035F18D6D197BE808F265BD17693F1EA8`.
- `DoctorStrangeGamePolicy.cs`: `510FDDD5563935BB29A22CF4165BA34346C37647361C126791D77950C131D8A1`.
- `DoctorStrangePublicGameTests.cs`: `C1033F7AA4144E2F1C56E1EA3F90097C233F34206DBC40A3AC32F6A8D6C0A559`.
