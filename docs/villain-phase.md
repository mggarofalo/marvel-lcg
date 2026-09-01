# The villain phase

`src/Marvel.Rules/Play/VillainPhase.cs` implements the phase as agenda steps so
every occurrence can open timing windows or suspend for a player decision.

## Phase order

The engine follows the six steps in `rr:villain-phase`:

| Step | Operation |
|---|---|
| 1 | place the main scheme's acceleration threat, plus active icons and tokens |
| 2 | activate the villain and each engaged minion against every player in player order |
| 3 | deal one encounter card per player, plus hazard cards |
| 4 | reveal dealt encounter cards in player order until every queue is empty |
| 5 | pass the first-player token clockwise |
| 6 | end the villain phase and round |

The villain attacks a player in hero form and schemes against one in alter-ego
form. Engaged minions activate after the villain. Attacks and schemes have their
own agenda steps; see [enemy-attacks.md](enemy-attacks.md) and
[timing.md](timing.md).

## Main-scheme threat

Step 1 places the scheme's printed acceleration value plus one for every active
acceleration icon and token. Icons come from cards in play. Acceleration tokens
belong beside the main-scheme deck and survive a stage leaving play.

All threat placement goes through `Threat.Place`, whether it comes from the
phase, an enemy scheme, Incite, or card text. Interrupts can modify the pending
amount, and a main scheme advances as soon as its threat reaches or exceeds its
target. Side schemes do not complete when they reach a printed value; they
remain in play until defeated or otherwise removed.

A completed main-scheme stage is removed without carrying excess threat. The A
side of the next stage enters and resolves its When Revealed ability before the
card flips to its B side, receives starting threat, and resolves that side. If
there is no later stage, the villain wins immediately and the remaining agenda
is abandoned.

## Enemy activations

An enemy scheme resolves its boost cards and Boost abilities before placing its
modified SCH threat. An attack gives facedown boost cards, asks for a defender,
reveals and resolves boosts, fixes the attack value, deals damage, and ends the
attack. Boost icons are scoped modifiers that expire with that activation.

Card abilities enter through `ICardAbilities`. A printed Boost or When Revealed
ability without an executable Core definition raises
`RulesNotImplementedException`; the phase never treats unknown text as silence.

## Encounter cards

Step 3 deals one facedown encounter card to every player and distributes one
additional card for each active hazard icon in player order. A card dealt by an
earlier effect remains in the same queue.

Step 4 is a loop, not a snapshot. It reveals the next card, lets that reveal
finish, and then checks the queue again. Surge and any effect that deals another
card during step 4 therefore extend the same phase step.

`rr:reveal.step.2` places a card before its revealed abilities resolve:

| Kind | Destination |
|---|---|
| minion | engaged in the revealing player's play area |
| side scheme | villain play area |
| obligation | revealing player's play area |
| attachment without its own destination | in front of the player, out of play |
| treachery or other encounter card | revealing area, then the appropriate discard |

Keyword-provided abilities such as Surge, Incite, Hinder, Quickstrike,
Toughness, and Teamwork use the same timing and resolution ledger as printed
ability rows. If multiple simultaneous forced abilities need ordering, the
first player receives an order prompt.

## Deck exhaustion

When the encounter deck empties, its discard pile is shuffled into a new deck
using the game RNG and one acceleration token is placed. If both piles are
empty, `rr:encounter-deck.4` defines the infinite acceleration loop as a player
loss, so the engine ends the game rather than spinning.

## Card state used by the phase

Printed stats and modifiers come from `datasets/cards/`. Token-pool registration
is persistent card state: once a card enters a zone that grants its printed
pools, those fields remain registered even after the card moves elsewhere.
Remaining hit points are the printed value minus `Card.Damage`; damage is not a
separate digest token.

Ownership, control, host relationships, and area determine whether modifiers
are active. An attachment in a discard pile no longer modifies its former host.
These state rules are shared with the player phase rather than special-cased for
the villain phase.

## Supported boundary

Rhino, Klaw, and Ultron run in Standard and Expert mode with the Core modular
sets listed in [scope.md](scope.md). The same engine primitives are tested
against broader rules patterns, but later scenarios require their complete
setup, product rules, ability rows, and behavioral scenes before becoming
executable.
