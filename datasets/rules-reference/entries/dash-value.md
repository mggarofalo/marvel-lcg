---
id: "rr:dash-value"
title: "DASH (VALUE)"
document: "Rules Reference"
version: "1.8"
page: 15
hash: "sha256:af3d36192a2e4915ea056538fd4d6b61e657880692bfe0786debe0aa39b53a8c"
see_also: ["rr:basic-power", "rr:non-numerical-variable"]
---

# DASH (VALUE)

A value presented as a dash (–) indicates that value cannot be used.

<a id="dash-value-1"></a>
1. If a card has a dash (–) as its cost value, that card cannot be played and can only enter play through other means.

<a id="dash-value-2"></a>
2. If a character’s power *(ATK, DEF, REC, SCH, and THW)* has a dash (–) as the value, the character cannot exhaust to use that power.

<a id="dash-value-3"></a>
3. If a game step or card ability references a value of dash (–), that value is treated as an unmodifiable 0. *(For example, if an ability targets “the ally with the lowest SCH,” an ally with a dash for its SCH is considered to have the same SCH as an ally with a SCH of 0.)*

**See also:** [Basic Power](basic-power.md), [Non-Numerical Variable](non-numerical-variable.md)
