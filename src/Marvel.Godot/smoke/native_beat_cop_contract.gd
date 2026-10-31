extends RefCounted

const Driver = preload("res://smoke/automation/ui_driver.gd")

# Prepared actual 10029 component scenes. Damage starts ready with three stored threat;
# no ready helper or gameplay mutation occurs after Start game.
static func run(smoke) -> bool:
    var damage: bool = smoke.contract == "beat-cop-damage"
    if not await _entry(smoke, damage): return false
    if not await _action(smoke, damage): return false
    if not await _target(smoke, damage): return false
    if not await _result(smoke, damage): return false
    print("BEAT_COP_PREPARED_CROSS_PACK %s explicit-commits=2 revision=2" % smoke.contract)
    return true

static func _entry(smoke, damage: bool) -> bool:
    if not await _capture(smoke, "entry"): return false
    if damage: return await _stored(smoke, "THREAT 3")
    if smoke._tabletop_card_named("Beat Cop").find_child("SourceThreat", true, false) != null:
        smoke._fail("absent stored threat must not be invented")
        return false
    return true

static func _action(smoke, damage: bool) -> bool:
    if not await smoke.ui.click(smoke.ui.named("CompleteChoiceSheet")): return false
    var offers: Array = smoke.main.find_children("Affordance*", "Button", true, false).filter(
        func(node): return node.is_visible_in_tree() and "Beat Cop" in node.text and (("discard" in node.text.to_lower()) == damage))
    if offers.size() != 1:
        smoke._fail("Beat Cop needs one exact offered action")
        return false
    if not await smoke.ui.click(smoke.ui.Locator.control(offers[0])): return false
    if not _costs(smoke, damage): return false
    return await _commit_action(smoke)

static func _costs(smoke, damage: bool) -> bool:
    var copy: String = smoke._visible_text(smoke.main.find_child("CompleteChoicesFrame", true, false))
    if "Exhaust Beat Cop" not in copy or (damage and "Discard Beat Cop" not in copy):
        smoke._fail("staged actual Beat Cop must explain its exhaustion and discard costs: " + copy)
        return false
    var effect := "Current damage: 3 before prevention/replacement" if damage else "move 1 threat onto this card from"
    if effect not in copy:
        smoke._fail("staged Beat Cop must explain its supplied known effect: " + copy)
        return false
    return true

static func _commit_action(smoke) -> bool:
    var commit: Button = smoke.main.find_child("CompleteChoicesFrame", true, false).find_child("Submit", true, false)
    if not await smoke.ui.expect_fully_visible_enabled(smoke.ui.Locator.control(commit)): return false
    if smoke.witness.Revision() != 0: return false
    if not await _capture(smoke, "action-staged"): return false
    if not await smoke.ui.click(smoke.ui.Locator.control(commit)): return false
    return await smoke.ui.expect(func(): return smoke.witness.Revision() == 1,
        "one explicit Beat Cop cost commitment creates its separate target choice", Driver.COMMIT_DEADLINE_MS)

static func _target(smoke, damage: bool) -> bool:
    var target: int = smoke.witness.Anchor("Tiger Shark") if damage else smoke.witness.AreaAnchor("The Break-In!", "MainSchemesArea")
    if target < 0:
        smoke._fail("missing prepared exact Beat Cop target")
        return false
    var effect := "Current damage: 3 before prevention/replacement" if damage else "move 1 threat onto this card from"
    if not await smoke.ui.expect(func(): return effect in smoke._visible_text(smoke.main),
        "paid Beat Cop target choice retains the supplied known effect"): return false
    if not await _capture(smoke, "target-choice"): return false
    if not await preload("res://smoke/native_melee_contract.gd")._choose_enemy(smoke, target, 1, Driver.COMMIT_DEADLINE_MS): return false
    return await smoke.ui.expect(func(): return smoke.witness.Revision() == 2 and smoke.witness.DecisionKind() == "PlayerAction",
        "explicit target commitment completes actual Beat Cop and returns to the player turn")

static func _result(smoke, damage: bool) -> bool:
    if not await smoke._wait_for_result_motion(): return false
    if damage:
        if not await smoke.ui.expect(func(): return smoke.witness.AreaAnchor("Beat Cop", "SupportsArea") < 0 and smoke.witness.AreaAnchor("Beat Cop", "DiscardPile") >= 0,
            "discarded Beat Cop leaves the tableau"): return false
    else:
        if not await _stored(smoke, "THREAT 1"): return false
        var state: Control = smoke._tabletop_card_named("Beat Cop").find_child("SourceState", true, false)
        if state.accessibility_name != "Exhausted": return false
    return await _capture(smoke, "result")

static func _stored(smoke, value: String) -> bool:
    return await smoke.ui.expect(func():
        var source = smoke._tabletop_card_named("Beat Cop")
        return source != null and value.to_lower() in smoke._visible_text(source).to_lower(),
        "Beat Cop locally displays its supplied stored " + value)

static func _capture(smoke, leaf: String) -> bool:
    var destination = smoke.ui.named("CloseChoiceSheet") if smoke.ui.named("CloseChoiceSheet").all().size() == 1 else smoke.ui.named("CompleteChoiceSheet")
    if not await smoke.ui.hover(destination): return false
    return await preload("res://smoke/native_heimdall_contract.gd")._capture(smoke, smoke.contract + "-" + leaf)
