---
id: "rr:attack-enemy-activation"
title: "ATTACK (ENEMY ACTIVATION)"
document: "Rules Reference"
version: "1.8"
page: 8
hash: "sha256:1336fdbd3362dce8d8907e27e94444e5e4753d34fba8619d7ceae082f417e265"
steps: 6
see_also: ["rr:activation", "rr:ally", "rr:attacks-against-allies", "rr:boost-boost-icon", "rr:damage", "rr:defend-defense", "rr:enemy", "rr:identity", "rr:minion", "rr:modifiers", "rr:retaliate-x", "rr:target", "rr:villain-villain-deck", "rr:villainous"]
---

# ATTACK (ENEMY ACTIVATION)

An attack is a type of enemy activation. When an enemy initiates an attack, it targets a specific player, then resolves that attack against that player.

<a id="attack-enemy-activation-step-1"></a>
1. **Give boost card:** If a villain, or a minion with the villainous keyword, is attacking, give it one facedown boost card from the encounter deck. *(If a minion without the villainous keyword is attacking, skip this step.)*

<a id="attack-enemy-activation-step-2"></a>
2. **Declare defender:** If a player wishes to defend, that player exhausts a hero or ally as the defender. The defending character becomes the target character for the attack. If a player other than the target player defends, the defending player becomes the target player for the attack.

<a id="attack-enemy-activation-step-3"></a>
3. **Flip boost cards:** Flip and resolve each of the attacking enemy’s boost cards, one at a time and in the order in which they were dealt, by doing the following:
    <a id="attack-enemy-activation-step-3-a"></a>
    a. Flip the boost card faceup.
    <a id="attack-enemy-activation-step-3-b"></a>
    b. Resolve any “**Boost**” abilities, indicated by the star icon in the boost area. *(All other abilities on the boost card are ignored.)*
    <a id="attack-enemy-activation-step-3-c"></a>
    c. Increase the attacking enemy’s ATK value by one for each boost icon on the card.
    <a id="attack-enemy-activation-step-3-d"></a>
    d. Discard the boost card.
    <a id="attack-enemy-activation-step-3-e"></a>
    e. If the enemy has any boost cards remaining, repeat these steps with the next boost card.

<a id="attack-enemy-activation-step-4"></a>
4. **Calculate damage:** Determine how much damage will be dealt by the attack. The base damage is equal to the attacking enemy’s ATK, including modifiers from abilities in play and boost icons resolved for the attack. If a hero has been declared the defender of the attack, reduce the amount of damage dealt by that hero’s DEF value.

<a id="attack-enemy-activation-step-5"></a>
5. **Deal damage:** Deal the amount of damage calculated in the previous step, based on the following:

<a id="attack-enemy-activation-step-6"></a>
6. The attack finishes resolving and the following types of abilities trigger in order:
    <a id="attack-enemy-activation-step-6-a"></a>
    a. Forced abilities *(such as the retaliate keyword)* with the following triggers (in any order): “after [character] attacks [and damages/defeats] [you/an ally]...” “after [character] is attacked...” “after [character] defends [and takes no damage]...” “after [character] [takes/deals] damage...”
    <a id="attack-enemy-activation-step-6-b"></a>
    b. Non-forced abilities with the triggers listed above. These rules also apply to enemy attacks:

<a id="attack-enemy-activation-1"></a>
1. Enemy attacks are always initiated against both a player and a character.
    <a id="attack-enemy-activation-1-1"></a>
    - Normally the attacked character is the player’s hero, but abilities can instead cause an enemy to attack a player’s alter-ego or an ally that player controls. In all of these cases, the player is still considered attacked.
    <a id="attack-enemy-activation-1-2"></a>
    - If a character other than the attacked character defends the attack, that character becomes the new target of that attack.
    <a id="attack-enemy-activation-1-3"></a>
    - If a player other than the attacked player defends the attack with a character they control, that player becomes the new target of that attack.
    <a id="attack-enemy-activation-1-4"></a>
    - Abilities that trigger “When/After [enemy] attacks you” are resolved when/after a player is attacked, regardless of which character they control was attacked. *(For example, Ultron I reads: “***Forced Response***: After Ultron attacks you, choose to either place 1 threat on the main scheme or put the top card of your deck into play facedown, engaged with you as a drone minion.” This effect resolves against the attacked player regardless of if that player used an ally to defend the attack.)* To resolve an enemy attack, follow these steps:

<a id="attack-enemy-activation-2"></a>
2. If a **hero was declared the defender** of the attack, the damage from the attack is dealt to that hero.
    <a id="attack-enemy-activation-2-1"></a>
    - The defending hero is considered to have been attacked.
    <a id="attack-enemy-activation-2-2"></a>
    - If a hero with a tough status makes a basic defense, the damage is first reduced by that hero’s DEF value. If the damage is reduced to 0, the hero keeps their tough status.

<a id="attack-enemy-activation-3"></a>
3. If an **ally was declared the defender** of the attack, all damage from the attack is dealt to the ally. *(If the ally is defeated by the attack, additional damage does not carry over to the identity.)*
    <a id="attack-enemy-activation-3-1"></a>
    - The defending ally is considered to have been attacked.
    <a id="attack-enemy-activation-3-2"></a>
    - If the defending ally leaves play prior to damage from the attack being dealt, the attack is considered to have no character defending and the identity of that ally’s controller becomes the target of the attack.

<a id="attack-enemy-activation-4"></a>
4. If **no character was declared the defender** of the attack, the attack is considered undefended. All damage from the attack is dealt to the character targeted by the attack.
    <a id="attack-enemy-activation-4-1"></a>
    - The targeted character is considered to have been attacked.

<a id="attack-enemy-activation-5"></a>
5. Interrupts that trigger “when [enemy name] attacks” have the same timing as interrupts that trigger “when [the villain/an enemy] initiates an attack.”

<a id="attack-enemy-activation-6"></a>
6. If an enemy attack ends before damage is dealt, abilities that trigger after an attack or after a character defends an attack resolve as normal.

<a id="attack-enemy-activation-7"></a>
7. Abilities that trigger when an enemy “attacks and damages” or “attacks and defeats” a character trigger only when that character is damaged/defeated by damage from the attack itself, and not by an ability that resolves during the attack.
    <a id="attack-enemy-activation-7-1"></a>
    - *For example, Sonic Converter reads: “***Forced Response***: After Klaw attacks and damages a character, stun that character.” If Klaw is given a boost ability that says “***Boost***: Deal 1 damage to each hero,” that boost ability does not trigger the ability on the Sonic Converter.*

**See also:** [Activation](activation.md), [Ally](ally.md), [Attacks Against Allies](attacks-against-allies.md), [Boost](boost-boost-icon.md), [Damage](damage.md), [Defend](defend-defense.md), [Enemy](enemy.md), [Identity](identity.md), [Minion](minion.md), [Modifiers](modifiers.md), [Retaliate X](retaliate-x.md), [Target](target.md), [Villain](villain-villain-deck.md), [Villainous](villainous.md)
