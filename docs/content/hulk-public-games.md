# Hulk public complete games

This bounded packet exercises ordinary admitted Hulk setup and accepted complete-game traces. It supplements the pack's focused card and authority review; it does not establish whole-pack acceptance or native Godot coverage.

## Method and fixed cases

`HulkPublicGameTests` uses the production `DatasetGameFactory.Load`, `GameSpecification`, deck assessment and `EngineHost.OpenGame` paths. It does not inject cards, move cards directly or construct a private research world. Each ordinary physical deck multiset after setup must equal its selected public recipe. Recommended modular sets come from the scenario recipe.

Eight fixed cases are declared: solo Rhino/1, Klaw/2, Ultron/3, Crossbones/4, Absorbing Man/5 and Rhino Expert/6; multiplayer Rhino/7; and multiplayer custom Rhino/8. Both multiplayer games use Captain Marvel in seat 0 and Hulk in seat 1. There is no victory gate or tactical seed search.

The custom deck uses Hulk's public starter with its last three cards replaced by three Resourceful (10032). Captain Marvel keeps her public starter. The forty-card custom deck must pass production construction assessment and its physical multiset must match after setup. This tests a legal custom-extras route; it does not claim every included card is played.

The deterministic policy prioritizes an offered legal Hulk basic attack, then payable legal Hulk-pack abilities, and otherwise delegates to `CoreGamePolicy`. Payment and targets use existing engine-offered constraints. This is an authored test tactic, not a legality rule. A focused policy control distinguishes using Experimental Research before changing form.

The independent factory game and host receive the same accepted decisions. At every step their current prompt, complete causal event sequence, cumulative RNG word count, state fingerprint and terminal result must agree. Policy selection must leave the canonical digest, full RNG state, pending prompt, phase, round, result, agenda and registered effects unchanged. Strict session JSON round-trip and production `SessionReplay.Verify` reconstruct the final complete trace and compare its canonical state, RNG and pending decision.

Per-decision output records answering seat, source, answer and pre-state digest hash. Characteristic counters record Hulk basic attack selections, signature plays, Experimental Research and the actual movement of every original card remaining in Hulk's hand when he declines his hero turn. The latter must move from that seat's hand to its discard pile. Each case asserts nonzero characteristic observations and a terminal engine result. Counters describe accepted selections and observed movement, not exhaustive branch coverage.

## Evidence

Runtime source: admission `728e47ab26ad451c87813f7b0747ac71852cfe16`, imported as `3cdeb07b` on the reviewed 718-row/all-33-face base `10af2709`. All eight complete-game cases passed. The final complete-case TRX also contains one failed policy-control label predicate; the control was corrected to identify the offered timed Banner source, then passed independently. That correction does not change game policy or any complete-game path. No engine divergence or unsupported operation occurred.

Command: `dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --filter FullyQualifiedName~HulkPublicGameTests --logger "trx;LogFileName=final.trx" --results-directory C:/Users/mggar/Source/evidence/hulk-public-games`.

The policy control passed after correction. A compiled mutant suppressing Hulk-pack ability priority (`100` replaced with `never`) failed the intended affordance assertion: expected Research handle 3, actual Change Form handle 1. The exact policy source was restored, a nonincremental Release build passed, and the same control passed again. `control.trx`, `policy-mutant.trx`, `restored-control.trx` and their logs retain the distinction. The earlier `mutant.trx` uses the uncorrected label predicate and is not mutation evidence.

The initial bounded run exposed a custom-fixture error (the She-Hulk donor included the identity-conflicting Hulk ally) and a policy coverage gap (defending/exhausting Hulk prevented basic attacks in several cases). The custom recipe and tactic were corrected without changing the eight seeds, runtime, admission rules or characteristic assertions.

| Case/seed | Heroes | Custom | Outcome | Round | Decisions | Hulk basic attacks | Signature plays | Hand discards/cards |
|---|---|---|---|---:|---:|---:|---:|---:|
| rhino/8 | captain_marvel,hulk | True | PlayersLose | 6 | 77 | 2 | 3 | 3/5 |
| absorbing_man/5 | hulk | False | PlayersLose | 5 | 30 | 2 | 4 | 4/7 |
| klaw/2 | hulk | False | PlayersLose | 5 | 34 | 2 | 2 | 4/6 |
| rhino/1 | hulk | False | PlayersLose | 6 | 40 | 3 | 5 | 3/5 |
| crossbones/4 | hulk | False | PlayersLose | 4 | 32 | 1 | 3 | 2/3 |
| rhino/7 | captain_marvel,hulk | False | VillainWins | 5 | 71 | 2 | 3 | 4/5 |
| rhino_expert/6 | hulk | False | PlayersLose | 3 | 21 | 2 | 2 | 2/5 |
| ultron/3 | hulk | False | PlayersLose | 2 | 29 | 2 | 2 | 1/2 |

Every game observed Experimental Research, paid for cards in every seat, played a card in every seat and reached a terminal result. Rhino multiplayer/7 also selected Hulk Smash (10003). Each complete archive passed strict durable JSON and production replay verification.

