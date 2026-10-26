# The setup dataset — authored

What Core Set, Rise of Red Skull, Captain America and Wrecking Crew games are
dealt from: scenario, villain and main scheme decks, encounter sets, and eight
admitted printed starter decks. The Ms. Marvel starter and A Mess of Things
modular set are declared as research and excluded by runtime product admission.

| | |
|---|---|
| Kind | authored |
| Declared products | Marvel Champions Core Set; Rise of Red Skull; The Wrecking Crew; Captain America; Ms. Marvel (research); Green Goblin (A Mess of Things research recipe) |
| Records | 35 scenario modes, 9 heroes, 11 encounter sets; runtime admission exposes 35 modes, 8 heroes and 10 sets |
| Gated by | `SetupDatasetTests`, `RiseOfRedSkullSetupDataTests`, `CaptainAmericaSetupTests`, `MsMarvelSetupTests`, `MessOfThingsSetupTests`, complete-product admission |

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
- the declared products contain 35 authored modes, 9 heroes, and 11 sets;
- required encounter sets remain fixed when optional modular sets change;
- the header counts match the file.

Every failure is one a game would otherwise meet while dealing a board.


Wrecking Crew recipes cite `pack:mc03:additional-setup` and
`pack:mc03:adjustable-difficulty` in the vendored rules corpus. Nineteen recipe
keys expose Standard, Expert, Extreme, and all sixteen independently selected
A/B combinations as Custom. Each physical villain group owns its printed side
scheme and fifteen-card deck; no obligations, nemeses, standard/expert encounter
sets, or modular sets are added. Wrecker starts active. Complete-product admission
exposes Wrecking Crew's accepted recipes. `WreckingCanonicalRecipeTests` checks
all nineteen recipes at one through four players.

Captain America's 40-card starter, obligation and five-card nemesis set follow
the pinned FFG divider and transcription in `datasets/rules-packs/mc04/UPSTREAM.md`.
`CaptainAmericaSetupTests` verifies every quantity and ordinary deck legality,
and proves that the accepted complete product enters runtime admission.

Ms. Marvel's 40-card starter, obligation and five-card nemesis set follow the
pinned FFG divider photograph and transcription in `datasets/rules-packs/mc05/UPSTREAM.md`.
`MsMarvelSetupTests` checks the exact quantities, ordinary deck legality and
continued exclusion from runtime discovery until complete product acceptance.

A Mess of Things uses the set membership and printed faces in
`datasets/cards/cards.json`. Physical quantities follow the pinned MarvelSDB
`datasets/marvelsdb/pack/gob_encounter.json` snapshot (commit
`7cdf83e7a2189b27e94e2b0c0302627b1ee9395c`): 02037 (A Mess of Things),
02038 (Scorpion) and 02039 (Gang-Up) have one copy each; 02040 (Tail Sweep)
has two copies. `MessOfThingsSetupTests` checks all five cards in composition
order and exclusion from runtime discovery even when every face is supplied
as authored. This recipe does not admit the Green Goblin product.
