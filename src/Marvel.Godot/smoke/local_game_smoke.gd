extends "res://smoke/local_game_smoke_resolution_checks.gd"

func _initialize() -> void:
	_prepare_art_pack()
	motion_enabled = OS.get_environment("MARVEL_SMOKE_MOTION") != "disabled"
	var viewport := OS.get_environment("MARVEL_SMOKE_VIEWPORT").split("x")
	if viewport.size() == 2:
		var fixed_viewport := SubViewport.new()
		fixed_viewport.size = Vector2i(int(viewport[0]), int(viewport[1]))
		fixed_viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
		root.add_child(fixed_viewport)
		# A standalone SubViewport does not receive this notification from a
		# SubViewportContainer, but native pointer injection still needs it.
		fixed_viewport.notify_mouse_entered()
		render_viewport = fixed_viewport
	else:
		render_viewport = root
	_run.call_deferred()


func _run() -> void:
	var packed := load("res://Main.tscn") as PackedScene
	if packed == null:
		_fail("Main.tscn could not be loaded")
		return
	if not await _open_setup(packed):
		return
	if not await _pointer_ownership_probe_is_strict():
		_fail("the pointer ownership probe did not distinguish persistent partial occlusion")
		return
	await _configure_seeded_game()
	if not await _open_and_validate_table():
		return
	var journey := await _play_seeded_journey()
	if journey.is_empty():
		return
	if not await _terminal_table_is_safe(journey):
		return
	print("LOCAL_GAME_SMOKE_OK decisions=%d motion=%s" % [
		journey.decisions,
		"enabled" if motion_enabled else "disabled",
	])
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _open_setup(packed: PackedScene) -> bool:
	main = packed.instantiate() as Control
	render_viewport.add_child(main)
	if not await _wait_for(func() -> bool: return _button_named("Start game") != null):
		_fail("setup never became ready")
		return false
	if not await _visual_system_is_resolved():
		return false
	if not await _entry_modes_are_explicit():
		return false
	return await _capture_checkpoint("setup")


func _configure_seeded_game() -> void:
	_select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Spider-Man")
	if OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true":
		_select_named_option(_node("Setup/Selections/Fields/Grid/SecondHero"), "Captain Marvel")
	_select_named_option(_node("Setup/Selections/Fields/Grid/Scenario"), "Rhino")
	_select_named_option(_node("Setup/Selections/Fields/Grid/Mode"), "Standard")
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "1"
	seed.text_changed.emit(seed.text)
	var motion := _node("Toolbar/Motion") as CheckButton
	motion.button_pressed = motion_enabled
	motion.toggled.emit(motion_enabled)
	await process_frame


func _open_and_validate_table() -> bool:
	var start := _button_named("Start game")
	if start == null or start.disabled:
		_fail("the visible Start game control is unavailable")
		return false
	if not await _pointer_activate(start):
		return false
	if not await _wait_for(func() -> bool: return _play().visible and _decision() != null):
		_fail("the opened table never became visible")
		return false
	if OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true" \
			and not await _cooperative_seat_switch_is_safe():
		return false
	if not await _live_scale_rebuilds_the_decision():
		return false
	if not await _procedural_cards_are_safe():
		return false
	if not await _board_layout_is_resolved():
		return false
	if not await _redesign_gate_table_interactions_are_safe(_table_interactions_are_safe):
		return false
	if not await _capture_checkpoint("open-table-prompt-dense-concealed"):
		return false
	return await _mulligan_result_and_payment_are_operable()


func _table_interactions_are_safe() -> bool:
	if (_node("Play/Board") as Control).is_visible_in_tree() \
			and main.find_child("VillainTable", true, false) != null:
		if not await _focused_board_area_is_visible():
			return false
		return await _mulligan_dock_is_safe()
	if OS.get_environment("MARVEL_SMOKE_VIEWPORT") == "1920x1080":
		_fail("the 1920 desktop opening prompt fell back instead of rendering VillainTable")
		return false
	var removed_sheet := main.find_child("CompleteChoiceSheet", true, false) as Control
	if removed_sheet != null and removed_sheet.is_visible_in_tree():
		return await _fallback_mulligan_sheet_is_focus_safe()
	if not await _keyboard_selection_is_operable():
		return false
	if not await _interaction_lifecycle_is_safe():
		return false
	if not await _event_presentation_is_nonblocking():
		return false
	return await _synchronization_preserves_history(false)


