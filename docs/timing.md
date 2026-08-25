# Timing

The order everything in the game happens in, and where the engine stops to ask.

## The spine

`rr:ability` opens with a list, and that list is the whole ordering the engine is
expressed in. Every occurrence — placing threat, dealing damage, playing a card,
revealing an encounter card, a scheme completing — is surrounded by it.

| | `TimingPriority` | |
|---|---|---|
| 1 | `Continuous` | constant abilities, delayed effects, lasting effects |
| 2a | `StatusForcedInterrupt` | status card "**Forced Interrupt**" abilities |
| 2b | `ForcedInterrupt` | "**Forced Interrupt**" abilities |
| 2c | `Interrupt` | "**Interrupt**" abilities |
| 3 | `Occurrence` | "**Boost**" and "**When Revealed**" — the occurrence itself |
| 4a | `ForcedResponse` | "**Forced Response**" abilities |
| 4b | `Response` | "**Response**" abilities |
| 5 | `ConsequentialDamage` | consequential damage |

The Rules Reference numbers five items and sub-numbers two of them. The eight
members are that structure flattened, because the sub-items are strict
priorities rather than groupings: a status card's forced interrupt beats an
ordinary one, and a forced response beats an ordinary one.

A ninth member, `Untimed`, is for ability types with a bold trigger and no place
on this list — `Action`, `Forced Action`, `Resource`, `Setup`, `Special`. An
action is taken during a player's turn, a resource ability while a cost is being
paid, a setup ability during setup. Giving them a tier would put them in windows
they do not belong in.

## Two places the Python engine has this wrong

Both produce a board that looks entirely plausible, which is why they are worth
naming rather than quietly fixing.

**"When Defeated" and "When Completed" are forced interrupts.**
`rr:when-defeated-abilities.1` and `rr:when-completed-abilities.1` define each as
exactly `Forced Interrupt: When this card is defeated…` / `…this scheme is
completed…`. That is tier 2b, and it is what makes
`rr:when-defeated-abilities.2.1` work — the card leaves play *after* its ability
resolves. `py_src/game/ability/ability_type.py` groups "When Defeated" with Boost
and When Revealed at tier 3, one tier too late, so a villain's dying ability
would resolve after it had already left play.

**A status card's forced interrupt is its own tier.** `rr:ability.step.2.a`, and
it is what makes Stun, Confuse and Tough beat whatever else wants the same
window. Collapsing 2a into 2b lets an ordinary forced interrupt resolve first and
cancel the attack the status card was there to change.

## An occurrence, not a moment

`Occurrence` is an object because two rules are about an occurrence's *identity*
and cannot be written without one.

`rr:triggering-condition.1` — each interrupt and response may be triggered once
per occurrence of its triggering condition. Keyed on the card in play, not the
printed face, because `rr:triggering-condition.1.1` lets two copies each have a
turn.

`rr:triggering-condition.2` — a single game occurrence that creates several
triggering conditions, such as one attack that both damages a character and
defeats it, gets **a single interrupt window and a single response window**. An
engine opening a window per condition would let one interrupt fire twice against
what the rules call one moment.

## A tier is a decision, not a sort

`AbilityWindow.Tiers` returns groups rather than a sorted list, and that is the
point. `rr:forced.5` gives the first player the order of simultaneous forced
abilities *regardless of who controls the cards*, and
`rr:simultaneous-resolution` says the same of any two effects sharing a bold
trigger. A tier holding more than one ability is a question for a player.
Returning a sorted list would answer it by object id, which is not a rule.

`rr:forced.6` is the other half: each forced ability resolves as completely as
possible before the next one from the same condition may initiate. A tier is
walked one ability at a time with the board re-read between them, never gathered
up and applied together.

## Continuous effects

Tier 1 holds three kinds, and the rulebook puts them together three separate
times — `rr:ability.step.1` lists them, `rr:delayed-effect.1.1` gives delayed
effects "the same timing priority as constant effects", and
`rr:lasting-effects.2` says a lasting effect "is treated as if it was a constant
ability and has the same timing priority".

They are one registered list, walked whenever the game state changes.
`rr:modifiers` describes exactly that: *"The game constantly checks and (if
necessary) updates the count of any variable quantity that is being modified."*
`rr:lasting-effects.3` says the same of lasting effects — they "update whenever
the game state updates". So the loop is the rule, and `ContinuousEffects.Active`
is meant to be cheap and called often rather than cached onto the board.

Where the three kinds differ is only in **how an entry leaves**:

| Kind | How it ends |
|---|---|
| constant ability | its card leaves play — derived |
| lasting effect | its timing point is reached (`Expire`), or it is cancelled |
| delayed effect | it resolves, and its registration is disposed |

A constant ability is derived rather than deregistered because the rules make it
derivable: `rr:ability` says it "becomes active as soon as its card enters play
and remains active while the card is in play", so being in force is a function of
the board. A forgotten deregistration would be a ghost — an ally's +1 ATK still
counting from the discard pile, on a board that looks entirely normal.

A lasting effect cannot be derived that way and needs the explicit handle. The
event that created it is in the discard pile and the board no longer records that
it was ever played. `rr:lasting-effects.1`.

`rr:lasting-effects.4` is why `Active` takes the world every time instead of
resolving the affected cards at registration: *"If a card enters play after the
creation of a lasting effect, it is still affected by that lasting effect."* An
entry names a condition, and the condition is re-read.

### An entry is data

A lasting effect outlives the card that made it and has to survive a save, so an
entry has to be something that can be written down. Anything holding a delegate
could not be. What an entry *does* is decided by reading its `Kind` and `Amount`,
which is the price of a game that can be put down and picked up.

## What this does not do yet

- **Nothing expires.** `rr:villain-phase.step.6` — *End of Villain Phase and
  Round* — is not implemented. `VillainPhase.Run` covers steps 1 to 5, so the
  point at which effects lasting "until the end of the round" would end is never
  reached. `ContinuousEffects.Expire` exists and nothing calls it.
- **Nothing opens a window.** No card in `CoreSetAbilities` has an interrupt or
  a response, and the recorded milestone game never reaches one: its hero never
  leaves alter-ego form and declines every option. So every claim in
  `AbilityWindowTests` rests on its citation and on nothing else — which is
  what the citations are for.
- **The decision function cannot express a window.** `Decision(Affordance,
  Targets)` has no way to say "I am using an interrupt in the window before you
  place that threat". See MARVEL-179.
