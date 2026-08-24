---
id: "rr:target"
title: "TARGET"
document: "Rules Reference"
version: "1.8"
page: 42
hash: "sha256:b72c77af1805835ae4cec80cfc4b2f14478f45ad1cf969979b40c9b8cedc231a"
see_also: ["rr:ability", "rr:choose-game-element", "rr:cost", "rr:game-element", "rr:labeled-ability"]
---

# TARGET

If a game function or card ability is directed toward a game element *(such as an attack that deals damage to an enemy)*, that game element becomes the target of that function or ability for the duration of that function’s or ability’s resolution.

<a id="target-1"></a>
1. *Examples of targets include but are not limited to: “the villain,” “a minion,” “an enemy,” “a scheme,” “a hero,” “an ally,” “a character,” “a player,” “you,” “a card.”*

<a id="target-2"></a>
2. If an ability or game function requires one or more targets, that ability or game function can only be initiated if it has at least one valid target. *For example, an ability that says “deal 5 damage to a minion” cannot be initiated if there are no minions in play.*
    <a id="target-2-1"></a>
    - Basic powers are game functions that require a valid target.
    <a id="target-2-2"></a>
    - The phrase “choose a [game element]” indicates that one or more targets must be selected in order for an ability to initiate.
    <a id="target-2-3"></a>
    - Abilities that cause a player to draw one or more cards always have a valid target so long as that player has at least one card in their deck.

<a id="target-3"></a>
3. A target is valid for an ability or game function if any part of that ability can affect that target.
    <a id="target-3-1"></a>
    - *Examples of effects on a target include but are not limited to: dealing/healing damage, adding/ removing threat, giving/removing a status card, exhausting/readying the target, defeating/ discarding the target.*
    <a id="target-3-2"></a>
    - **Exception**: A character with an ATK, SCH, or THW of 0 can perform an activation or basic power using that value against a target that is otherwise valid for that activation or basic power. *(For example, a hero with a THW of 0 can perform a basic thwart against a scheme with threat on it.)*
    <a id="target-3-3"></a>
    - The cost of an ability or game function is not considered when determining if that ability or game function can affect a target.
    <a id="target-3-4"></a>
    - If an ability or game function has multiple effects on its target, the target is valid if at least one of those effects can affect the target.
    <a id="target-3-5"></a>
    - A target is not valid for an ability if that ability would cause the target to perform a game function that another ability says the target cannot perform. *For example, a character with an attachment that says “attached character cannot ready” is not a valid target for a card that readies a character*.
    <a id="target-3-6"></a>
    - Damage that is dealt but not taken *(for example, if the damage is prevented)* is considered to affect a target.
    <a id="target-3-7"></a>
    - A target that “cannot take damage” is not a valid target for an ability or game function whose only effect on that target is to deal it damage.
    <a id="target-3-8"></a>
    - A target that cannot be attacked is not a valid target for an attack-labeled ability.
    <a id="target-3-9"></a>
    - A target that cannot be thwarted is not a valid target for a thwart-labeled ability.

<a id="target-4"></a>
4. An ability or game function that targets multiple game elements of a specific type *(for example, “each enemy”)* can be initiated as along as at least one of those game elements is a valid target.
    <a id="target-4-1"></a>
    - That ability or game function does not resolve against any of those game elements that is not a valid target.
    <a id="target-4-2"></a>
    - *For example, the crisis (*[crisis]*) icon prevents threat from being removed from the main scheme. An ability that says “remove 1 threat from each scheme” can be used while there is a crisis icon in play if there is at least 1 scheme from which threat can be removed. In this case, no threat would be removed from the main scheme.*

<a id="target-5"></a>
5. An ability that refers to a future target *(i.e. “the next card you play”)* does not require a target to initiate.

<a id="target-6"></a>
6. An ability with a search effect requires only a searchable game area in order to initiate.

**See also:** [Ability](ability.md), [Choose (Game Element)](choose-game-element.md), [Cost](cost.md), [Game Element](game-element.md), [Labeled Ability](labeled-ability.md)
