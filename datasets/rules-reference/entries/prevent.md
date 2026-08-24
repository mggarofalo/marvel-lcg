---
id: "rr:prevent"
title: "PREVENT"
document: "Rules Reference"
version: "1.8"
page: 35
hash: "sha256:3419d73df4a4bb38c1dac25a4f98300d2484b195aeb1bbbf8d9fe58e34a298e1"
see_also: ["rr:ability", "rr:cost", "rr:damage", "rr:scheme-card-type", "rr:target", "rr:threat"]
---

# PREVENT

Some card abilities prevent damage or threat.

<a id="prevent-1"></a>
1. When damage is prevented, reduce the amount of damage the target takes *(i.e. the amount of damage that is placed on the target)*.
    <a id="prevent-1-1"></a>
    - When an effect prevents damage dealt to a character, the amount of damage that character “takes” is reduced, but the amount of damage “dealt” is not reduced.
    <a id="prevent-1-2"></a>
    - If an effect prevents all damage dealt to a character, that character is not considered to have taken damage.
    <a id="prevent-1-3"></a>
    - If all damage from an attack is prevented, the attacking character is considered to have dealt damage, but is not considered to have “attacked and damaged” the attacked character.
    <a id="prevent-1-4"></a>
    - If dealing damage is a cost, that cost is considered paid even if some or all of that damage is prevented.
    <a id="prevent-1-5"></a>
    - If taking damage is a cost, that cost is not considered paid unless all of that damage was taken. *(If any of the damage is prevented, then the cost has not been paid.)*

<a id="prevent-2"></a>
2. When threat is prevented, reduce the amount of threat being assigned before it is placed on the scheme.

**See also:** [Ability](ability.md), [Cost](cost.md), [Damage](damage.md), [Scheme (Card Type)](scheme-card-type.md), [Target](target.md), [Threat](threat.md)
