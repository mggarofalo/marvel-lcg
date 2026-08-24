---
id: "rr:swap"
title: "\u201cSWAP\u201d"
document: "Rules Reference"
version: "1.8"
page: 42
hash: "sha256:dcbdc9f0bbee976d7199874075dfbd99f430c49b8ca4014a19978bbe571ad85a"
see_also: ["rr:ability", "rr:target"]
---

# “SWAP”

An instruction to “swap” two components means to exchange the location of those two components.

<a id="swap-1"></a>
1. A swap cannot be completed if there is not a component in both locations.
    <a id="swap-1-1"></a>
    - *For example, you cannot “swap a card in your hand with the top card of your deck” if you have no cards in hand.*

<a id="swap-2"></a>
2. Swapped cards maintain the orientation (such as ready or exhausted, faceup or facedown) of the original card.

<a id="swap-3"></a>
3. Swapping a card in hand with the top card of a deck is **not** considered drawing that card.

<a id="swap-4"></a>
4. When swapping a card in a play area with a card in an out-of-play area, if those two cards:
    <a id="swap-4-1"></a>
    - Share a title, neither card is considered to enter or leave play. Tokens, attached cards, tucked cards, and status cards on the previously in-play card are transferred to the other card and the other card maintains the state *(ready or exhausted)* of the previously in-play card. If the swapped card has an associated hit point dial, that dial remains at the same value.
    <a id="swap-4-2"></a>
    - Do not share a title, the in-play card is considered to leave play and the out-of-play card is considered to enter play. Tokens, attached cards, tucked cards, and status cards on the previously in-play card are **not** transferred to the other card and the other card enters play ready. If the swapped card has an associated hit point dial, that dial is reset to the new card’s printed hit point value.

**See also:** [Ability](ability.md), [Target](target.md)
