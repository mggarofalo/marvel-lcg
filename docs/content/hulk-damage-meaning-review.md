# Beat Cop damage meaning independent review

Reviewed source `e50a87fe2def07ca87a85b73fc491287dba1cc8b` on
`test/hulk-damage-meaning-review`, based on native source `9e49ff89`.
This is a narrow engine-meaning and native rendering review, not whole-pack
acceptance or a new interaction design approval.

## Blocking finding on the reviewed source

The known damage amount disappears if Beat Cop's discard cost resets an empty
player deck. `rr:player-deck.4` explicitly permits an empty player deck alongside
an empty discard pile until a discard allows the reset. This is reachable play.
Replacing the empty player deck fixture with a nonempty deck hides this case.

The exact public Beat Cop action advertises three damage and pays its actual
exhaust/discard cost. The reset moves the same source incarnation into PlayerDeck.
The target draft then says `Tiger Shark · 6/6 → 3/6 HP`, losing the known amount
and prevention/replacement qualification. The effect still evaluates three damage.
`AbilitySourceThreatDescription.AllowsAmount` requires current DiscardPile
membership and current faceup state, although the quantity comes from retained
public source state. Those current-zone restrictions are too narrow.

Independent compiled reproduction varied only whether the existing projection
fixture supplied two player deck cards, added a second encounter card for the
reset penalty, and checked the source's post-cost area. Both permissive and
restricted empty-deck variants fail; the nonempty controls pass. The selected
target draft uses the real DecisionComposer. The temporary failing variants are
preserved outside the suite pending repair:

- `C:/Users/mggar/AppData/Local/Temp/hulk-meaning-review-regression.patch`
- `C:/Users/mggar/AppData/Local/Temp/hulk-meaning-review-managed.log`

Required repair: preserve the known quantity from the exact public resolving
source without inspecting concealed current state. Keep source-incarnation,
provenance, hidden-quantity and wrong-cost protections. Recheck the same compiled
regression and normal controls.

## Native evidence on the reviewed source

Added narrow assertions to the existing actual 10029 prepared component journeys:
staged move includes `move 1 threat onto this card from`; staged damage includes
`Current damage: 3 before prevention/replacement`; the post-cost target choice
retains the same supplied effect. Existing cost, counter, exhaustion, physical
source, engine-owned damage and two-commitment assertions remain intact.

Rendered Windows Godot 4.7.1 .NET runs pass move and damage at both 100% and 150%,
1920×1080, reduced motion, with fresh Debug client and Release native-test builds.
The 100% cases use the representative manifest. The 150% runs invoke the same
NativeContractJourneyTests with MARVEL_UI_SCALE=150 and MARVEL_NATIVE_RENDERED=true.

Artifacts and logs:

- `C:/Users/mggar/AppData/Local/Temp/hulk-meaning-review-native-100-fixed-script/`
  contains results.jsonl, both contract logs and per-case entry, action-staged,
  target-choice and result PNGs under artifacts/contract-beat-cop-*-100-reduced/.
- `C:/Users/mggar/AppData/Local/Temp/hulk-meaning-review-native-150/` contains the
  same PNG phases in damage/ and move/; adjacent
  hulk-meaning-review-native-150-{damage,move}.log records each passing run.

Inspected move action-staged, damage action-staged and damage target-choice
captures at each scale. Known movement, costs, amount and caveat are readable;
the postdiscard decision retains the Beat Cop cause and answering player.
These normal native fixtures have nonempty decks and do not resolve the blocker.

An initial reviewer script incorrectly attempted a separate staged element
choice after clicking a direct commitment; it failed after revision two.
Restoring the established target-input helper fixed the script. That failed
attempt remains in hulk-meaning-review-native-100/ and is excluded from pass
claims. Selected-target meaning is asserted through the real composer in the
managed regression; no native staged-target state is claimed on the direct route.

No production rules, book/setup data or wire contracts were changed by this
review. Broader native profiles, Linux, exported packages and full-pack acceptance
were not rerun for this narrow review.
