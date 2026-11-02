# The setup dataset

`datasets/setup/setup.json` declares scenario and hero recipes. Complete-product
admission determines which declared products the runtime can start; an incomplete
expansion is excluded from public discovery as a whole.

`RuntimeProductAdmission` owns the closed accepted-product policy: Core Set,
Rise of Red Skull, Captain America, Wrecking Crew and the complete Green Goblin,
Ms. Marvel, Thor, Black Widow, Doctor Strange, Hulk and Ant-Man integration candidates. Merely adding an authored ability or setup
recipe does not admit another product. Unaccepted product-owned recipes are excluded; a retained accepted recipe that references an unavailable face
still fails validation. The same admitted face set controls discovery, research
cards' runtime-support flags, deck assessment and game creation.

The dataset is authored. Scenario composition and starter-deck contents are
printed in rules inserts, Learn to Play material and product lists rather than
on individual cards. There is no upstream dataset that can generate the same
answer. [`datasets/setup/UPSTREAM.md`](../datasets/setup/UPSTREAM.md) records the
sources, and `SetupDatasetTests` is the dataset gate.

## Supported records

The file has 3 groups:

| Group | Admitted rows | Declared rows |
|---|---:|---:|
| `campaigns` | 39 Core Set, Rise of Red Skull, Wrecking Crew and Green Goblin scenario/mode combinations; the two declared Kang modes and two declared Brotherhood of Badoon modes are not admitted | 43 |
| `heroes` | 14 Core Set, Rise of Red Skull, Captain America, Ms. Marvel, Thor, Black Widow, Doctor Strange, Hulk and Ant-Man starter decks | 14 |
| `encounter_sets` | 20 Core Set, Rise of Red Skull, Green Goblin, Kree Fanatic and Galaxy's Most Wanted fixed/modular sets; the three declared Kang modular sets, Ship Command and Galactic Artifacts are not admitted | 25 |

Ms. Marvel is admitted as a complete integration candidate. Her 40-card starter,
obligation and nemesis set follow the pinned FFG divider in
`datasets/rules-packs/mc05/UPSTREAM.md`. Admission requires all 34 printed faces
and the exact recipe; any missing face or changed required composition closes
the complete pack. Public setup covers one through four seats in Core and Rise
of Red Skull scenarios, legal custom decks, and a pending Morphogenetics response
across session restart. These checks do not establish whole-game or native acceptance.

Four Green Goblin modular sets are also declared and admitted by the complete-product candidate policy: A Mess of Things contains 02037, 02038, 02039
and two copies of 02040; Goblin Gimmicks contains two copies each of
02033–02036; Power Drain contains one copy each of 02041–02045. Running Interference contains 02046, 02047, 02048 and two copies of 02049.
All four sets require exact physical quantities and modular roles for admission.

Risky Business declares Standard and Expert recipes: eighteen scenario
encounter cards, the selected Norman I/II or II/III stage pair, two physical main
schemes, and Criminal Enterprise/State of Madness set aside as one physical card.
The normal recipe recommends one Goblin Gimmicks modular set (02033–02036, two
copies each). Goblin Gimmicks has authored abilities and composition data.
`RiskyBusinessSetupTests` exercises the production dealer and authored setup
with one chosen Core modular set at one through four players in both modes.
Public factory tests additionally cover both scenarios and modes at one through
four players, legal custom decks and modular replacement in Core, Rise of Red
Skull and both Goblin scenarios. These setup checks do not prove whole-game,
restart or native acceptance.

The scenario rows are Rhino, Klaw and Ultron in Standard and Expert modes. The
hero rows are Spider-Man, Captain Marvel, She-Hulk, Iron Man and Black Panther.
Rise of Red Skull adds Crossbones, Absorbing Man, Taskmaster, Zola and Red Skull
in both modes, plus Hawkeye and Spider-Woman. The encounter rows include Standard,
Expert, five Core modular sets and three Rise of Red Skull modular sets.
Crossbones keeps Legions of Hydra fixed; Taskmaster keeps Hydra Patrol fixed,
as required by their scenario instructions.

Captain America adds an admitted starter deck. Wrecking Crew adds nineteen
scenario recipes: Standard, Expert, Extreme and sixteen Custom A/B combinations.

Names are keys inside their own group. A campaign, hero and encounter set are
different namespaces even when a future product gives them the same display
name.

The complete generated card catalog remains available for printed facts. A card
appearing there does not make its hero, scenario or product a valid setup key.

The server exposes these authored records through protocol setup discovery.
Clients submit the returned keys rather than reading this file or deriving
support from the broader card catalog. Standard and Expert remain separate
campaign records even when a client presents their shared scenario name as one
control.

