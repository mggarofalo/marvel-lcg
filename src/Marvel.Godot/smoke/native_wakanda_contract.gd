extends RefCounted

static func run(smoke) -> bool:
    if not await _pay_event(smoke): return false
    if not await smoke._wait_for(func() -> bool: return smoke.witness.DecisionKind() == "SpecialAbilityNext" and smoke._search_frame() != null): return false
    var first := "Energy Daggers" if smoke.contract == "wakanda-daggers-first" else "Panther Claws"
    if not await _choose_first(smoke, first): return false
    if not await _target_special(smoke, first, false): return false
    var final := "Panther Claws" if first == "Energy Daggers" else "Energy Daggers"
    if smoke.witness.DecisionKind() == "SpecialAbilityNext":
        smoke._fail("sole remaining special invented a second ordering choice")
        return false
    if not await _target_special(smoke, final, true): return false
    return await smoke._wait_for(func() -> bool: return smoke.witness.DecisionKind() == "PlayerAction" and smoke.witness.Revision() == 4)

static func _choose_first(smoke, first: String) -> bool:
    var frame: Control = smoke._search_frame()
    var choices := frame.find_children("Affordance*", "Button", true, false)
    if choices.size() != 2:
        smoke._fail("Wakanda sequence must offer both installed specials exactly once")
        return false
    var selected: Button
    for button: Button in choices:
        if first in button.accessibility_name: selected = button
    if selected == null or not await smoke._pointer_activate(selected): return false
    if not await preload("res://smoke/native_scale_checkpoint.gd").verify(smoke, "wakanda-order"): return false
    if smoke.witness.Revision() != 1:
        smoke._fail("choosing a special committed before explicit confirmation")
        return false
    var commit := smoke._search_frame().find_child("Submit", true, false) as Button
    if commit == null or commit.disabled or first not in commit.text: return false
    if not await smoke._pointer_activate(commit): return false
    if not await smoke._wait_for(func() -> bool: return smoke.witness.Revision() == 2): return false
    return true

static func _pay_event(smoke) -> bool:
    var source: Control = smoke._tabletop_card_named("Wakanda Forever!")
    if source == null or not await smoke._drag_to_prompt_owner_lane(source): return false
    if not await smoke._wait_for(func() -> bool: return smoke._payment_modal() != null): return false
    var commit: Button = smoke._payment_button("Submit")
    if commit == null or not commit.disabled:
        smoke._fail("unpaid Wakanda Forever can be committed")
        return false
    var resource: Button = smoke._visible_button_beginning(smoke._payment_modal(), "Discard Vibranium")
    if resource == null or not await smoke._pointer_activate(resource): return false
    if not await preload("res://smoke/native_scale_checkpoint.gd").verify(smoke, "wakanda-payment"): return false
    if smoke.witness.Revision() != 0: return false
    return await smoke._commit_once("Wakanda Forever!")

static func _target_special(smoke, title: String, final: bool) -> bool:
    if not await smoke._wait_for(func() -> bool: return smoke._search_frame() == null and smoke.witness.CanResolve()): return false
    if final:
        await smoke._wait_for(func() -> bool: return "Final step" in smoke._visible_text(smoke.main))
    var text: String = smoke._visible_text(smoke.main)
    if title not in text or (final and "Final step" not in text):
        smoke._fail("nested special target lost its source or final-step meaning: " + title)
        return false
    var target := "Black Panther" if title == "Energy Daggers" else "Rhino"
    var anchor: int = smoke.witness.Anchor(target) if title == "Energy Daggers" else smoke.witness.AreaAnchor(target, "VillainArea")
    var revision: int = smoke.witness.Revision()
    var action = smoke._attached(smoke._attached_name(anchor, "Action"))
    if action == null or target not in action.accessibility_name:
        smoke._fail("nested special lacks its named player or enemy commitment: " + target)
        return false
    if not await smoke._pointer_activate_attached(action): return false
    return await smoke._wait_for(func() -> bool: return smoke.witness.Revision() == revision + 1)
