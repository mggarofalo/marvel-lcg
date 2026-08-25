# Play areas and game areas

Where a card is, and what that changes about what its own text means.

`src/Marvel.Rules/State/PlayArea.cs`, `GameArea.cs`, `Places.cs`. MARVEL-175.

`docs/event-stream.md` settled what these are *not* — not an event, not a card
tag. This is what they are.

## The word is overloaded three ways in published rules

This is the hazard, and it is worth naming before anything else. "Game area"
appears in the Rules Reference and in eleven packs, meaning three different
things:

| Meaning | Where | Modelled as |
|---|---|---|
| A synonym for a player's play area | `rr:player-s-play-area`, `rr:villain-s-play-area` | `PlayArea` |
| A grouping over play areas that cards cannot reach across | `pack:mc11:game-areas`, `pack:mc55:game-areas` | `GameArea` |
| Any region a card can be in, including out of play | `rr:victory-display`, `rr:search`, `mc16:the-collection` | nothing — it is loose usage |

The Rules Reference says a player's play area is *"also sometimes referred to as
a 'player's game area'"*, and The Once and Future Kang's insert uses the same
phrase for a partition of the table. **Two published meanings for one phrase, one
of which this codebase needs and one of which it must not accidentally
implement.** The third is the rulebooks using "game area" the way a person points
at the table.

## Play areas

`rr:play-area`:

> There are two types of play areas: a player's play area and the villain's play
> area. […] A card cannot be in more than one play area at a time.

So a game has **players + 1**, and every card is in exactly one. Not a new
concept a scenario introduces — the ordinary structure of every game.

An `Area` (a zone: `HandsArea`, `PlayerDeck`) sits in exactly one `PlayArea`, so
a card's play area is looked up through the area it is in and is total by
construction. `rr:play-area.1` puts a player's deck, hand and discard pile in
their play area, and `rr:play-area.2` puts the villain deck, main scheme deck,
encounter deck and encounter discard in the villain's — which is why the
villain's play area is a **value and not a null**. A card in no *game* area is a
real and different thing.

### Three seat-shaped integers, and none of them is the others

The reason `PlayArea` is a type:

| | Question | Where |
|---|---|---|
| the digest's `owner` | who **controls** this card | `CardRecord.Owner` |
| `Area.CardOwner` | who a card **made here** belongs to | `Area` |
| `Area.PlayArea` | which play area this area **sits in** | `Area`, and `AreaRef.Owner` on the wire |

They are all `int`, all seat-shaped, and the first and third agree **98.1%** of
the time — exactly often enough for a confusion to pass its tests and fail on the
cards where whose-is-it drives rules. Before MARVEL-175 the second was called
`Owner` and the third `RelatedPlayer`, which put the *least* alike pair under the
most similar names.

A player's nemesis pile is the worked example: it is **theirs** (play area 1) and
the **scenario's** property (card owner -1). `Places.Reference` is the single
conversion to `AreaRef`, so the wire type and the state cannot drift about which
of the three it carries.

## Game areas

`pack:mc11:game-areas`:

> Cards and components in one game area cannot affect another game area […]
> Players cannot attack or defend enemies in other game areas, and they cannot
> target any game elements in the other game areas. While the players are in
> separate game areas, they continue to use the same encounter deck and encounter
> discard pile.

Three properties, all load-bearing:

- **It contains play areas, not cards.** Stage 3A goes "directly in front of your
  play area" (`pack:mc11:areas`). A card's game area is looked up through its
  play area, which is what makes joining one operation instead of 47.
- **It is a visibility partition, not a partition of the world.** The encounter
  deck stays shared. Never use it to decide where a card physically is.
- **It holds any number of players, including none.**

That last one is the trap. Kang gives each player their own, which makes "one
game area per player" the tempting model. **God of Lies rules it out twice** —
`pack:mc55:game-areas` has *"a collection of 1 to 4 players who work as a team to
fight the villain in their game area"*, and puts Loki himself in *"a neutral game
area that is outside of any group's game area"* with nobody in it. The mc55 case
is not in MARVEL-175's description; it turned up surveying every rules file that
uses the phrase, and it is the one that fixes the shape.

**An ordinary game has exactly one**, made with the world, holding every play
area. Nothing in the rules distinguishes that from having none, so every
predicate is trivially true there — which is the property to preserve. The cost
of this model is meant to be paid only by the scenarios that need it, and
`AnOrdinaryGameIsUnaffectedByAnyOfThis` is the regression guard.

## The rules that resolve by place

`Places`. Each carries the rule it comes from; the tests cite it too.

### "The main scheme" — `pack:mc60:separate-main-schemes`

> Cards in a player's play area (identities, allies, upgrades, minions, etc.)
> that refer to "the main scheme" refer only to the main scheme in the same play
> area. Cards that are not in any player's play area (the villain, side schemes,
> and environments) that refer to "the main scheme" apply to all main schemes.

