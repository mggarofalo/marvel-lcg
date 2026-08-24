---
id: "rr:for-each"
title: "\u201cFOR EACH\u201d"
document: "Rules Reference"
version: "1.8"
page: 20
hash: "sha256:475ee169b4e0d50b627a1f69e09a00aeb87c347c6c1c65667b52409e6a07d6df"
see_also: ["rr:ability", "rr:target"]
---

# “FOR EACH”

“For each” indicates an effect is repeated based on the number of a countable game element.

<a id="for-each-1"></a>
1. If an effect with “for each” requires a target, that effect applies to a single target unless the “for each” clause includes a “choose” instruction.
    <a id="for-each-1-1"></a>
    - *For example, an effect that reads “For each treachery looked at this way, remove 1 threat from a scheme” removes threat from a single scheme. Alternatively, an effect that reads “For each upgrade you control, choose an enemy and deal 2 damage to it” allows you to choose a different enemy for each upgrade you control.*

<a id="for-each-2"></a>
2. If a “for each” effect without a “choose” instruction deals damage or removes threat, it is considered a single instance of damage dealt or threat removed, respectively.

<a id="for-each-3"></a>
3. If a “for each” effect has a “choose” instruction, each iteration of that choice is considered a separate instance of that effect, even if the same target is chosen multiple times.
    <a id="for-each-3-1"></a>
    - The game state updates after each instance *(for example, if a minion or side scheme is defeated)*.
    <a id="for-each-3-2"></a>
    - Responses can be triggered after each instance.: *For example, if a player is engaged with a guard minion and uses an effect that says “for each resource you spent this way, choose an enemy and deal 2 damage to it,” that player can defeat the guard minion with one instance of 2 damage, making the villain a valid target for further instances. That player can trigger a response ability like “After you deal damage to an enemy” after each instance.*

<a id="for-each-4"></a>
4. If another ability modifies a “for each” effect, that modifier is applied to each instance of the “for each” effect. *(For example, Flurry of Blades is an event that has the effect: “For each Psi-Katana, choose an enemy and deal 2 damage to it.” If modified by an effect that says “that event deals 1 additional damage,” Flurry of Blades deals 3 damage to each chosen enemy.)*

**See also:** [Ability](ability.md), [Target](target.md)
