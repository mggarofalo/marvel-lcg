# The setup dataset — authored

What Core Set, Rise of Red Skull, Captain America and Wrecking Crew games are
dealt from: scenario, villain and main scheme decks, encounter sets, and eight
admitted printed starter decks. The Ms. Marvel starter, Risky Business modes,
A Mess of Things and Goblin Gimmicks modular sets are declared research and
excluded by runtime admission.

| | |
|---|---|
| Kind | authored |
| Declared products | Marvel Champions Core Set; Rise of Red Skull; The Wrecking Crew; Captain America; Ms. Marvel (research); Green Goblin (Risky Business and modular research recipes) |
| Records | 37 scenario modes, 9 heroes, 12 encounter sets; runtime admission exposes 35 modes, 8 heroes and 10 sets |
| Gated by | `SetupDatasetTests`, `RiseOfRedSkullSetupDataTests`, `CaptainAmericaSetupTests`, `MsMarvelSetupTests`, `MessOfThingsSetupTests`, `RiskyBusinessSetupTests`, complete-product admission |

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
- the declared products contain 37 authored modes, 9 heroes, and 12 sets;
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

Risky Business Standard and Expert recipes follow printed 02004a Contents and
`pack:mc02:how-to-use-this-pack`. The pinned `gob_encounter.json` snapshot above
supplies 02007×3, 02008×4, 02009×1, 02010×2, 02011×2, 02012×4 and 02013×2
(eighteen encounter copies), three physical double-sided villain stages, two
physical double-sided main schemes and one double-sided environment. Each mode
allocates only its selected two villain stages. Criminal Enterprise/State of
Madness (02006a/b) begins set aside; printed 02004a setup puts it into play.
Goblin Gimmicks is the printed one-set recommendation: 02033–02036×2 each,
eight encounter cards. This composition record does not implement those cards.
The normal Expert convention adds the Expert set alongside Standard.
`RiskyBusinessSetupTests` checks exact recipes, 1–4-player production deals with
one supported chosen modular set, authored setup and continued public exclusion.

Power Drain is a Green Goblin research modular recipe with five distinct faces
and five physical cards (02041–02045), one each, from the generated printed set
facts and pinned MarvelSDB `gob_encounter.json` quantities. Its outside-Core
adjudication and `PowerDrainSetupTests` gate composition and public exclusion.
