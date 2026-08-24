# The semantic event stream

MARVEL-160. The fold's return signature gains a list of events describing what
happened, alongside the state it produced.

This was settled before the interpreter exists, deliberately. Retrofitting a
semantic stream after 3,457 card ports is not viable, and the shape of the
records decides what the interpreter has to emit as it walks an effect tree.

## Why a snapshot is not enough

A board snapshot is enough to draw a board. It is not enough to animate one.

A snapshot says the discard pile got taller. It cannot say that card 01096 went
from a player's hand to their discard **because an ability's cost consumed it** —
which is a different animation from the same card being discarded to the same
pile by an encounter card, and a different one again from being played and
resolving. Cause is not recoverable from state, and cause is most of what an
animation is about.

## The vocabulary was measured, not designed

Every recorded step in the frozen corpus carries its full v2 digest, so the set
of state transitions the engine can produce is countable rather than a matter of
opinion. `py_src/tools/events/census.py` diffs consecutive digests across the
whole corpus.

**1,773 scenes. 201,870 transitions. 1,365,439 individual changes.** They fall
into twelve shapes and no more — *as far as a digest can see*, which is a real
limit and is picked up in [When the table splits](#when-the-table-splits):

| shape | share |
|---|---|
| a card moved zone | 23.2% |
| a named field changed value | 21.8% |
| a card changed position within its zone | 20.3% |
| a card flipped | 13.8% |
| a field appeared | 10.4% |
| a field disappeared | 4.8% |
| a card attached | 1.9% |
| a card became a different face | 1.6% |
| a card detached | 1.5% |
| a card was created | 0.5% |
| control of a card changed | 0.2% |
| a card moved to a different host | 0.1% |

Two results from that table are worth stating outright.

**`card.vanished` never fires.** Object ids are never reused and the card
dictionary is append-only, so a card removed from the game moves to the removed
area rather than ceasing to exist. There is therefore no destruction event, and
that is a measurement rather than an assumption.

**35.3% of steps change nothing at all.** An input that only opens a prompt
produces an empty event list. Empty and absent are different, and the fold
returns the former.

## Position is a consequence, not an event

The naive reading of that table is that position needs an event: it is 20% of
all observed change. It mostly does not.

A zone is an ordered list. Taking a card out of the middle of a deck shifts every
card above it down by one, and the digest faithfully records a position change
for each of them. Those are *consequences* of the move. An animation that played
them would show the deck rippling every time a card is drawn, which is not what
happened.

So the model applies the moves, compacts the source, inserts into the
destination, and emits an event only for what is left over. **That removed 85% of
apparent reorderings** — 345 down to 52 on the sample where it was first
measured. What survives is a genuine shuffle, and it is one `AreaReordered` for
the area rather than one event per card.

The same logic makes `CardsMoved` a batch. Drawing five cards is one thing that
happened, so it is one event carrying five cards, not five events.

## The vocabulary

Ten records. `py_src/tools/events/model.py` on the Python side,
`src/Marvel.Rules/Events/GameEvent.cs` on the C#, and
`datasets/events/vocabulary.json` holding the two to each other.

| event | payload |
|---|---|
| `CardsCreated` | `area`, `cards` |
| `CardsMoved` | `from`, `to`, `cards` |
| `AreaReordered` | `area`, `order` |
| `CardFormChanged` | `card`, `from`, `to` |
| `CardsFlipped` | `cards`, `face_up` |
| `CardAttached` | `card`, `host` |
| `CardDetached` | `card`, `host` |
| `ControlChanged` | `card`, `from`, `to` |
| `FieldSet` | `card`, `field`, `from`, `to` |
| `CardsChangedBoard` | `cards`, `from`, `to` |

Nine of the ten were measured. The tenth is
[below](#when-the-table-splits), and the reason it could not be measured is the
point of that section.

Every event also carries `kind`, plus `trigger` and `verb` — the engine's own
names for why the transition happened, e.g. `WhenPlayerInTurn` and `Play`. Those
are the half a digest can never show.

`FieldSet` treats absent and zero as different. A field that is gone means the
card no longer registers it at all, which is how a granted trait expires;
serialising that as `0` would say the card still has the trait, at zero.

### Is it complete?

`Apply(before, Derive(before, after)) == after`, checked over **27,895 steps
drawn from all 58 corpus shards**:

- **100.0%** reproduce the next state exactly, with **no residue**, on every
  field the digest records except position.
- All nine kinds measured from digests fire. None is speculative.

Position is 61.1%, and the gap is not a gap in the vocabulary. It is the next
section — and MARVEL-163 closed it: **100%, position included.** See
[Verified against engine state](#verified-against-engine-state).

## Why the digest cannot verify position

An area is not a zone name. `HandsArea` names one area per player and
`UpgradesArea` one per host, so "moved to `HandsArea`" is ambiguous the moment a
second player exists.

The obvious repair is to key an area by `(zone, owner, host)` from the digest's
own fields. **That does not work, and the reason is worth writing down: the
digest's `owner` is the card's controller, not the area's owner.** A side scheme
controlled by player 3 sits in the scenario's side-scheme area next to cards with
no controller at all, so grouping by that triple splits one area in two and
renumbers across the join.

Digest v2 therefore contains no area identity, and cannot acquire one — adding a
field would change every recorded digest and invalidate the frozen corpus.

**This does not block MARVEL-163.** The reducer that verifies the stream applies
events to *engine state*, where areas are real objects, and computes the digest
of the result for comparison. The digest is the comparison surface, not the
reducer's input. The 61.1% figure is a property of the prototype — which reduces
over digests because no engine exists yet — and not of the design.

What it does fix is `AreaRef`: its `Owner` is **the area's owner**, filled in by
an engine that knows, and explicitly not the field of the same name in the
digest.

## Verified against engine state

MARVEL-163. The prediction above was checkable, so it was checked.

`tools/replay/observe.py` replays corpus scenes in-process and hands every
decision to a callback. The seam is `Controller.ChoiceOne` — the only place the
engine stops and asks, and the same place the per-step digest is taken, so a
snapshot made there *is* the state that digest describes rather than something
argued to be equivalent. `tools/events/state.py` builds the board from engine
objects; `tools/events/verify.py` derives, applies and compares.

Every step is checked twice, and the first check is what makes the second one
mean anything:

1. the snapshot must serialise to the digest the engine computed at the same
   instant, **byte for byte**;
2. `Apply(before, Derive(before, after))` must reproduce `after` — both as a v2
   document and as *placement*, every card in the same area object at the same
   index.

**58 scenes, one from each shard, every one replayed to the end. 6,554 steps,
6,496 transitions, 31,980 events.**

| | |
|---|---|
| snapshot == digest | **100.0%** (6,554/6,554) |
| digest reproduced | **100.0%** (6,496/6,496) |
| placement reproduced | **100.0%** (6,496/6,496) |

Position went from 61.1% to 100%, and 35.0% of steps are still silent — the
same figure the digest census measured, from a different source.

### What it found: an area needs an identity

`AreaRef` was `(Zone, Owner, Host)`. Measured over those steps, that triple
**names more than one area** in three cases:

| triple | steps | areas |
|---|---|---|
| `AsideDeck`, scenario, no host | 5,969 | one set-aside nemesis deck per player |
| `RemovedArea`, scenario, no host | 4,318 | 2 |
| `EncounterDeck`, scenario, host c51 | 16 | 2 |

So the triple *describes* an area and cannot *address* one. `AreaRef` gained an
`Id`, empty when a consumer is working from digests and filled in by an engine
that knows. `IsIdentified` is how a reader tells the two apart.

Getting the owner itself right took two attempts, and the first is worth
recording: `Deck2.GetOwner()` is not it. `player.engaged_minions` is
`Deck2(world.GetScenario(), ..., related_player=self)` — the minions engaged
with a player are *owned* by the scenario and *sit* in front of that player.
Reading only the owner answers `-1` for every player's engagement area at once,
and that alone accounted for 380 of the first run's 621 ambiguous steps.
`play_area` is the field that answers the question `AreaRef.Owner` is asking.

### What it found: a landing index describes the final area

The other finding is a reducer bug, and it is the kind that only a second source
of truth can surface.

In one Rhino game, an encounter discard pile received five cards in a single
step from four different source areas. `Apply` walked the `CardsMoved` events in
order and spliced each source's batch in as it came — which put the third
arrival at the index the fifth was going to occupy. Three cards came out in the
wrong order.

**An index a card carries is a position in the area as the step leaves it, not
as the area stands part-way through.** That is forced by where the number comes
from: it is read off the recorded next state, where all of the step's arrivals
are already present. So every removal happens before any insertion, and the
insertions run in destination-index order.

The reason this survived a 100% round trip over digest diffs is worth stating,
because the same shape of mistake will recur: `Derive` predicted the positions
*correctly*, so it emitted no `AreaReordered` to disagree with, and only `Apply`
was wrong. The prediction and the placement were two pieces of code that had to
agree and were never made to. They are now one function, `_Settle`, called by
both.

## When the table splits

**Some scenarios are more than one game at once.** The Once and Future Kang
gives each player their own board at main-scheme stage 3 — their own main
scheme, their own Kang — and rejoins them as each stage completes. Newer
scenarios, including in Fear No Evil, put different main schemes in different
play areas. The boards share a round structure and cannot target each other.

This is not a future problem. `py_src` implements it today:
`World.game_areas` is a list, `World.CreateGameArea()` exists, and
`cards/pack/toafk/kang/` calls both.

### A board is a property of the card, not of the area

This is the one place the model's ordering is counterintuitive, so it is worth
stating flatly. The engine keeps **one** `MainSchemesArea` deck
(`world.area_schemes_main`) no matter how many boards exist, and scopes a query
by filtering it:

```python
def GetMainSchemes(game_area_effect):
    game_area = Worlds.CastGameArea(game_area_effect)
    return [x for x in world.area_schemes_main.Get()
            if x.card.GetGameArea() == game_area]
```

Two consequences the client and the port both need:

- **`AreaRef` can span boards.** Its `Id` addresses a deck, and a deck is not a
  table. A client laying out two tables splits an area's contents card by card.
- **A board change is not a move.** No card crosses an area and no field
  changes, so it needs its own event — `CardsChangedBoard`, batched, because
  Kang's split moves every one of a player's cards at once.

### The digest cannot see any of this

`CardDescriptor` has always sent `game_area` to the client. The **v2 digest
does not record it**, and `digest.CARD_KEYS` is a frozen format — adding a field
would change every recorded digest and invalidate the corpus (MARVEL-158).

Constructed at an ordinary Kang step, because no recorded step reaches the real
split: create a board and move 47 cards onto it, the main scheme among them.

| | |
|---|---|
| v2 digest | **byte-identical** |
| events derived from digests | **none** |
| events derived from engine state | one `CardsChangedBoard`, 47 cards |

**A port that put every card in the right zone at the right index but on the
wrong board would pass every corpus digest check.** That is a hole in the
oracle, not in the vocabulary, and it is recorded in
[state-digest-v2.md](state-digest-v2.md#what-the-digest-cannot-see) and filed
as MARVEL-174.

### And the corpus never gets there

All 42 `the_once_and_future_kang` scenes, replayed in full: **0 of 3,462 steps
reached a second board.** The split is behind a main scheme at stage 3 and the
bot never advances it that far.

So `CardsChangedBoard` is the one member of this vocabulary that a measurement
did not produce, and `tools/events/verify.py` prints `never fired` beside it
rather than hiding that. It is here because the mechanic is implemented and
shipped, not because a diff found it — and the alternative was discovering it
at the point 3,457 card ports were already written against a nine-member
vocabulary.

Covering it needs a **spec**, not a corpus entry — filed as MARVEL-175.

## The signature

```
(state, input) -> (state, Prompt?, GameEvent[])
```

Affordances are MARVEL-161 and are designed alongside this so the two are not
invented twice; they arrive inside a `Prompt`, which carries why the engine is
asking as well as what may be done. The event half is settled here.

The two sides are deliberately asymmetric. A prompt is **absent** when the game
is over and never empty; the event list is empty on 35% of steps.

Three constraints on the records, all of which are cheap now and expensive later:

**Wire types.** Every payload is an integer, a string, or a list of them. Events
cross a socket when the server is hosted rather than embedded, so they need
stable serialisable representations — and a record holding a live card reference
would let the view layer walk the entire state graph, hidden parts included,
through a field that was only meant to say what moved.

**Derived, never maintained.** The interpreter emits these as a byproduct of
executing effect nodes. A parallel hand-written path drifts from the rules, and
then the animations start lying about what happened.

**No presentation metadata.** A sequence of ten nodes might be one visual beat or
ten, and the temptation will be to annotate the event or the DSL. Do neither.
Pacing belongs in a side table keyed by event kind, as
[presentation-layer.md](presentation-layer.md#one-rule-to-protect-the-dsl)
already requires of the card DSL.

## Reproducing the numbers

```bash
cd py_src
python -m tools.events.census ~/Source/marvel-lcg-corpus --json census.json
python -m tools.events.verify ~/Source/marvel-lcg-corpus --per-shard 1
python -m tools.events.emit_vocabulary --check
python -m unittest unit_test.test_event_model unit_test.test_event_verify
```

`verify` boots the engine once per scene, so one shard-wide pass is a couple of
minutes. It exits non-zero on any shortfall.

The census needs the corpus, pinned in `datasets/corpus/UPSTREAM.md`. The unit
tests do not: they state the same properties on boards small enough to reason
about, which is why they run in the fast tier.

## What is not settled here

- **The stream is derived, not emitted.** MARVEL-163 verified it against engine
  *state*; the events still come from comparing two snapshots rather than from
  an interpreter executing effect nodes. That last step lands with the
  interpreter, and what this proves for it is that the vocabulary and the
  reducer are not what will be wrong.
- **Nothing exercises `CardsChangedBoard`.** The corpus cannot reach the split
  and a unit test only states the shape. A behavioural spec that drives Kang to
  stage 3 is what would make it real — MARVEL-175.
- **Ordering within a step.** The prototype emits creations, then moves, then
  reorderings, then per-card changes. The interpreter will emit in execution
  order instead, which is more useful and is not checkable until it exists.
- **Grouping into beats.** How many events make one animation is a view concern
  and belongs to `Marvel.View`.
