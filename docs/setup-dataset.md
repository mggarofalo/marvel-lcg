# The setup dataset

`datasets/setup/setup.json` defines every game the runtime can start. It is the
public product-selection boundary described in [scope.md](scope.md).

The dataset is authored. Scenario composition and starter-deck contents are
printed in rules inserts, Learn to Play material and product lists rather than
on individual cards. There is no upstream dataset that can generate the same
answer. [`datasets/setup/UPSTREAM.md`](../datasets/setup/UPSTREAM.md) records the
sources, and `SetupDatasetTests` is the dataset gate.

## Supported records

The file has 3 groups:

| Group | Supported rows |
|---|---:|
| `campaigns` | 6 Core Set scenario and mode combinations |
| `heroes` | 5 Core Set starter decks |
| `encounter_sets` | 7 Core Set fixed and modular sets |

The scenario rows are Rhino, Klaw and Ultron in Standard and Expert modes. The
hero rows are Spider-Man, Captain Marvel, She-Hulk, Iron Man and Black Panther.
The encounter rows include Standard, Expert and the 5 Core modular sets.

Names are keys inside their own group. A campaign, hero and encounter set are
different namespaces even when a future product gives them the same display
name.

The complete generated card catalog remains available for printed facts. A card
appearing there does not make its hero, scenario or product a valid setup key.

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
and the 25-card customization block from one printed starter deck. The runtime
does not accept an arbitrary user-built deck.

An encounter-set row names its cards in printed composition order. Recommended
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
scenario group begins.

Creation order is a wire-format choice made by this engine. It allocates card
object ids, so changing it changes `World.Digest()` and every seeded game.

The Rules Reference decides where cards go after creation. The deal moves cards
to those areas, shuffles the player decks and encounter deck with the game’s one
seeded RNG stream, draws opening hands, and resolves supported setup abilities.

## Deck-construction checks

`Marvel.Content.Setup.DeckConstruction` validates product-independent rules for
the supplied starter decks:

- exactly one identity per player;
- identity-specific cards share the identity set icon;
- obligations and nemesis cards match the selected identity;
- customization cards have an allowed player classification;
- team-up requirements match the identity where applicable;
- deck size excludes Permanent cards where the rules require it; and
- matching unique cards are rejected at deck and identity selection boundaries.

These checks use structured facts from `datasets/cards/`. They do not parse
printed prose at runtime.

The runtime still selects one of the 5 published Core starter decks. Supplying a
general deck-building interface would open a new product boundary and would need
its own complete validation contract.

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
- arbitrary deck construction;
- later heroes, scenarios or encounter sets;
- player-side-scheme deck construction;
- challenge or evidence setup; or
- foldable 3-sided identities.

A request that needs one of these surfaces fails at setup or card registration.
It must not produce a partial board.
