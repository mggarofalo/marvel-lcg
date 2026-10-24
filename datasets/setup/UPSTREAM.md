# The setup dataset — authored

What Core Set and Rise of Red Skull games are dealt from: scenario, villain and
main scheme decks, encounter sets, and seven admitted printed starter decks.
The Captain America starter is declared as research and excluded by runtime product admission.

| | |
|---|---|
| Kind | authored |
| Declared products | Marvel Champions Core Set; Rise of Red Skull; Captain America (research) |
| Records | 16 scenario modes, 8 heroes, 10 encounter sets |
| Gated by | `SetupDatasetTests`, `RiseOfRedSkullSetupDataTests`, `CaptainAmericaSetupTests`, complete-product admission |

## Why it is authored

Most setup facts are printed in the Learn to Play guide, scenario instructions,
and deck lists rather than on cards. There is no complete upstream dataset to
vendor, and the values cannot be regenerated from `datasets/cards/`.

The Rise of Red Skull recipes follow the vendored `pack:mc10` scenario and
starter-deck entries. Physical copy counts are checked against the pinned
MarvelSDB pack snapshot. The complete-product admission gate requires all 181
Rise of Red Skull printed faces before exposing its runtime recipes.

## What the gate holds

`SetupDatasetTests` verifies that:

- every referenced card exists in `datasets/cards/`;
- every named encounter or modular set is defined;
- every scenario has valid main schemes and villain stages;
- every hero has two identity faces, an obligation, and a nemesis set;
- the declared products contain 16 modes, 8 heroes, and 10 sets;
- required encounter sets remain fixed when optional modular sets change;
- the header counts match the file.

Every failure is one a game would otherwise meet while dealing a board.

Captain America's 40-card starter, obligation and five-card nemesis set follow
the pinned FFG divider and transcription in `datasets/rules-packs/mc04/UPSTREAM.md`.
`CaptainAmericaSetupTests` verifies every quantity and ordinary deck legality,
and proves that even complete authoring does not bypass explicit product admission.
