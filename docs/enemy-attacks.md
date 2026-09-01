# Enemy attacks

`src/Marvel.Rules/Play/Attack.cs`, `src/Marvel.Rules/Play/Damage.cs`, and the
Core ability rows in `datasets/abilities/abilities.json` implement enemy
attacks. The attack is an agenda sequence because defense and ability windows
can require player decisions.

## Initiation window

`rr:attack-enemy-activation.5` gives interrupts triggered when an enemy attacks
the same timing as interrupts triggered when it initiates an attack. The engine
therefore opens one initiation occurrence before boost cards are dealt.

Forced interrupts resolve before optional interrupts. On the Core Rhino board,
Charge can grant overkill and schedule its own discard before Spider-Man's
Spider-Sense is offered. Both are data-driven abilities interpreted through the
card DSL rather than card-specific C#.

The occurrence records the enemy actor, attacked player, and target character.
Those roles are distinct: an ally may become the target while its controller
remains the attacked player. Actor and target facts are captured when the
window opens so control changes inside the window do not redefine the attack.

## Six attack steps

`rr:attack-enemy-activation` defines this order:

1. give the attacking enemy its facedown boost cards;
2. declare a defender, if any;
3. reveal boost cards, resolve Boost abilities, and count icons;
4. calculate and store the attack damage;
5. deal that stored amount to the target character; and
6. resolve the end of the attack.

The boost card remains facedown while the player chooses a defender. Steps 4
and 5 are separate occurrences: effects between them do not recalculate the
amount that step 4 fixed.

`World.Attack` persists the active enemy, attacked player, target, defender,
and calculated damage across prompts. Only one attack is active because an
activation initiated during another activation is queued to run afterwards.

## Defense

A ready hero or ally may make the normal step-2 defense. A hero making a basic
defense exhausts and applies DEF; an ally becomes the damage target without
applying a DEF value. Card text may declare a defender earlier, including an
already exhausted character where the text permits it.

Status replacement has the highest interrupt priority. If stunned replaces
the attack, the prepared attack and activation are cleared before ordinary card
interrupts observe them.

## Boosts and scoped effects

Each boost icon adds one to the attacking enemy's ATK for that attack. The
modifier is a lasting effect with `EndOfAttack` duration, so it survives any
prompt between reveal and damage and expires in step 6.

A star in an attachment's modified stat field tells the engine to inspect that
attachment when the enemy uses the stat. A star in a boost field instead marks
a Boost ability. The printed field and card location decide which rule applies.

Charge demonstrates both non-immediate effect shapes supported by the Core
ability data:

- a lasting effect grants overkill until the attack ends; and
- a delayed effect discards Charge when the attack ends.

## Damage and defeat

`Damage.Attack` applies defense, prevention, tough, piercing, and overkill in
rule order. `Card.Damage` stores sustained damage; remaining hit points are
derived from printed hit points minus that value.

When damage defeats a character, the same occurrence gains the defeat
condition and opens the corresponding windows before the card leaves play.
Overkill snapshots the defeated ally's controller or the attacked minion's
villain target before moving the defeated card.

Retaliate happens only after the attack resolves and only if the retaliating
character remains eligible. Ranged suppresses retaliate for that attack.

## Enemy schemes

Scheming uses the same activation framework. Boost cards and abilities resolve
before a separate step places the enemy's modified SCH threat. The scheme has
distinct initiation and ending conditions, so responses that refer to the
completed scheme resolve after the threat placement.

## Supported boundary

The Core villains, minions, attachments, statuses, and player responses are
executable. Later attack patterns helped validate the general timing and DSL
shapes, but they are not runtime content. See [scope.md](scope.md).
