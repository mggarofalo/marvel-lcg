extends "res://smoke/local_game_smoke_redesign_gate.gd"


func _terminal_table_is_safe(state: Dictionary) -> bool:
	if not _required_journey_paths_were_seen(state):
		return false
	if not await _synchronization_preserves_history(true):
		return false
	if not await _terminal_decision_is_safe():
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
	if not state.tested_attached_focus:
		_fail("the journey never reached an attached action control")
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
	if not await _wait_for(func() -> bool:
		return "DEFEAT" in _visible_text(_decision()).to_upper()):
		_fail("the null-prompt terminal decision copy does not identify defeat")
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
	if not (_node("Play/Board") as Control).is_visible_in_tree():
		_fail("the terminal outcome hid the settled table")
		return false
	var result := _node("Play/Prompt/Margin/Stack/Workbench/Action/LastResult") as Control
	var text := _visible_text(result).to_lower()
	if result.visible and ("villain won the game" in text or "players lost the game" in text):
		return true
	_fail("the terminal outcome did not remain in the primary action pane")
	return false


func _terminal_page_is_visible() -> bool:
	if not (_node("Play/Board") as Control).is_visible_in_tree():
		_fail("the terminal outcome hid the settled table")
		return false
	if main.find_child("VillainTable", true, false) != null:
		return await _fixed_terminal_dock_is_visible()
	if await _wait_for(func() -> bool:
		return _control_text_is_visible(_node("Title") as Control) \
			and _control_text_is_visible(_node("Description") as Control)):
		return true
	_fail("the responsive terminal table did not reveal its outcome")
	return false


func _fixed_terminal_dock_is_visible() -> bool:
	var fixed_page := main.get_node("Margin") as ScrollContainer
	fixed_page.scroll_vertical = 0
	fixed_page.set_deferred("scroll_vertical", 0)
	for _frame in 3:
		await process_frame
	if await _wait_for(func() -> bool:
		return _control_text_is_visible(_node(
			"Play/Prompt/Margin/Stack/PromptHeader/Heading") as Control) \
				and _control_text_is_visible(_node(
				"Play/Prompt/Margin/Stack/PromptHeader/Progress") as Control)):
		return true
	var heading := _node("Play/Prompt/Margin/Stack/PromptHeader/Heading") as Control
	var progress := _node("Play/Prompt/Margin/Stack/PromptHeader/Progress") as Control
	_fail("the fixed tabletop did not reveal its terminal outcome in the decision dock" \
		+ " heading=%s/%s progress=%s/%s" % [
			heading.get_global_rect(), _visible_control_rect(heading),
			progress.get_global_rect(), _visible_control_rect(progress),
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
