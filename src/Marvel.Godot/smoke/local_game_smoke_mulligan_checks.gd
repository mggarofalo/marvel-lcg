extends "res://smoke/local_game_smoke_mulligan_selection.gd"

func _mulligan_result_and_payment_are_operable() -> bool:
	if not await _select_mulligan_cards():
		return false
	if not await _submit_mulligan():
		return false
	if not await _mulligan_result_is_operable():
		return false
	if OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true":
		if not await _dismiss_mulligan_result() \
				or not await _complete_second_opening_hand() \
				or not await _post_mulligan_desktop_resize_is_safe():
			return false
	if not await _post_mulligan_table_is_safe():
		return false
	var hand_card := _hand_surface().find_child(
		"ProceduralCard*", true, false) as Control
	if hand_card == null:
		_fail("the post-mulligan hand has no card for the pinned-inspector probe")
		return false
	if not await _pinned_inspector_mouse_filter_is_safe(hand_card):
		return false
	if not await _action_card_preview_is_safe():
		return false
	return await _direct_table_journey_is_operable()


func _post_mulligan_table_is_safe() -> bool:
	if not await _redesign_gate_post_mulligan_is_safe():
		return false
	return await _upcoming_stages_are_safe()


func _cooperative_seat_switch_is_safe() -> bool:
	var strip := main.find_child("SpatialSeatSummaries", true, false) as Control
	var switch_two := main.find_child("SeatSwitch1", true, false) as Button
	var switch_one := main.find_child("SeatSwitch0", true, false) as Button
	if strip == null or switch_two == null or switch_one == null \
			or "CAPTAIN MARVEL" not in _visible_text(strip).to_upper():
		_fail("the cooperative opening table has no public second-seat summary")
		return false
	if not await _pointer_activate(switch_two):
		return false
	var expanded := _expanded_seat_surface()
	var heading := _hand_heading()
	var destination := main.find_child("PileEmptyPlayerDiscard", true, false) as Control
	if destination == null:
		destination = main.find_child("ExpandedDiscardPile", true, false) as Control
	var cards: Array[Node] = []
	for candidate in _hand_surface().find_children(
			"ProceduralCard*", "PanelContainer", true, false):
		if candidate.has_meta("spatial_hand_index"):
			cards.append(candidate)
	if expanded == null or heading == null or destination == null \
			or int(expanded.get_meta("expanded_seat", -1)) != 1 \
			or not heading.text.to_lower().begins_with("player 1 opening hand") \
			or not _hand_surface().is_ancestor_of(destination) \
			or cards.size() != 6 or _task_commit() == null:
		_fail("switching public workspaces changed the prompt-owner surface: " \
				+ "expanded=%s heading=%s destination=%s ancestor=%s hand_cards=%d submit=%s" % [
				expanded != null and int(expanded.get_meta("expanded_seat", -1)) == 1,
				heading != null and heading.text.to_lower().begins_with("player 1 opening hand"),
				destination != null,
				destination != null and _hand_surface().is_ancestor_of(destination),
				cards.size(), _task_commit() != null])
		return false
	switch_one = main.find_child("SeatSwitch0", true, false) as Button
	if switch_one == null or not await _pointer_activate(switch_one):
		return false
	return main.find_child("SeatSwitch0", true, false) is Button \
		and _task_commit() != null


func _submit_mulligan() -> bool:
	var submit := _task_commit()
	if submit == null or submit.disabled or not ("replace" in submit.text.to_lower() or "keep hand" in submit.text.to_lower()):
		_fail("the three-card mulligan cannot be submitted")
		return false
	if not await _pointer_activate(submit):
		return false
	if OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true":
		if not await _wait_for(func() -> bool:
			var heading := _hand_heading()
			return heading != null and heading.text.to_lower().begins_with("player 2 opening hand") \
				and not _hand_surface().find_children(
					"MulliganDiscard*", "Button", true, false).is_empty()):
			_fail("the first player's submission did not hand the opening decision to player 2")
			return false
		return true
	if not await _wait_for(func() -> bool:
		return _attached(_attached_name(IDENTITY, "Action")) != null):
		_fail("the seeded mulligan did not reach the player-action affordances")
		return false
	return true


