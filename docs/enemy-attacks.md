# Enemy attacks, and cards waiting in windows

`src/Marvel.Rules/Play/Attack.cs`, `src/Marvel.Content/Cards/CoreSetAbilities.cs`.

The timing spine in [timing.md](timing.md) was built and cited before anything
used it, and until now nothing did: `CoreSetAbilities.Waiting` returned an empty
list and said so in as many words. This is what makes it load-bearing.

**Three ported cards now act through it**, and two of them wait in the same
window.

| card | ability | tier |
|---|---|---|
| Charge (01099) | **Forced Interrupt**: when Rhino attacks, the attack gains overkill; at the end of this attack, discard Charge | 2b |
| Spider-Man (01001a) | Spider-Sense — **Interrupt**: when the villain initiates an attack against you, draw 1 card | 2c |
| "I'm Tough" (01105) | **When Revealed** (unchanged) | 3 |

## Why they land in one window

`rr:attack-enemy-activation.5`: *"Interrupts that trigger 'when [enemy name]
attacks' have the same timing as interrupts that trigger 'when [the villain/an
enemy] initiates an attack.'"*

That rule exists to put these two together. So the window that matters is the
one around the attack **initiating** — before the boost card is even given — and
not around any of the attack's six steps. One occurrence, two abilities, and
`rr:forced.4` orders them: Charge resolves without anybody being asked, and only
then is Spider-Sense offered as a question a player may decline.

## The attack, step by step

`rr:attack-enemy-activation` numbers six, and they are six entries on the agenda
rather than six calls — because step 2 asks a player something, and a phase that
is a call has nowhere to stop.

```
Attack                 rr:activation.1              ← the window both cards wait in
  GiveBoostCard        rr:attack-enemy-activation.step.1
  DeclareDefender      rr:attack-enemy-activation.step.2   ← asks
  FlipBoostCards       rr:attack-enemy-activation.step.3
  DealAttackDamage     rr:attack-enemy-activation.step.4, .step.5
  EndAttack            rr:attack-enemy-activation.step.6
```

Steps 4 and 5 are one entry, not two: `rr:triggering-condition.2` makes
calculating the damage and dealing it one occurrence, and nothing can happen
between the amount being fixed and it landing.

**The boost card waits facedown on the enemy across step 2.**
`rr:boost-boost-icon` puts the flip *"after any defenders are declared if the
villain is attacking"* — so a defender is chosen without knowing what the boost
card is. Giving and flipping in one call would hand the player information the
rules withhold, which is why `BoostCardsDeck` is a real stop and not a
formality. It is also the one entry in `DeckTypes.FaceDownOnEntry` the recording
cannot vouch for: nothing recorded ever sits there, so it is cited from
`rr:attack-enemy-activation.step.1` — *"one **facedown** boost card"* — instead.

## An attack is state, because it spans a question

`World.Attack` carries the enemy, the attacked player, the target character and
the defender. **Player and character are two questions** and they come apart the
moment an ally defends: `rr:defend-defense.3.1` makes the ally the target
character and `.5` makes its controller the target player. An ability triggering
"when the villain attacks *you*" reads the player
(`rr:attack-enemy-activation.1.4`); the damage goes to the character.

One value rather than a stack, because `rr:activation.8` queues activations
rather than nesting them: one initiated during another *"resolves after the
current activation has finished resolving"*.

## Boost icons are a lasting effect

`rr:attack-enemy-activation.step.3.c` increases the enemy's ATK by one per boost
icon — **for that attack**. A modifier with a stated duration is a lasting
effect, so it goes on the same registered list as everything else continuously in
force, with `Duration.UntilEndOf(TimingPoints.EndOfAttack)`, and comes off by
itself at step 6.

That means `StateFields` now reads two sources for a modified value: a printed
`ATK+` on an attached card, and a continuous effect naming the field. The rules
do not rank them, and `rr:modifiers` describes the whole arrangement — *"the game
constantly checks and (if necessary) updates the count of any variable quantity
that is being modified."*

## What Charge actually is

The star on Charge is in its **ATK field**, not its boost field, and
`rr:star-icon.2` says what that means: a reminder *"to check that attachment's
text box whenever the attached enemy uses the value that field is modifying to
attack or scheme."* So it is an attachment ability, live while Charge is in play
and attached to the attacking enemy — not a "Boost" ability, which is what a star
in the boost field would make it (`rr:star-icon.6`).

Both halves of what it does are bounded by the attack and neither happens when
it resolves:

- the overkill it grants is a **lasting effect** whose duration is
  `rr:lasting-effects`' own example, "until the end of this attack";
- discarding itself is a **delayed effect** waiting on a future condition
  (`rr:delayed-effect.1`).

So resolving Charge emits no event at all. Nothing on the board changes at that
moment; what a client sees is the attack landing differently and Charge going to
the discard when the attack ends. `DelayedEffects` is what resolves the second
half — a `Kind` string it switches on rather than a closure the effect carries,
because a delayed effect has to survive a save.

## Damage

`Card.Damage`, and **not** a token pool. The digest records a character's
remaining `health` and no damage key at all, so damage is what is subtracted
from printed hit points rather than something counted beside them. On every
recorded board it is zero everywhere, which is exactly why the recording cannot
tell a subtraction from a printed constant.

## What is named rather than skipped

Each of these throws. An attack that quietly skipped one would produce a board
that is plausible and wrong.

| | Cited |
|---|---|
| an ally defending | `rr:defend-defense.3` |
| a player defending an attack aimed at somebody else | `rr:defend-defense.5` |
| the target being defeated by the damage | `rr:damage.1` |
| a minion attacking | `rr:activation.2` |
| drawing from an empty deck | `rr:player-deck` |

**Overkill is granted and never applied.** `rr:overkill.1` only does anything
when an attack defeats an ally or a minion, and defeat is on the list above. The
keyword is registered, expires correctly, and carries no damage anywhere yet.

**A hero in hero form registers no digest fields.** `StateFields.Registered` has
an entry for `AlterEgo` and none for `Hero`, because the recorded game never
flips and there was nothing to measure. That was invisible while nothing could
attack a hero; now a hero can take damage and the digest will not show it. It is
a measurement gap, not a guess to be filled in — see
[state-digest-v2.md](state-digest-v2.md).

## Reproducing

```bash
# the real Rhino board: two ported cards in one window
dotnet test tests/Marvel.Content.Tests --filter CardsInWindowsTests

# the six steps, separated from each other on synthetic boards
dotnet test tests/Marvel.Rules.Tests --filter AttackTests
```

Every claim here rests on a citation and a hand-built board, because the
recording cannot reach any of it: its sampling policy declines every decision,
so its hero never leaves alter-ego form, and `rr:activation.1` makes a villain
facing an alter-ego scheme rather than attack.
