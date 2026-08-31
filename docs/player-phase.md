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

### A generator that is not a card in hand

`rr:resource-ability.1`: one "can be triggered **anytime the player who controls
the ability is generating resources to pay a cost**" — so it sits beside the
cards in hand rather than in a window. Another way to make a resource, not
another moment. 75 cards print one.

**The recorded prompt has been carrying it all along.** It lists *six*
generators for a hand of six cards one of which is being played, and a list
built from hand cards alone was short by exactly that one. It is Peter Parker's
"Scientist — **Resource**: generate a [mental] resource", printed on the
alter-ego face the recorded board is showing.

Using one is **not** discarding it: `rr:cost.3`'s "by discarding cards from
their hand" is the other way, and an identity cannot be discarded at all
(`rr:identity.3`).

**"Limit once per round" is kept as a lasting effect, not a token.** A card's
tokens are the digest's `fields`, so counting uses there would put a number in
every recorded board that the recording does not have. A lasting effect is not
digested and expires at the end of the round without anything having to
remember. `rr:limit` counts "per instance of that ability", which is per card in
play — two Peter Parkers at one table would have one use each.

`PlayerPhaseTests` could not assert the recorded generators at all before this.
It now compares them **by what they make**: the count and the letters match
exactly. The ids still cannot be compared, and that is a separate finding —
the recording's `effect` is the Python engine's own effect id rather than an
object id (MARVEL-223).

### Triggering an action

`rr:player-turn.5` — "trigger an **Action** ability on: a. a card in play they
control. b. an encounter card in play. c. any card in play with text that allows
that player to trigger its action ability. d. an event card in their hand *(by
playing that event)*."

**966 cards in the pool print one**, 445 of them events — and an event is
reached *only* this way, which is why `CardPlay.Price` refuses to offer one:
`rr:player-turn.2`'s list of what may be played from hand does not include
events at all.

An action is **not in a window**. It happens because a player says so on their
turn, so it is offered beside the basic powers rather than in an interrupt or a
response — which is why `AbilityTypes.PriorityOf` has always refused to give it
a tier, and why `ICardAbilities` asks for it separately from `Waiting`.

Acceptance starts an occurrence, however. `Steps.TurnAction` goes onto the
agenda with the resolving player, source card, chosen targets and payment. Its
interrupt window precedes payment; its response window follows the completed
effect. Costs and effects use that same occurrence, including a defeat caused
by a self-damage cost. A choice merely suspends that occurrence: it is not a
second occurrence and does not open a second pair of windows.

`.c` is a card's own text and belongs to whichever card says it, so there is
nothing general to write for it. `.6` lets the active player ask another player
to trigger anything they could trigger on their own turn, and lets that player
offer. The engine presents those actions directly. Taking one represents the
request being accepted or the action being offered; there is no separate
request/accept handshake. The other player remains the ability's resolving
player, so their form, resources, targets and limits apply.

If that resolving player is eliminated by the Action, the ability still
finishes (`rr:player-elimination.5`). The active player's turn continues when
the active player remains in the game; if the active player was eliminated,
the next participating player takes their turn.

**The form gate is `.5.1`**: "if the action ability is preceded by *Hero* or
*Alter-Ego*, the player must be in the specified form", and 728 of the 966 are.
It is a field on the trigger rather than two more ability types, because it is a
form and not a timing.

**Costs are the ability's, not the card's.** `rr:cost` — "a cost is anything a
player must do or pay in order to initiate an ability" — and 560 of the 966
print one. Two forms are written, and they are the two commonest: **exhausting**
the card the ability is on (280 cards) and **spending resources** (142).
`rr:initiating-abilities.step.3` asks whether the cost can be paid *before the
ability is offered*, because step 5 aborts "without paying any costs" and an
affordance that would abort is a trap rather than an offer.

Only a resource cost reaches the wire, because only a resource cost is a
*choice*: exhausting the card has one way to be paid, so there is nothing to ask
and nothing to carry. `rr:resource.4` applies as it does to a card — a cost of
three physicals is not a cost of three — and the payment itself is
`CardPlay.Spend`, shared with playing a card, because `rr:cost` is one rule for
both and playing a card is initiating an ability.

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

The verb strings are on the wire: the four are spelled `Attack`, `Defense`,
`Thwart` and `Recover`, the rulebook's own names capitalised. A client renders
them, so they are a contract and not a label.

