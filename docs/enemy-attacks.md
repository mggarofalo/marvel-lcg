# Enemy attacks, and cards waiting in windows

`src/Marvel.Rules/Play/Attack.cs`, `src/Marvel.Cards/`,
`datasets/abilities/abilities.json`.

The timing spine in [timing.md](timing.md) was built and cited before anything
used it, and until now nothing did: no card the engine had could wait in a
window. This is what makes it load-bearing.

**Three authored cards now act through it**, and two of them wait in the same
window. None of them is code: each is a row in
`datasets/abilities/abilities.json`, run by the interpreter in
[card-dsl.md](card-dsl.md).

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
  CalculateAttackDamage rr:attack-enemy-activation.step.4
  DealAttackDamage     rr:attack-enemy-activation.step.5
  EndAttack            rr:attack-enemy-activation.step.6
```

Step 4 fixes the number on `World.Attack`; step 5 deals that saved amount.
The v1.8 procedure makes them separate occurrences and says step 5 deals "the
amount of damage calculated in the previous step." Effects between the two
therefore do not recalculate the attack.

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

The value begins during the initiation interrupt window, before the six steps
are scheduled. That is necessary for card text such as "when an enemy attacks,
declare [a character] the defender": `rr:attack-enemy-activation.5` puts that
instruction in the initiation window, and the chosen defender must survive the
window into step 2. A status card still has priority; if stunned replaces the
attack, the prepared attack and activation are cleared before any authored
interrupt can observe them.

Card-declared defense is distinct from the ordinary step-2 choice. A declared
hero makes a basic defense and applies DEF (`rr:defend-defense.2.1`); a declared
ally does not (`.3.2`). Neither declaration itself exhausts the character,
because `.2.2` and `.3.3` explicitly allow card text to declare an already
exhausted character without exhausting it.

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

Data, first of all — this, in the ability dataset:

```json
{ "name": "Charge",
  "trigger": { "event": "WhenAttackInitiated",
               "timing": "ForcedInterrupt", "actor": "attachedTo" },
  "effect": { "seq": [
    { "grantUntil": { "keyword": "overkill", "card": "trigger.actor",
                      "until": "EndOfAttack" } },
    { "delayUntil": { "condition": "WhenAttackEnds",
                      "effect": { "discard": "this" } } } ] } }
```

`WhenAttackInitiated` is source-neutral. The occurrence names the attacking
card as `actor`, the attacked character as `target`, and the attacked seat as
`player`. Spider-Sense matches a villain actor and its controller as the
attacked player. Charge and Webbed Up match the card they are attached to as
the actor. A character attacking an enemy uses the same event: Shocker matches
itself as the target and acts on `trigger.actor`.

The occurrence also captures each participant's kind, owner, controller, and
friendly or enemy classification. It captures them when the interrupt window
opens. Moving a card or changing control during that window does not change the
attack that opened it.


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

Overkill snapshots an ally's controller before defeat moves the ally to its
owner's discard pile. The excess goes to that controller's identity, as
`rr:overkill.1` requires; owner and controller are deliberately different on a
card played under another player's control.

## Reproducing

```bash
# the real Rhino board: two authored cards in one window
dotnet test tests/Marvel.Content.Tests --filter CardsInWindowsTests

# the six steps, separated from each other on synthetic boards
dotnet test tests/Marvel.Rules.Tests --filter AttackTests

# the dataset held against the engine it is written for
dotnet test tests/Marvel.Content.Tests --filter AbilityDataTests
```

Every claim here rests on a citation and a hand-built board, because the
recording cannot reach any of it: its sampling policy declines every decision,
so its hero never leaves alter-ego form, and `rr:activation.1` makes a villain
facing an alter-ego scheme rather than attack.

## The other activation is steps too

`rr:activation` gives an enemy two ways to activate and the rules give each
three or six numbered steps. The attack has been steps on the agenda from the
start. The **scheme** was one call that did all three of
`rr:scheme-enemy-activation` in a row, which is fine until step 2 stops to ask:

> 2. Resolve each of the scheming enemy's boost cards … b. Resolve any
>    "**Boost**" abilities.
> 3. Place threat on the main scheme equal to the scheming enemy's **modified**
>    SCH value.

A boost ability that offers the player a choice suspends. Resolved inline, the
threat went onto the scheme while the question was still on the table, and
whatever the player chose arrived after the number it was meant to change. So
step 3 is `Steps.SchemeThreat`, exactly as `CalculateAttackDamage` is step 4 of
the attack — and the boost icons became a registered modifier rather than a
local number, because a number cannot cross a step boundary.

**And a scheme now has an ending.** `Steps.SchemeThreat` carries
`WhenSchemeEnds`, the parallel of `AttackEnds`. The attack has always kept its
two moments apart — `rr:attack-enemy-activation.5` puts "when [enemy name]
attacks" at the *initiation*, before any step, and `.step.6.a` is where the
abilities that ask what the attack **did** live. A scheme has the same two
moments and had one name for both.

That is only visible once the threat is placed in between. Prelate Armor's
"**Forced Response:** After Unus schemes, give him a tough status card" had been
resolving at the *start* of the activation, and nothing showed it: a tough card
is a tough card whichever side of the scheme it lands on. The event order is
what shows it, which is what its test asserts.
