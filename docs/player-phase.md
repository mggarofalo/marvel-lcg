# The player phase

`src/Marvel.Rules/Play/Game.cs`, `src/Marvel.Rules/Play/PhaseEnd.cs`,
`src/Marvel.Rules/Play/PlayerDeck.cs`.

`rr:player-phase` gives each participating player one turn in player order,
starting with the first player. The engine re-reads the first-player token when
the phase begins and skips eliminated players. A turn continues until its player
chooses to end it; taking another option does not end the turn.

## Turn options

`rr:player-turn` permits a player to:

- change form once voluntarily each round;
- play an ally, upgrade, support, or player side scheme from hand;
- use the basic attack, thwart, or recover power when legal;
- trigger an Action ability;
- ask another player to trigger an Action they could use; or
- end their turn.

Affordances expose only legal choices. For example, attack requires a legal
enemy, thwart requires a legal scheme with threat, recovery requires damage,
and a cost must be payable before a card or ability is offered. After a choice
resolves, the engine derives the next affordances from the new board.

### Actions and resources

An Action is initiated during a turn, not from an interrupt or response window.
It still creates an occurrence: its interrupt window precedes payment and its
response window follows the completed effect. A choice inside the action
suspends and resumes that same occurrence.

Resource abilities are available while paying a cost. They sit beside resource
cards in hand as generators and do not discard their source unless the printed
cost says so. Form requirements, usage limits, legal targets, and costs belong
to the individual ability definition.

The resource letters `B`, `Y`, `R`, and `G` mean mental, energy, physical, and
wild. Their spelling is an engine wire-format choice. Exact requirements are
part of the total cost; a wild resource is assigned only where an exact resource
does not satisfy the requirement.

### Basic powers

`rr:basic-power.1` defines the three powers a player uses during their turn:

| Power | Form | Effect |
|---|---|---|
| Attack | hero | exhaust the character and deal its ATK damage to an enemy |
| Thwart | hero | exhaust the character and remove its THW threat from a scheme |
| Recover | alter-ego | exhaust the identity and heal its REC damage |

Attack and thwart are agenda steps rather than immediate method calls. Their
interrupt and response windows may suspend the game, and an ally's consequential
damage follows the triggered abilities for the power it used.

Defense is a basic power used during an enemy attack, not a player-turn option.
Enemy scheme is likewise an activation power rather than a player action.

## Playing a card

Card play follows `rr:initiating-abilities` in rule order: check restrictions,
determine cost, choose and generate resources, pay, and resolve. A failure before
payment changes no state. Event cards are played through their printed Action,
Interrupt, or Response ability rather than through the permanent-card option.

The engine distinguishes ownership, control, and the play area receiving the
card. Limits such as unique, restricted, and maximum-per-player are checked both
when an affordance is built and when the submitted action is applied.

## Damage, defeat, and elimination

All damage uses `Damage.Deal`. A character with zero or fewer remaining hit
points is defeated; prevention, tough, defense, piercing, overkill, and defeat
windows are resolved through the same pipeline.

An ally, minion, or side scheme normally goes to the appropriate discard pile.
An identity defeat eliminates that player. A villain stage defeat reveals the
next stage or wins the game when the final stage is defeated. Excess damage does
not carry to a later villain stage.

Elimination follows `rr:player-elimination`: the first-player token is passed if
necessary, the player's game elements are removed rather than discarded, and
the seat remains available for per-player values while ordinary effects ignore
the eliminated player.

## Ending the phase

`rr:end-of-player-phase` defines five ordered steps:

| Step | Execution |
|---|---|
| 1. discard cards and come down to hand size | one prompt per player, in player order |
| 2. draw up to hand size | one simultaneous table step |
| 3. ready all cards | one simultaneous table step, including encounter cards |
| 4. expire effects ending with the phase | `PhaseEnd.EndPlayerPhase` |
| 5. resolve abilities triggered by the phase ending | the phase-end occurrence |

A player may discard any number of cards in step 1 but must finish at or below
their current hand size. The engine never chooses hidden cards on the player's
behalf. Step 2 draws one card at a time and re-evaluates hand size after every
draw. Step 3 walks every in-play area so exhausted encounter cards ready too.

## When a player deck empties

The empty deck is rebuilt immediately from its discard pile, using the game's
single seeded random stream, and the player is dealt one facedown encounter
card. That card joins the encounter queue and is revealed during the next
villain phase.

If both deck and discard pile are empty, the board remains stable until a card
enters the discard pile. That first discard pays the pending reset: it becomes
the new deck and the encounter card is dealt.

## Supported boundary

The Core Set player phase, starter decks, and card abilities are executable.
The card DSL refuses unsupported operations by name. Broader card records and
synthetic rules tests validate general primitives but do not make later products
playable. See [scope.md](scope.md) and [card-dsl.md](card-dsl.md).