func _mulligan_dock_is_safe() -> bool:
	var submit := _attached(_attached_name(1, "Submit"))
	var history := main.find_child("ToggleHistory", true, false) as Button
	if submit == null or history == null or submit.disabled:
		_fail("the opening table has no operable card-local action or history drawer")
		return false
	if main.find_child("CompleteChoiceSheet", true, false) != null:
		_fail("the removed ordered action selector is still present on the desktop table")
		return false
	if not await _control_owns_point(history, _visible_control_rect(history).get_center()) \
			or not await _prepare_activation(submit):
		return false
	if not _control_is_fully_visible(history) or not _control_is_fully_visible(submit):
		_fail("a required table-object control is clipped")
		return false
	return await _synchronization_preserves_history(false)


func _play_seeded_journey() -> Dictionary:
	var direct_form_undo := main.has_meta("smoke_direct_form_undo")
	var multiplayer := OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true"
	var state := {
		"saw_mulligan": true,
		"saw_pass": false,
		"saw_end_phase": false,
		"saw_nonblocking_motion": false,
		"tested_active_motion_toggle": false,
		"captured_villain_phase": false,
		# The deterministic one-player journey owns the exact form/undo action.
		# Multiplayer exercises seat switching without hard-coding one seat's
		# identity anchor as the other seat's current action.
		"changed_form": direct_form_undo or multiplayer,
		"form_before_change": "",
		"tested_undo": direct_form_undo or multiplayer,
		"tested_attached_focus": false,
		"saw_attack_resolution": false,
		"decisions": 0,
	}
	while not _is_complete():
		if not await _play_one_decision(state):
			return {}
	return state


func _play_one_decision(state: Dictionary) -> bool:
	if state.decisions >= MAX_DECISIONS:
		_fail("the visible-control journey exceeded %d decisions" % MAX_DECISIONS)
		return false
	if not await _decision_controls_are_safe(state):
		return false
	var ending_player_phase := _observe_decision(state)
	if ending_player_phase and not await _capture_checkpoint("player-phase"):
		return false
	if not await _advance_visible_decision(state):
		return false
	state.decisions += 1
	await process_frame
	if not await _settle_decision(state.decisions):
		return false
	var had_tested_undo: bool = state.tested_undo
	if not await _undo_first_form_change(state):
		return false
	if not had_tested_undo and state.tested_undo:
		return true
	if not await _redesign_gate_motion_state_is_safe(_motion_state_is_safe, state):
		return false
	if not await _active_resolution_is_safe(state):
		return false
	return await _villain_history_checkpoint_is_safe(state)


func _attached_controls_are_safe(state: Dictionary) -> bool:
	if state.tested_attached_focus:
		return true
	return await _attached_control_focus_is_safe(state)


func _decision_controls_are_safe(state: Dictionary) -> bool:
	if not _visible_buttons_meet_pointer_floor():
		return false
	return await _attached_controls_are_safe(state)


func _observe_decision(state: Dictionary) -> bool:
	var context := main.find_child("ContextualDecision", true, false) as Control
	var text := _visible_text(context) if context != null else _visible_text(_decision())
	state.saw_mulligan = state.saw_mulligan or "discard and redraw" in text.to_lower()
	var ending_player_phase := "End Phase" in text
	state.saw_end_phase = state.saw_end_phase or ending_player_phase
	return ending_player_phase


func _advance_visible_decision(state: Dictionary) -> bool:
	if main.find_child("AstraTableSurface", true, false) != null:
		return await _advance_table_decision(state)
	return await _advance_fallback_decision(state)


