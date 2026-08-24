# The setup dataset, and the order a board is dealt

Tracked as `MARVEL-176`. Written against engine build `0.5.9.x`, measured on
2026-08-24.

Three datasets already describe how the engine *computes*: the RNG stream
(`datasets/rng/`), the state digest (`datasets/digest/`) and the cards
(`datasets/cards/`). This is the fourth, and it describes what the engine is
asked to compute *with* — which scenario holds which encounters, which hero
opens with which forty cards.

Until it existed, that data lived only in `py_src/data/` and `py_src/deck/`,
which meant a C# engine could not deal a board without reading the oracle it is
meant to replace. `py_src/` is the behavioural oracle
([migration.md](migration.md)); it is not a runtime dependency of anything.

```bash
cd py_src
python -m tools.setup.emit_setup           # regenerate
python -m tools.setup.emit_setup --check   # the CI gate; byte for byte
```

## What is in it

One file, `datasets/setup/setup.json`, about 195 KB, three groups keyed by the
name the engine resolves:

| group | records | from |
|---|---|---|
| `campaigns` | 135 | `data/scenarios/`, `data/challenges/`, `data/scenarios_custom/` |
| `heroes` | 63 | `deck/starter/` |
| `encounter_sets` | 184 | `data/encounter_sets/`, `data/nemesis/` |

**Names, not paths.** The engine resolves a bare name against an ordered folder
list (`engine/file/manager.py:FindJsonPath`), so the name is the identifier and
the folder is an implementation detail. The emitter walks the folders in that
same order, the first hit wins, and any later hit is written into a `shadowed`
key rather than silently discarded — a name whose meaning depends on a search
order is exactly what a second engine will get wrong. It is empty today.

Three folders the engine searches are **not** read. `./deck/` is gitignored, so
it holds whatever decks the developer built this morning, and a fixture compared
byte for byte cannot read a folder that differs per checkout — first-party
starter decks are content, a deck somebody made is not. The other two hold no
file of any of these kinds: `./data/` holds `cards.json` and its three
neighbours, none of them a scenario or an encounter set; and `.`, the working
directory, which `FindJsonPath` prepends to *every* list and searches first,
holds `launch.json` and nothing else — reading it would emit a `launch`
campaign, a `launch` hero and a `launch` encounter set out of one editor config.

`.` is the exclusion worth stating out loud, because it is searched **first**.
A collision there would win in the engine and would not appear under
`shadowed`, which only records the later hit. So the emitter's folder list is
held against the order `FindJsonPath` really walks — read out of the function by
spying on `FileManager.Exists`, not re-derived from the module constants — and
against the claim that `py_src/` holds no name any group also holds. Add a
folder to `SCENARIOS_FOLDERS` and that test fails; without it the dataset would
have quietly stopped covering a scenario the engine can still load, with every
byte-comparison gate still green.

## It is a projection, not a translation

Every record is produced by loading the file through the same dataclass the
engine loads it through — `CampaignDescriptor`, `HeroDescriptor`,
`EncounterSetDescriptor` — so **a key the engine ignores is a key this file does
not have**. `deck/starter/spider_man.json` carries `set_aside` and `metadata`;
`HeroDescriptor` declares neither; `Json.ConvertDictToDataclass` drops both; so
does the dataset. A port written from the raw files would implement fields the
oracle does not have.

One field is dropped on purpose: `version`. It stamps the format the Python
engine wrote, `UpdateVersion` is `pass` on all three descriptors, and carrying it
would churn the fixture on a bump that changes no setup.

`modular_sets` stays separate from `encounter_sets`. `SceneLoader.NewFromJson`
appends one to the other *only when the caller names no sets of its own*, so
folding them at emit time would make the other case — a scenario played with
chosen modulars — inexpressible. `deal.EncounterSetNames` does the join.

## The order a board is dealt in

`py_src/tools/setup/deal.py`. This is a separate contract from the dataset and a
stricter one: **a card's `object_id` is its position in this sequence**, and
`object_id` is on the wire in every state digest — checklist item 1 of
[state-digest-v2.md](state-digest-v2.md), *"everything else depends on this"*.

Read out of the engine, not invented:

