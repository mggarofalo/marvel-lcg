# The setup dataset, and the order a board is dealt

Tracked as `MARVEL-176`.

This dataset describes what the engine is asked to compute *with* — which
scenario holds which encounters, which hero opens with which forty cards.

It is **authored**: most of what it records is printed in a scenario's rules
insert and on the back of a product box rather than on any card, so there is
nothing to generate it from and no upstream to vendor it from. See
[its UPSTREAM.md](../datasets/setup/UPSTREAM.md) for what that means and what
gates it instead.

## What is in it

One file, `datasets/setup/setup.json`, with three groups keyed by the name the
engine resolves:

| group | records |
|---|---|
| `campaigns` | 6 Core Set scenario modes |
| `heroes` | 5 Core Set starter decks |
| `encounter_sets` | 7 Core Set fixed and modular sets |

**Names, not paths.** A scenario, a hero and an encounter set are each
identified by a bare name, and the three groups are separate tables. Their Core
Set keys do not collide. The complete generated card catalog remains available
for printed facts, but these setup keys are the runtime product boundary.

`modular_sets` stays separate from `encounter_sets`. A scenario's printed
insert names the sets it always uses and the modular sets it draws from, and
those are different questions — resolving them into one list would make a
scenario played with chosen modulars inexpressible. `Dealer` does the join.

## The order a board is dealt in

`Marvel.Content.Setup.Dealer`. This is a separate contract from the dataset and
a stricter one: **a card's `object_id` is its position in this sequence**, and
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

### Held against the rulebook

`SetupDealTests` checks the deal in two separable ways.

**Where a card ends up** is `rr:appendix-ii-setup`, cited step by step: the
nemesis set is set aside (step 5); the encounter deck is the listed sets plus
the obligations and nothing else (step 10); the villain deck and main scheme
deck are in play (step 8); each player has their own shuffled deck (steps 1
and 6); one seat holds the first player token (step 3).

**Which id a card is given** is not in the rulebook at all, so those tests cite
no rule and say why. The allocation runs are unbroken and in a fixed sequence,
and every dealt card is on the board exactly once — the completeness claim a
zone-by-zone check cannot make, because a card left in no area is invisible to
every test that does not know to look for it.

One distinction the deal turns on: a card **dealt for** a player is not
therefore **owned by** them. An obligation and a nemesis set are dealt per
identity and belong to the scenario, because `rr:obligation` calls an obligation
"an encounter card type" and `rr:encounter-card` lists obligations among the
eight.

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

### Historical source measurements

The measurements below describe the broader source material used while the
setup format was designed. They are not runtime support claims. The runtime
dataset now contains only the Core Set boundary listed above.

Measured over **48 boards** — 24 heroes against `rhino`, 24 scenarios against
`spider_man`, comparing the declared order against the names the engine actually
passes to `CardFactory.GenerateCard` — **38 reproduce it exactly**. The ten that
do not fall into three classes, and only the first is data:

**Linked cards.** `Blueprints.From` reads the generated card attributes and
creates one copy of every linked product card for each deck containing the card
that brings it. Linked cards are inserted deterministically before the first
bringing card and routed to the set-aside area, so they do not count toward deck
size. The printed link may name a title, a qualified title such as `Titania
minion`, or a face id. When a player later takes control, `Reveal.EnterPlay`
also makes that player the linked card's owner.

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

Not re-runnable; see MARVEL-252.

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
> owner, host, face and field identical to the recording.

That acceptance test no longer runs; see above.

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

`datasets/cards/` carries one trait list and it is the printed one, so
deriving the key from it is the whole of the answer. It did not always: the
dataset was once a join carrying a second, engine-side list beside the printed
one, and the two disagreed about a hundred and forty cards. See
[card-dataset.md](card-dataset.md).

### What this does not yet do

No card abilities and no resolve, so a board whose setup fires an ability is out of
scope — the three deviation classes above say which. Steps 1–20 of the same game
are MARVEL-173.
