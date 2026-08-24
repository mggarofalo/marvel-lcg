---
id: "rr:event"
title: "EVENT"
document: "Rules Reference"
version: "1.8"
page: 18
hash: "sha256:7e7c9d0a8056fce224de0c9976871426234442dc471e398dae10029c56e0fdf3"
see_also: ["rr:card-types", "rr:discard", "rr:identity", "rr:labeled-ability", "rr:player", "rr:player-card", "rr:ownership-and-control"]
---

# EVENT

Event is a player card type that is generally played for an instantaneous effect. Each time a player plays an event card, that player places it faceup on the table in front of them *(the event is not in play)*, pays its costs, resolves its effects *(unless those effects are canceled)*, and then places the card in its owner’s discard pile after those effects resolve *(or are canceled)*.

<a id="event-1"></a>
1. If an event has more than one triggered ability on it, the player playing it chooses one of those abilities to trigger when playing that event..

<a id="event-2"></a>
2. If the effects of an event are canceled, the card is still considered to have been played, and its costs remain paid. Only the effects are canceled.

<a id="event-3"></a>
3. An event card cannot be played if it requires one or. more targets and does not have at least one valid target.

<a id="event-4"></a>
4. Event cards are considered to be an extension of an identity. Attacks, thwarts, defenses, action abilities, and triggered abilities that resolve from a player playing an event are also considered to be performed by that player’s identity.

<a id="event-5"></a>
5. If an effect modifies the amount of damage an event deals or the amount of threat an event removes, and that event deals multiple instances of damage or removes multiple instances of threat, each of those instances is modified.
    <a id="event-5-1"></a>
    - If an effect modifies the amount of damage “an attack” deals (rather than “an event”), and an event initiates multiple attacks, only the first of those attacks has its damage modified.

**See also:** [Card Types](card-types.md), [Discard](discard.md), [Identity](identity.md), [Labeled Ability](labeled-ability.md), [Player](player.md), [Player Card](player-card.md), [Ownership and Control](ownership-and-control.md)
