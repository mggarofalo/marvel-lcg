# The setup dataset

`datasets/setup/setup.json` declares scenario and hero recipes. Complete-product
admission determines which declared products the runtime can start; an incomplete
expansion is excluded from public discovery as a whole.

`RuntimeProductAdmission` owns the closed accepted-product policy: Core Set and
the complete Rise of Red Skull expansion. Merely adding an authored ability or
setup recipe does not admit another product. Unaccepted product-owned recipes
are excluded; a retained accepted recipe that references an unavailable face
still fails validation. The same admitted face set controls discovery, research
cards' runtime-support flags, deck assessment and game creation.

The dataset is authored. Scenario composition and starter-deck contents are
printed in rules inserts, Learn to Play material and product lists rather than
on individual cards. There is no upstream dataset that can generate the same
answer. [`datasets/setup/UPSTREAM.md`](../datasets/setup/UPSTREAM.md) records the
sources, and `SetupDatasetTests` is the dataset gate.

## Supported records

The file has 3 groups:

| Group | Supported rows |
|---|---:|
| `campaigns` | 16 Core Set and Rise of Red Skull scenario/mode combinations |
| `heroes` | 7 Core Set and Rise of Red Skull starter decks |
| `encounter_sets` | 10 Core Set and Rise of Red Skull fixed/modular sets |

One additional declared research hero is Captain America. Its 40-card starter,
obligation and nemesis set follow the vendored FFG divider; the runtime product
gate excludes this recipe until complete pack acceptance. The file therefore
contains eight hero records while public discovery still exposes seven.

The scenario rows are Rhino, Klaw and Ultron in Standard and Expert modes. The
hero rows are Spider-Man, Captain Marvel, She-Hulk, Iron Man and Black Panther.
Rise of Red Skull adds Crossbones, Absorbing Man, Taskmaster, Zola and Red Skull
in both modes, plus Hawkeye and Spider-Woman. The encounter rows include Standard,
Expert, five Core modular sets and three Rise of Red Skull modular sets.
Crossbones keeps Legions of Hydra fixed; Taskmaster keeps Hydra Patrol fixed,
as required by their scenario instructions.

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

- the supported keys are exactly the Core Set boundary;
- every referenced card face resolves in the generated card catalog;
- every scenario contains the required Standard or Expert set;
- modular recommendations resolve and remain replaceable;
- starter decks satisfy construction rules;
- no group contains duplicate or ambiguous keys; and
- the committed source metadata matches the authored file.

The test suite reads the committed dataset and writes nothing. Unlike a generated
dataset, setup has no regeneration command that can serve as its oracle.

## Unsupported setup

The runtime does not currently accept:

- campaign state or campaign logs;
- Heroic, Skirmish or combined modes;
- deck construction outside the admitted executable Core pool;
- later heroes, scenarios or encounter sets;
- player-side-scheme deck construction;
- challenge or evidence setup; or
- foldable 3-sided identities.

A request that needs one of these surfaces fails at setup or card registration.
It must not produce a partial board.
