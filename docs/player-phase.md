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

## What a turn offers

`rr:player-turn` lists six options, "in any order", and "each option, **except
'change form'**, may be performed as many times as the player is able". So
using one does not end the turn — the same prompt is put again, offering
whatever is still possible.

**Only what can actually be taken is offered.** An affordance that would throw
when taken is worse than an absent one; MARVEL-130 was that same defect on the
action menu. So a basic attack appears only when there is an enemy that can be
attacked, a basic thwart only when a scheme holds at least one threat, and a
basic recovery only when there is damage to heal.

### The basic powers

`rr:basic-power.1` lists five. Three are a player's to use on their turn:

| power | form | `rr:player-turn.3` |
|---|---|---|
| **Attack** | hero | exhaust, deal ATK damage to an enemy |
| **Thwart** | hero | exhaust, remove THW threat from a scheme |
| **Recover** | alter-ego | exhaust, heal REC damage from yourself |

Defence is the fourth and belongs to an enemy's attack rather than to a turn —
see [enemy-attacks.md](enemy-attacks.md). Scheme is the fifth and is an
enemy's, not a player's.

The verb strings are on the wire. The oracle's `Effect.GetDisplayName` names
the four `Attack`, `Defense`, `Thwart` and `Recover`, and
`datasets/digest/prompts.json` checks the half of the return value they appear
in.

Two limits are easy to miss and both are cited on the tests: a thwart cannot
take more threat than is on the scheme (`rr:threat` counts tokens, and a scheme
cannot hold a negative number), and `rr:guard.1` is engagement-specific — a
minion guarding *another* player does not stop you.

## Damage, and defeat

`rr:damage` is one rule however the damage arrived, so an enemy attacking a
hero and a hero attacking a minion go through the same `Damage.Deal`. It
returns whether the target was defeated, because `rr:defeat` is the other half
of the same moment: *"if a character has zero or fewer remaining hit points [...]
it is defeated"* — **zero or fewer**, not fewer than zero.

`rr:defeat.1` and `.2` split what happens next by card type. An ally, minion or
side scheme is **discarded**; an identity or stage of the villain is **removed
from the game**.

`rr:villain-defeat` then reveals the next stage, and *"if the final stage of the
villain deck is defeated, the players win the game"*. That is why `World.IsOver`
became `World.Result`: the rules name **two** endings — this one and the villain
completing the final main scheme (`rr:main-scheme-main-scheme-deck.2.1`) — and a
boolean can say a game is over without saying which happened, which is the one
thing a player wants to know.

`rr:villain-defeat.2`: excess damage does not carry over, so a new stage starts
clean.

## Playing a card

`rr:player-turn.2` — "play an ally, upgrade, support, or player side scheme card
from hand" — and `rr:initiating-abilities`' seven steps, numbered in the code
because the order **is** the rule: restrictions before cost, cost before
payment, and step 5 aborting *without paying anything* when the payment falls
short.

Resources are letters, because `ResourceSource.Generates` carries "resource-type
letters — one per resource". `B` is mental, `Y` energy, `R` physical and `G`
wild, measured over the 1,717 single-type cards that carry both the engine's
`RES` field and MarvelSDB's stat; three disagree and are filed rather than
decided.

`rr:resource.4`'s specific types are **part of** the cost rather than additional
to it, and a wild is spent only when no exact match is left — spending one that
did not have to be spent can fail a later requirement.

**Two restrictions the recorded prompt pinned.** Its opening hand holds six
cards and it offers four: `01088` Energy is a resource card, which
`rr:player-turn.2` does not list, and `01003` Backflip is an event whose ability
is an *Interrupt* — `rr:player-turn.5.d` reaches an event only through an
**Action** ability, and 555 of the 602 events in the pool have none.

## Losing

`rr:player-elimination` — a player whose identity is defeated. Five steps, and
step 1 hands the first player token on **before** step 5 removes the play area
it was sitting in.

Step 5 **removes** rather than discards: "remove the eliminated player's play
area and each other game element within it *(hand, deck, discard pile, cards in
play, hit point dial, etc.)*". Discarding a deck into its own discard pile is
[`rr:player-deck.1`'s trigger](#when-a-deck-runs-out), so an eliminated player
would reshuffle a deck they no longer have.

`rr:player-elimination.6` is why the seat stays: "effects that refer to the
players in the game ignore eliminated players, **except for the per player
icon**". A villain's `14*` hit points do not shrink when somebody dies.

## What is not implemented

- **Ally actions** (`rr:player-turn.4` covers using an ally to attack or thwart,
  which is written; a card's own **Action** ability is not), **triggered
  actions** (`.5`) and **asking another player** (`.6`).
- **`rr:resource-ability`** — the "**Resource**" timing trigger. The recorded
  prompt lists six generators for a hand of six cards, one of which is being
  played, so one generator is not a card in hand at all. That one is this.
- Cost modifiers, `X` costs and the per-player icon on a cost — all refused by
  name rather than read as a number.
- **`rr:when-defeated-abilities`.** A forced interrupt, so it resolves *before*
  the card leaves play — which makes it an agenda step with a window rather than
  part of the defeat call. Nothing in the dataset has one yet.
- **Carrying attachments across a villain stage.** `rr:villain-defeat.3` and
  `.4` decide by whether the new stage shares a title; a villain defeated with
  cards attached throws rather than carrying the wrong set.

`WholeGameTests` plays the Rhino board to an ending on four seeds — 24 to 53
decisions across 4 to 8 rounds, cards paid for and played, defenders declared —
and checks that one seed plays the same game twice, digest and all. It plays the
same board at two and three players, because a second player makes every "in
player order" sentence reachable and a third makes "the next clockwise player"
stop meaning "the other one".