| # | source | where |
|---|---|---|
| 1 | `rules` — the `rule_a,rule_b` pseudo-card | `game/event/manager.py:RegisterPlayRule` |
| 2 | `challenge` — campaign challenge cards | same |
| 3 | `identity`, **b-face first** | `player_setup.py:SelectIdentity` |
| 4 | `obligation` | same |
| 5 | `nemesis` | same |
| 6 | `hero_deck` then `player_deck`, one run | same |
| 7 | `main_scheme` | `world.py:SelectScenario` |
| 8 | `villain`, every stage in printed order | `player/scenario.py:SelectVillain` |
| 9 | `encounter` — `campaign.encounters` | `world.py:Initialize` |
| 10 | `encounter_set` — each named set in order | same |

Steps 3–6 run per player in seat order, and all players finish before step 7.

It does not shuffle, and it does not say where a card ends up. Those are the
step after. An obligation is *created* into its player's nemesis pile and
*moved* onto the encounter deck before the shuffle; both are true and only the
first one is an id.

### Held against a real game

`datasets/digest/vectors.json` records the card at every `object_id` for
`rhino / spider_man / 12345`. All **81** agree, in order —
`unit_test/test_setup_dataset.py`.

### The one flip

The face showing at step 0 is the **first** face of the created spec, with a
single exception.

An identity needs no flip: the engine's `move_b_to_front`
(`player_setup.py:216`) reorders `01001a,01001b` into `01001b,01001a` so the
alter-ego side is already first — which is why the digest's `card` for a hero at
step 0 is the `b` id. A **main scheme** is created `01097a,01097b` and turned to
its `1B` side by `PutIntoPlay`, so it is the one card whose recorded face is not
the first one dealt.

A port that flipped both, or neither, would still pass a test that only checked
which card was at which id. That is why the exception is pinned by name.

### The one hero who is not who she says she is

SP//dr declares `31001a,31001b` and the engine creates `31002a,31002b`.
`SelectIdentity` tests the first spec against `HACK_HERO_ID` — the literal
string `'3100'` — and on a hit discards the descriptor's list for a hard-coded
`31002a,31002b`, with no `move_b_to_front`.

It is the same job the `b`-face reorder does everywhere else — begin the game
in alter-ego form — done by substitution instead, because SP//dr's two sides
are two *cards* rather than two faces of one. `31001` is the SP//dr Suit and
`31002` is Peni Parker, whom the descriptor carries under the dropped
`set_aside` key. Peni is created as the identity, her `a` face is already the
alter-ego so no reorder is needed, and her own setup ability puts the suit into
play. The card at SP//dr's first `object_id` is `31002a`, not `31001b`.

`deal.IdentitySpecs` reproduces the branch. It is the one piece of card-specific
engine behaviour the deal order does implement, because it is the one that is
decidable from the dataset: the branch reads the hero spec and nothing else.
Everything else in this shape is a card script and waits for phase 5.

### What the deal order does not cover yet

Measured over **48 boards** — 24 heroes against `rhino`, 24 scenarios against
`spider_man`, comparing the declared order against the names the engine actually
passes to `CardFactory.GenerateCard` — **38 reproduce it exactly**. The ten that
do not fall into three classes, and only the first is data:

**Linked cards.** A card whose attributes carry `Linked` is created into the
aside deck *before* the card that names it (`factory.py:create_linked_faces`),
so it takes the lower id. Fourteen cards have the attribute and it is in
`datasets/cards/` already — `51036` Redemption names *Show of Empathy*,
`53034` Captain America's Shield names `53023`. Note the two spellings: the
engine resolves the value as a name *or* an id (`CardsDB.FindCardPaper`). This
belongs with the card data rather than here, and lands with it.

**Setup abilities on a card.** Doctor Strange's Invocation cards appear right
after his identity, put there by an ability firing on `WhenPlayerSelectHero`.
That is card script behaviour and it ports with the card DSL, phase 5.

This class also runs the other way, which is easy to miss because the common
case only ever *adds* cards. `AbilityFactory.SetModularSetsAside` — Mojo's
`39025a`, The Hood's `24004a` — rewrites `message.encounter_set_names` during
`WhenGameBeginSetup` and caps how many modular sets reach the encounter deck.
Mojo declares six and a solo game keeps two, so the board is *smaller* than the
campaign record describes, not larger. The Hood declares exactly the seven its
cap allows, which is why it reproduces the deal order exactly and Mojo does not.
Same class, same phase; the dataset is still right about what each campaign
declares.

**Status cards created during setup.** Six of the challenge campaigns allocate a
`Tough` while the encounter deck is being built. Same class as the last one, and
the same phase.

