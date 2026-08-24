---
id: "rr:choose-option"
title: "CHOOSE (OPTION)"
document: "Rules Reference"
version: "1.8"
page: 12
hash: "sha256:9e430a96706ae0e838f4039d752427e330bd73e3e3c4d232724a75a55107e7c9"
see_also: ["rr:ability", "rr:player", "rr:target"]
---

# CHOOSE (OPTION)

Some abilities instruct a player to choose between multiple options. *For example, “Choose to either take 1 damage or discard 1 card from your hand.”*

<a id="choose-option-1"></a>
1. When an encounter card requires a player to choose an option, they cannot choose an option that requires one or more targets if there are no valid targets for that option.

<a id="choose-option-2"></a>
2. When a player card requires a player to choose an option, they cannot choose an option that cannot be at least partially resolved. This includes options that:
    <a id="choose-option-2-1"></a>
    - Have a cost the player cannot pay.
    <a id="choose-option-2-2"></a>
    - Require one or more targets and there are no valid targets.

<a id="choose-option-3"></a>
3. When a card requires a player to choose multiple options from a list, that player cannot choose the same option multiple times.

**See also:** [Ability](ability.md), [Player](player.md), [Target](target.md)
