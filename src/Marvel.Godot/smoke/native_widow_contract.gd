extends RefCounted

# Explicitly prepared cross-pack scenes; every ability is the unmodified authored row.
static func run(smoke) -> bool:
    if smoke.contract == "widow-safe-house": return await _safe_house(smoke)
    return await _preparation(smoke)

static func _safe_house(smoke) -> bool:
    var chosen: int = await _open_safe_house(smoke)
    if chosen < 0: return false
    if not await _stage_safe_house(smoke, chosen): return false
    return await _finish_safe_house(smoke, chosen)

static func _open_safe_house(smoke) -> int:
    if not await smoke.ui.click(smoke.ui.named("CompleteChoiceSheet")): return -1
    var offers: Array = smoke.main.find_children("Affordance*", "Button", true, false).filter(
        func(node): return node.is_visible_in_tree() and "Safe House" in node.text)
    if offers.size() != 1:
        _fail(smoke, "Safe House needs one exact offered action")
        return -1
    if not await smoke.ui.click(smoke.ui.Locator.control(offers[0])): return -1
    var targets: Array = smoke.main.find_children("Target*", "Button", true, false).filter(
        func(node): return node.is_visible_in_tree() and "Widow's Bite" in node.text)
    if targets.size() != 2:
        _fail(smoke, "Safe House cost must expose two physical discarded Preparations")
        return -1
    targets.sort_custom(func(a, b): return int(str(a.name).trim_prefix("Target")) < int(str(b.name).trim_prefix("Target")))
    return int(str(targets[1].name).trim_prefix("Target"))

static func _stage_safe_house(smoke, chosen: int) -> bool:
    if not await _target(smoke, chosen): return false
    if not await smoke.ui.expect(func(): return Array(smoke.witness.SelectedTargets()) == [chosen] and smoke.witness.Revision() == 0,
        "selecting an exact own-discard Preparation stages its cost without exhausting Safe House"): return false
    if not await _safe_house_meaning(smoke): return false
    if not await _capture(smoke, "widow-safe-house-staged"): return false
    if not await _unstage_safe_house(smoke, chosen): return false
    if not await _target(smoke, chosen): return false
    for cycle in range(2):
        if not await _reopen_safe_house(smoke, chosen): return false
    return true

static func _unstage_safe_house(smoke, chosen: int) -> bool:
    if not await _target(smoke, chosen): return false
    if not await smoke.ui.expect(func(): return Array(smoke.witness.SelectedTargets()).is_empty() and smoke.witness.Revision() == 0,
        "unstaging the selected discard changes no authoritative state"): return false
    return await smoke.ui.expect(func():
        var summaries: Array = smoke.ui.named("CompleteChoiceSummary").all()
        return summaries.size() == 1 and "0 selected" in summaries[0].text and "Widow's Bite" not in summaries[0].text,
        "the pinned explanation removes an unstaged card")

static func _reopen_safe_house(smoke, chosen: int) -> bool:
    if not await smoke.ui.click(smoke.ui.named("CloseChoiceSheet")): return false
    if not await smoke.ui.expect(func(): return smoke.ui.named("CompleteChoiceSummary").all().is_empty(),
        "closing removes the old summary before reopening"): return false
    if not await smoke.ui.click(smoke.ui.named("CompleteChoiceSheet")): return false
    if not await smoke.ui.expect(func(): return smoke.ui.named("CompleteChoiceSummary").all().size() == 1,
        "reopening installs exactly one current summary"): return false
    if not await _safe_house_meaning(smoke): return false
    return await smoke.ui.expect(func(): return Array(smoke.witness.SelectedTargets()) == [chosen] and smoke.witness.Revision() == 0,
        "dismissal and reopening preserve staging without a game commitment")

static func _safe_house_meaning(smoke) -> bool:
    var summary = smoke.ui.named("CompleteChoiceSummary")
    if not await smoke.ui.expect_visible(summary): return false
    if not await smoke.ui.expect(func():
        var node = summary.all()[0]
        var bounds: Rect2 = node.get_global_rect()
        return smoke.render_viewport.get_visible_rect().encloses(bounds) and smoke.main.find_child("CompleteChoicesFrame", true, false).get_global_rect().encloses(bounds),
        "the modal's selected-action explanation fits entirely on screen"): return false
    var copy: String = summary.all()[0].text
    if "Safe House #29" not in copy or "Exhaust" not in copy or "PREPARATION" not in copy or "discard pile" not in copy.to_lower():
        return _fail(smoke, "the modal must retain source, exhaustion and discard choice together: " + copy)
    if "1 selected" not in copy or "Widow's Bite" not in copy:
        return _fail(smoke, "the modal must show the reversible staged choice: " + copy)
    return true

static func _finish_safe_house(smoke, chosen: int) -> bool:
    var copy: String = smoke._visible_text(smoke.main)
    if "Safe House #29" not in copy or "PREPARATION" not in copy or "discard pile" not in copy.to_lower():
        return _fail(smoke, "Safe House composition lacks source and discard-selection cost: " + copy)
    if not await _commit(smoke): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 1 and smoke.witness.AreaAnchor("Widow's Bite", "HandsArea") == chosen,
        "one explicit cost commitment exhausts Safe House and returns only the chosen physical Preparation"): return false
    return await _capture(smoke, "widow-safe-house-result")