Two limits are easy to miss and both are cited on the tests: a thwart cannot
take more threat than is on the scheme (`rr:threat` counts tokens, and a scheme
cannot hold a negative number), and `rr:guard.1` is engagement-specific — a
minion guarding *another* player does not stop you.

#### A basic power is scheduled, not called

`BasicPowers.BasicAttack` and `BasicPowers.BasicThwart` pay the cost — the
character exhausts — and then put a **step** on the agenda. Neither one deals
its damage or takes its threat off before returning, and a test that calls
either has to run the agenda out (`Agendas.Finish`) before asking what the
board looks like.

The reason is that a basic power has windows around it and a window can ask.
For the attack the rules write the windows out: `rr:attack-player-ability-type.step.7`
and `.step.8` are the abilities triggered by it. For the thwart they do not —
`rr:thwart` lists no steps at all — so the case is made by
`rr:consequential-damage.1`, which deals an ally's consequential damage *"after
resolving abilities that are triggered by the ally attacking **or
thwarting**"*. A rule that orders something after the abilities triggered by a
thwart takes for granted that there are such abilities and that they have a
place to go.

So both halves are three steps rather than one call:

| step | what it is |
|---|---|
| `Steps.CharacterAttacks` | the ATK damage lands |
| `Steps.CharacterThwarts` | the THW threat comes off |
| `Steps.AllyConsequentialDamage` | an ally that attacked takes its icons |
| `Steps.AllyThwartConsequentialDamage` | an ally that thwarted takes its icons |

The last two are one rule and two steps only because they differ in what they
record: the event stream spells the damage under the verb the ally used, and a
reader telling an attack from a thwart reads that. **Which field** the icons
came from is a third question and is asked when the damage is dealt rather than
when the step was scheduled — `rr:assault.2` sends an ally thwarting an
assaulted scheme to the icons under its ATK, and `rr:ability.9` makes a
constant ability true only while its condition holds, which a step is long
enough for it to stop doing.

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
wild. Those letters are the engine's wire-format choice; the generated card
dataset maps MarvelSDB's structured resource flags to them.

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

- Cost modifiers, `X` costs and the per-player icon on a cost — all refused by
  name rather than read as a number.
- **The interrupt window around a defeat.**
  `rr:when-defeated-abilities` itself is written — the ability resolves before
  the card leaves play, which is `.2.1` — but a defeat opens no window, so
  nothing *else* can interrupt one. No card in the pool does except by its own
  "When Defeated".
- **Who defeated a card.** 58 minions say "the player who defeated [this]", and
  `Defeat` does not carry it: a defeat happens inside `Damage.Deal`, which does
  not know who dealt the damage. A card that asks for a resolving player it has
  not got is refused by name rather than given the first player.
- **Carrying attachments across a villain stage.** `rr:villain-defeat.3` and
  `.4` decide by whether the new stage shares a title; a villain defeated with
  cards attached throws rather than carrying the wrong set.

### Playing it with the cards doing what they say

`WholeGameTests` plays with `NoCardAbilities`, so what it proves is that the
**rules** reach an ending. It could not prove more: until recently every
encounter card resolved to silence, and a game where nothing a card says happens
is not the game.

`RealCardsGameTests` plays the same board with the real interpreter, and states
the gap as a **list** rather than a number. Forty seeds either run to an ending
or stop on a card nobody has written, and *which cards* is asserted — so
authoring one is a visible change there, and a new blocker is a failure.

**All forty reach an ending.** Every one of the scenario's twenty-four cards is
written, and so is every card of the nemesis set that Shadow of the Past brings
in.

It carried a list of the cards that blocked it while there were any, which is
how it earned its keep: authoring Eviction Notice let the seeds get further,
they reached Shadow of the Past, and Highway Robbery appeared as a blocker
nobody had a reason to look for.

The policy declines what it can, which is what keeps the coverage broad rather
than lucky: a hero who never acts meets more of the encounter deck than one who
wins. `Question.Option` is the only question it must answer, because
`rr:choose-option` offers a choice between things that happen.

`WholeGameTests` plays the Rhino board to an ending on four seeds — 24 to 53
decisions across 4 to 8 rounds, cards paid for and played, defenders declared —
and checks that one seed plays the same game twice, digest and all. It plays the
same board at two and three players, because a second player makes every "in
player order" sentence reachable and a third makes "the next clockwise player"
stop meaning "the other one".
