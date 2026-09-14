extends "res://smoke/local_game_smoke_lifecycle_checks.gd"

func _initialize() -> void:
	_prepare_art_pack()
	motion_enabled = OS.get_environment("MARVEL_SMOKE_MOTION") != "disabled"
	var viewport := OS.get_environment("MARVEL_SMOKE_VIEWPORT").split("x")
	if viewport.size() == 2:
		var fixed_viewport := SubViewport.new()
		fixed_viewport.size = Vector2i(int(viewport[0]), int(viewport[1]))
		fixed_viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
		root.add_child(fixed_viewport)
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
	await _configure_seeded_game()
	if not await _open_and_validate_table():
		return
	if OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true":
		print("LOCAL_GAME_SMOKE_OK two-player-seat-switch")
		main.queue_free()
		await process_frame
		quit(0)
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
	if OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true":
		return await _cooperative_seat_switch_is_safe()
	if not await _live_scale_rebuilds_the_decision():
		return false
	if not await _procedural_cards_are_safe():
		return false
	if not await _board_layout_is_resolved():
		return false
	if not await _table_interactions_are_safe():
		return false
	if not await _capture_checkpoint("open-table-prompt-dense-concealed"):
		return false
	return await _mulligan_result_and_payment_are_operable()


func _table_interactions_are_safe() -> bool:
	if main.find_child("VillainTable", true, false) != null:
		if not await _focused_board_area_is_visible():
			return false
		return await _mulligan_dock_is_safe()
	if main.find_child("CompleteChoiceSheet", true, false) != null:
		return await _fallback_mulligan_sheet_is_focus_safe()
	if not await _keyboard_selection_is_operable():
		return false
	if not await _interaction_lifecycle_is_safe():
		return false
	if not await _event_presentation_is_nonblocking():
		return false
	return await _synchronization_preserves_history(false)


func _mulligan_dock_is_safe() -> bool:
	var dock := _node("Play/Prompt") as Control
	var sheet := main.find_child("CompleteChoiceSheet", true, false) as Button
	var submit := _submit_button()
	if dock == null or sheet == null or submit == null or submit.disabled:
		_fail("the opening table has no operable compact decision dock")
		return false
	if not await _prepare_activation(sheet) or not await _prepare_activation(submit):
		return false
	if not _visible_control_rect(submit).has_point(submit.get_global_rect().get_center()):
		_fail("the opening commit is not fixed inside the viewport")
		return false
	return await _synchronization_preserves_history(false)


