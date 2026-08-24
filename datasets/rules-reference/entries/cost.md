---
id: "rr:cost"
title: "COST"
document: "Rules Reference"
version: "1.8"
page: 13
hash: "sha256:a1f3038ae0a861e6844e00ec58580ac437be7ddc769bcd3e20032e191dc56b14"
see_also: ["rr:ability", "rr:cost-arrow-icon", "rr:game-element", "rr:initiating-abilities", "rr:keywords"]
---

# COST

A card’s resource cost is the numerical value that must be paid to play the card. Some abilities have a cost described in the ability text that must be paid to use the ability.

<a id="cost-1"></a>
1. A cost arrow icon () in ability text distinguishes a cost from an effect, in a “pay cost resolve effect” format.
    <a id="cost-1-1"></a>
    - Text indicating the timing of an interrupt or response trigger that precedes a cost arrow is not considered part of the cost.

<a id="cost-2"></a>
2. A resource cost with the per-player icon ([per-player]) is multiplied by the number of players who **started** the scenario.
    <a id="cost-2-1"></a>
    - If a cost with the per-player icon is reduced, the total cost of the card is reduced, not the value that is multiplied by the number of players.

<a id="cost-3"></a>
3. To pay a resource cost, a player spends resources that they generate by discarding cards from their hand or by using “**Resource**” card abilities.
    <a id="cost-3-1"></a>
    - Resources generated to pay for an ability on a card are considered to have been paid for that card.

<a id="cost-4"></a>
4. While paying a cost, a player is permitted to generate resources beyond the specified cost.
    <a id="cost-4-1"></a>
    - Resources generated beyond the specified cost are considered to have been overpaid for that cost and were not paid for that cost.
    <a id="cost-4-2"></a>
    - Any resources that are generated beyond the specified cost are lost after paying that cost.

<a id="cost-5"></a>
5. If multiple costs for a single card or ability require payment, those costs must be paid simultaneously.
    <a id="cost-5-1"></a>
    - A player generating resources for those costs chooses how to divide those resources between those costs. *(For example, a player paying costs for an event with a resource cost of 1 and an ability that reads “***Hero Action***: Spend X* [energy] *resouces...” can spend a resource card that generates* [energy] [energy] *resources and use one of those icons to pay for the card’s resource cost and the other to pay for the cost before the arrow.)*

<a id="cost-6"></a>
6. An ability’s cost cannot be paid if that ability’s effect requires one or more targets and there is not at least one valid target.

<a id="cost-7"></a>
7. While a player is paying a cost, that player must pay costs with cards and/or game elements they control.
    <a id="cost-7-1"></a>
    - If a cost uses the word “choose,” the player can choose targets they do not control.
    <a id="cost-7-2"></a>
    - If a cost targets a “friendly” card, the player can target cards they do not control.

<a id="cost-8"></a>
8. If a cost requires a game element that is not in play, the player paying the cost may only use game elements that are in their own out-of-play areas.

<a id="cost-9"></a>
9. A cost requiring “any number” or “up to” some number of game elements requires a minimum of one such game element.

<a id="cost-10"></a>
10. Some card abilities may reference an “additional cost.” A player must pay all additional costs simultaneously with the cost that is being added to, even if multiple cards or abilities are adding separate additional costs. A player cannot pay the original cost or any of the additional costs individually; if they cannot pay for all of the costs at once, then they do not pay any of the costs and the effect associated with the costs does not occur.

<a id="cost-11"></a>
11. If dealing damage is a cost, that cost is considered paid even if some or all of that damage is prevented.

<a id="cost-12"></a>
12. If taking damage is a cost, that cost is not considered paid unless all of that damage was taken. *(If any of the damage is prevented, then the cost has not been paid.)*

**See also:** [Ability](ability.md), [Cost Arrow Icon](cost-arrow-icon.md), [Game Element](game-element.md), [Initiating Abilities](initiating-abilities.md), [Keywords](keywords.md)
