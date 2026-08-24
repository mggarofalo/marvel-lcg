---
id: "rr:interrupt"
title: "INTERRUPT"
document: "Rules Reference"
version: "1.8"
page: 25
hash: "sha256:d000dca2449d3277c7e6fd8a654ac488edba09e63e6527cb36391bc97fc53956"
see_also: ["rr:cancel", "rr:replacement-effect", "rr:triggered-ability", "rr:would"]
---

# INTERRUPT

An interrupt ability is a type of triggered ability, indicated by the bold “**Interrupt**” timing trigger. Interrupt abilities may be resolved anytime the specified triggering condition occurs, as described in the interrupt’s ability text. The interrupt ability interrupts the resolution of the specified triggering condition, and resolves immediately before that triggering condition resolves.

<a id="interrupt-1"></a>
1. Players can only trigger interrupt abilities on cards they control or on encounter cards.
    <a id="interrupt-1-1"></a>
    - Players cannot trigger interrupt abilities on obligations in other players’ play areas.

<a id="interrupt-2"></a>
2. Multiple interrupts may be triggered by the same triggering condition, but each interrupt can only be triggered once per occurrence of the triggering condition.
    <a id="interrupt-2-1"></a>
    - Multiple copies of a card with an interrupt can each be triggered by the same triggering condition.

<a id="interrupt-3"></a>
3. An interrupt ability is resolved when its triggering condition initiates, but before that triggering condition resolves.
    <a id="interrupt-3-1"></a>
    - Interrupts that use the word “would” resolve before its triggering condition initiates, when that condition becomes imminent.

<a id="interrupt-4"></a>
4. If an interrupt changes *(via a replacement effect)* or cancels an imminent triggering condition, further interrupts to the original triggering condition cannot be triggered.

<a id="interrupt-5"></a>
5. Once all players decide they do not wish to resolve any *(further)* interrupts to a triggering condition, *(further)* interrupts to that instance of that triggering condition cannot be used.

**See also:** [Cancel](cancel.md), [Replacement Effect](replacement-effect.md), [Triggered Ability](triggered-ability.md), [“Would”](would.md)
