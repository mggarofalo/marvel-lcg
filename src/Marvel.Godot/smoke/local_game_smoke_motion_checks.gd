extends "res://smoke/local_game_smoke_resolution_checks.gd"


func _settle_decision(decisions: int, state: Dictionary) -> bool:
	var started := Time.get_ticks_msec()
	while Time.get_ticks_msec() - started < TIMEOUT_MILLISECONDS:
		if not await _motion_state_is_safe(state): return false
		if not bool(main.get_meta("response_staged", false)) and not _status().text.begins_with("DECISION SENT"):
			return true
		await process_frame
	_fail("the engine and staged consequences did not reconcile decision %d" % decisions)
	return false


func _motion_state_is_safe(state: Dictionary) -> bool:
	var skip := main.find_child("Skip", true, false) as Button
	if not skip.disabled:
		_fail("the chronological account unexpectedly requires timed playback")
		return false
	if not bool(main.get_meta("response_staged", false)): return true
	if not motion_enabled or _current_table_prompt_is_operable():
		_fail("staged playback exposed gameplay controls or ran with reduced motion")
		return false
	var timeline := main.find_child("EventTimeline", true, false) as Control
	if timeline == null or not _control_is_fully_visible(timeline):
		_fail("accepted consequences have no visible phase timeline during staging")
		return false
	if not bool(timeline.get_meta("activity_running", false)): return true
	state.saw_staged_motion = true
	if not state.tested_active_motion_toggle:
		if not await _toggle_active_motion(skip): return false
		state.tested_active_motion_toggle = true
	return true

func _current_table_prompt_is_operable() -> bool:
	for pattern in ["ContextualCommit", "ContextualDecline", "Card*Action", "Card*Target", "Card*Generator", "CompleteChoiceSheet"]:
		for candidate in main.find_children(pattern, "Button", true, false):
			if candidate.is_visible_in_tree() and not candidate.disabled and not _accepted_decision_entry(candidate):
				return true
	return _first_enabled_choice() != null

func _accepted_decision_entry(candidate: Button) -> bool:
	if candidate.name != &"CompleteChoiceSheet" or not bool(candidate.get_meta("accepted_decision_entry", false)):
		return false
	var normal := candidate.get_theme_stylebox("normal") as StyleBoxFlat
	return not candidate.text.strip_edges().is_empty() and candidate.text != "Complete Choices" \
		and candidate.theme_type_variation == &"ChoiceButton" and normal != null and normal.bg_color.a >= 0.9


func _toggle_active_motion(skip: Button) -> bool:
	var timeline := main.find_child("EventTimeline", true, false) as Control
	var count := int(timeline.get_meta("timeline_count", 0))
	var revision := int(main.get_meta("staged_revision", -1))
	var motion := _node("Toolbar/Motion") as CheckButton
	motion.button_pressed = false
	motion.toggled.emit(false)
	if not await _wait_for(func(): return not bool(main.get_meta("response_staged", false))):
		_fail("disabling motion did not apply the accepted response")
		return false
	if not skip.disabled or bool(timeline.get_meta("activity_running", false)):
		_fail("disabling active motion did not settle playback")
		return false
	if int(timeline.get_meta("timeline_count", 0)) != count or (_node("Toolbar/SyncStatus") as Label).text != "Last synced · r%d" % revision:
		_fail("settling motion changed the accepted chronology or revision")
		return false
	motion.button_pressed = true
	motion.toggled.emit(true)
	return true


func _wait_for_table_response() -> bool:
	if not await _wait_for(func() -> bool:
		return not bool(main.get_meta("response_staged", false)) and not _status().text.begins_with("DECISION SENT")):
		_fail("the accepted response did not settle before composing its next decision")
		return false
	await process_frame
	return true
