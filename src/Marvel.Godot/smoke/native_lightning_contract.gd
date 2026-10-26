extends RefCounted

# Actual Lightning Strike after public Thor setup; labelled fixture hand/form preparation.
static func run(smoke) -> bool:
    if not await _prepare(smoke): return false
    if not await smoke.ui.click(smoke.ui.named("CancelCardPlay")): return false
    if not await smoke.ui.expect(func(): return smoke._payment_modal() == null and smoke.witness.Revision() == 0,
        "cancelled combined payment has no authoritative commitment"): return false
    if not await _prepare(smoke): return false
    return await _commit(smoke)

static func _prepare(smoke) -> bool:
    if not await _open(smoke): return false
    if not await _amount(smoke, 2): return false
    if not await _shortfall(smoke, 0, 1): return false
    if not await _shortfall(smoke, 1, 2, "Energy, Energy"): return false
    if not await _capture(smoke, "lightning-x-two"): return false
    if not await _resource(smoke, "Energy"): return false
    if not await _shortfall(smoke, 0, 1): return false
    if not await _shortfall(smoke, 1, 0): return false
    if not await _capture(smoke, "lightning-partial"): return false
    return await _finish_payment(smoke)

static func _finish_payment(smoke) -> bool:
    if not await _resource(smoke, "Genius"): return false
    if not await _progress(smoke, "Assign selected resources"): return false
    if not await _assign(smoke, "Energy", "Cost 2", 2): return false
    if not await _assign(smoke, "Genius", "Cost 1", 1): return false
    if not await _progress(smoke, "Payment ready"): return false
    return await _capture(smoke, "lightning-ready")

static func _commit(smoke) -> bool:
    var commit: Button = smoke._payment_button("Submit")
    var locator = smoke.ui.Locator.control(commit)
    if not await smoke.ui.expect_fully_visible_enabled(locator): return false
    if "Lightning Strike" not in commit.text: return false
    if not await smoke.ui.click(locator): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 1 and smoke.witness.DecisionKind() == "PlayerAction",
        "one explicit payment commits Lightning Strike exactly once"): return false
    return await _capture(smoke, "lightning-result")

static func _shortfall(smoke, component: int, remaining: int, resources := "") -> bool:
    return await smoke.ui.expect(func():
        var row = smoke._payment_modal().find_child("PaymentComponentProgress" + str(component), true, false)
        if row == null: return false
        var copy = row.get_child(0) as Label
        var symbols = row.find_child("ResourceSymbols", true, false) as TextureRect
        var expected = "covered" if remaining == 0 else str(remaining) + " more resource"
        return expected in copy.text and symbols.tooltip_text == resources and smoke.witness.Revision() == 0,
        "separate printed and energy component shortages remain visible")

static func _assign(smoke, title: String, cost: String, count: int) -> bool:
    var names: Array[String] = []
    for option in smoke._payment_modal().find_children("Allocation*", "OptionButton", true, false):
        var copy = option.get_parent().get_child(0).get_child(0) as Label
        if title in copy.text: names.append(option.name)
    if names.size() != 2: return false
    for index in count:
        if not await _choose_cost(smoke, names[index], cost): return false
    return true

static func _choose_cost(smoke, option_name: String, cost: String) -> bool:
    var option = smoke._payment_modal().find_child(option_name, true, false) as OptionButton
    var selected := -1
    for index in option.item_count:
        if option.get_item_text(index) == cost: selected = index
    if selected < 0: return false
    var keyboard = preload("res://smoke/native_keyboard_traversal.gd")
    if not await keyboard.reach(smoke, option): return false
    await keyboard.key(smoke, KEY_ENTER)
    var declarations = preload("res://smoke/native_declaration_contract.gd")
    if not await declarations._choose_from_menu(smoke, option.get_popup(), selected): return false
    return await smoke.ui.expect(func():
        var bound = smoke._payment_modal().find_child(option_name, true, false) as OptionButton
        return bound != null and bound.selected == selected and smoke.witness.Revision() == 0,
        "chosen resource allocation is retained without committing")

static func _open(smoke) -> bool:
    if not await smoke._drag_to_prompt_owner_lane(smoke._tabletop_card_named("Lightning Strike")): return false
    return await smoke.ui.expect(func(): return smoke._payment_modal() != null,
        "Lightning Strike opens an uncommitted payment modal")

static func _amount(smoke, amount: int) -> bool:
    var variable: SpinBox = smoke._payment_modal().find_child("VariableX", true, false)
    if variable == null: return false
    var keyboard = preload("res://smoke/native_keyboard_traversal.gd")
    if not await keyboard.reach(smoke, variable.get_line_edit()): return false
    if not await smoke.ui.press_key(KEY_A, false, true): return false
    for pressed in [true, false]:
        var event := InputEventKey.new()
        event.keycode = KEY_0 + amount
        event.unicode = 48 + amount
        event.pressed = pressed
        smoke.render_viewport.push_input(event, true)
        await smoke.process_frame
    if not await smoke.ui.press_key(KEY_ENTER): return false
    return await smoke.ui.expect(func():
        var bound = smoke._payment_modal().find_child("VariableX", true, false) as SpinBox
        return bound != null and int(bound.value) == amount and smoke.witness.Revision() == 0,
        "X input is bound to the chosen numeric value")

static func _resource(smoke, title: String) -> bool:
    var source: Button = smoke._visible_button_beginning(smoke._payment_modal(), "Discard " + title)
    if source == null: return false
    if not await smoke.ui.click(smoke.ui.Locator.control(source)): return false
    return smoke.witness.Revision() == 0

static func _progress(smoke, expected: String) -> bool:
    return await smoke.ui.expect(func():
        var modal: Control = smoke._payment_modal()
        var progress := modal.find_child("PaymentProgress", true, false) as Label
        return progress != null and expected in progress.text and smoke.witness.Revision() == 0,
        "printed one plus typed energy X payment: " + expected)

static func _capture(smoke, leaf: String) -> bool:
    if DisplayServer.get_name() == "headless": return true
    await smoke.process_frame
    await RenderingServer.frame_post_draw
    var directory := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
    DirAccess.make_dir_recursive_absolute(directory)
    return smoke.render_viewport.get_texture().get_image().save_png(directory.path_join(leaf + ".png")) == OK