An optional `setup_mode` explicitly labels a recipe `standard`, `expert`,
`custom`, or `extreme`. Without it, the existing `expert` flag supplies the
ordinary mode. Independent encounter-group recipes may label each group with
`variant`, such as `A`, `B`, or `A → B`. Discovery carries that ordered label
beside the printed villain name and advertises that modular sets are unavailable.
The client selects a complete returned recipe key; it does not construct stages
or infer setup rules from those labels. Metadata alone does not admit a product.

## Dataset shape

A campaign names its main schemes, villain stages, fixed encounter sets,
recommended modular sets, set-aside cards and mode:

```json
{
  "main_scheme": ["01097a,01097b"],
  "villain": ["01094,01095,01096"],
  "encounter_sets": ["standard", "rhino"],
  "modular_sets": ["bomb_scare"],
  "set_aside": [],
  "expert": false
}
```

A hero names its physical identity and faces, obligation, nemesis set, 15 signature cards
and the 25-card customization block from one printed starter deck. An optional
ordered `PlayerDeckRecipe` per seat supplies a complete validated Core deck.
Omitting recipes selects the authored starter defaults. Identities, obligations
and nemesis sets always come from the selected hero record. Each hero declares
`nemesis_set_icon` explicitly; the engine checks that association without
constructing a set name from the identity’s name.

An optional `aspect_rule: "equal_two_aspects"` records Jessica Drew's printed
Double Agent construction instruction (04031b). The engine requires equal totals
for the two chosen aspects, including their signature cards, while allowing the
required signature cards from other aspects. Omission retains ordinary one-aspect
construction. Both explicit recipes and starter substitutions use this rule.

An encounter-set row names its cards in printed composition order and declares
its `role`: `modular`, `standard`, `expert`, `scenario` or `nemesis`. Roles are
authored composition metadata; no card-id or set-name spelling determines them.
The `expert` campaign flag must be a JSON boolean. Recommended
modular sets remain separate from fixed sets so the caller can make the setup
choice the scenario permits.

## Card abilities during setup

### Independent encounter groups

A scenario may declare `encounter_groups`, containing `active_group` (a zero-based
index) and ordered `groups`. Each group contains `stages`, one `scheme` and an
ordered `encounters` array. The scenario's `villain` list must equal the flattened
stage lists. These relationships are explicit authored composition, not inferred
from card names. Missing, duplicate and unknown group fields are rejected.

`ScenarioDealer.Prepare` returns blueprints and an optional `EncounterSetupPlan`.
The rules-owned plan validation requires Wrecking Crew's four groups, fifteen
encounter cards per group, legal component kinds, and either one stage per group
or two for every group. Other encounter sets and caller-selected modular sets
are forbidden. Hero obligations and nemesis sets are excluded before allocation,
as required by `pack:mc03:additional-setup.prepare-encounter-decks`.

The engine chooses this allocation spelling: ordinary player cards, main schemes
and scenario set-aside cards precede complete group blocks in authored order.
Each block contains its stages, paired scheme and encounter cards. Blueprint
binding happens after linked player cards are inserted, so group IDs account for
those insertions without guessing from face names. Each villain deck shuffles in
group order after the player decks, on the one seeded stream. Recipes without
groups retain their ordinary allocation and encounter-card composition.

The group recipe contract does not itself admit a product. Whole-pack card,
setup, client and compatibility acceptance still controls public discovery.

The setup dataset says which cards a product supplies. It does not duplicate
instructions printed on those cards.

Main-scheme and identity setup text is authored as typed `Setup` abilities in
`datasets/abilities/abilities.json`. The dealer creates the declared cards, and
the rules engine resolves those abilities at the applicable Rules Reference
setup step.

This separation keeps product composition in the setup dataset and card behavior
in the ability dataset. A change to printed card text has one executable source.

## Deterministic deal order

`Marvel.Content.Setup.Dealer` creates cards in this order:

1. rules pseudo-cards;
2. each player’s identity, with the alter-ego face first;
3. that identity’s obligation;
4. that identity’s nemesis set;
5. that identity’s signature cards;
6. that player’s selected published customization block;
7. the main-scheme deck;
8. villain stages in printed order;
9. fixed scenario encounter cards; and
10. chosen modular encounter cards.

Player groups are created in seat order. Every player group finishes before the
scenario group begins. Explicit decks replace steps 5 and 6 with the supplied
full recipe order. The engine orients identity faces by their card kind, placing
Alter-Ego first regardless of id spelling.

Creation order is a wire-format choice made by this engine. It allocates card
object ids, so changing it changes `World.Digest()` and every seeded game.

