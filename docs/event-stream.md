# The semantic event stream

MARVEL-160. The engine's return signature gains a list of events describing what
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
opinion. The census diffed consecutive digests across the whole of it.

**1,773 scenes. 201,870 transitions. 1,365,439 individual changes.** They fall
into twelve shapes and no more — *as far as a digest can see*, which is a real
limit and is picked up in
[Play areas and game areas](#play-areas-and-game-areas):

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
produces an empty event list. Empty and absent are different, and the engine
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

Nine derivable records and two emitted-only records, defined by
`src/Marvel.Rules/Events/GameEvent.cs`.

**The tables below are checked, not decorative.** `EventVocabularyTests` parses
each class out of this file and holds it against what the serialiser actually
writes — kind for kind and key for key. Renaming a field in C# without renaming
it here fails, and so does the reverse.

### Derivable events

These are exactly the nine kinds measured from the corpus. A digest transition
can derive each one, all nine fired, and no observed change needs a tenth.

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

### Emitted-only events

This separate class is for a change the engine directly observes and a digest
cannot represent. It is part of the serialised `GameEvent` union without
changing the claim that the derivable set is exactly the measured nine.

| event | payload |
|---|---|
| `PlayAreaJoined` | `play_area`, `game_area` |
| `PlayAreaDetached` | `play_area`, `game_area` |

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
- All nine kinds fire. None is speculative.

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

## Play areas and game areas

The design note that prompted this section was that some scenarios put
different main schemes in different play areas. Following it up changed the
answer twice, so the rules are quoted rather than paraphrased.

### The vocabulary is the game's, and it is overloaded

Rules Reference v1.8, [`rr:play-area`](../datasets/rules-reference/entries/play-area.md):

> There are two types of play areas: a player's play area and the villain's
> play area. […] A card cannot be in more than one play area at a time.

So a play area is a **place**, every game has *players + 1* of them, and a card
is in exactly one. That is not a new concept for a scenario to introduce — it is
the ordinary structure of every game, and `AreaRef.Owner` already is it.

The overload to watch: RR calls a player's play area "*also sometimes referred
to as a 'player's game area'*", while The Once and Future Kang's insert uses
"game area" for something else entirely. Two published meanings for one phrase,
one of which this codebase needs and one of which it must not accidentally
implement.

### Protection Racket needs nothing new

Fear No Evil, [`pack:mc60:separate-main-schemes`](../datasets/rules-packs/mc60/separate-main-schemes.md):

> Each main scheme is in the play area of the player who chose it. […] Cards in
> a player's play area (identities, allies, upgrades, minions, etc.) that refer
> to "the main scheme" refer only to the main scheme in the same play area.
> Cards that are not in any player's play area (the villain, side schemes, and
> environments) that refer to "the main scheme" apply to all main schemes.

One game. No new containers. A main scheme sits in a player's play area, which
`AreaRef("MainSchemesArea", owner: 2, …)` already expresses.

**What it does need is a rule, and the rule is generic.** "The main scheme"
resolves relative to the play area of the card that said it, and falls back to
*all* main schemes when the source is in no player's play area. That is
resolution, and it belongs to the engine. The card text is unchanged, and no
card datum records which scenario it is in — a card is in a place, and what the
place means is the engine's business.

The same paragraph shows why: a crisis icon on a side scheme "prevents threat
from being removed from any main scheme" precisely *because* a side scheme is in
no player's play area. The rule is stated in terms of place, so an engine that
models place gets it for free, and one that special-cases the scenario does not.

### Kang's game areas contain play areas

[`pack:mc11:areas`](../datasets/rules-packs/mc11/areas.md):

> Each stage 3A tells the player who revealed it to "create your own game area
> and place this scheme in it." To do this, place your stage 3A on the table
> directly in front of your play area. […] Stage 2B remains in play in a central
> location […] though it is not part of any other game area.

[`pack:mc11:game-areas`](../datasets/rules-packs/mc11/game-areas.md):

> Cards and components in one game area cannot affect another game area […]
> While the players are in separate game areas, they continue to use the same
> encounter deck and encounter discard pile. […] When you defeat Kang (II) in
> your game area, you are instructed to join another game area […] choose a game
> area and reorient the cards on the table to indicate that you have joined that
> game area. Any side schemes that were in play in your previous game area
> become part of the game area that you join.

