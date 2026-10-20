extends RefCounted

static func reach(smoke, target: Control, reverse := false) -> bool:
    if target == null:
        smoke._fail("keyboard route has no visible destination")
        return false
    var visited: Dictionary = {}
    for step in range(160):
        var focused: Control = smoke.render_viewport.gui_get_focus_owner()
        if focused == target: return await _focused_visible(smoke, target)
        if focused != null:
            var identity := focused.get_instance_id()
            if visited.has(identity): break
            visited[identity] = true
        await key(smoke, KEY_TAB, reverse)
    smoke._fail("real Tab route cannot reach " + str(target.name))
    return false

static func _focused_visible(smoke, target: Control) -> bool:
    if await smoke._wait_for(func() -> bool: return smoke._control_is_fully_visible(target)): return true
    smoke._fail("keyboard focus reached a clipped control %s: visible=%s bounds=%s" % [target.name,
        smoke._visible_control_rect(target), target.get_global_rect()])
    return false

static func key(smoke, code: Key, shift := false, control := false) -> void:
    for pressed in [true, false]:
        var event := InputEventKey.new()
        event.keycode = code
        event.physical_keycode = code
        event.shift_pressed = shift
        event.ctrl_pressed = control
        event.pressed = pressed
        smoke.render_viewport.push_input(event, true)
        await smoke.process_frame

static func activate(smoke, target: Control) -> bool:
    if not await reach(smoke, target): return false
    await key(smoke, KEY_ENTER)
    return true

static func payment(smoke) -> bool:
    var source: Control = smoke._tabletop_card_named("Black Cat")
    if not await reach(smoke, source): return false
    if not await _inspect_and_dismiss(smoke, source): return false
    var action: Button = smoke._attached(smoke._attached_name(smoke.witness.Anchor("Black Cat"), "Action"))
    if not await activate(smoke, action): return false
    if not await smoke._wait_for(func() -> bool: return smoke._payment_modal() != null): return false
    var resource: Button = smoke._visible_button_beginning(smoke._payment_modal(), "Discard Energy")
    if not await activate(smoke, resource): return false
    return await _commit_payment(smoke)

static func _inspect_and_dismiss(smoke, source: Control) -> bool:
    var draft: String = smoke.witness.Draft()
    await key(smoke, KEY_ENTER)
    var popup := smoke.main.find_child("CardInspector", true, false) as Control
    if not await smoke._wait_for(func() -> bool: return popup.visible):
        smoke._fail("keyboard card activation did not open its inspector")
        return false
    await key(smoke, KEY_ESCAPE)
    if not await smoke._wait_for(func() -> bool: return not popup.visible): return false
    if not source.has_focus() or smoke.witness.Draft() != draft or smoke.witness.Revision() != 0:
        smoke._fail("keyboard modal dismissal changed the draft or failed to restore its source focus")
        return false
    return true

static func _commit_payment(smoke) -> bool:
    var commit: Button = smoke._payment_button("Submit")
    if commit == null or commit.disabled: return false
    if not await reach(smoke, commit): return false
    await key(smoke, KEY_TAB, true)
    if smoke.render_viewport.gui_get_focus_owner() == commit:
        smoke._fail("Shift+Tab did not leave the payment commitment")
        return false
    if not _payment_contains_focus(smoke): return false
    if not await reach(smoke, commit): return false
    if smoke.witness.Revision() != 0:
        smoke._fail("keyboard traversal committed before Enter")
        return false
    await key(smoke, KEY_ENTER)
    return await smoke._wait_for(func() -> bool: return smoke.witness.Revision() == 1)

static func _payment_contains_focus(smoke) -> bool:
    var focus: Control = smoke.render_viewport.gui_get_focus_owner()
    if focus == null or not smoke._payment_modal().is_ancestor_of(focus):
        smoke._fail("real payment traversal escaped its active modal")
        return false
    return true

static func targeted_attack(smoke) -> bool:
    var hero: int = smoke.witness.Anchor("Spider-Man")
    var villain: int = smoke.witness.AreaAnchor("Rhino", "VillainArea")
    if not await reach(smoke, smoke._card_for_anchor(hero)): return false
    if not await activate(smoke, smoke._attached(smoke._attached_name(hero, "Action"))): return false
    var chooser: Control = smoke.main.find_child("CardActionChoices", true, false)
    if chooser != null and chooser.is_visible_in_tree():
        if not await activate(smoke, smoke._offered_action_button(chooser, "Attack")): return false
    if not await reach(smoke, smoke._card_for_anchor(villain)): return false
    if not await activate(smoke, smoke._attached(smoke._attached_name(villain, "Target"))): return false
    var commit: Button = smoke._task_commit()
    if commit == null or commit.text != "Attack Rhino" or smoke.witness.Revision() != 0:
        smoke._fail("keyboard target navigation did not preserve the named uncommitted attack")
        return false
    if not await activate(smoke, commit): return false
    return await smoke._wait_for(func() -> bool: return smoke.witness.Revision() == 1 and smoke.witness.Outcome() == "PlayersWin")
