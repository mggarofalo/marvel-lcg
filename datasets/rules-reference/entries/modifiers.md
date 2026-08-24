---
id: "rr:modifiers"
title: "MODIFIERS"
document: "Rules Reference"
version: "1.8"
page: 29
hash: "sha256:0901ef9d014099d6d76373653f385b2a3f248a008aea26481cdbf94cd82e3404"
see_also: ["rr:base-value", "rr:dash-value", "rr:printed"]
---

# MODIFIERS

The game constantly checks and *(if necessary)* updates the count of any variable quantity that is being modified. Any time a new modifier is applied or removed, the entire quantity is recalculated from the start, considering the unmodified base value and all active modifiers.

<a id="modifiers-1"></a>
1. The “per player” icon ([per-player]) is not considered a modifier and is applied before any modifiers are applied.

<a id="modifiers-2"></a>
2. The calculation of a value treats all modifiers as being applied simultaneously. However, while performing the calculation, all additive and subtractive modifiers are calculated before doubling and/or halving modifiers are calculated.

<a id="modifiers-3"></a>
3. If a value is “set” to a specific number, the set modifier overrides all non-set modifiers. If multiple set modifiers are in conflict, the most recently resolved set modifier takes precedence.

<a id="modifiers-4"></a>
4. After all active modifiers have been taken into account, if a value is below zero, it is treated as zero: a card cannot have “negative” icons, attributes, traits, cost, or keywords.

<a id="modifiers-5"></a>
5. Fractional values are rounded up after all modifiers have been applied.

<a id="modifiers-6"></a>
6. If a card ability causes a character to “get” a statistic *(such as +1 ATK or 4 hit points)*, the ability modifies the character’s statistic while it is active.
    <a id="modifiers-6-1"></a>
    - If such an ability expires or otherwise becomes inactive, the modified statistic reverts to the value it would have without the modifier.

<a id="modifiers-7"></a>
7. A value of a dash (–) cannot be modified.

**See also:** [Base Value](base-value.md), [Dash (Value)](dash-value.md), [Printed](printed.md)