Three things follow, and none of them is a card property:

- A game area **contains play areas** — "directly in front of your play area" —
  plus unowned cards like side schemes. Players + one, the extra being 2B's
  central location.
- It is a **visibility partition**, not a location: cards cannot affect, target,
  attack or defend across it. Meanwhile the encounter deck stays shared, so it
  is not a partition of the whole world either.
- Joining is a **player-level** operation. One player joins; their side schemes
  come with them; their engaged minions stay engaged.

### The shortcut this deliberately does not take

Tagging every card with a game area and keeping one deck per zone regardless,
then filtering by the tag, works. It is an implementation shortcut rather than
the rule, and it is not the model here.

It was briefly copied into this document as a tenth event, `CardsChangedBoard`,
carrying the 47 cards a split retags at once. That was wrong on three counts and
has been reverted: "board" is not a word in this game; the unit is a player
joining a game area, not 47 cards changing a tag; and it put a rules concept
onto card data, which is the thing that makes data know how it is being used.

**Where it belongs is state, not the event vocabulary.** A game area is a
grouping over play areas, so the engine's state needs the grouping and an event
would describe a *player* joining one. That was filed as MARVEL-175 and is now
answered in [places.md](places.md): `PlayArea`, `GameArea` and the rules that
resolve by place live in `Marvel.Rules.State`, and `World.Join` moves a play area
in one operation.

Surveying every rules file that uses the phrase turned up a third published
meaning and a second scenario. God of Lies' Epic Multiplayer Mode
(`pack:mc55:game-areas`) puts *a group of one to four players* in a game area and
Loki in a neutral one with nobody in it, which rules out the tempting "one game
area per player" model that Kang alone would have suggested.

**The topology events are emitted-only.** Joining or leaving a game area changes
state that the engine can announce and a reducer over digests can never
discover, because the digest cannot see it (MARVEL-174). `PlayAreaJoined` and
`PlayAreaDetached` therefore live in the separate emitted-only class above.
Each operation emits one record for the moving play area; no card changes area
or carries a game-area tag.

### The oracle is blind to all of it

Whichever way it is modelled, the v2 digest cannot see it. Its `owner` is the
card's **controller**, not the play area the card sits in, and every main scheme
shares one `MainSchemesArea` — so a main scheme in player 2's play area and one
in the villain's are indistinguishable to it.

Demonstrated on the legacy engine, constructed at an ordinary Kang step because
no recorded step reaches the real split: creating a game area and moving 47
cards into it left the digest **byte-identical**.

That is a property of the digest, not of any scenario, and it is the same
weakness `AreaRef.Id` exists to work around one level up: **the digest describes
areas rather than identifying them.** MARVEL-174 keeps v2 frozen and assigns
this behavior to rule-cited executable tests; the full boundary, including the
game-area topology a future digest would also have to carry, is recorded in
[state-digest-v2.md](state-digest-v2.md#the-boundary-is-deliberate).

### And nothing exercises either scenario

- Kang: all 42 `the_once_and_future_kang` corpus scenes replayed in full,
  **0 of 3,462 steps** reached a second game area. The split is behind a main
  scheme at stage 3 and the bot never advances that far.
- Protection Racket: a forward requirement, with published rules already in
  hand.

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

The census and verification tools are gone; these numbers are not re-runnable.

`verify` boots the engine once per scene, so one shard-wide pass is a couple of
minutes. It exits non-zero on any shortfall.

The census needs the corpus, pinned in `datasets/corpus/UPSTREAM.md`. The unit
tests do not: they state the same properties on boards small enough to reason
about, which is why they run in the fast tier.

## What is not settled here

- **The completeness proof is derived.** MARVEL-163 verified the nine measured
  kinds by comparing engine states. Rule paths now emit events while executing,
  and the play-area topology events can only be covered that way; the corpus
  still cannot prove execution ordering for the stream.
- **Ordering within a step.** The prototype emits creations, then moves, then
  reorderings, then per-card changes. The interpreter will emit in execution
  order instead, which is more useful and is not checkable until it exists.
- **Grouping into beats.** How many events make one animation is a view concern
  and belongs to `Marvel.View`.