func _advance_table_decision(state: Dictionary) -> bool:
	var identity_action := _attached(_attached_name(1, "Action"))
	if not state.changed_form and identity_action != null and not identity_action.disabled:
		state.form_before_change = "Peter Parker" \
				if "Peter Parker\nREC" in _visible_text(_play()) else "Spider-Man"
		if not await _pointer_activate_attached(identity_action):
			return false
		await process_frame
		if not await _choose_change_form() or not await _compose_table_decision():
			return false
		state.changed_form = true
		return true
	var decline := _attached("Card*Decline")
	if decline != null and not decline.disabled:
		state.saw_pass = true
		state.saw_end_phase = true
		return await _pointer_activate_attached(decline)
	return await _compose_table_decision()


func _choose_change_form() -> bool:
	var chooser := main.find_child("CardActionChoices", true, false) as Control
	if chooser == null or not chooser.is_visible_in_tree():
		return true
	var change := _visible_button_beginning(chooser, "Change Form")
	if change == null or not await _pointer_activate(change):
		return false
	await process_frame
	return true


func _advance_fallback_decision(state: Dictionary) -> bool:
	var change_form := _visible_button_beginning(_decision(), "Change Form")
	var pass_button := _visible_button(_decision(), "Pass / decline")
	if not state.changed_form and change_form != null and not change_form.disabled:
		state.form_before_change = "Peter Parker" \
			if "Peter Parker\nREC" in _visible_text(_play()) else "Spider-Man"
		if not await _mixed_submit_is_single_shot():
			return false
		state.changed_form = true
		return true
	if pass_button != null and not pass_button.disabled:
		state.saw_pass = true
		return await _pointer_activate(pass_button)
	return await _compose_visible_decision()


func _compose_table_decision() -> bool:
	var trace: Array[String] = []
	for selection in 8:
		var submit := _attached("Card*Submit")
		if submit != null and not submit.disabled:
			return await _pointer_activate_attached(submit)
		var choice := _first_action_choice()
		if choice != null:
			trace.append("chooser:%s" % choice.text)
			if not await _pointer_activate(choice): return false
			await process_frame
			continue
		var contextual := main.find_child("ContextAction*", true, false) as Button
		if contextual != null and contextual.is_visible_in_tree() and not contextual.disabled:
			trace.append("context:%s" % contextual.text)
			if not await _pointer_activate(contextual):
				return false
			await process_frame
			continue
		var control := _first_unselected_table_control()
		if control != null:
			trace.append("control:%s:%s" % [control.name, control.text])
		if control == null or not await _pointer_activate_attached(control):
			_fail("no card-local control can advance the current decision (%s)" % ", ".join(trace))
			return false
		await process_frame
	_fail("the card-local draft did not become executable")
	return false


func _first_action_choice() -> Button:
	var chooser := main.find_child("CardActionChoices", true, false) as Control
	if chooser == null or not chooser.is_visible_in_tree(): return null
	for button in _visible_buttons(chooser):
		if not button.disabled: return button
	return null


func _first_unselected_table_control() -> Button:
	for pattern in ["Card*Target", "Card*Cost", "Card*Generator", "Card*Action"]:
		for candidate in main.find_children(pattern, "Button", true, false):
			var button := candidate as Button
			if button != null and button.is_visible_in_tree() and not button.disabled \
					and not button.text.begins_with("✓"):
				return button
	return null


func _compose_visible_decision() -> bool:
	var submit := _submit_button()
	for selection in 3:
		if submit != null and not submit.disabled:
			break
		var choice := _first_enabled_target()
		if choice == null:
			choice = _first_enabled_choice()
		if choice == null:
			_fail("no visible control can advance the current decision")
			return false
		if not await _pointer_activate(choice):
			return false
		await process_frame
		submit = _submit_button()
	if submit == null or submit.disabled:
		_fail("the selected visible decision cannot be submitted")
		return false
	return await _pointer_activate(submit)


func _settle_decision(decisions: int) -> bool:
	if await _wait_for(func() -> bool:
		return _is_complete() or not _status().text.begins_with("DECISION SENT")):
		return true
	_fail("the engine did not reconcile decision %d" % decisions)
	return false