Exact complete trace summaries:

```text
rhino, heroes=captain_marvel,hulk, custom=True, seed=8: PlayersLose; round=6, decisions=77; basicAttacks=2, signaturePlays=3, research=1, handDiscards=3, discardedCards=5; played=01013,01015,01017,01018,01070,01071,10004,10005,10006,10015,10016,10019,10032; abilities=01010a,01013,01015,01018,01070,01071,01100,10001b,10004,10005,10006,10015,10016,10019; rngWords=160; finalSha256=30315A20C01178FDD2C1E1ECDDE129DA6A064D3FD0C39788E3B69472B403B5B8; traceSha256=552C44EC83095F4EDF26E8371D5E50C3B78E46DBB7FB698D2558FA054F7C470A; deck=10002,10002,10003,10003,10004,10004,10005,10005,10006,10006,10007,10007,10008,10009,10010,10011,10012,10013,10014,10014,10014,10015,10015,10015,10016,10016,10016,10017,10017,10018,10018,10018,10019,10019,10019,10020,10021,10032,10032,10032; archive=C:\Users\mggar\AppData\Local\Temp\marvel-hulk-public-games\rhino-8-True-True.session.json
absorbing_man, heroes=hulk, custom=False, seed=5: PlayersLose; round=5, decisions=30; basicAttacks=2, signaturePlays=4, research=1, handDiscards=4, discardedCards=7; played=10002,10005,10006,10009,10015,10019; abilities=10001b,10002,10005,10006,10015,10019; rngWords=165; finalSha256=496F39316BD09CDDB00A9B17B3DCC161C5A383D7A013C26DBAF952F25BD20F8C; traceSha256=A84DC83D72696AF2A3EED3D51C82392F48FF40A2FAB97CF77B1F353D565B4062; deck=10002,10002,10003,10003,10004,10004,10005,10005,10006,10006,10007,10007,10008,10009,10010,10011,10012,10013,10014,10014,10014,10015,10015,10015,10016,10016,10016,10017,10017,10018,10018,10018,10019,10019,10019,10020,10021,10022,10023,10024; archive=C:\Users\mggar\AppData\Local\Temp\marvel-hulk-public-games\absorbing_man-5-False-False.session.json
klaw, heroes=hulk, custom=False, seed=2: PlayersLose; round=5, decisions=34; basicAttacks=2, signaturePlays=2, research=1, handDiscards=4, discardedCards=6; played=10005,10006,10014,10015,10016; abilities=10001b,10005,10006,10014,10015,10016; rngWords=158; finalSha256=C0AA3E87730D678AACBA8DA3D64F52FE89657C88F0F204D553F1A60882D65A77; traceSha256=E5A1CFF35605EE2B7FBD9F5A26989391E4BCF755AB29D97DA48B2FC3C885ABEE; deck=10002,10002,10003,10003,10004,10004,10005,10005,10006,10006,10007,10007,10008,10009,10010,10011,10012,10013,10014,10014,10014,10015,10015,10015,10016,10016,10016,10017,10017,10018,10018,10018,10019,10019,10019,10020,10021,10022,10023,10024; archive=C:\Users\mggar\AppData\Local\Temp\marvel-hulk-public-games\klaw-2-False-False.session.json
rhino, heroes=hulk, custom=False, seed=1: PlayersLose; round=6, decisions=40; basicAttacks=3, signaturePlays=5, research=2, handDiscards=3, discardedCards=5; played=10002,10004,10006,10009,10015,10016,10018,10019; abilities=10001b,10002,10004,10006,10015,10016,10019; rngWords=125; finalSha256=DEBBB3AE33F57ED09A0E48B80461E0F9F6B469D4380B3175DFFE0BC0E0270593; traceSha256=1CE699CEE798322366F82F2F20914A7B4CDEFF5BB91AFC1E3DE68DF404606640; deck=10002,10002,10003,10003,10004,10004,10005,10005,10006,10006,10007,10007,10008,10009,10010,10011,10012,10013,10014,10014,10014,10015,10015,10015,10016,10016,10016,10017,10017,10018,10018,10018,10019,10019,10019,10020,10021,10022,10023,10024; archive=C:\Users\mggar\AppData\Local\Temp\marvel-hulk-public-games\rhino-1-False-False.session.json
crossbones, heroes=hulk, custom=False, seed=4: PlayersLose; round=4, decisions=32; basicAttacks=1, signaturePlays=3, research=1, handDiscards=2, discardedCards=3; played=10002,10010,10015,10016,10019; abilities=10001b,10002,10015,10016,10019; rngWords=157; finalSha256=CD7068C469AB38737C40CE80F63282208D8E23EBBE3D98D208291B8F7CE72E62; traceSha256=35CC8580853CFE63E654ABD1B12E3A55C5B198892F6E45DDA05796EB97F05B29; deck=10002,10002,10003,10003,10004,10004,10005,10005,10006,10006,10007,10007,10008,10009,10010,10011,10012,10013,10014,10014,10014,10015,10015,10015,10016,10016,10016,10017,10017,10018,10018,10018,10019,10019,10019,10020,10021,10022,10023,10024; archive=C:\Users\mggar\AppData\Local\Temp\marvel-hulk-public-games\crossbones-4-False-False.session.json
rhino, heroes=captain_marvel,hulk, custom=False, seed=7: VillainWins; round=5, decisions=71; basicAttacks=2, signaturePlays=3, research=1, handDiscards=4, discardedCards=5; played=01013,01016,01018,01067,01071,10003,10006,10008,10016,10019; abilities=01010a,01013,01018,01071,10001b,10003,10006,10016,10019; rngWords=193; finalSha256=F1B2A55445186B5EBCD261C95F88DEE96F3C616D44C2A69A7D6B2A5929147766; traceSha256=34F46B35119C96A73FDC17B047A3D307A069BC6C55B06F9F994CC22E173E1B57; deck=10002,10002,10003,10003,10004,10004,10005,10005,10006,10006,10007,10007,10008,10009,10010,10011,10012,10013,10014,10014,10014,10015,10015,10015,10016,10016,10016,10017,10017,10018,10018,10018,10019,10019,10019,10020,10021,10022,10023,10024; archive=C:\Users\mggar\AppData\Local\Temp\marvel-hulk-public-games\rhino-7-True-False.session.json
rhino_expert, heroes=hulk, custom=False, seed=6: PlayersLose; round=3, decisions=21; basicAttacks=2, signaturePlays=2, research=1, handDiscards=2, discardedCards=5; played=10006,10009,10016; abilities=10001b,10006,10016; rngWords=127; finalSha256=950634CD06B140C192C37080491CB52F597563F8CA7A673F842BBE3C82690746; traceSha256=6887A3A61DAEE0CEB4BFF7120407470FE9AFF4D944B1A9E8A6F04DD7E1A8E5F4; deck=10002,10002,10003,10003,10004,10004,10005,10005,10006,10006,10007,10007,10008,10009,10010,10011,10012,10013,10014,10014,10014,10015,10015,10015,10016,10016,10016,10017,10017,10018,10018,10018,10019,10019,10019,10020,10021,10022,10023,10024; archive=C:\Users\mggar\AppData\Local\Temp\marvel-hulk-public-games\rhino_expert-6-False-False.session.json
ultron, heroes=hulk, custom=False, seed=3: PlayersLose; round=2, decisions=29; basicAttacks=2, signaturePlays=2, research=1, handDiscards=1, discardedCards=2; played=10002,10006,10015; abilities=10001b,10002,10006,10015; rngWords=133; finalSha256=F36A412BB0D370A1824428FE2D06527B90A5BAAF0A17CBE6DF4941B5F62A4DB3; traceSha256=6E16349E47A667DAC067040ADC27BC7DEEF421A62E5EBE8933F42C1963505E83; deck=10002,10002,10003,10003,10004,10004,10005,10005,10006,10006,10007,10007,10008,10009,10010,10011,10012,10013,10014,10014,10014,10015,10015,10015,10016,10016,10016,10017,10017,10018,10018,10018,10019,10019,10019,10020,10021,10022,10023,10024; archive=C:\Users\mggar\AppData\Local\Temp\marvel-hulk-public-games\ultron-3-False-False.session.json
```

