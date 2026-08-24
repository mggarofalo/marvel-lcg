---
id: "rr:otherwise"
title: "\u201cOTHERWISE\u201d"
document: "Rules Reference"
version: "1.8"
page: 31
hash: "sha256:459b9383e3a539ba8ee09fc0cfc8ffe0d2277546004f094e57837bf503e546b1"
see_also: ["rr:replacement-effect", "rr:target"]
---

# “OTHERWISE”

Effects beginning with “otherwise” resolve only if the preceding effect was not resolved.

<a id="otherwise-1"></a>
1. An “otherwise” effect will resolve if one or more of the following are true of the preceding effect:
    <a id="otherwise-1-1"></a>
    - It has a condition that is not true. *(For example, an ability reads: “If you are in hero form, take 2 damage. Otherwise, place 2 threat on the main scheme.” The “otherwise” portion resolves if the player is not in hero form.)*
    <a id="otherwise-1-2"></a>
    - It has an effect that cannot at least partially resolve. *(For example, an ability reads: “Discard 2 cards from your hand. Otherwise, exhaust your identity.” The “otherwise” portion resolves if the player cannot discard at least 1 card from their hand.)*

<a id="otherwise-2"></a>
2. If “otherwise” is preceded by a semicolon, the “preceding effect” refers to the effects before the semicolon in the same sentence. If the “otherwise” effect is its own sentence, the “preceding effect” refers to the sentence coming directly before the “otherwise” sentence.

**See also:** [Replacement Effect](replacement-effect.md), [Target](target.md)
