extends RefCounted

static func run(smoke) -> bool:
    if not await smoke._select_attached_action(smoke.witness.Anchor("Tony Stark"), "Action"): return false
    if not await smoke._commit_once("Futurist"): return false
    if not await smoke._wait_for(func() -> bool: return smoke.witness.DecisionKind() == "CardLook" and smoke._search_frame() != null): return false
    if not await preload("res://smoke/search_inspection_checks.gd").perform(smoke): return false
    if not await _select(smoke, "Arc Reactor"): return false
    if not await _select(smoke, "Mark V Armor"): return false
    var commit := smoke._search_frame().find_child("Submit", true, false) as Button
    if commit == null or commit.disabled or "Mark V Armor" not in commit.text or smoke.witness.Revision() != 1:
        smoke._fail("look inspection or selection lost the explicit named commitment")
        return false
    if not await smoke._pointer_activate(commit): return false
    return await smoke._wait_for(func() -> bool: return smoke.witness.Revision() == 2 and smoke._search_frame() == null)

static func _select(smoke, title: String) -> bool:
    var frame: Control = smoke._search_frame()
    var choices := frame.find_children("Affordance*", "Button", true, false)
    if choices.size() != 3:
        smoke._fail("Futurist must expose exactly its three authorized top cards")
        return false
    for choice: Button in choices:
        if title in choice.accessibility_name: return await smoke._pointer_activate(choice)
    smoke._fail("missing authorized look candidate " + title)
    return false
