---
id: "rr:initiating-abilities"
title: "INITIATING ABILITIES"
document: "Rules Reference"
version: "1.8"
page: 24
hash: "sha256:36cd5f7b244e4746f0df84399f8fb394b63853dd34ce0231a858f5cf6d7674db"
steps: 7
see_also: ["rr:ability", "rr:cost", "rr:play-restrictions-and-permissions", "rr:target"]
---

# INITIATING ABILITIES

When a player wishes to play a card or initiate a triggered ability, that player first declares their intent. Then, the player checks the following conditions in order:

<a id="initiating-abilities-step-1"></a>
1. If playing a card, the player places that card faceup on the table in front of them. *(This card is not in play.)*

<a id="initiating-abilities-step-2"></a>
2. Check play restrictions: can the card be played, or the ability initiated, at this time? If the card or ability specifies one or more targets, check that it has at least one valid target. If the card or ability does not have at least one valid target, it cannot be played or initiated. If the card or ability has a form requirement *(for example, “Hero form only” or “***Hero Action**”), the form of the player playing that card or initiating that ability is checked now.

<a id="initiating-abilities-step-3"></a>
3. Determine the cost *(or costs)* to play the card or initiate the ability and the player’s ability to pay them, taking modifiers into account. If a card has a resource cost of X, the player playing that card chooses the value of X during this step. If both conditions are met, follow these steps in order:

<a id="initiating-abilities-step-4"></a>
4. Apply any modifiers to the cost(s).

<a id="initiating-abilities-step-5"></a>
5. Pay the cost(s). If this step is reached and the cost(s) cannot be paid, abort this process without paying any costs.

<a id="initiating-abilities-step-6"></a>
6. The card commences being played, or the effects of the ability attempt to initiate.

<a id="initiating-abilities-step-7"></a>
7. The card is played or the ability *(if not canceled in the previous step)* resolves. The card enters play or, if it is an event card, its effects resolve and it is then placed in its owner’s discard pile.

<a id="initiating-abilities-1"></a>
1. If any of the above steps would make the triggering condition of an interrupt ability true, that ability may be initiated just before that triggering condition becomes true.

<a id="initiating-abilities-2"></a>
2. If any of the above steps would make the triggering condition of a response ability true, that ability may be initiated immediately after that triggering condition becomes true.

<a id="initiating-abilities-3"></a>
3. If the ability being initiated is on a card that is in play, the sequence does not stop from completing if that card leaves play during this sequence unless the card leaving play prevents a required cost from being paid.

**See also:** [Ability](ability.md), [Cost](cost.md), [Play Restrictions and Permissions](play-restrictions-and-permissions.md), [Target](target.md)
