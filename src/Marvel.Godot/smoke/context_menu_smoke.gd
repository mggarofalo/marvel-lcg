extends "res://smoke/recipient_commitment_smoke.gd"

func _play_recipient_source() -> bool:
    var matches := _visible_cards_named(recipient_title)
    if matches.is_empty():
        _fail("seed did not supply recipient support")
        return false
    var card := matches[-1] as Control
    recipient_anchor = int(str(card.name).trim_prefix("ProceduralCard"))
    if not await _open_context(card): return false
    var play := _context_row_containing(recipient_title)
    if play == null:
        _fail("menu omitted named hand play: " + _visible_text(_context_menu()))
        return false
    if not await _pointer_activate(play): return false
    if not await _wait_for(func() -> bool: return _payment_modal() != null):
        _fail("context hand play did not continue into payment")
        return false
    if not await _compose_payment(): return false
    return await _wait_for(func() -> bool: return _recipient_entry() != null)

func _recipient_cancel_and_commit() -> bool:
    var before := _recipient_revision()
    var source := _visible_cards_named(recipient_title)[0] as Control
    if not await _open_context(source): return false
    if not await preload("res://smoke/table_bounds_continuity.gd").fractional_translation(self,
            main.find_child("Menu0Item0", true, false) as Control): return false
    if not await _capture_checkpoint("context-source"): return false
    _menu_key(KEY_ESCAPE)
    if not await _wait_for(func() -> bool: return _context_menu() == null):
        _fail("Escape did not close context menu")
        return false
    if _recipient_revision() != before:
        _fail("opening or cancelling context menu committed")
        return false
    return await _choose_recipient(source, before)

func _choose_recipient(source: Control, before: int) -> bool:
    if not await _open_context(source): return false
    _menu_key(KEY_RIGHT)
    if not await _wait_for(func() -> bool: return main.find_child("Menu1Item0", true, false) != null):
        _fail("Right did not open recipient submenu")
        return false
    if not await _wait_for(func() -> bool: return render_viewport.gui_get_focus_owner() == main.find_child("Menu1Item0", true, false)): return false
    if not await _capture_checkpoint("context-recipients"): return false
    _menu_key(KEY_DOWN)
    _menu_key(KEY_ENTER)
    if not await _wait_for(func() -> bool: return _recipient_revision() > before):
        _fail("context recipient did not commit")
        return false
    if _recipient_revision() != before + 1:
        _fail("context recipient committed more than once")
        return false
    return await _finish_context_journey()

func _finish_context_journey() -> bool:
    if not await _identity_menu_actions(): return false
    if not await _wait_for_result_motion(): return false
    if not await preload("res://smoke/table_bounds_continuity.gd").physical_resize(self): return false
    print("CONTEXT_MENU_SMOKE_OK")
    await process_frame
    return true

func _menu_key(code: Key) -> void:
    var press := InputEventKey.new()
    press.keycode = code
    press.pressed = true
    render_viewport.push_input(press)
    var release := InputEventKey.new()
    release.keycode = code
    render_viewport.push_input(release)

func _context_menu() -> Node:
    return main.find_child("CardContextMenu", true, false)

func _open_context(source: Control) -> bool:
    await _scroll_control_into_view(source)
    var point := await _context_point(source)
    if point == Vector2.INF:
        _fail("context source has no exposed card body")
        return false
    var motion := InputEventMouseMotion.new()
    motion.position = point
    motion.global_position = point
    render_viewport.push_input(motion, true)
    await process_frame
    var press := InputEventMouseButton.new()
    press.button_index = MOUSE_BUTTON_RIGHT
    press.position = point
    press.global_position = point
    press.pressed = true
    render_viewport.push_input(press, true)
    var release := InputEventMouseButton.new()
    release.button_index = MOUSE_BUTTON_RIGHT
    release.position = point
    release.global_position = point
    render_viewport.push_input(release, true)
    if not await _wait_for(func() -> bool: return _context_menu() != null):
        _fail("right-click did not open context menu")
        return false
    await process_frame
    return true

func _identity_menu_actions() -> bool:
    if not await _wait_for_result_motion(): return false
    var identity := _visible_cards_named("Peter Parker")[-1] as Control
    if not await _open_context(identity): return false
    var form := _context_row_containing("Change form")
    if form == null:
        _fail("identity context menu omitted change form")
        return false
    var before := _recipient_revision()
    if not await _pointer_activate(form): return false
    if not await _wait_for(func() -> bool: return _recipient_revision() == before + 1):
        _fail("explicit identity form leaf did not commit once")
        return false
    return await _hero_menu_attack()

func _hero_menu_attack() -> bool:
    if not await _wait_for_result_motion(): return false
    var hero := _visible_cards_named("Spider-Man")[-1] as Control
    hero.grab_focus()
    _menu_key(KEY_MENU)
    if not await _wait_for(func() -> bool: return _context_menu() != null):
        _fail("keyboard context entry did not open hero actions")
        return false
    var attack := _context_row_containing("Attack")
    if attack == null:
        _fail("hero context menu omitted its offered basic attack")
        return false
    attack.grab_focus()
    _menu_key(KEY_RIGHT)
    if not await _wait_for(func() -> bool: return main.find_child("Menu1Item0", true, false) != null): return false
    if not await _wait_for(func() -> bool: return render_viewport.gui_get_focus_owner() == main.find_child("Menu1Item0", true, false)): return false
    if not await _capture_checkpoint("context-attack"): return false
    var target := main.find_child("Menu1Item0", true, false) as Button
    if not "Rhino" in target.text:
        _fail("attack submenu did not name legal villain")
        return false
    var before := _recipient_revision()
    if not await _pointer_activate(target): return false
    if not await _wait_for(func() -> bool: return _recipient_revision() == before + 1):
        _fail("explicit attack target leaf did not commit once")
        return false
    return true

func _context_row_containing(text: String) -> Button:
    for row in main.find_children("Menu0Item*", "Button", true, false):
        if text.to_lower() in row.text.to_lower(): return row as Button
    return null

func _context_point(source: Control) -> Vector2:
    for attempt in 6:
        for fraction in [Vector2(0.5, 0.5), Vector2(0.15, 0.2), Vector2(0.85, 0.2)]:
            var point: Vector2 = source.get_global_transform() * (source.size * fraction)
            if await _control_owns_point(source, point): return point
    return Vector2.INF

func _payment_modal() -> Control:
    var local := main.find_child("LocalDecisionWorkspace", true, false) as Control
    return local if local != null and local.is_visible_in_tree() else super._payment_modal()