"Not in any player's play area" **is** the villain's play area — `rr:play-area.2`
lists the same three examples. Two rulebooks, one partition, one test.

Fear No Evil's separate sentence about an event a player plays needs no separate
case: their hand and discard pile are in their play area, so the general rule
finds it.

**One condition is not in the rulebook, and it has to be there.** The sentence
presupposes Fear No Evil's own setup, where every player has a main scheme. In an
ordinary game the single main scheme is in the *villain's* play area, so reading
it literally answers **nothing** for every ally and upgrade in every ordinary game
ever played. So the narrowing applies only when the source's play area actually
holds a main scheme. It is local — no board scan, no flag saying which scenario
this is — and it makes one rule cover both. The test suite caught this on the
first run.

### "Each player" — `pack:mc11:rules-clarifications`

> "Each player" refers to each player in the same game area. If you are the only
> person in your game area, then "each player" refers only to you.

The second sentence is the one worth keeping: the answer is *the players in your
game area*, not *the others*, and it can be just you. An implementation reading
it as "the others" is wrong by one in the exact case the clarification exists to
settle. This rule is not in MARVEL-175's description either.

### Reach — `pack:mc11:game-areas`, `pack:mc55:game-areas`

Same game area, or either card in **no** game area.

**The exception falls out of placement.** mc11 says cards cannot affect another
game area *"(with the exception of the text on stage 2B)"*, and separately that
2B "remains in play in a central location […] though it is not part of any other
game area". So 2B reaches everyone because of **where it sits**, and needs no
flag on the card. That is the whole argument for modelling place: an engine that
models it gets the exception for free, and one that special-cases the scenario
gets it for the cards somebody remembered.

**One half is inferred rather than quoted.** The rules say a card in no game area
affects everyone; they do not say whether everyone can affect *it*. Reach is
implemented as symmetric, which keeps 2B thwartable by the players racing it. If
a scenario ever distinguishes the directions, `Places.CanAffect` is the line that
splits.

### Composition

Nothing published needs both partitions at once, but both say what a card
*cannot* reach, so the answer is the intersection. Asserted, because getting it
wrong is invisible until a scenario needs both.

## What is not here

**No oracle, and that is measured.** The v2 digest cannot see a play area: on the
legacy engine, creating a game area and moving 47 cards into it left the digest
**byte-identical** (MARVEL-174). Kang reaches a second game area in **0 of 3,462
steps** across all 42 recorded scenes. `py_src` has no Fear No Evil cards at all.
So these rules are held against the published text, and the tests name the rule
each one comes from. Nine mutations were watched failing.

**Uniqueness is scoped per game area, and is not implemented.**
`pack:mc11:rules-clarifications`: *"Can two players control the same unique cards
while they are in different game areas? A: Yes. […] When players combine game
areas, they must discard copies of unique cards until only one of each remains in
that game area. If the players cannot agree which one to discard, the first player
decides."* That needs a uniqueness concept the engine does not have, and the
combine case needs a prompt. It is a fourth rule of place and it belongs here when
uniqueness exists.

**The fold cannot tell a client a join happened.** `World.Join` is one operation
on state, which is what MARVEL-175 asks for and what PR #115 got wrong. But no
event describes it, and adding one is a decision rather than an oversight — see
below.

## The event question

`datasets/events/vocabulary.json` is a closed set of nine kinds, defined as *the
set that explains every state change in the frozen corpus with nothing left over
and no member that never fires*. `tools/events/model.py` says so in as many words,
and adds:

> Scenarios that place cards outside the usual play areas […] are a question
> about *state*, not about this list.

A player joining a game area is the first change that is **emittable but not
derivable**: the fold could announce it, and a reducer over digests can never
discover it, because the digest cannot see it. So the vocabulary cannot grow this
member from measurement, which is how every other member got there.

The physical action the rules describe is *"reorient the cards on the table to
indicate that you have joined that game area"* — a client plainly needs to be told.
Three ways out, and it is the same shape of call as MARVEL-174's:

1. Leave the vocabulary alone. A client re-reads the grouping from state after
   every fold. Cheapest; loses the "one visual beat" property every other event
   has.
2. Add a tenth kind and accept that one member is justified by published rules
   rather than by the corpus. Honest, but it weakens the fixture's stated
   contract for every other member.
3. Split the fixture: the **derivable** set (nine, corpus-verified) and an
   **emitted-only** set. Keeps both claims true and costs a fixture shape change
   plus the C# test that reads it.

Not taken here, because `model.py` already records the opposite decision and
overriding it silently inside an unrelated change is how a contract stops meaning
anything.

## Reproducing

```bash
dotnet test tests/Marvel.Rules.Tests            # the rules of place
grep -rli "game area" datasets/rules-packs/ datasets/rules-reference/
```