func _undo_first_form_change(state: Dictionary) -> bool:
	if not state.changed_form or state.tested_undo:
		return true
	if not await _show_history_tab():
		return false
	var history := _node("Play/Prompt/Margin/Stack/Workbench/History/EventLog") as RichTextLabel
	var undo := main.find_child("UndoLast", true, false) as Button
	if undo.disabled or "Spider-Man changed form." not in history.get_parsed_text():
		_fail("the reversible form change has no authoritative undo action: disabled=%s history=%s" % [
			undo.disabled,
			history.get_parsed_text(),
		])
		return false
	if "Undo to before this action" not in history.get_parsed_text():
		_fail("the reversible form change has no authoritative undo description")
		return false
	if not await _pointer_activate(undo):
		return false
	if not await _wait_for(func() -> bool:
		return not (_node("Play/Prompt/Margin/Stack/PromptHeader/Progress") as Label) \
			.text.begins_with("UNDOING")):
		_fail("undoing the form change did not settle")
		return false
	if state.form_before_change not in _visible_text(_play()):
		_fail("undoing the form change did not restore %s" % state.form_before_change)
		return false
	if not await _show_action_tab():
		return false
	state.tested_undo = true
	state.changed_form = false
	return true


func _show_history_tab() -> bool:
	var toggle := main.find_child("ToggleHistory", true, false) as Button
	if toggle != null:
		var log := _node("Play/Prompt/Margin/Stack/Workbench/History/EventLog") as Control
		if log.visible:
			return true
		if not await _activate_exposed_control_point(toggle):
			return false
		return await _wait_for(func() -> bool: return log.visible)
	var workbench := _node("Play/Prompt/Margin/Stack/Workbench") as TabContainer
	if workbench.current_tab == 1:
		return true
	return await _navigate_workbench_tab(workbench, KEY_RIGHT, 1)


func _show_action_tab() -> bool:
	var toggle := main.find_child("ToggleHistory", true, false) as Button
	if toggle != null:
		var log := _node("Play/Prompt/Margin/Stack/Workbench/History/EventLog") as Control
		if not log.visible:
			return true
		if not await _activate_exposed_control_point(toggle):
			return false
		return await _wait_for(func() -> bool: return not log.visible)
	var workbench := _node("Play/Prompt/Margin/Stack/Workbench") as TabContainer
	if workbench.current_tab == 0:
		return true
	return await _navigate_workbench_tab(workbench, KEY_LEFT, 0)


func _navigate_workbench_tab(workbench: TabContainer, key: Key, expected: int) -> bool:
	var tabs := workbench.get_tab_bar()
	tabs.grab_focus()
	await process_frame
	var press := InputEventKey.new()
	press.keycode = key
	press.pressed = true
	render_viewport.push_input(press)
	var release := InputEventKey.new()
	release.keycode = key
	render_viewport.push_input(release)
	await process_frame
	if workbench.current_tab == expected:
		return true
	_fail("keyboard navigation did not select the requested workbench tab")
	return false


func _motion_state_is_safe(state: Dictionary) -> bool:
	var skip := main.find_child("Skip", true, false) as Button
	if motion_enabled and not skip.disabled \
			and (_is_complete() or _first_enabled_choice() != null):
		state.saw_nonblocking_motion = true
		if not state.tested_active_motion_toggle:
			if not await _toggle_active_motion(skip):
				return false
			state.tested_active_motion_toggle = true
	if not motion_enabled and not _disabled_motion_is_settled(skip):
		return false
	return true


func _toggle_active_motion(skip: Button) -> bool:
	var history := (_node(
		"Play/Prompt/Margin/Stack/Workbench/History/EventLog") as RichTextLabel).text
	var motion := _node("Toolbar/Motion") as CheckButton
	motion.button_pressed = false
	motion.toggled.emit(false)
	await process_frame
	if not skip.disabled:
		_fail("disabling active motion did not settle playback")
		return false
	if history != (_node(
			"Play/Prompt/Margin/Stack/Workbench/History/EventLog") as RichTextLabel).text:
		_fail("disabling active motion changed history")
		return false
	motion.button_pressed = true
	motion.toggled.emit(true)
	return true
