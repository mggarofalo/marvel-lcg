# The setup dataset — authored

What Core Set, Rise of Red Skull, Captain America, Wrecking Crew and Green
Goblin games are dealt from: scenario, villain and main scheme decks, encounter
sets, and printed starter decks. The complete Ms. Marvel, Thor and Black Widow integration candidates
also admit their pinned starters, obligations and nemesis sets; declared data alone never
admits a product without its complete face and recipe gates.

| | |
|---|---|
| Kind | authored |
| Declared products | Marvel Champions Core Set; Rise of Red Skull; The Wrecking Crew; Captain America; Ms. Marvel; Thor; Black Widow; Doctor Strange; Hulk; Ant-Man; Wasp; Green Goblin (both scenarios and four modular sets); Ronan Kree Fanatic modular set; The Once and Future Kang modular sets (declared, not admitted) |
| Records | 39 scenario modes, 15 heroes, 18 encounter sets; candidate runtime admission exposes 39 modes, 15 heroes and 15 sets |
| Gated by | `SetupDatasetTests`, `RiseOfRedSkullSetupDataTests`, `CaptainAmericaSetupTests`, `MsMarvelSetupTests`, `ThorSetupTests`, `BlackWidowSetupTests`, `MessOfThingsSetupTests`, `RiskyBusinessSetupTests`, `KreeFanaticAdmissionTests`, `KangModularSetupTests`, complete-product admission |

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
- every hero has one physical identity with hero and alter-ego faces, an obligation, and a nemesis set;
- the declared products contain 39 authored modes, 15 heroes, and 18 sets;
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
complete candidate admission. Independent admission tests close the whole product
when any required face or pinned recipe component is missing or incorrect.

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

Running Interference is a Green Goblin research modular recipe: 02046–02048
have one copy each and 02049 has two, five physical cards and four distinct
faces. Generated printed set facts and the pinned MarvelSDB
`gob_encounter.json` above supply membership and quantities.
`RunningInterferenceSetupTests` gates composition and public exclusion.

Mutagen Formula Standard/Expert research recipes follow printed 02017a Contents
and `pack:mc02:how-to-use-this-pack`. The same pinned gob_encounter snapshot
supplies 26 physical encounter cards: 02019–02022×1 each, 02023×4, 02024×6,
02025×1, 02026×2, 02027×1, 02028×2, 02029×2, 02030–02031×1 each, 02032×2.
The full scenario has 21 distinct faces on 31 physical cards (33 printed face
instances counting both scheme sides); each mode allocates only its selected
two villain stages. The default module is Goblin Gimmicks. Printed setup recruits
one Thrall per player from the assembled deck, shuffles, and advances to 1B.
Expert stage II deals two encounter cards per player during setup.
`MutagenFormulaSetupTests` checks exact composition and 1–4-player production
setup in both modes with a chosen Core module. Public admission stays closed.

Thor is a complete integration candidate, with all 35 printed faces and an exact
40-card starter: 15 signature, 17 Aggression and 8 Basic. The pinned divider in
`datasets/rules-packs/mc06/UPSTREAM.md` supplies the starter; numbered nemesis
components resolve its quantity discrepancy as two Frost Giants and one Trickster.
`ThorSetupTests` and `ThorAdmissionTests` pin composition and whole-pack closure.
Extra customization cards 06031–06034 are admitted but absent from the starter.
This candidate remains on hold for independent Loki and Hammer Throw preflight
review; setup evidence does not establish whole-game or native acceptance.

Black Widow candidate setup follows the pinned FFG divider transcription in
`datasets/rules-packs/mc07/UPSTREAM.md`: 15 signature, 17 Justice and 8 Basic
cards, plus one obligation and five nemesis cards outside the player deck.
Extra customization cards 08030–08033 are absent from the starter.
`BlackWidowSetupTests`, `BlackWidowAdmissionTests` and inventory closure tests
check exact composition and complete-product public admission.