func _complete_second_opening_hand() -> bool:
	if not await _wait_for_result_motion(): return false
	var toggles := _hand_surface().find_children(
		"MulliganDiscard*", "Button", true, false)
	var second_toggle := toggles[0] as Button if not toggles.is_empty() else null
	if second_toggle == null or not await _activate_exposed_control_point(second_toggle):
		_fail("the second player's opening hand has no operable discard target")
		return false
	var submit := _task_commit()
	if submit == null or submit.disabled or not ("replace" in submit.text.to_lower() or "keep hand" in submit.text.to_lower()):
		_fail("the second player's selected mulligan cannot be submitted once")
		return false
	if not await _pointer_activate(submit):
		return false
	if not await _wait_for_result_motion(): return false
	var player_one := _visible_seat_switch(0)
	if player_one != null and not player_one.disabled and not await _pointer_activate(player_one):
		_fail("the completed cooperative mulligan could not restore player one's tableau")
		return false
	if not await _wait_for(func() -> bool:
		var caption := _expanded_seat_surface()
		return caption != null and int(caption.get_meta("expanded_seat", -1)) == 0):
		_fail("the action owner was not restored as the expanded tableau")
		return false
	if not await _wait_for(func() -> bool:
		return _attached(_attached_name(IDENTITY, "Action")) != null):
		_fail("the completed cooperative mulligan did not expose player one's actions")
		return false
	return true


func _wait_for_result_motion() -> bool:
	# Result cues can focus either tableau. Acquire input controls only after
	# the public playback state settles and its queued layout has run.
	if not await _wait_for(func() -> bool:
		var skip := main.find_child("Skip", true, false) as Button
		return skip != null and skip.disabled):
		_fail("the mulligan result animation did not finish")
		return false
	await process_frame
	return true


func _dismiss_mulligan_result() -> bool:
	if not await _set_mulligan_history_expanded(true):
		return false
	var dismiss := main.find_child("DismissHistoryResult", true, false) as Button
	if dismiss == null or not await _keyboard_activate(dismiss):
		var drawer := main.find_child("ToggleHistory", true, false) as Button
		var focused := render_viewport.gui_get_focus_owner()
		print("MULLIGAN_DISMISS_FOCUS_FAILURE toggle=%s visible=%s dismiss_id=%s focus=%s focus_id=%s" % [
			drawer.text if drawer != null else "missing",
			dismiss != null and dismiss.is_visible_in_tree(),
			dismiss.get_instance_id() if dismiss != null else 0,
			focused.name if focused != null else "none",
			focused.get_instance_id() if focused != null else 0])
		_fail("the first player's mulligan result cannot be dismissed before the next seat answers")
		return false
	if not await _wait_for(func() -> bool:
		var latest := main.find_child("LatestResult", true, false) as Label
		return latest != null and latest.text.is_empty()):
		return false
	return await _set_mulligan_history_expanded(false)


func _mulligan_result_is_operable() -> bool:
	if not await _set_mulligan_history_expanded(true):
		return false
	var history := _node("Play/Prompt/Margin/Stack/Workbench/History/EventLog") as RichTextLabel
	var latest := main.find_child("LatestResult", true, false) as Label
	var summary := history.get_parsed_text() + " " + (latest.text if latest != null else "")
	if "Avengers Mansion" not in summary or "Aunt May" not in summary \
			or "Swinging Web Kick" not in summary:
		_fail("the expanded history omits the discarded cards: %s" % summary)
		return false
	if "Daredevil" not in summary or "Black Cat" not in summary or "Jessica Jones" not in summary:
		_fail("the expanded history omits the drawn cards: %s" % summary)
		return false
	await process_frame
	await process_frame
	var surface := main.find_child("AstraTableSurface", true, false) as Control
	var prompt := _node("Play/Prompt") as Control
	var viewport_rect := Rect2(Vector2.ZERO, main.size)
	var surface_rect := surface.get_global_rect() if surface != null else Rect2()
	var prompt_rect := prompt.get_global_rect() if prompt != null else Rect2()
	if surface == null or prompt == null \
			or surface_rect.position.x < viewport_rect.position.x - 0.5 \
			or surface_rect.end.x > viewport_rect.end.x + 0.5 \
			or prompt_rect.position.x < viewport_rect.position.x - 0.5 \
			or prompt_rect.end.x > viewport_rect.end.x + 0.5:
		_fail("expanded history displaced the table or drawer: table=%s history=%s viewport=%s" % [
			surface_rect,
			prompt_rect,
			viewport_rect,
		])
		return false
	if not await _capture_checkpoint("mulligan-result"):
		return false
	return await _set_mulligan_history_expanded(false)