None of the three touches `rhino / spider_man / 12345`, which is why the first
milestone can be exact before any of them is implemented.

## What a port still has to decide

This dataset says nothing about **where** a card goes, only which cards exist
and in what order they are made. Areas, and the fact that an area needs an
identity rather than a description, are `MARVEL-175`. The `fields` on each card
are checklist item 7 and come from the card data plus the face class hierarchy.

## How the measurements were taken

```bash
cd py_src
python -m tools.setup.emit_setup
python -m unittest unit_test.test_setup_dataset
```

The 38/48 figure comes from a throwaway probe that wraps
`CardFactory.GenerateCard`, runs `tools.determinism.headless.run_headless` for
one step per board, and compares the recorded `(object_id, name)` pairs with
`deal.DealOrder`. It is not checked in: it measures a gap that is closing, and a
tool that tests the tool that tests the engine is the third generation
[AGENTS.md](../AGENTS.md#scope-discipline) warns about. Re-derive it if the
figure needs refreshing.

## Dealing it in C#

`Marvel.Content` reads this dataset and `datasets/cards/`; `Marvel.Rules.State`
holds the world and lays the board out. The acceptance test is the digest, byte
for byte:

> `rhino / spider_man / 12345`, step 0 — 81 cards, ids 0–80, every zone, index,
> owner, host, face and field identical to `datasets/digest/vectors.json`.

That is the whole state model and none of the rules. Five things had to be right
at once, and each was measured rather than guessed.

**Two RNG calls, in this order.** The player deck, then the encounter deck.
Nothing else in setup consumes randomness — not the opening hand, which is dealt
off the top of an already-shuffled deck. Both shuffles draw from one seeded
stream, so swapping them changes every card's position, and the obligations have
to go on top of the encounter deck **before** it is shuffled rather than after.

**A card is face down in exactly three places.** A draw pile that is not a
discard pile, and a hand. The obvious candidate, `DeckTypeFlags.is_face_up`, is
wrong: it is `False` for `RemovedArea` and `VillainDeck` and cards in both are
recorded face up. The predicate that holds agrees with all **571 card records
across the seven recorded steps**, over twelve distinct zones, in which `face_up`
is a function of the zone with no exceptions — including
`EncounterDiscardPile`, which is a deck and is nonetheless face up, and which is
what rules out the simpler "decks are hidden".

**An area's owner is not the seat whose area it is.** A card takes the owner of
the place it was *made* in. A player's nemesis pile is *theirs* and is owned by
the *scenario*, so an obligation dealt for seat 0 records `owner: -1` while
sitting in a pile that plainly belongs to that seat. This was the first and only
byte difference in the first full run of the comparison — the digest named the
card and the field, which is exactly what it is for. `Area` therefore carries
both `Owner` and `RelatedPlayer`, the same distinction MARVEL-163 found between
`Deck2.GetOwner()` and `deck.play_area`.

**Out of play, every field is zero.** Across the 78 out-of-play cards on this
board the only non-zero entries are the `t_<TRAIT>` keys and `printed_stage` —
which is set when the card is built rather than when it enters play, so a villain
still in the villain deck records its stage and nothing else. The three cards in
play carry their printed values. `k_` keys are token pools and appear **only in
play**, which is why the two villain stages register different key sets.

**Fourteen face classes, one key set each.** Read off the classes a real board
instantiates. The registered key set is part of the contract — zero-valued fields
are emitted precisely so that a port which forgets to register `recover` fails on
the key rather than passing by luck.

### The trait gap

The digest keys traits as `t_HERO_FOR_HIRE` and `t_S.H.I.E.L.D`; the card dataset
carries MarvelSDB's printed spelling, `Hero for Hire` and `S.H.I.E.L.D.`.
Upper-casing, dropping a trailing full stop and turning spaces into underscores
reproduces every `t_` key on this board.

**Derivation is not the same as having the engine's list.** Compared across 3,999
cards, the printed traits and the Python engine's own trait lists disagree
outright on **142** — the engine gives `01172` the `CRIMINAL` trait and the
printed card has none; `02033` is the other way round. None is on the milestone
board, so this is a gap rather than a failure, and it will surface the moment a
replay reaches one of them. The fix is for `datasets/cards/` to carry the
engine's trait list beside the printed one.

### What this does not yet do

No card abilities and no fold, so a board whose setup fires an ability is out of
scope — the three deviation classes above say which. Steps 1–20 of the same game
are MARVEL-173.
