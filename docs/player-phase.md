# The player phase

`src/Marvel.Rules/Play/Game.cs`, `src/Marvel.Rules/Play/PhaseEnd.cs`,
`src/Marvel.Rules/Play/PlayerDeck.cs`.

`rr:player-phase`: *"During the player phase, **each player** (in player order)
takes one turn. After each player has taken a turn, the players discard down or
draw up to their hand size and ready each exhausted card."*

## Each player takes a turn

The engine used to give exactly **one** turn per round however many players
were at the table, so at two players the second never acted. The recorded
milestone game could not notice: it has one player.

Turns go round from whoever holds the first player token, and `Game.Next`
returns null at the end of the table rather than wrapping — both callers want
to know when the round of opportunities is *complete*, which is
`rr:in-player-order.1`'s condition for stopping.

The first player is re-read from the board when the phase starts, not carried
over from the deal, because a scenario or a card can move the token.

## The five steps of ending it

`rr:end-of-player-phase` lists five, and the split between them is *not*
arbitrary:

| step | who | where it lives |
|---|---|---|
| 1. discard down to hand size | **in player order** | the end-phase prompt, once per seat |
| 2. draw up to hand size | **simultaneously** | `Steps.DrawToHandSize`, one agenda step |
| 3. ready every card | **simultaneously** | `Steps.ReadyCards`, one agenda step |
| 4. effects ending "until the end of the phase" | — | `PhaseEnd.EndPlayerPhase` |
| 5. "when/after the phase ends" effects | — | the same |

Step 1 is "in player order" and steps 2 and 3 are "simultaneously", so step 1 is
a question asked once per seat and the other two are one step each for the whole
table. Nothing may happen between one player's draw and another's.

### Step 1 has two clauses and they are different rules

> "In player order, each player **may** discard any number of cards from their
> hand, and **must** discard down to their hand size if they have more cards
> than their hand size."

The first makes an empty answer legal. The second makes it *illegal* for an
over-full hand — so this is not a prompt that can simply be declined, and a
decline that leaves too many cards is refused **by name** rather than the engine
discarding on the player's behalf. Which cards go is their decision.

This bites the moment a player changes form: Peter Parker's hand size is 6 and
Spider-Man's is 5, so a hero who flips and then ends their turn is holding one
card too many. Two tests that ended the phase with a decline started failing
when this landed, and they were the ones that were wrong.

### Step 2 draws one card at a time

`rr:hand-size.1`: *"a player draws cards one at a time, **checking after each
card is drawn** whether they are at their hand size."* That is not the same as
computing a count and taking that many — a card drawn can change the hand size,
and `rr:player-deck.2` has the deck run out and reshuffle mid-draw.

### Step 3 readies encounter cards too

*"Each player simultaneously readies all of their cards. **Ready each exhausted
encounter card.**"* The second sentence is why this walks every place in play
rather than each player's: an exhausted minion is nobody's card and readies
anyway.

## When a deck runs out

`rr:player-deck.1`: *"If a player deck empties, the player shuffles their
discard pile to make a new deck. **That player immediately deals themself one
facedown encounter card** from the top of the encounter deck."*

The second sentence is the price of the first. Three details are load-bearing:

- **The trigger is the deck emptying, not the next attempt to draw from it.**
  The reshuffle draws from the game's one random stream, so moving it one draw
  later changes every card drawn afterwards for the rest of the game.
- **`rr:player-deck.4`** — an empty deck beside an empty discard pile is a
  legal, stable board. The player simply draws nothing, and the reset is *owed*:
  it happens the moment a card lands in the discard pile, which is why
  `Discard.Card` calls `PlayerDeck.Reset`. The rule says "at least one card",
  and it means one — the first discard rebuilds a one-card deck and costs the
  encounter card.
- **The dealt card is not revealed then.** It joins the queue
  (`rr:deal-deal-an-encounter-card`) and is revealed in the next villain phase.
  That is why [villain-phase.md](villain-phase.md)'s step 4 had to become a
  queue before this could be written at all.

## What is not implemented

A turn currently offers only "change form" and "end the turn". The rest of
`rr:player-turn` — playing a card, the basic powers, ally actions, triggered
actions, asking another player — is not written, and a player who declines
everything loses in three rounds because nobody ever thwarts.