The Rules Reference decides where cards go after creation. The deal moves cards
to those areas, shuffles the player decks and encounter deck with the game’s one
seeded RNG stream, resolves scenario setup abilities, then draws opening hands.
Scenario setup decisions suspend at `GamePhase.ScenarioSetup` and resume through
the same `Game.Resolve` prompt boundary as later decisions; opening hands and
mulligans wait until those effects finish.

## Deck-construction checks

`Marvel.Rules.Setup.DeckConstruction` and `PlayerDeckAdmission` validate engine
deck rules using the supplied structured card facts:

- exactly one identity per player;
- identity-specific cards share the identity set icon;
- obligations and nemesis cards match the selected identity;
- customization cards have an allowed player classification;
- team-up requirements match the identity where applicable;
- full recipes contain exactly their identity’s signature-card quantities;
- full recipes contain 40–50 cards, one aspect plus Basic, and obey copy limits;
- deck size excludes Permanent cards where the rules require it; and
- matching unique cards are rejected at deck and identity selection boundaries.

These checks use structured facts from `datasets/cards/`. They do not parse
printed prose at runtime.

`DatasetGameFactory` admits the complete setup catalog against the executable
ability pool before publishing it and compiles that immutable program once.
Every game receives its own execution state. Research-only card faces cannot
enter a recipe. Recipes are immutable snapshots and are persisted in session
setup so replay reconstructs their exact allocation order. The engine supports
one through four seats. There is no deck editor yet.

## Ownership and placement

A card dealt for a player is not necessarily owned by that player. An obligation
and nemesis set are associated with an identity but remain encounter cards owned
by the scenario.

The state model therefore keeps these concepts separate:

- `Owner` says who owns a card or area;
- `Controller` says who currently controls a card;
- `RelatedPlayer` says which seat a player-shaped area belongs to; and
- `CreationSource` says why setup created the card.

The setup and state tests hold each distinction independently.

## Dataset gates

`SetupDatasetTests` checks that:

- declared recipes resolve and runtime discovery respects complete-product admission;
- every referenced card face resolves in the generated card catalog;
- ordinary scenarios contain the required Standard or Expert set, while independent encounter groups use their own decks;
- modular recommendations resolve and remain replaceable;
- starter decks satisfy construction rules;
- no group contains duplicate or ambiguous keys; and
- the committed source metadata matches the authored file.

The test suite reads the committed dataset and writes nothing. Unlike a generated
dataset, setup has no regeneration command that can serve as its oracle.

## Unsupported setup

The runtime does not currently accept:

- Heroic, Skirmish or combined modes;
- deck construction outside the admitted executable pool;
- later heroes, scenarios or encounter sets;
- player-side-scheme deck construction;
- challenge or evidence setup; or
- foldable 3-sided identities.

A request that needs one of these surfaces fails at setup or card registration.
It must not produce a partial board.

The Once and Future Kang declares its three modular sets and two scenario
recipes, `kang` and `kang_expert`. Temporal has 11030×3, 11031×2, 11032 and
11033; Anachronauts has 11040–11044, 11045×2 and 11046×2; Master of Time (key
`mot`, the printed set code) has 11047, 11048×2, 11049×2, 11050 and 11051×2.
The pinned MarvelSDB `pack/toafk_encounter.json` supplies the quantities, and
`pack:mc11:adjustable-difficulty` makes each set usable in other scenarios.
Complete-product admission excludes the scenario and all three sets, even when
every face is authored. `KangModularSetupTests` and `KangRecipeAdmissionTests`
gate composition and that exclusion.

The recipes follow printed 11007a. Its contents are "Kang (I), each Kang (II),
and Kang (III). Kang and Standard encounter sets. One modular encounter set
(Temporal)." Its setup sets every Kang (II), Kang (III) and Kang's Dominion
aside, so Kang (I) is the only villain in the villain deck (`pack:mc11:setup`),
and removes each player's obligation cards from the game. A recipe records:

- `villain`: Kang (I) alone, 11001 (Expert 11034);
- `schemes`: stages 1, 2 and 4 as the main scheme deck in that order, so 4A
  waits in the deck to hold the Dominions stage 3B places under it (MARVEL-894
  Q13);
- `set_aside`: the four Kang (II) (11002–11005, Expert 11035–11038), Kang (III)
  (11006, Expert 11039), the four double-sided stage-3 schemes and the four
  Kang's Dominion copies. The stage-3 schemes are the pool 2A draws from; neither
  the cards nor the insert place them, and the set-aside area is our reading;
- `encounters`: the other 26 Kang-set cards at their pinned quantities;
- `encounter_sets`: Standard, plus Expert in `kang_expert`; and
- `remove_hero_obligations: true`, so each identity's obligation is dealt into
  the removed-from-game area while every nemesis set is still set aside for
  stage 4B.