Doctor Strange has candidate public admission for all 40 printed faces
(09001a/b and 09002-09039). The pinned mc08 divider in
`datasets/rules-packs/mc08/UPSTREAM.md` supplies 15 signature, 17 Protection and
8 Basic cards, a separate obligation, five nemesis cards and five Invocations.
The typed Invocation recipe has one each of 09032-09036, outside the ordinary
40-card deck. Missing any required face or changing the pinned recipe closes
the entire product. Extra cards 09037-09039 remain outside the starter and enter
legal custom decks at printed copy limits. Discovery now has fourteen heroes,
39 scenario/mode recipes and fifteen encounter sets. Public setup and focused
action evidence are recorded in `docs/content/doctor-strange-public-admission.md`;
complete-game, fresh-host replay and final combined acceptance belong to the
separate MARVEL-838 packet.


Hulk has candidate public admission for all 33 printed faces (10001a/b and
10002–10032). The pinned FFG divider in `datasets/rules-packs/mc09/UPSTREAM.md`
supplies the exact 40-card starter: 15 signature, 17 Aggression and 8 Basic.
Identity, Inner Demons (10025 ×1) and the five-card nemesis set
(10026 ×1, 10027 ×1, 10028 ×3) are separate. Missing any required authored or
catalog face, changed pack membership or an incorrect required recipe closes
the complete pack. Extras 10029–10032 stay outside the starter and enter legal
custom decks at printed copy limits. Public discovery has fourteen heroes,
39 scenario/mode recipes and fifteen encounter sets. Focused setup/admission
evidence is recorded in `docs/content/hulk-public-admission.md`; this candidate
does not establish complete-game, native or final combined acceptance.


Kree Fanatic has candidate public admission as a modular encounter set only.
The pinned seven-page FFG PDF recorded in
`docs/content/research/remaining/ronan-source-pin.md` supplies one copy each of
90001–90003 and two copies each of 90004–90005. All five authored/catalog faces,
exact pack membership, modular role and the complete seven-copy recipe are required
together. A missing face or malformed recipe closes the entire set while preserving
predecessor products. Standard and Expert scenario slots accept its normal modular
selection with starter or legal custom player decks. Recommended defaults remain
scenario-owned. Discovery has fourteen heroes, 39 scenario/mode recipes and fifteen
encounter sets, thirteen of them modular. The separate Galaxy’s Most Wanted Ronan
scenario remains unsupported. Focused evidence is in
`docs/content/ronan-public-admission.md`; this gate does not establish complete-game,
fresh-host or native acceptance.

## Ant-Man candidate

The independently inspected printed starter is recorded in
`docs/content/research/remaining/ant-man-implementation-handoff.md`. It supplies
15 signature, 16 Leadership and 9 Basic cards; the three-face identity is one
physical card. Obligation 12025 and nemesis 12026, 12027, two 12028 and 12029
are separate. Extras 12030–12033 belong to the pack, not its starter. All 35
faces and the exact recipe are required for candidate admission. This does not
establish complete-game, replay, native or final combined acceptance.

## Wasp candidate

The starter is pinned to the vendored MarvelSDB quantities in
`datasets/marvelsdb/pack/wsp.json` and `wsp_encounter.json`; no printed divider
was inspected, so the quantities are a repository choice from that snapshot. It
supplies 15 signature, 16 Aggression and 9 Basic cards; the three-face identity
is one physical card. Obligation 13026 and nemesis 13027, 13028, 13029 and two
13030 are separate. Extras 13031–13034 belong to the pack, not its starter. All
36 faces and the exact recipe are required for candidate admission.

## The Once and Future Kang modular sets

Temporal, Anachronauts and Master of Time (key `mot`) are declared from the
generated printed set facts and the pinned MarvelSDB
`datasets/marvelsdb/pack/toafk_encounter.json` quantities: seven, nine and
eight physical cards. `pack:mc11:adjustable-difficulty` names the three sets
and lets each be used in other scenarios. The Kang scenario itself has no
recipe; `docs/setup-dataset.md` records what its recipe needs.
`KangModularSetupTests` checks exact composition and that complete-product
admission excludes the sets even when every face is authored.
