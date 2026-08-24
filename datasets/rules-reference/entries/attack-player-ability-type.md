---
id: "rr:attack-player-ability-type"
title: "ATTACK (PLAYER ABILITY TYPE)"
document: "Rules Reference"
version: "1.8"
page: 10
hash: "sha256:ba7ff75c1f1f34f0293b3cb27d5a27a87a540b22558539e3715c33fb3746e6ad"
steps: 3
see_also: ["rr:ally", "rr:basic-power", "rr:damage", "rr:defend-defense", "rr:enemy", "rr:identity", "rr:labeled-ability", "rr:minion", "rr:modifiers", "rr:retaliate-x", "rr:target", "rr:villain-villain-deck"]
---

# ATTACK (PLAYER ABILITY TYPE)

Some game effects and card abilities reference an attack. There are a few different ways an attack can occur:

<a id="attack-player-ability-type-step-7"></a>
7. Forced abilities *(such as the retaliate keyword)* with the following triggers (in any order): “after [character] attacks [and damages/defeats] [an enemy/a minion]...” “after [character] is attacked...”

<a id="attack-player-ability-type-step-8"></a>
8. Non-forced abilities with the triggers listed above.

<a id="attack-player-ability-type-step-9"></a>
9. Consequential damage (for allies).

<a id="attack-player-ability-type-1"></a>
1. A hero or ally can use their basic attack power to attack an enemy. A character must exhaust to use this power. This deals damage equal to the character’s ATK value to the enemy.
    <a id="attack-player-ability-type-1-1"></a>
    - A character can only initiate a basic attack if there is an enemy that can be attacked by that character or if that character is stunned.
    <a id="attack-player-ability-type-1-2"></a>
    - An ability that allows a hero or ally to “make a basic attack without exhausting” can allow an exhausted character to make a basic attack.

<a id="attack-player-ability-type-2"></a>
2. If a triggered ability is labeled as an attack—such as “**Hero Action** *(attack)*”—resolving that ability is considered to attack the specified target. Unless specified by the ability’s text, a hero does not exhaust when using such an ability.
    <a id="attack-player-ability-type-2-1"></a>
    - An ability labeled as an attack is considered a single attack, even if that attack deals multiple instances of damage.
    <a id="attack-player-ability-type-2-2"></a>
    - When an attack ability has its damage increased by another ability, each instance of damage in that attack ability that does not use the word “additional” is increased by the specified amount.

<a id="attack-player-ability-type-3"></a>
3. If an ability says “Make the following X attacks in order,” followed by two or more instances of damage, each of those instances is considered a separate attack.
    <a id="attack-player-ability-type-3-1"></a>
    - An ability that increases the damage of an attack only increases the damage of one of that ability’s attacks, though such an ability can be triggered separately for each attack.

<a id="attack-player-ability-type-4"></a>
4. Hero and ally attacks can target any enemy, unless a card ability *(such as guard)* is preventing that enemy from being attacked.

<a id="attack-player-ability-type-5"></a>
5. When an attack targets multiple enemies, the attacking character is considered to have attacked each of those enemies.
    <a id="attack-player-ability-type-5-1"></a>
    - Each attacked enemy with the retaliate X keyword that is still in play after the attack resolves deals its retaliate damage to the attacking character.

<a id="attack-player-ability-type-6"></a>
6. The order of resolution for abilities triggered by the resolution of an attack is as follows:

**See also:** [Ally](ally.md), [Basic Power](basic-power.md), [Damage](damage.md), [Defend](defend-defense.md), [Enemy](enemy.md), [Identity](identity.md), [Labeled Ability](labeled-ability.md), [Minion](minion.md), [Modifiers](modifiers.md), [Retaliate X](retaliate-x.md), [Target](target.md), [Villain](villain-villain-deck.md)
