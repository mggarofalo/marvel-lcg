extends "res://smoke/local_game_smoke_direct_intent.gd"

func _clearing_mulligan_replacements_preserves_task() -> bool:
	var card := _mulligan_card("Avengers Mansion")
	if card == null: return false
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	if not await _keyboard_stage_card(card): return false
	if not await preload("res://smoke/native_scale_checkpoint.gd").requested(self, "mulligan" ): return false
	card = _mulligan_card("Avengers Mansion")
	var clear := main.find_child("ContextualCancelDraft", true, false) as Button
	if clear == null or not await _pointer_activate(clear):
		_fail("opening-hand replacements cannot be cleared without abandoning the task")
		return false
	var submit := _task_commit()
	if submit == null or submit.disabled or submit.text != "Keep hand" \
		or str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision:
		_fail("clearing replacements changed the game or hid Keep hand")
		return false
	return not bool(card.get_meta("mulligan_staged", false))


func _fallback_mulligan_sheet_is_focus_safe() -> bool:
	var target := await _open_fallback_mulligan_sheet()
	if target == null:
		return false
	var target_name := target.name
	if not await _pointer_activate(target):
		return false
	var close := main.find_child("CloseChoiceSheet", true, false) as Button
	if close == null or not await _keyboard_activate(close):
		_fail("the complete choice sheet cannot return by keyboard")
		return false
	if not await _wait_for(func() -> bool:
		var restored := main.find_child("CompleteChoiceSheet", true, false) as Button
		return restored != null and render_viewport.gui_get_focus_owner() == restored):
		_fail("closing the complete choice sheet did not restore the opening-hand focus")
		return false
	if not await _keyboard_activate(main.find_child("CompleteChoiceSheet", true, false) as Button):
		return false
	var restored_target := _decision().find_child(target_name, true, false) as Button
	if restored_target == null or not restored_target.text.begins_with("✓"):
		_fail("returning to the generic choice sheet lost its shared opening-hand draft")
		return false
	if not await _pointer_activate(restored_target):
		return false
	close = main.find_child("CloseChoiceSheet", true, false) as Button
	if close == null or not await _keyboard_activate(close):
		_fail("the generic choice sheet cannot return after restoring its draft")
		return false
	return true


func _open_fallback_mulligan_sheet() -> Button:
	var review := main.find_child("CompleteChoiceSheet", true, false) as Button
	if review == null or not await _keyboard_activate(review):
		_fail("the generic opening-choice sheet cannot receive keyboard focus")
		return null
	var target := _first_enabled_choice()
	if target == null or not target.name.begins_with("Target") \
			or render_viewport.gui_get_focus_owner() != target:
		_fail("opening the complete choice sheet did not focus its first canonical target")
		return null
	var submit := _decision().find_child("Submit", true, false) as Button
	if submit == null or not _control_is_fully_visible(submit) or _task_commit() != null:
		await _capture_checkpoint("complete-choice-sheet-failure")
		_fail("complete choices lost its visible canonical commitment or duplicated the task commitment: submit=%s visible=%s task=%s" % [submit.get_global_rect() if submit != null else Rect2(), _visible_control_rect(submit) if submit != null else Rect2(), _task_commit() != null])
		return null
	if not await _capture_checkpoint("complete-choice-sheet"):
		return null
	return target


func _select_mulligan_cards() -> bool:
	if not _active_cues_leave_printed_values_readable(): return false
	if main.find_child("VillainTable", true, false) == null:
		return await _select_mulligan_cards_from_fallback()
	for title in ["Avengers Mansion", "Aunt May"]:
		if not await _keyboard_stage_card(_mulligan_card(title)): return false
	if not await _drag_capture_is_safe(): return false
	return await _keyboard_stage_card(_mulligan_card("Swinging Web Kick"))


func _keyboard_stage_card(card: Control) -> bool:
	if card == null: return false
	card.grab_focus()
	await process_frame
	var press := InputEventKey.new()
	press.keycode = KEY_SPACE
	press.pressed = true
	render_viewport.push_input(press)
	press = InputEventKey.new()
	press.keycode = KEY_SPACE
	press.pressed = false
	render_viewport.push_input(press)
	await process_frame
	await process_frame
	return true


func _drag_capture_is_safe() -> bool:
	var card := _mulligan_card("Swinging Web Kick")
	if card == null: return false
	var original := card.global_position
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	if not await _drag_mulligan_to_discard(card): return false
	await preload("res://smoke/gesture_settlement.gd").hand(self)
	if not bool(card.get_meta("mulligan_staged", false)) or card.global_position.distance_to(original) < 40:
		_fail("mulligan drop did not physically move the selected card out of the hand")
		return false
	if not await _drag_mulligan_to_discard(card): return false
	if not bool(card.get_meta("mulligan_staged", false)):
		_fail("repeat discard drop toggled the staged card back")
		return false
	await preload("res://smoke/gesture_settlement.gd").hand(self)
	var hand := main.find_child("HandDropArea", true, false) as Control
	if hand == null: hand = main.find_child("SpatialHandArea", true, false) as Control
	if hand == null:
		_fail("the hand has no reversible staging destination")
		return false
	if not await _drag_mulligan(card, hand.get_global_rect().get_center()): return false
	await preload("res://smoke/gesture_settlement.gd").hand(self)
	if bool(card.get_meta("mulligan_staged", false)) or str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision:
		_fail("moving a staged card back failed to preserve the uncommitted game")
		return false
	return true