static func _preparation(smoke) -> bool:
    if not await _begin_preparation(smoke): return false
    return await _finish_preparation(smoke)

static func _begin_preparation(smoke) -> bool:
    if smoke.witness.AreaAnchor("Widow's Bite", "UpgradesArea") < 0:
        return _fail(smoke, "prepared Bite lacks a unique physical source in play")
    if not await _capture(smoke, "widow-bite-entry"): return false
    if not await _window(smoke, "Widow's Bite"): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 1,
        "Bite's discard cost and minion attack finish before the completion response"): return false
    var copy: String = smoke._visible_text(smoke.main)
    print("WIDOW_COMPLETION_COPY " + copy)
    if "Widow's Bite" not in copy or "Black Widow" not in copy or "Synth-Suit" not in copy or "Pass" not in copy:
        return _fail(smoke, "completion must retain discarded Preparation cause, answerer and optional alternatives: " + copy)
    var rhino: Array = smoke.main.find_children("WindowChoice*", "Button", true, false).filter(
        func(node): return node.is_visible_in_tree() and "Rhino" in node.text)
    if rhino.size() != 1 or "Current damage: 1" not in rhino[0].text:
        return _fail(smoke, "Widowmaker must disclose the supplied damage amount on its choice")
    var suit: Array = smoke.main.find_children("WindowAction*", "Button", true, false).filter(
        func(node): return node.is_visible_in_tree() and "Synth-Suit" in node.text)
    if suit.size() != 1 or "Exhaust Synth-Suit" not in suit[0].text:
        return _fail(smoke, "Synth-Suit must disclose its offered exhaustion cost")
    if not await _capture(smoke, "widow-completion-options"): return false
    return true

static func _finish_preparation(smoke) -> bool:
    if not await _window(smoke, "Rhino"): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 2,
        "explicit Widowmaker enemy choice commits once"): return false
    var suit: Array = smoke.main.find_children("WindowAction*", "Button", true, false).filter(
        func(node): return node.is_visible_in_tree() and "Synth-Suit" in node.text)
    if suit.size() != 1 or "Exhaust Synth-Suit" not in suit[0].text or "Ready Black Widow" not in suit[0].text:
        return _fail(smoke, "the remaining response must show both exhaustion and named readiness")
    if not await smoke.ui.expect_fully_visible_enabled(smoke.ui.Locator.control(suit[0])): return false
    if not await _capture(smoke, "widow-ready-response"): return false
    if not await _window(smoke, "Synth-Suit"): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 3 and smoke.witness.DecisionKind() == "PlayerAction",
        "explicit Synth-Suit response exhausts suit and readies Black Widow"): return false
    return await _capture(smoke, "widow-responses-result")

static func _window(smoke, text: String) -> bool:
    var matches: Array = smoke.main.find_children("Window*", "Button", true, false).filter(
        func(node): return node.is_visible_in_tree() and text in node.text and (str(node.name).begins_with("WindowChoice") or str(node.name).begins_with("WindowAction")))
    if matches.size() != 1: return _fail(smoke, "expected one explicit window choice for " + text + ": " + smoke._visible_text(smoke.main))
    if not await smoke.ui.expect_fully_visible_enabled(smoke.ui.Locator.control(matches[0])): return false
    return await smoke.ui.click(smoke.ui.Locator.control(matches[0]))

static func _target(smoke, id: int) -> bool:
    var locator = smoke.ui.named("Target%d" % id)
    if not await preload("res://smoke/native_heimdall_contract.gd")._reach_target(smoke, "Target%d" % id): return false
    return await smoke.ui.click(locator)

static func _commit(smoke) -> bool:
    var button: Button = smoke._task_commit()
    if button == null: button = smoke.main.find_child("Submit", true, false) as Button
    if button == null: return _fail(smoke, "missing explicit Safe House commitment")
    if not await smoke.ui.expect_fully_visible_enabled(smoke.ui.Locator.control(button)): return false
    return await smoke.ui.click(smoke.ui.Locator.control(button))

static func _fail(smoke, message: String) -> bool:
    smoke._fail(message)
    return false

static func _capture(smoke, leaf: String) -> bool:
    if leaf == "widow-safe-house-staged":
        if not await smoke.ui.hover(smoke.ui.named("CloseChoiceSheet")): return false
    elif leaf in ["widow-completion-options", "widow-ready-response"]:
        if not await smoke.ui.hover(smoke.ui.named("WindowPass")): return false
    if leaf in ["widow-safe-house-staged", "widow-completion-options", "widow-ready-response"]:
        if not await smoke.ui.expect(func():
            var inspector = smoke.main.find_child("CardInspector", true, false)
            return inspector == null or not inspector.is_visible_in_tree(),
            "hover preview dismisses before recording the decision"): return false
    return await preload("res://smoke/native_heimdall_contract.gd")._capture(smoke, leaf)
