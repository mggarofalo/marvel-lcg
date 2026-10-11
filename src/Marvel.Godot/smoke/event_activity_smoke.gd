extends "res://smoke/local_game_smoke.gd"


func _run() -> void:
	for animated in [true, false]:
		if not await _open_phase_activity(animated): return
		if not await _verify_phase_activity(animated): return
		if not await _sync_clears_activity(): return
		main.queue_free()
		await process_frame
		await process_frame
	print("EVENT_ACTIVITY_SMOKE_OK ordered=true decision_preserved=true reduced_motion=true sync_clears=true")
	quit(0)


func _open_phase_activity(animated: bool) -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	var motion := _node("Toolbar/Motion") as CheckButton
	motion.button_pressed = animated
	motion.toggled.emit(animated)
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _task_commit() != null): return false
	if not await _commit_once("Keep hand"): return false
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if not await _pointer_activate(_button_named("End turn")): return false
	if not await _wait_for(func() -> bool:
		return (_node("Toolbar/SyncStatus") as Label).text != revision): return false
	if _button_named("Keep hand") != null:
		if not await _commit_once("Keep hand"): return false
	return await _wait_for(func() -> bool:
		var activity := _activity()
		return activity != null and activity.is_visible_in_tree() and int(activity.get_meta("activity_count", 0)) > 1)


func _activity() -> Control:
	return main.find_child("EventActivity", true, false) as Control


func _verify_phase_activity(animated: bool) -> bool:
	var activity := _activity()
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	var prompt := _logical_prompt_identity()
	var account := activity.get_node("ActivityAccount/ActivityText") as Label
	if not _control_is_fully_visible(activity) or account.text.is_empty():
		_fail("the villain phase has no readable bounded activity account")
		return false
	if animated:
		if not await _capture_checkpoint("phase-activity-first-beat"): return false
		if not await _ordered_beats_remain_interactive(activity): return false
	else:
		if not _static_account_is_complete(activity): return false
	if (_node("Toolbar/SyncStatus") as Label).text != revision or _logical_prompt_identity() != prompt:
		_fail("presentation advanced or replaced the engine decision")
		return false
	return await _capture_checkpoint("phase-activity-%s" % ("animated" if animated else "reduced"))


func _ordered_beats_remain_interactive(activity: Control) -> bool:
	if not bool(activity.get_meta("activity_running", false)):
		_fail("the accepted phase batch skipped directly to its final result")
		return false
	var index := int(activity.get_meta("activity_index"))
	var before := (activity.get_node("ActivityAccount/ActivityText") as Label).text
	var heading := main.find_child("ContextualHeading", true, false) as Label
	if heading == null or not heading.is_visible_in_tree() or heading.text.is_empty():
		_fail("activity concealed the current decision's explanation")
		return false
	if not await _wait_for(func() -> bool:
		return int(activity.get_meta("activity_index")) > index): return false
	if int(activity.get_meta("activity_index")) != index + 1 \
			or (activity.get_node("ActivityAccount/ActivityText") as Label).text == before:
		_fail("activity skipped or failed to distinguish ordered consequences")
		return false
	var pulse := main.find_child("EventActivityPulse", true, false) as Control
	if pulse == null or pulse.mouse_filter != Control.MOUSE_FILTER_IGNORE:
		_fail("activity cues intercept table input")
		return false
	return true


func _static_account_is_complete(activity: Control) -> bool:
	var text := (activity.get_node("ActivityAccount/ActivityText") as Label).text
	var count := int(activity.get_meta("activity_count"))
	if bool(activity.get_meta("activity_running", true)):
		_fail("reduced motion lost or delayed an accepted consequence")
		return false
	for index in range(count):
		if "%d. " % (index + 1) not in text:
			_fail("reduced motion omitted accepted consequence %d" % (index + 1))
			return false
	if "threat" not in text.to_lower():
		_fail("the villain phase account lost its scheme consequence")
		return false
	return true


func _sync_clears_activity() -> bool:
	var sync := _node("Toolbar/Synchronize") as Button
	if not await _pointer_activate(sync): return false
	if not await _wait_for(func() -> bool:
		var activity := _activity()
		return activity == null or not activity.is_visible_in_tree()):
		_fail("synchronization replayed or retained stale activity")
		return false
	return true