func _play_seeded_journey() -> Dictionary:
	var state := {
		"saw_mulligan": true,
		"saw_pass": false,
		"saw_end_phase": false,
		"saw_nonblocking_motion": false,
		"tested_active_motion_toggle": false,
		"captured_villain_phase": false,
		"changed_form": false,
		"tested_undo": false,
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
	if not _visible_buttons_meet_pointer_floor():
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
	if not await _motion_state_is_safe(state):
		return false
	if not await _active_resolution_is_safe(state):
		return false
	return await _villain_history_checkpoint_is_safe(state)


func _observe_decision(state: Dictionary) -> bool:
	var text := _visible_text(_decision())
	state.saw_mulligan = state.saw_mulligan or "discard and redraw" in text.to_lower()
	var ending_player_phase := "End Phase" in text
	state.saw_end_phase = state.saw_end_phase or ending_player_phase
	return ending_player_phase


func _advance_visible_decision(state: Dictionary) -> bool:
	var change_form := _visible_button_beginning(_decision(), "Change Form")
	var pass_button := _visible_button(_decision(), "Pass / decline")
	if not state.changed_form and change_form != null and not change_form.disabled:
		if not await _mixed_submit_is_single_shot():
			return false
		state.changed_form = true
		return true
	if pass_button != null and not pass_button.disabled:
		state.saw_pass = true
		return await _pointer_activate(pass_button)
	return await _compose_visible_decision()


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
	var undo := _node(
		"Play/Prompt/Margin/Stack/Workbench/History/EventHeader/UndoLast") as Button
	if undo.disabled or "Spider-Man changed form." not in history.get_parsed_text():
		_fail("the reversible form change has no authoritative undo action")
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
	if "Peter Parker" not in _visible_text(_play()):
		_fail("undoing the form change did not restore alter-ego form")
		return false
	if not await _show_action_tab():
		return false
	state.tested_undo = true
	state.changed_form = false
	return true


func _show_history_tab() -> bool:
	var workbench := _node("Play/Prompt/Margin/Stack/Workbench") as TabContainer
	if workbench.current_tab == 1:
		return true
	return await _navigate_workbench_tab(workbench, KEY_RIGHT, 1)


func _show_action_tab() -> bool:
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
	var skip := _node("Play/Prompt/Margin/Stack/Workbench/History/EventHeader/Skip") as Button
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


func _active_resolution_is_safe(state: Dictionary) -> bool:
	var active := _node("Play/Prompt/Margin/Stack/ActiveResolution") as Control
	if not active.visible:
		return true
	var text := _visible_text(active).to_lower()
	if "enemy attack" not in text or "interrupt window" not in text:
		return true
	state.saw_attack_resolution = true
	if "rhino" not in text or "spider-man" not in text:
		_fail("the attack resolution does not name its actor and target")
		return false
	if not await _capture_checkpoint("attack-interrupt"):
		return false
	if not state.captured_villain_phase:
		if not await _capture_checkpoint("villain-phase"):
			return false
		state.captured_villain_phase = true
	return true


func _villain_history_checkpoint_is_safe(state: Dictionary) -> bool:
	if state.captured_villain_phase or _is_complete():
		return true
	var history := (_node(
		"Play/Prompt/Margin/Stack/Workbench/History/EventLog") as RichTextLabel) \
		.get_parsed_text().to_lower()
	if "villain phase" not in history:
		return true
	if not await _capture_checkpoint("villain-phase"):
		return false
	state.captured_villain_phase = true
	return true


func _terminal_table_is_safe(state: Dictionary) -> bool:
	if not _required_journey_paths_were_seen(state):
		return false
	if not await _synchronization_preserves_history(true):
		return false
	if not _terminal_decision_is_safe():
		return false
	if not _terminal_history_is_safe():
		return false
	if not _terminal_result_is_safe():
		return false
	if not await _terminal_page_is_visible():
		return false
	if not await _capture_checkpoint("terminal"):
		return false
	return await _dismiss_terminal_result()


func _required_journey_paths_were_seen(state: Dictionary) -> bool:
	if not state.saw_mulligan or not state.saw_pass or not state.saw_end_phase:
		_fail("the journey missed a required visible decision path")
		return false
	if not state.changed_form or not state.tested_undo:
		_fail("the journey did not change form again after proving undo")
		return false
	if not state.saw_attack_resolution or not state.captured_villain_phase:
		_fail("the journey did not expose its attack and villain-phase checkpoints")
		return false
	if motion_enabled and not state.saw_nonblocking_motion:
		_fail("the journey never exposed an operable prompt during event motion")
		return false
	if motion_enabled and not state.tested_active_motion_toggle:
		_fail("the journey never disabled event motion during active playback")
		return false
	if not motion_enabled and state.saw_nonblocking_motion:
		_fail("the motion-disabled journey exposed active event playback")
		return false
	return true


func _terminal_decision_is_safe() -> bool:
	if "VILLAIN WINS" not in _status().text and "PLAYERS LOSE" not in _status().text:
		_fail("the terminal UI did not report the seeded loss")
		return false
	var decision := _visible_text(_decision()).to_upper()
	var prompt := _visible_text(_node("Play/Prompt/Margin/Stack/PromptHeader")).to_upper()
	if "DEFEAT" not in decision:
		_fail("the null-prompt terminal decision copy does not identify defeat")
		return false
	if "VILLAIN WON" not in decision and "PLAYERS LOST" not in decision:
		_fail("the null-prompt terminal decision copy does not identify the loss")
		return false
	if "VILLAIN WON" not in prompt and "PLAYERS LOST" not in prompt:
		_fail("the terminal prompt header does not identify the loss")
		return false
	if _node("Status").theme_type_variation != &"DangerStatusPanel":
		_fail("the loss did not receive the semantic danger treatment")
		return false
	return true


func _terminal_history_is_safe() -> bool:
	var event_log := _node("Play/Prompt/Margin/Stack/Workbench/History/EventLog") as RichTextLabel
	var text := event_log.get_parsed_text().strip_edges()
	if text.is_empty() or text == "No events yet.":
		_fail("the visible event log is empty")
		return false
	if "villain won the game" not in text.to_lower() \
			and "players lost the game" not in text.to_lower():
		_fail("the terminal outcome did not remain in recent history")
		return false
	return true


func _terminal_result_is_safe() -> bool:
	var result := _node("Play/Prompt/Margin/Stack/Workbench/Action/LastResult") as Control
	var text := _visible_text(result).to_lower()
	if result.visible and ("villain won the game" in text or "players lost the game" in text):
		return true
	_fail("the terminal outcome did not remain in the primary action pane")
	return false


func _terminal_page_is_visible() -> bool:
	if await _wait_for(func() -> bool:
		return _control_text_is_visible(_node("Title") as Control) \
			and _control_text_is_visible(_node("Description") as Control)):
		return true
	var title := _node("Title") as Control
	var description := _node("Description") as Control
	var page := main.get_node("Margin") as ScrollContainer
	_fail("the terminal page did not reveal its outcome and explanation" \
		+ "\nPage rect: %s scroll: %d" % [page.get_global_rect(), page.scroll_vertical] \
		+ "\nTitle rect: %s visible: %s" % [title.get_global_rect(), _visible_control_rect(title)] \
		+ "\nDescription rect: %s visible: %s" % [
			description.get_global_rect(),
			_visible_control_rect(description),
		])
	return false


func _dismiss_terminal_result() -> bool:
	var result := _node("Play/Prompt/Margin/Stack/Workbench/Action/LastResult") as Control
	var dismiss := _node(
		"Play/Prompt/Margin/Stack/Workbench/Action/LastResult/Margin/Copy/Header/Dismiss") as Button
	if not await _pointer_activate(dismiss):
		return false
	await process_frame
	if result.visible:
		_fail("the latest result cannot be dismissed")
		return false
	return true
