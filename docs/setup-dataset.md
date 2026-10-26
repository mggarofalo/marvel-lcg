# The setup dataset

`datasets/setup/setup.json` declares scenario and hero recipes. Complete-product
admission determines which declared products the runtime can start; an incomplete
expansion is excluded from public discovery as a whole.

`RuntimeProductAdmission` owns the closed accepted-product policy: Core Set,
Rise of Red Skull, Captain America and Wrecking Crew. Merely adding an authored
ability or setup recipe does not admit another product. Unaccepted product-owned
recipes are excluded; a retained accepted recipe that references an unavailable face
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
| `campaigns` | 35 Core Set, Rise of Red Skull and Wrecking Crew scenario/mode combinations | 37 |
| `heroes` | 8 Core Set, Rise of Red Skull and Captain America starter decks | 9 |
| `encounter_sets` | 10 Core Set and Rise of Red Skull fixed/modular sets | 13 |

Ms. Marvel is a declared research hero. Her 40-card starter, obligation and
nemesis set follow a pinned FFG divider; the runtime product gate excludes her
recipe until complete pack acceptance. Three Green Goblin modular sets are
also declared research recipes: A Mess of Things contains 02037, 02038, 02039
and two copies of 02040; Goblin Gimmicks contains two copies each of
02033–02036; Power Drain contains one copy each of 02041–02045. The Green
Goblin product remains outside runtime admission, so these recipes are excluded
from public discovery even when all their faces have authored abilities.

Risky Business declares Standard and Expert research recipes: eighteen scenario
encounter cards, the selected Norman I/II or II/III stage pair, two physical main
schemes, and Criminal Enterprise/State of Madness set aside as one physical card.
The normal recipe recommends one Goblin Gimmicks modular set (02033–02036, two
copies each). Goblin Gimmicks has authored research abilities and composition data.
`RiskyBusinessSetupTests` exercises the production dealer and authored setup
with one chosen Core modular set at one through four players in both modes.
This nonpublic evidence does not admit Green Goblin or prove complete gameplay.

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

A hero names its 2 identity faces, obligation, nemesis set, 15 signature cards
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
