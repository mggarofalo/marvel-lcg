---
id: "rr:triggering-condition"
title: "TRIGGERING CONDITION"
document: "Rules Reference"
version: "1.8"
page: 45
hash: "sha256:f98888555bbd3fcd9dc7cd7620e4e022057d83ce2603cd09ffe38c87f89cd44a"
see_also: ["rr:interrupt", "rr:response"]
---

# TRIGGERING CONDITION

A triggering condition is a specific occurrence that takes place in the game. On card abilities, the triggering condition is the element of the ability that references such an occurrence, indicating the timing point at which the ability may be used. The description of an ability’s triggering condition usually follows the word “when” or “after.”

<a id="triggering-condition-1"></a>
1. Each “**Interrupt**“ and “**Response**” ability can only be triggered once per occurrence of its triggering condition.
    <a id="triggering-condition-1-1"></a>
    - Multiple copies of a card with an interrupt or response can each be triggered by the same triggering condition.

<a id="triggering-condition-2"></a>
2. If a single game occurrence creates multiple triggering conditions *(such as a single attack causing a character to both take damage and be defeated)*, those triggering conditions are handled with a single interrupt window and a single response window. During each of these windows, abilities that refer to any of the triggering conditions created by the occurrence may be used in any order.

**See also:** [Interrupt](interrupt.md), [Response](response.md)
