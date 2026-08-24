---
id: "rr:move"
title: "MOVE"
document: "Rules Reference"
version: "1.8"
page: 30
hash: "sha256:5f05669f58a54be333495e5e5fc350d2254afb7714a4db5b0aed63583101eb7e"
see_also: ["rr:game-element"]
---

# MOVE

Some abilities allow players to move game elements, such as cards, damage, or threat.

<a id="move-1"></a>
1. When an element moves, it cannot move to its same *(current)* placement.

<a id="move-2"></a>
2. If there is no valid source or destination for a move, the move cannot be made.

<a id="move-3"></a>
3. It is possible for damage to move between dials and cards *(and vice versa)*.
    <a id="move-3-1"></a>
    - If damage is moved from a dial to a card, increase the hit points tracked by the dial by the specified amount *(no higher than the card’s maximum hit points)*, and place the same amount of damage on the card.
    <a id="move-3-2"></a>
    - If damage is moved from a card to a dial, remove damage from the card and reduce the dial by the same amount.

<a id="move-4"></a>
4. If damage is moved off a character, the moved damage is considered to be healed from that character.

<a id="move-5"></a>
5. If damage is moved to a character, the moved damage is considered to be dealt to that character.

<a id="move-6"></a>
6. If threat is moved off a scheme, the moved threat is considered to be removed from that scheme.

<a id="move-7"></a>
7. If threat is moved to a scheme, the moved threat is considered to be placed on that scheme.

**See also:** [Game Element](game-element.md)
