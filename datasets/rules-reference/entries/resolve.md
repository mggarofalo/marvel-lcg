---
id: "rr:resolve"
title: "RESOLVE"
document: "Rules Reference"
version: "1.8"
page: 37
hash: "sha256:c92eb2c71b9485c959cf4a13d3da2893447e848a8cc0a4ddae9a5861dea863f7"
see_also: ["rr:ability", "rr:cancel", "rr:event", "rr:treachery"]
---

# RESOLVE

Effects, abilities, event cards, and treachery cards are each resolved under the following conditions:

<a id="resolve-1"></a>
1. An effect is resolved when it is applied to the game state.

<a id="resolve-2"></a>
2. An ability is resolved when it is triggered and one or more of its effects resolve.

<a id="resolve-3"></a>
3. An event card is resolved when it is played and one or more of its abilities resolve.

<a id="resolve-4"></a>
4. A treachery card is resolved when it is revealed and one or more of its abilities resolve.

<a id="resolve-5"></a>
5. Constant abilities are never considered resolved. They are always active while the card they are on is in play.

<a id="resolve-6"></a>
6. Card types other than events and treacheries are not considered resolved, though abilities on those other card types can be resolved.

<a id="resolve-7"></a>
7. If all of the effects of an ability are canceled, that ability is not considered to have resolved.

<a id="resolve-8"></a>
8. If all of the abilities of an event or treachery are canceled, that card is not considered to have resolved.

**See also:** [Ability](ability.md), [Cancel](cancel.md), [Event](event.md), [Treachery](treachery.md)
