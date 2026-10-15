extends "res://smoke/staged_response_smoke.gd"

var natural_ms := 0


func _run() -> void:
	for mode in ["natural", "pointer", "keyboard", "sync"]:
		motion_enabled = false
		if not await _open_window_game(): return
		if not await _exercise_access(mode): return
		main.queue_free()
		await process_frame
		await process_frame
	if not await _exercise_player_action(): return
	print("NEXT_DECISION_ACCESS_OK natural=true pointer=true keyboard=true stale=true no_auto_answer=true")
	quit(0)


func _exercise_access(mode: String) -> bool:
	var before := _recipient_revision()
	if not await _start_observed_play(): return false
	var started := Time.get_ticks_msec()
	var entry := main.find_child("CompleteChoiceSheet", true, false) as Button
	if not _valid_entry(entry): return false
	if mode == "pointer" and not await _capture_checkpoint("next-decision-available"): return false
	var stale: Callable = entry.pressed.get_connections()[0]["callable"]
	if not await _complete_mode(mode, entry, started): return false
	stale.call()
	if not await _assert_one_revision(before + 1): return false
	if not await _assert_settled_payment(): return false
	var window := _window()
	var choice := window.find_child("WindowChoice*", true, false) as Button
	if not _named_response_layout_is_clear(window, choice): return false
	if not await _capture_checkpoint("next-decision-" + mode): return false
	return await _verify_named_response()


func _valid_entry(entry: Button) -> bool:
	if entry == null or entry.disabled or not entry.has_meta("accepted_decision_entry") \
			or entry.text in ["All actions", "All choices"] or entry.text.is_empty():
		_fail("accepted next prompt has no enabled named entry")
		return false
	if _window() != null or not _old_action_controls_are_quiet():
		_fail("automatic playback exposed the next modal or old executable controls")
		return false
	return _pending_entry_has_visible_surface(entry)


func _pending_entry_has_visible_surface(entry: Button) -> bool:
	var normal := entry.get_theme_stylebox("normal") as StyleBoxFlat
	if entry.theme_type_variation != &"ChoiceButton" or normal == null or normal.bg_color.a < 0.9:
		_fail("pending decision entry lacks its visible slate choice-button surface")
		return false
	return true


func _complete_mode(mode: String, entry: Button, started: int) -> bool:
	if mode == "natural":
		if not await _wait_for(func(): return not bool(main.get_meta("response_staged", false))): return false
		natural_ms = Time.get_ticks_msec() - started
	elif mode == "sync":
		if not await _pointer_activate(_node("Toolbar/Synchronize") as Button): return false
		if not await _wait_for(func(): return not bool(main.get_meta("response_staged", false))): return false
	else:
		if not await _open_accepted_entry(entry, mode): return false
		var elapsed := Time.get_ticks_msec() - started
		if elapsed >= natural_ms or bool(main.get_meta("response_staged", false)):
			_fail("explicit next decision waited for the automatic playback tail")
			return false
		print("NEXT_DECISION_ACCESS_TIMING mode=", mode, " elapsed_ms=", elapsed, " natural_ms=", natural_ms)
	return true


func _open_accepted_entry(entry: Button, mode: String) -> bool:
	if mode == "keyboard":
		if not _tab_to_entry(entry): return false
		if not await _keyboard_activate_without_settle(entry): return false
	else:
		if not await _pointer_activate_without_settle(entry): return false
	if bool(main.get_meta("response_staged", false)):
		_fail("next decision entry did not synchronously settle its accepted response")
		return false
	return await _wait_for(func(): return _window() != null)


func _tab_to_entry(entry: Button) -> bool:
	_tabletop_card_named("Mockingbird").grab_focus()
	for _step in range(80):
		if render_viewport.gui_get_focus_owner() == entry: return true
		var key := InputEventKey.new()
		key.keycode = KEY_TAB
		key.pressed = true
		render_viewport.push_input(key)
		key = InputEventKey.new()
		key.keycode = KEY_TAB
		render_viewport.push_input(key)
	_fail("Tab could not reach the accepted next decision from the prior table")
	return false


func _exercise_player_action() -> bool:
	motion_enabled = false
	if not await _open_window_game(): return false
	if not await _select_attached_action(IDENTITY, "Change Form"): return false
	var before := _recipient_revision()
	var motion := _node("Toolbar/Motion") as CheckButton
	motion.button_pressed = true
	motion.toggled.emit(true)
	if not await _pointer_activate(_task_commit()): return false
	if not await _wait_for(func(): return bool(main.get_meta("response_staged", false))): return false
	var entry := main.find_child("CompleteChoiceSheet", true, false) as Button
	if not _valid_entry(entry): return false
	if not await _pointer_activate_without_settle(entry): return false
	if not await _wait_for(func(): return main.find_child("CompleteChoicesFrame", true, false) != null): return false
	if not await _assert_one_revision(before + 1): return false
	if _tabletop_card_named("Spider-Man") == null:
		_fail("next action editor opened against the prior alter-ego board")
		return false
	return await _capture_checkpoint("next-decision-player-action")
