---
id: "rr:all-purpose-counter"
title: "ALL-PURPOSE COUNTER"
document: "Rules Reference"
version: "1.8"
page: 6
hash: "sha256:8760a7093f3cc30151811cf8c4c3248464de8dc21c8eb46684bdab034139386e"
see_also: ["rr:component-limitations", "rr:uses-x-type"]
---

# ALL-PURPOSE COUNTER

All-purpose counters can be used to track a variety of different game states and statuses. They have no inherent rules. Card abilities can create and define a number of different counter types, such as “arrow counters” or “web counters.” If a counter is called for, an all-purpose counter is used to track its presence in the game.

<a id="all-purpose-counter-1"></a>
1. All-purpose counters are considered tokens for all game purposes.

<a id="all-purpose-counter-2"></a>
2. An ability that refers to an “all-purpose counter” can refer to any all-purpose counter, regardless of what other types that counter might have.

<a id="all-purpose-counter-3"></a>
3. When an all-purpose counter is moved from one card to another, it loses any previous type it had and gains the type defined on the new card it occupies. If the new card does not define a type, it is considered only an “all-purpose counter.”

**See also:** [Component Limitations](component-limitations.md), [Uses (X “Type”)](uses-x-type.md)
