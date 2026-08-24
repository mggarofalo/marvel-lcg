---
id: "rr:defend-defense"
title: "DEFEND, DEFENSE"
document: "Rules Reference"
version: "1.8"
page: 15
hash: "sha256:4944e775e00ff5f32948e458d70057beb2d0a0139ffade1d3e25b7b8c5140409"
see_also: ["rr:ability", "rr:ally", "rr:attack-enemy-activation", "rr:damage", "rr:friendly", "rr:identity", "rr:labeled-ability", "rr:player"]
---

# DEFEND, DEFENSE

During an enemy attack, a player may defend against that attack using cards they control.

<a id="defend-defense-1"></a>
1. Only one player at a time can defend against an enemy attack. While a player is defending, other players cannot defend against that same attack.

<a id="defend-defense-2"></a>
2. A hero can use their basic defense power to defend against an enemy attack. A hero must exhaust to use this power. The amount of damage dealt by the attack is reduced by the hero’s DEF value, and any remaining damage is dealt to that hero. While a hero is defending against an attack, other friendly characters cannot defend against that attack.
    <a id="defend-defense-2-1"></a>
    - When a card ability says to “declare [a hero] the defender” of an attack, that hero is considered to be making a basic defense.
    <a id="defend-defense-2-2"></a>
    - A card ability that allows a hero to be declared as a defender without exhausting can be used on an exhausted hero.

<a id="defend-defense-3"></a>
3. An ally can exhaust to defend against an enemy attack. Damage from the attack is dealt to that ally. While an ally is defending against an attack, other friendly characters cannot defend against that attack.
    <a id="defend-defense-3-1"></a>
    - When an ally defends an attack, that ally becomes the target character for that attack, and its controller becomes the target player for that attack.
    <a id="defend-defense-3-2"></a>
    - When a card ability says to “declare [an ally] the defender” of an attack, that ally becomes the defender of the attack.
    <a id="defend-defense-3-3"></a>
    - A card ability that allows an ally to be declared as a defender without exhausting can be used on an exhausted ally.

<a id="defend-defense-4"></a>
4. When a player initiates a triggered ability labeled as a defense—such as “**Hero Interrupt** *(defense)*”— during an enemy attack, that player’s identity becomes the defender and is considered to have defended the attack if there is not already a defender.
    <a id="defend-defense-4-1"></a>
    - The player’s identity is considered to be the defender as soon as the defense-labeled ability begins resolving.
    <a id="defend-defense-4-2"></a>
    - Abilities that trigger “when your hero defends against an attack” can be triggered when resolving a defense-labeled ability.
    <a id="defend-defense-4-3"></a>
    - Resolving a defense-labeled ability is not a basic defense and does not cause a hero to reduce the amount of damage dealt by that hero’s DEF. That hero can still be declared the defender of the attack during the “Declare Defender” step or by another card ability.
    <a id="defend-defense-4-4"></a>
    - Unless specified by the ability’s text, a hero does not exhaust when using a defense-labeled ability.
    <a id="defend-defense-4-5"></a>
    - The defending player may resolve any number of defense abilities during an enemy attack *(as long as the triggering conditions of those abilities are met)*.
    <a id="defend-defense-4-6"></a>
    - Once a player resolves a defense-labeled ability during an enemy attack, other players cannot resolve defense-labeled abilities for that same attack.
    <a id="defend-defense-4-7"></a>
    - Defense-labeled abilities can be played during an attack by a player whose ally is defending that attack. In that case, the player’s identity does **not** become the defender.
    <a id="defend-defense-4-8"></a>
    - A player can trigger abilities labeled as a defense outside of an attack if the ability’s triggering condition is met. When triggered this way, the player’s identity is not considered to have defended an attack.

<a id="defend-defense-5"></a>
5. If a player defends against an enemy attack that targets a different player *(either by defending with a character they control or by resolving a defense ability)*, the defending player becomes the new target of that attack.
    <a id="defend-defense-5-1"></a>
    - Any triggered ability that refers to “you” refers to the player who was the target of the attack when that ability resolved. *(For example, the “you” in an ability that triggers “when [enemy] attacks you” refers to the player against whom the attack initiated, while the “you” in an ability that triggers “after [enemy] attacks you” refers to the player whose character defended the attack.)*
    <a id="defend-defense-5-2"></a>
    - Any constant or boost abilities that refer to “you” refer to the defending player.

<a id="defend-defense-6"></a>
6. If no character is used to defend against an enemy attack, that attack is considered undefended. Additionally, if a defending ally is defeated before damage from the attack is dealt *(such as through a “Boost” ability)*, the attack is considered undefended

<a id="defend-defense-7"></a>
7. Abilities that trigger after a character defends an attack resolve after that attack ends.
    <a id="defend-defense-7-1"></a>
    - If an effect causes a defended attack to end before fully resolving, the attack is still considered to have been defended.
    <a id="defend-defense-7-2"></a>
    - If an ability triggers after a character uses a basic power, that ability triggers after an attack in which a character made a basic defense resolves.

**See also:** [Ability](ability.md), [Ally](ally.md), [Attack (Enemy Activation)](attack-enemy-activation.md), [Damage](damage.md), [“Friendly”](friendly.md), [Identity](identity.md), [Labeled Ability](labeled-ability.md), [Player](player.md)