`pack:mc11:adjustable-difficulty` defines Expert as replacing "all six villains
in the Kang encounter set with the six villains from the Expert Kang set" and
adding the Expert set; that is the whole difference between the two rows. The
insert recommends Temporal and permits Master of Time or Anachronauts in its
place. Those three modular choices, both modes and one to four players are the
24 recommended combinations; `KangRecipeSetupTests` deals each through the
production dealer.

`remove_hero_obligations` is optional and false when absent; a non-boolean
value is rejected. A removed card still exists in the removed-from-game area,
and cards read it there: 27135 Loose Ends searches that area for your obligation
(ruling 78ee6578c6efb661), and `pack:mc11:adjustable-difficulty` lets modular
sets move between scenarios. The dealer therefore creates each obligation
directly in that area, in its usual allocation position. Creating it there
rather than in the encounter deck and moving it out before the step-10 shuffle
is our choice; the board is the same.

Mutagen Formula declares Standard and Expert recipes with 26 encounter cards,
the selected I/II or II/III villain pair, and two physical double-sided main
schemes. Goblin Gimmicks is the one-module recommendation. Authored setup and
stage-II reveal run through the production dealer at one through four players
in `MutagenFormulaSetupTests`. The complete Green Goblin candidate supplies all
57 printed faces and admits both scenarios and all four modular sets through
the public setup contract. Whole-game, restart and native acceptance remain
separate from these admission and setup checks.

Thor is a complete integration candidate, with all 35 printed faces and an exact
40-card starter: 15 signature, 17 Aggression and 8 Basic. The pinned divider in
`datasets/rules-packs/mc06/UPSTREAM.md` supplies the starter; numbered nemesis
components resolve its quantity discrepancy as two Frost Giants and one Trickster.
`ThorSetupTests` and `ThorAdmissionTests` pin composition and whole-pack closure.
Extra customization cards 06031–06034 are admitted but absent from the starter.
This candidate remains on hold for independent Loki and Hammer Throw preflight
review; setup evidence does not establish whole-game or native acceptance.


Black Widow is admitted as a complete integration candidate. All 34 printed faces
(08001a/b and 08002–08033) and the exact 40-card divider starter, obligation
and five-card nemesis set are required together. The pinned authority is
`datasets/rules-packs/mc07/UPSTREAM.md`; extra customization cards 08030–08033
are admitted separately from the starter. Public tests cover one through four
seats in Core and Rise of Red Skull, custom-deck ownership, all four extra cards,
and Natasha's pending Preparation-play response across strict JSON save and a
fresh host. Admission and these focused journeys do not establish complete-game,
native or platform acceptance.


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
scenario-owned. Discovery has nineteen heroes, 39 scenario/mode recipes and twenty
encounter sets, eighteen of them modular. The separate Galaxy’s Most Wanted Ronan
scenario remains unsupported. Focused evidence is in
`docs/content/ronan-public-admission.md`; this gate does not establish complete-game,
fresh-host or native acceptance.

The Galaxy’s Most Wanted's Band of Badoon is admitted as a modular set of ten
physical cards: two Badoon Assassin, three Badoon Grunt, one Badoon Lieutenant,
two Badoon Sentry and two Badoon Warlord (16117–16121), the quantities of the
vendored MarvelSDB snapshot. All five faces, exact pack membership, the modular
role and the complete ten-copy recipe are required together. The Brotherhood of
Badoon scenario declares Standard and Expert recipes: Drang (I)/(II) or
(II)/(III), Terrestrial Invasion and Protect the Planet, the Standard (and Expert)
and Ship Command sets, the seven Brotherhood of Badoon encounter cards and the
Badoon Ship set aside for its main scheme's setup. The scenario's faces are not
admitted, since the scenario waits for the withheld Ship Command set, so both
recipes name closed faces and stay outside discovery. Evidence is in
`docs/content/brotherhood-of-badoon.md`.

The Galaxy's Most Wanted declares its Ship Command, Power Stone, Galactic
Artifacts, Kree Militants, Menagerie Medley and Space Pirates modular sets
(ruling 408decdf6cd8c4b5) with the MarvelSDB snapshot's physical quantities:
Galactic Artifacts one copy each of 16122–16130; Kree Militants one 16131, three
16132, two 16133 and three 16134; Menagerie Medley four 16135, three 16136 and two
16137; Space Pirates one 16138, four 16139, one 16140 and two 16141. Each set is
admitted alone when every face is authored and its recipe is exact, in order, with
the modular role (`GalaxyModularSetAdmission`). Galactic Artifacts stays closed
while Crystal Ball (16130) is unauthored.
