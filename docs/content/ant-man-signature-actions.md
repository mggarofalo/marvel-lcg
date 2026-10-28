# Ant-Man signature actions packet

Base `b13de4fa0f00d13d158cbd5355c49b3887f80258`; source is the commit containing this record. This is a bounded content packet for MARVEL-569, awaiting independent review. It does not establish complete Ant-Man implementation or public admission.

The production book appends four actual faces: Wasp `12002`, Hive Mind `12004`, Army of Ants `12007`, and Wrist Gauntlets `12010`. There are 742 rows and 19 Ant-Man faces. The complete 743,478-byte baseline row prefix is unchanged. Exact expected totals and the independent Ant-Man face inventory include these four additions; unsupported-pack controls remain.

Authority is the joined printed text and attributes in `datasets/cards/cards.json`, with current RR 1.8 ability, cost, control, wild-resource and ally clauses. Local MarvelCDB FAQ and ruling searches found no specific entries for Giant Stomp, Hive Mind, Army of Ants or Wrist Gauntlets. Wasp's printed entry response is distinct from the unrelated later Wasp hero and ally rulings. No datasets were acquired or regenerated.

`AntManSignatureActionTests` uses the complete current production book on explicitly synthetic multiplayer boards, with Ant-Man as player 1. It covers Wasp's current-controller form branch, no alter-ego response, printed ATK/THW/HP and consequential damage; Hive Mind with zero, one and three controlled supports and foreign/out-of-play exclusions; Army's Tiny form gate, source exhaustion and controller; Gauntlets' form branches, two typed resources, declared wilds, insufficient payment, rejected wrong-resource payment before any cost, status kind and exhaustion. Same-cost controls make wrong-form absence meaningful. Accepted actions drain actual continuations and assert an unfinished game and/or idle agenda.

## Verification

Windows Release, `MSBUILDDISABLENODEREUSE=1`, `-m:1`, in the isolated `marvel-ant-signature-actions` worktree:

- Initial focused run: 58 passed, three failed. Two Wasp fixtures supplied targets to the optional response instead of answering its subsequent conditional target prompt. The diagnostic expected the wrong substring. These fixture assertions were corrected without engine changes.
- Corrected focused run: 61 passed, none failed or skipped.
- Four-row data mutation: Wasp Giant damage 2 to 3; Hive base threat 2 to 3; Army damage 1 to 2; both Gauntlet typed costs two icons to one. The compiled test assembly ran 18 tests: seven failed at the intended observations (one Wasp, three Hive, one Army, two Gauntlet single-wild affordability checks), 11 passed.
- Restored book followed by a forced `dotnet build --no-incremental`: zero warnings/errors. The full affected Ant-Man, ability-data and form-destination class filter passed 121 tests, none failed/skipped.
- Final diagnostic-fixture cleanup replaced an unused duplicate Thor identity with a directly constructed Ant-Man board. The same 121-test filter passed again, none failed/skipped.

External evidence: `C:/Users/mggar/Source/evidence/ant-signature-{initial,corrected,mutant,restored-build,restored-tests,final}.log`; the restored book is `ant-signature-restored.json`. The filter is `FullyQualifiedName~AntMan|FullyQualifiedName~AbilityData|FullyQualifiedName~AbilityFormDestination`. These are focused results, not a complete managed or cross-platform candidate run.

## Remaining scope

Giant Stomp `12003` remains unauthored. `AntManGiantStompBoundaryTests` preserves one bounded diagnostic: an attack whose singular `target` is the plural `minions` query is rejected before payment, with an otherwise legal same-cost Swinging Web Kick control. This demonstrates an invalid candidate encoding. It does **not** establish that the printed mechanic lacks a supported DSL composition or justify an engine prerequisite. A targeted authority/DSL composition packet must decide that separately.

Giant Strength `12009` remains outside this packet (MARVEL-874), and Pym Particles `12006` belongs to its separate resource packet. No engine, setup, replay, admission or schema code changes are included.