func _sheet_selection_stays_bound(title: String, select: bool) -> bool:
	var sheet := main.find_child("CompleteChoiceSheet", true, false) as Button
	if sheet == null or not await _pointer_activate(sheet):
		_fail("the complete opening-choice sheet could not open")
		return false
	var target := _mulligan_target(title)
	if target == null or not await _pointer_activate(target):
		_fail("the complete opening-choice sheet has no target for %s" % title)
		return false
	var close := main.find_child("CloseChoiceSheet", true, false) as Button
	if close == null or not await _pointer_activate(close):
		_fail("the complete opening-choice sheet cannot return to the table")
		return false
	var tabletop := _mulligan_card(title)
	if tabletop == null or bool(tabletop.get_meta("mulligan_staged", false)) != select:
		_fail("the complete opening-choice sheet and tabletop toggles diverged for %s" % title)
		return false
	return true


func _select_mulligan_cards_from_fallback() -> bool:
	var sheet := main.find_child("CompleteChoiceSheet", true, false) as Button
	if sheet != null:
		if not await _pointer_activate(sheet):
			return false
	else:
		var mulligan := _visible_button_beginning(_decision(), "Choose cards to discard and redraw")
		if mulligan == null:
			mulligan = _visible_button_beginning(_decision(), "✓ Choose cards to discard and redraw")
		if mulligan == null or mulligan.disabled:
			_fail("the seeded opening hand has no operable mulligan action")
			return false
		if not mulligan.text.begins_with("✓") and not await _pointer_activate(mulligan):
			return false
	await process_frame
	for title in ["Avengers Mansion", "Aunt May", "Swinging Web Kick"]:
		var target := _mulligan_target(title)
		if target == null or not await _pointer_activate(target):
			_fail("the seeded mulligan cannot select %s" % title)
			return false
		await process_frame
	return true


func _mulligan_card(title: String) -> Control:
	for candidate in _hand_surface().find_children("ProceduralCard*", "", true, false):
		var card := candidate as Control
		var name := card.find_child("Title", true, false) as Label
		if name != null and name.text == title:
			return card
	return null


func _drag_mulligan_to_discard(card: Control) -> bool:
	# Use the fixed discard destination in the near player area. The compact
	# duplicate inside the horizontally scrolling hand is a click/tap cue, not
	# a reliable cross-scroll drag destination.
	var discard := main.find_child("PileEmptyPlayerDiscard", true, false) as Control
	if discard == null:
		discard = main.find_child("ExpandedDiscardPile", true, false) as Control
	if discard == null:
		# A projected pile retains its normal area identity even while empty.
		for area_node in main.find_children("Area*", "PanelContainer", true, false):
			var area := area_node as Control
			if "DISCARD PILE" in _visible_text(area):
				discard = area
				break
	if discard == null:
		discard = main.find_child("MulliganDiscardPile", true, false) as Control
	if discard == null or not await _prepare_activation(discard):
		_fail("the player discard place is not a reachable mulligan drop destination")
		return false
	return await _drag_mulligan(card, _visible_control_rect(discard).get_center())


func _drag_mulligan_outside_discard(card: Control) -> bool:
	if card == null:
		return false
	# This remains on the source card while travelling farther than the drag
	# threshold, so it is outside every registered drop destination.
	return await _drag_mulligan(card, _visible_control_rect(card).get_center() + Vector2(12, 0))


func _drag_mulligan(card: Control, finish: Vector2) -> bool:
	if card == null or not card.is_visible_in_tree():
		return false
	var start := await _exposed_card_body_point(card)
	if start == Vector2.INF:
		_fail("the mulligan card has no exposed body point for drag capture")
		return false
	var discard := main.find_child("PileEmptyPlayerDiscard", true, false) as Control
	var discard_variation := discard.theme_type_variation if discard != null else &""
	var discard_bounds := discard.get_global_rect() if discard != null else Rect2()
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = start
	press.global_position = start
	render_viewport.push_input(press, true)
	var move := InputEventMouseMotion.new()
	move.position = finish
	move.global_position = finish
	render_viewport.push_input(move, true)
	await process_frame
	if discard != null and (discard.theme_type_variation != discard_variation or discard.get_global_rect() != discard_bounds):
		_fail("mulligan target highlighting changed the discard silhouette or layout")
		return false
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.position = finish
	release.global_position = finish
	render_viewport.push_input(release, true)
	await process_frame
	return true


func _mulligan_target(title: String) -> Button:
	for candidate in _visible_buttons(_decision()):
		if "replace" in candidate.text.to_lower() and title in candidate.text:
			return candidate
	return null
