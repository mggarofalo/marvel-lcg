extends RefCounted

static func run(smoke) -> bool:
    if not await _open_editor(smoke): return false
    var revision: int = smoke.witness.Revision()
    for amount in [2, 1, 2]:
        if not await _enter_amount(smoke, amount): return false
        if smoke.witness.Revision() != revision:
            smoke._fail("editing X committed the ability")
            return false
    var source: int = smoke.witness.AreaAnchor("Energy", "HandsArea")
    var resource := smoke._search_frame().find_child("Resource%d" % source, true, false) as Button
    if resource == null or not await smoke._pointer_activate(resource): return false
    if not await preload("res://smoke/native_scale_checkpoint.gd").verify(smoke, "variable-payment"): return false
    var amount: SpinBox = smoke._search_frame().find_child("VariableX", true, false)
    var commit: Button = smoke._search_frame().find_child("Submit", true, false)
    if amount == null or amount.value != 2 or commit == null or commit.disabled:
        smoke._fail("selected variable and its allocated payment are not ready together")
        return false
    if not await smoke._pointer_activate(commit): return false
    return await smoke._wait_for(func() -> bool: return smoke.witness.Revision() == revision + 1)

static func _open_editor(smoke) -> bool:
    var anchor: int = smoke.witness.AreaAnchor("Energy Channel", "UpgradesArea")
    if not await smoke._select_attached_action(anchor, "Action"): return false
    var open := smoke.main.find_child("CompleteChoiceSheet", true, false) as Button
    if open == null or not await smoke._pointer_activate(open): return false
    return await smoke._wait_for(func() -> bool: return smoke._search_frame() != null)

static func _enter_amount(smoke, amount: int) -> bool:
    var variable: SpinBox = smoke._search_frame().find_child("VariableX", true, false)
    if variable == null: return false
    var edit := variable.get_line_edit()
    if not await preload("res://smoke/native_keyboard_traversal.gd").reach(smoke, edit): return false
    # Real editing events exercise the SpinBox's authored value-change route.
    await preload("res://smoke/native_keyboard_traversal.gd").key(smoke, KEY_A, false, true)
    for pressed in [true, false]:
        var event := InputEventKey.new()
        event.keycode = KEY_0 + amount
        event.unicode = 48 + amount
        event.pressed = pressed
        smoke.render_viewport.push_input(event, true)
        await smoke.process_frame
    await preload("res://smoke/native_keyboard_traversal.gd").key(smoke, KEY_ENTER)
    variable = smoke._search_frame().find_child("VariableX", true, false)
    if variable == null or variable.value != amount:
        smoke._fail("keyboard editing did not retain the chosen X")
        return false
    return true