func _set_mulligan_history_expanded(expanded: bool) -> bool:
	# Observe each public transition before sending another toggle. Visibility
	# alone can still describe the previous deferred layout.
	if not await _wait_for(func() -> bool:
		return _mulligan_history_matches(not expanded)):
		_fail("history did not settle before its mulligan toggle")
		return false
	var drawer := main.find_child("ToggleHistory", true, false) as Button
	var pressed := [0]
	var observe := func() -> void: pressed[0] += 1
	drawer.pressed.connect(observe)
	var activated := await _activate_exposed_control_point(drawer)
	drawer.pressed.disconnect(observe)
	if not activated or pressed[0] != 1:
		_fail("the mulligan history toggle did not receive its pointer activation")
		return false
	if not await _wait_for(func() -> bool:
		return _mulligan_history_matches(expanded)):
		_fail("history did not reach its requested mulligan layout")
		return false
	return true


func _mulligan_history_matches(expanded: bool) -> bool:
	var drawer := main.find_child("ToggleHistory", true, false) as Button
	var dismiss := main.find_child("DismissHistoryResult", true, false) as Button
	return drawer != null and dismiss != null \
		and drawer.text == ("Collapse history" if expanded else "History") \
		and dismiss.is_visible_in_tree() == expanded


func _result_toggle_is_operable(summary: Label) -> bool:
	var toggle := _node(
		"Play/Prompt/Margin/Stack/Workbench/Action/LastResult/Margin/Copy/Header/Toggle") as Button
	if summary.visible or toggle.text != "Expand":
		_fail("the transient result does not begin compact and inspectable")
		return false
	if not await _activate_exposed_control_point(toggle):
		return false
	if not summary.visible or toggle.text != "Collapse":
		_fail("the transient result cannot be expanded")
		return false
	if not await _activate_exposed_control_point(toggle):
		return false
	if summary.visible or toggle.text != "Expand":
		_fail("the transient result cannot be collapsed")
		return false
	return true


func _start_web_shooter_draft() -> bool:
	var web_shooter := _web_shooter_action()
	if web_shooter == null and not await _open_card_play_menu():
		return false
	web_shooter = _web_shooter_action()
	if web_shooter == null or web_shooter.disabled:
		_fail("Web-Shooter is not playable after the mulligan")
		return false
	if _web_shooter_draft_is_prepared():
		_fail("the isolated preview probe leaked its Web-Shooter draft into payment")
		return false
	if not await _pointer_activate(web_shooter):
		return false
	if not await _wait_for_web_shooter_draft(
		"the post-mulligan action draft is not Play Web-Shooter"):
		return false
	await process_frame
	await process_frame
	if not _web_shooter_draft_is_prepared():
		_fail("the post-mulligan action draft changed before Web-Shooter payment")
		return false
	var result := _node("Play/Prompt/Margin/Stack/Workbench/Action/LastResult") as Control
	if result.visible:
		_fail("opening a new draft did not clear the transient result")
		return false
	return true


func _payment_is_keyboard_operable() -> bool:
	if not _web_shooter_draft_is_prepared():
		_fail("the payment controls are not bound to the prepared Play Web-Shooter draft")
		return false
	var generators := _decision().find_children("Resource*", "Button", true, false)
	if generators.is_empty():
		_fail("Web-Shooter exposes no post-mulligan payment generators")
		return false
	var generator := generators[0] as Button
	generator.grab_focus()
	await process_frame
	await process_frame
	if not _web_shooter_draft_is_prepared():
		_fail("the prepared Play Web-Shooter draft changed while payment focus settled")
		return false
	if render_viewport.gui_get_focus_owner() != generator:
		_fail("Web-Shooter's post-mulligan resource control cannot receive focus")
		return false
	if _visible_control_rect(generator).size.y < _scaled_metric(24):
		_fail("Web-Shooter's post-mulligan resource control is clipped")
		return false
	var press := InputEventAction.new()
	press.action = &"ui_accept"
	press.pressed = true
	render_viewport.push_input(press)
	await process_frame
	press.pressed = false
	render_viewport.push_input(press)
	await process_frame
	await process_frame
	if not _web_shooter_draft_is_prepared():
		_fail("the prepared Play Web-Shooter draft changed while payment was entered")
		return false
	var progress := _node("Play/Prompt/Margin/Stack/PromptHeader/Progress") as Label
	if "PAYMENT 1/1 ICONS" not in progress.text or "READY" not in progress.text:
		_fail("Peter Parker's post-mulligan resource cannot complete Web-Shooter's payment")
		return false
	return await _capture_checkpoint("post-mulligan-payment")
