---
id: "rr:indirect-damage"
title: "INDIRECT DAMAGE"
document: "Rules Reference"
version: "1.8"
page: 24
hash: "sha256:d1c4c348ba5e3d63493529102ad1bd2293074703b8e6e4352bda6366649f8a3d"
see_also: ["rr:ally", "rr:attack-enemy-activation", "rr:damage", "rr:defeat", "rr:player"]
---

# INDIRECT DAMAGE

Some card abilities may deal “indirect damage.”

<a id="indirect-damage-1"></a>
1. Indirect damage dealt to a player can be divided as that player chooses among characters under their control.

<a id="indirect-damage-2"></a>
2. Indirect damage dealt to a group of players *(or among players)* can be divided as the group chooses among friendly characters in play.

<a id="indirect-damage-3"></a>
3. All indirect damage from a single source is first assigned and then resolved simultaneously.
    <a id="indirect-damage-3-1"></a>
    - While assigning indirect damage, a character cannot be assigned more indirect damage than would cause it to be defeated. This is assessed without accounting for interactions with other abilities.
    <a id="indirect-damage-3-2"></a>
    - A character with a tough status card can be assigned indirect damage up to its remaining hit points, and all damage assigned to it is prevented by its tough status card.

<a id="indirect-damage-4"></a>
4. Characters that cannot take damage cannot be assigned indirect damage.
    <a id="indirect-damage-4-1"></a>
    - If indirect damage dealt to a player cannot be assigned to any character that player controls, that damage is ignored.

<a id="indirect-damage-5"></a>
5. If an enemy’s attack deals indirect damage, the indirect damage is dealt during step four of the enemy activation *(after player’s have the opportunity to defend against the attack)*.
    <a id="indirect-damage-5-1"></a>
    - Only the defending character, or the attacked player’s identity if the attack was undefended, is considered to have been attacked, even if other characters were assigned some or all of the indirect damage.

<a id="indirect-damage-6"></a>
6. *For example, if you take 5 indirect damage, but you control an ally with 4 hit points remaining, you may assign 4 of that indirect damage to the ally, then assign the remaining 1 indirect damage to your identity.*

**See also:** [Ally](ally.md), [Attack (Enemy Activation)](attack-enemy-activation.md), [Damage](damage.md), [Defeat](defeat.md), [Player](player.md)
