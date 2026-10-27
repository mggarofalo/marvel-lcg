# Actual Judge, Jury, Executioner authoring

This packet appends actual card 90002 to the production ability book at base
`7ba172f66610afbce156eb3f73716eba985a81fb`. It does not admit Kree Fanatic or
change setup, engine behavior, or replay contracts. Production rows now total
722, including 90001, 90003, 90005 and 90002; 90004 remains unauthored.

Printed authority is `datasets/cards/cards.json`, 90002: after a friendly
character is defeated by an enemy attack, place 2 threat on the main scheme;
its Boost puts this side scheme into play. Starting threat is 3 per player.
The vendored MarvelCDB FAQ contains no card-specific 90002 entry.
RR 1.8 `rr:friendly` includes all player-controlled cards;
`rr:attack-enemy-activation.step.6.a` and `rr:triggering-condition.2` place
attack damage and defeat responses together after attack completion.
`rr:triggering-condition.1` permits one response per actual character defeat,
so two indirect recipients or ally-plus-overkill identity defeat produce four
threat. Current `rr:overkill.1` requires actual ally defeat; `.2` treats the
excess identity damage as attack damage even in alter-ego form.

The authored trigger uses WhenDamageDealt plus WhenCardDefeated, enemy actor,
and friendly target, with damageFromAttack. These DSL spellings are engine
choices. AbilityTriggerInstances matches the exact attack-damage recipient and
defeat object/incarnation before admission. The friendly role reads captured
OccurrenceCard controller facts. It does not compare with the resolving player's
identity or depend on current play-area membership after departure/elimination.
Boost follows ordinary printedDestination entry and survives boost cleanup.

The default RonanJudgeProbeFixture now uses actual 90002 while preserving
synthetic observers and explicit test-local alternative-trigger probes. Actual
ordinary-board tests use the production ability book. Focused evidence covers
reveal/Boost entry with six threat at two players; living and defeated allies
of both controllers; foreign defending hero elimination with the game surviving;
foreign ally overkill defeating its alter-ego controller; surviving-game attacked
identity elimination; simultaneous indirect defeats; prevented/replaced defeat;
nested nonattack damage; boost damage; retaliation; cancelled following attacks;
and an enemy-defeat exclusion. The enemy-target attack is explicitly synthetic
and uses the real damage procedure to isolate the friendly-role guard.

Windows focused results: 59/59 passed, including exact-count owning methods,
AbilityDataTests, RuleCitationTests and enemy-defeat review cases. This is focused
engine evidence, not a full-suite, Linux, native-client or public scenario claim.
The initial diagnostic run had 28 passes and two fixture failures: the optional
observer belonged to the eliminated helping player. Keeping that synthetic
observer with the surviving player exposes the intended completed response
window without changing production behavior or weakening the threat assertions.

A compiled actual-card mutation changed target friendly to ally. Both foreign
identity cases failed for their two/four-threat assertions. Exact original bytes
were restored, then a non-incremental Release build passed and the same complete
focused selection passed 59/59. Logs outside the repository:

- `C:/Users/mggar/Source/evidence/ronan-90002-focused.log`
- `C:/Users/mggar/Source/evidence/ronan-90002-mutant.log`
- `C:/Users/mggar/Source/evidence/ronan-90002-restored-build.log`
- `C:/Users/mggar/Source/evidence/ronan-90002-restored.log`
- `C:/Users/mggar/Source/evidence/ronan-90002-filter.txt`

All 721 preceding raw JSON objects are byte-exact; only a comma and the new
object are appended before the original closing array/object. UTF-8 and LF are
preserved. Baseline ability-book SHA-256:
`9411a9fd446fb0c50d0957d9f214bb0a67a51ffb0d41e658dc4501662334bee8`.
Candidate ability-book SHA-256:
`e9dab33c11d889586167f0b451a8168d8cba60fe8a622ef9ecd570ad56d10850`.

Independent content review remains separate. Whole Ronan completion still needs
actual 90004 after its independent engine prerequisite, complete modular setup
and public admission, combined inventory/behavior checks, public gameplay and
save/replay evidence. The attack-defeat prerequisite's 08031 timing replay
boundary is owned by the separate replay packet. This authoring packet does not
claim any of those outcomes or require owner acceptance for ordinary content.
