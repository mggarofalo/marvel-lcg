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
into twelve shapes and no more:

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

Nine records. `py_src/tools/events/model.py` on the Python side,
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
section.

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

## The signature

```
(state, input) -> (state, Affordance[], GameEvent[])
```

Affordances are MARVEL-161 and are designed alongside this so the two are not
invented twice. The event half is settled here.

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
python -m tools.events.emit_vocabulary --check
python -m unittest unit_test.test_event_model
```

The census needs the corpus, pinned in `datasets/corpus/UPSTREAM.md`. The unit
tests do not: they state the same properties on boards small enough to reason
about, which is why they run in the fast tier.

## What is not settled here

- **MARVEL-163**, verifying the stream against the corpus, once an engine emits
  it rather than a differ deriving it.
- **Ordering within a step.** The prototype emits creations, then moves, then
  reorderings, then per-card changes. The interpreter will emit in execution
  order instead, which is more useful and is not checkable until it exists.
- **Grouping into beats.** How many events make one animation is a view concern
  and belongs to `Marvel.View`.