Retained evidence: `C:/Users/mggar/Source/evidence/hulk-public-games/` holds the final complete-case TRX/log, corrected policy control and mutation evidence, restored nonincremental build, eight canonical terminal archives under `archives/` and `characteristic-actions.json`. That index names an actual signature action in each archive, its one-based decision ordinal, exact before/after prompt, revision, state fingerprint and after-RNG count, for a separate fresh-host resumption packet. It is an index into accepted complete saves, not a new truncated or edited save.

Exact tested harness SHA-256 values:

- `HulkGameEvidence.cs`: `6AEC8AB2EC05C0CF0FD467E201BBE7466CB0D1E784E2C6274E199044E5140140`.
- `HulkGamePolicy.cs`: `C590F65B1D406F7E08EFB2BEB074B8AC8E7215621433F3C493C16A03A6D7EE69`.
- `HulkPublicGameTests.cs`: `E8EB96B628F03D0478A86F898C8F3A4AF0C9CF6E1842E78F9805B70FA77DBB84`.

Successful and failed-prefix canonical session archives are written outside the repository under the system temporary directory's `marvel-hulk-public-games` leaf. No test writes repository files.

## Scope

These cases address public setup, solo and shared-table engine decisions, legal deck customization and strict durable replay. They do not establish hosted network behavior, fresh-host resumption during a nested decision, campaign continuation, all-card play coverage, all encounter combinations, native interaction or player comprehension.

The separate [independent restart review](hulk-public-restart-review.md) covers
natural Unstoppable Force and paid multiplayer Thunderclap fresh-host boundaries.
