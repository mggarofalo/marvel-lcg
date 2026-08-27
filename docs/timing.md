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
resolves. Grouping them with Boost and When Revealed at tier 3 instead is one
tier too late: a villain's dying ability would resolve after it had already left
play.

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

Where the three kinds differ is only in **how an entry leaves**, and that is
stated by the effect itself.

## A duration is a condition, not a timing point

`rr:lasting-effects` describes a duration as what the card says — *"for a
specified duration (such as 'until the end of the phase' or 'until the end of
this attack')"*. Timing points are its examples, not its definition, and
`rr:delayed-effect.1` names both shapes in one sentence: a delayed effect
resolves after its *"specified timing point **or future condition** occurs or
becomes true"*.

So `Duration` carries three nullable bounds:

| | Means | Cited |
|---|---|---|
| `Until` | a timing point — "until the end of the round" | `rr:lasting-effects.5` |
| `OnCondition` | a future condition — "the next time an enemy attacks you" | `rr:delayed-effect.1` |
| `Uses` | how many applications remain — "the next card you play" | |

**An effect ends at whichever comes first.** "Reduce the cost of the next ally
you play this phase by 1" carries a timing point *and* a use count: play the ally
and it is spent, play nothing and the phase ending takes it. Modelling only one
bound leaves a discount available next round, or spends one that was never used.
That is why these are three bounds on one record rather than three kinds of
effect.

`Uses` is also the count on a condition — "the next 2 times X happens" is
`OnCondition` with `Uses` of 2.

`Duration.WhileInPlay` is all three absent. A constant ability states no duration
of its own; that its card must stay in play is the general rule from
`rr:ability`, not something the card says.

| Kind | How it ends |
|---|---|
| constant ability | its card leaves play — **derived** |
| lasting effect | `Expire` at its timing point, or `Use` spends its last application |
| delayed effect | `Occur` when its condition happens, or its registration is disposed |

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

## Ending a phase

The rules state this twice in the same shape, so `PhaseEnd` implements it once.

| | |
|---|---|
| `rr:villain-phase.step.6` | *End of Villain Phase and Round.* (a) effects bound to the phase or the round end; (b) resolve "when/after the phase ends" and "when/after the round ends" effects |
| `rr:end-of-player-phase.step.4`, `.step.5` | the same two steps for the player phase |

**Ending a phase is an occurrence, so it has an interrupt window before it.**
That is not read into the rules — `rr:temporary.1` states it outright, that the
temporary keyword *"is equivalent to the following triggered ability: **Forced
Interrupt:** When the round ends, discard this card from play"*. A forced
interrupt resolves before its triggering condition (`rr:interrupt.3`), so a
temporary card is discarded **before** step 6a expires anything. The full order:

```
interrupt window   →   6a expire   →   delayed effects come due   →   response window
```

Delayed effects sit in the middle because `rr:delayed-effect.1` puts them
"before responses to that point or condition may be used".

The villain phase's ending is **one occurrence carrying two conditions**, the
phase ending and the round ending. `rr:triggering-condition.2` is why that is one
interrupt window and one response window rather than two of each — an ability
answering "when the round ends" gets a single turn even though both conditions
became true at once.

The player phase ends effects bound to `EndOfPlayerPhase` and **not** those bound
to `EndOfRound`, which outlive it. Expiring both would end a lasting effect half
a round early on a board that looks entirely normal.

## The window stack

Where the game is, when it is part-way through resolving something, is a value
on `World` — `World.Windows`, a stack of open windows.

A stack because windows nest: an interrupt that plays a card is itself an
occurrence with windows of its own, and the outer window is still open
underneath. `rr:initiating-abilities.3` is why the inner sequence outlives its
source — it "does not stop from completing if that card leaves play during this
sequence".

Data, and on the board, because the alternative is to suspend the engine
mid-call and resume it. A suspended iterator or a blocked thread cannot be
written to a save, cannot be diffed against a recorded step, and cannot tell a
client that the game is two windows deep.

### Offering a window round the table

Three rules, and together they are the whole loop:

- `rr:first-player.4` and `.5` — "the first player has the first opportunity to
  use an interrupt / a response at each appropriate game moment". Not the active
  player, and not whoever the occurrence is happening to.
- `rr:in-player-order` and `.2` — then clockwise, and "next player" always means
  the next clockwise player.
- `rr:interrupt.5` and `rr:response.4` — the window closes once **all** players
  decline any **further** abilities.

Two consequences worth stating, because both are easy to get wrong and neither
shows up in a one-player game:

**A window is not one pass round the table.** `rr:in-player-order.1`: "If a
sequence performed in player order does not conclude after each player has
performed their part of the sequence once, the sequence of opportunities
continues in a clockwise manner until it is complete." A window that closed
after one lap would silently refuse the second interrupt of a player who had
two.

**Using an ability gives everyone another opportunity.** The word doing the work
is "(further)" — a player who passed on an untouched board may have something to
say now that it has changed. So the count of consecutive declines resets, while
the opportunity itself carries on clockwise rather than restarting at the first
player.

`Close` exists for `rr:interrupt.4`: an interrupt that cancels or replaces the
imminent triggering condition ends the window, because there is nothing left to
interrupt.

## What a player is asked

`Question` has one member per kind of question the Rules Reference describes,
and it is **not** a timing. When a question is asked is `TimingPriority`, which
a prompt carries separately. An interrupt and a response are the same question
in two tiers, not two questions.

| | Cited |
|---|---|
| `TurnOption` | `rr:player-turn` |
| `Opportunity` | `rr:first-player.4`, `.5` |
| `Element` | `rr:choose-game-element` |
| `Option` | `rr:choose-option` |
| `Order` | `rr:first-player.3`, `rr:forced.5`, `rr:simultaneous-resolution`, `rr:each-player.1`, `rr:activation.8.1` |
| `Payment` | `rr:initiating-abilities.step.5`, `rr:resource-ability.1` |
| `Discard` | `rr:end-of-player-phase.step.1` |
| `Defender` | `rr:attack-enemy-activation.step.2` |

The four members this replaced were a census of what one sampled corpus happened
to contain, which is a sample rather than a domain — and they flattened the two
questions the rules keep apart.

The recording spells a prompt's kind with the name of a member of the Python
engine's `TimingPriority`, four of whose twelve members name nothing in the
rulebook. That is a corpus spelling, so the translation lives at the corpus
boundary — `PlayerPhaseTests.RecordedKind` — rather than in the engine's
vocabulary.

## Working a window

`rr:ability` puts an interrupt window before every occurrence and a response
window after it, so that is the shape of everything the engine does — placing
threat, dealing damage, revealing a card, ending a phase. `Sequence.Work` writes
it once, because a step that forgot its windows would look exactly like a step
that had none to open.

**Almost every one of those windows asks nobody anything**, and that is what
makes wrapping every occurrence cheap enough to be the default. `Offering` keeps
three cases apart:

| | |
|---|---|
| **nothing eligible for a player** | skipped, no prompt — they were never asked, because there was nothing to ask about |
| **a forced ability** | resolved, and the player is *told*: what reaches the client is an event, not a question |
| **an optional ability** | offered, always |

The third is worth being precise about. An interrupt window holding exactly one
ability is still a real choice, because `rr:ability.11` makes declining the other
answer — so the prompt is cancellable. That is a different thing from a question
with one possible answer, which should never be asked at all.

Two forced abilities at one moment are the exception that *is* a question, and a
different one: `rr:forced.5` gives the first player the order, so the prompt is a
`Question.Order` and it is **not** cancellable — they all resolve either way.

Between two forced abilities the board is re-read, never applied from a stale
list: `rr:forced.6`, "each forced ability must resolve as completely as possible
before the next forced ability being triggered by the same triggering condition
may initiate."

## A phase is a list, not a call

`rr:ability` puts a window before and after every occurrence, and any of those
windows may hold an ability somebody has to be asked about. A phase that is a
method call has nowhere to stop, so a phase is not a method call.

`World.Agenda` is what the game still has to do: a list of steps, each part-way
through three parts — `Interrupts`, `Apply`, `Responses`. `Sequence.Work` walks
it until something needs an answer and returns; the next answer picks it up
exactly where it was. Nothing is on a call stack, so all of it survives a save.

It also makes `rr:villain-phase`'s six steps **visible**. They used to be the
order of six method calls, which a reader has to reconstruct:

```
PlaceThreat            rr:villain-phase.step.1
EnemiesActivate        rr:villain-phase.step.2   (a heading)
  Attack | Scheme × players     rr:activation.1
    ... the attack's own six    rr:attack-enemy-activation
DealEncounterCards     rr:villain-phase.step.3
  RevealEncounterCard × dealt   rr:villain-phase.step.4
PassFirstPlayerToken   rr:villain-phase.step.5
EndVillainPhase        rr:villain-phase.step.6
```

Steps 2, 3 and the attack **schedule** what happens under them rather than doing
it, so the per-player activations, the per-card reveals and the attack's own six
steps are occurrences with windows of their own. A heading is not an occurrence
and opens no windows.

An attack occurrence carries explicit `Actor` and `Target` roles beside its
`Player` and conditions. `rr:attack-enemy-activation.1.4` turns "when the
villain attacks **you**" into a question about the attacked player, while
`rr:star-icon.2` makes Charge ask which enemy is the actor. A response such as
Shocker asks whether it was the target. Without distinct roles a window can say
that an attack happened but not who acted on whom.

The occurrence is made **once, when its window begins**, and not on every read.
Most steps can create it when scheduled; an attack step waits until it begins
because declaring a defender may change its target first. The occurrence then
snapshots both roles and their kind, owner, controller, and friendly/enemy
classification. `rr:triggering-condition.1` lets each ability trigger once per
occurrence, and the occurrence is what remembers which have — a fresh one per
read would forget across the answer that suspended the step.

`Agenda.Then` puts a scheduled step after the current one's *response* window,
not before it: a step that schedules another has not itself finished happening.
`rr:villain-phase.step.3` deals the cards and `.step.4` reveals them, in that
order and not interleaved — and the recorded discard pile is what catches it if
they are.

Threat placement carries its assignment on the occurrence: scheme, source,
assigned and remaining amount, player and cause. `rr:prevent.2` changes the
remaining amount in the interrupt window before any token is placed. The
`WhenThreatPlaced` condition joins the occurrence only when a positive amount
actually lands; a replacement removes the step and both of its remaining
windows under `rr:replacement-effect.1`. The villain phase and an enemy scheme
derive and freeze their assignments when their interrupt window begins, after
the preceding steps have updated the board.

The villain winning abandons the rest — `rr:main-scheme-main-scheme-deck.2.1`,
the villain wins the game, and the encounter cards are not dealt.

## A step may ask, as well as a window

Not every question in a phase comes from a window. Declaring a defender is a
step of the attack with a name of its own — `rr:attack-enemy-activation.step.2` —
and nobody is using an ability when it is asked. So `VillainPhase.Take` returns a
`Prompt?`, the agenda stays on that step's `Apply`, and the answer is what makes
the step happen.

Which of the two answered a prompt is read off the board rather than off the
prompt: a window open means the window absorbs the answer, no window open means
the step does. A window that has finished polling has already closed itself, so
the two never overlap.

Being asked follows the same rule either way — **only where there is something
to ask**. A player with no ready character cannot defend (`rr:defend-defense.2`
and `.3` both require exhausting one), so the step passes in silence exactly as
an empty window does.

## What this does not do yet

- **Steps 1 to 3 of `rr:end-of-player-phase`** — discard down to hand size, draw
  up to it, ready every card — are not implemented. `rr:player-phase.1` puts
  them before the expiry point, and the recorded game cannot say when they
  happen: its hand is full at every step and its one player readies nothing.
- **One delayed effect kind resolves; the rest throw.** `DiscardFromPlay` is
  written because Charge needs it. The vocabulary beyond that is
  `docs/card-dsl.md`'s business.
- **Three cards register continuous effects, and no more.** The list is real —
  Charge grants overkill, boost icons raise an enemy's ATK — but three authored
  cards is the whole card pool. Growing it is adding rows to
  `datasets/abilities/abilities.json` and, where a row names a node nothing has,
  one case in the interpreter.

What this **does** do now is [enemy-attacks.md](enemy-attacks.md): two authored
cards waiting in one window on the real Rhino board, one forced and one
optional, and the lasting and delayed effects the forced one creates. Neither is
code — see [card-dsl.md](card-dsl.md).
