---
id: "rr:delayed-effect"
title: "DELAYED EFFECT"
document: "Rules Reference"
version: "1.8"
page: 16
hash: "sha256:70def220c7a705761e8a4923a355db657563a769044e3fabfbdd93f47e5c1dc6"
see_also: ["rr:ability"]
---

# DELAYED EFFECT

Some abilities contain delayed effects. Such abilities specify a future timing point, or indicate a future condition that may arise, and dictate an effect that is to happen at that time.

<a id="delayed-effect-1"></a>
1. Delayed effects resolve automatically and immediately after their specified timing point or future condition occurs or becomes true, and before responses to that point or condition may be used.
    <a id="delayed-effect-1-1"></a>
    - Delayed effects have the same timing priority as constant effects.

<a id="delayed-effect-2"></a>
2. When a delayed effect resolves, it is not treated as a new triggered ability, even if the delayed effect was originally created by a triggered ability.

**See also:** [Ability](ability.md)
