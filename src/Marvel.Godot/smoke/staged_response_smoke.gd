extends "res://smoke/ability_window_smoke.gd"


func _run() -> void:
	if OS.get_environment("MARVEL_STAGE_UNDO_ONLY") == "true":
		if await _exercise_undo(): quit(0)
		return
	for mode in ["finish", "sync", "reduced"]:
		motion_enabled = false
		if not await _open_window_game(): return
		if not await _exercise_mode(mode): return
		main.queue_free()
		await process_frame
		await process_frame
	if not await _exercise_undo(): return
	print("STAGED_RESPONSE_SMOKE_OK prior_table=true next_modal_deferred=true sync_undo_cancel=true reduced_immediate=true")
	quit(0)


func _exercise_mode(mode: String) -> bool:
	var before := _recipient_revision()
	if mode == "reduced":
		if not await _play_mockingbird(): return false
	else:
		if not await _start_observed_play(): return false
		if not await _verify_stable_prior_table(): return false
		if _recipient_revision() != before:
			_fail("presentation exposed the next board revision during earlier consequences")
			return false
		if not await _replace_pending(mode): return false
	if not await _wait_for(func(): return not bool(main.get_meta("response_staged", false))): return false
	if not await _assert_settled_payment(): return false
	return await _assert_one_revision(before + 1)


func _assert_one_revision(expected: int) -> bool:
	for _frame in range(12): await process_frame
	if expected != _recipient_revision() or bool(main.get_meta("response_staged", false)):
		_fail("presentation issued an extra command or stale playback displaced the replacement")
		return false
	return true


func _assert_settled_payment() -> bool:
	if not await _wait_for(func(): return _window() != null): return false
	if _tabletop_card_named("Energy") != null or _tabletop_card_named("Emergency") != null:
		_fail("settled board retained consumed payment cards")
		return false
	return true


func _replace_pending(mode: String) -> bool:
	if mode == "sync":
		return await _pointer_activate(_node("Toolbar/Synchronize") as Button)
	return true


func _exercise_undo() -> bool:
	motion_enabled = false
	if not await _open_window_game(): return false
	if not await _set_history_drawer(true): return false
	if not await _select_attached_action(IDENTITY, "Change Form"): return false
	var before := _recipient_revision()
	var motion := _node("Toolbar/Motion") as CheckButton
	motion.button_pressed = true
	motion.toggled.emit(true)
	if not await _pointer_activate(_task_commit()): return false
	if not await _wait_for(func(): return bool(main.get_meta("response_staged", false))): return false
	var undo := main.find_child("UndoLast", true, false) as Button
	if undo == null or undo.disabled or not await _pointer_activate(undo):
		_fail("legal completed form-change undo unavailable during presentation")
		return false
	if not await _wait_for(func(): return _recipient_revision() == before + 2): return false
	if bool(main.get_meta("response_staged", false)) or _tabletop_card_named("Peter Parker") == null:
		_fail("undo failed to cancel pending form change and restore prior identity")
		return false
	main.queue_free()
	await process_frame
	await process_frame
	return true


func _start_observed_play() -> bool:
	if not await _drag_to_prompt_owner_lane(_tabletop_card_named("Mockingbird")): return false
	if not await _wait_for(func(): return _payment_modal() != null): return false
	for title in ["Energy", "Emergency"]:
		if not await _pointer_activate(_visible_button_beginning(_payment_modal(), "Discard " + title)): return false
	var motion := _node("Toolbar/Motion") as CheckButton
	motion_enabled = true
	motion.button_pressed = true
	motion.toggled.emit(true)
	if not await _pointer_activate(_payment_button("Submit")): return false
	return await _wait_for(func(): return bool(main.get_meta("response_staged", false)))


func _verify_stable_prior_table() -> bool:
	if not bool(main.get_meta("response_staged", false)) or _window() != null:
		_fail("new response modal appeared before its accepted card-play consequences")
		return false
	for title in ["Energy", "Emergency", "Mockingbird"]:
		if _tabletop_card_named(title) == null:
			_fail("accepted playback removed " + title + " from the prior authorized table")
			return false
	if not _old_action_controls_are_quiet(): return false
	var timeline := main.find_child("EventTimeline", true, false) as Control
	if timeline == null or not bool(timeline.get_meta("activity_running", false)):
		_fail("staged response has no ordered visible activity")
		return false
	return await _capture_checkpoint("staged-before-mockingbird-response")


func _old_action_controls_are_quiet() -> bool:
	for button in main.find_children("Card*Action", "Button", true, false):
		if button.is_visible_in_tree() and not button.disabled:
			_fail("old card action remains visibly enabled during accepted playback")
			return false
	return true
