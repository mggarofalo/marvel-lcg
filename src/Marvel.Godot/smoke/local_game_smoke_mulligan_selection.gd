extends "res://smoke/local_game_smoke_direct_intent.gd"

func _clearing_mulligan_replacements_preserves_task() -> bool:
	var toggles := _hand_surface().find_children("MulliganDiscard*", "Button", true, false)
	if toggles.is_empty(): return false
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if not await _pointer_activate(toggles[0] as Button): return false
	var clear := main.find_child("ContextualCancelDraft", true, false) as Button
	if clear == null or clear.text != "Clear replacements" or not await _pointer_activate(clear):
		_fail("opening-hand replacements cannot be cleared without abandoning the task")
		return false
	var submit := _task_commit()
	if submit == null or submit.disabled or submit.text != "Keep hand" \
			or (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("clearing staged replacements changed the game or hid the required Keep hand commitment")
		return false
	for candidate in _hand_surface().find_children("MulliganDiscard*", "Button", true, false):
		if (candidate as Button).button_pressed:
			_fail("clearing replacements left a hand card staged")
			return false
	return true


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
	var mansion := _mulligan_discard("Avengers Mansion")
	var aunt := _mulligan_card("Aunt May")
	var kick := _mulligan_card("Swinging Web Kick")
	if mansion == null or aunt == null or kick == null:
		_fail("the opening hand has no explicit discard controls for the seeded cards")
		return false
	if not await _keyboard_activate(mansion):
		return false
	if not await _keyboard_activate(_mulligan_discard("Aunt May")):
		return false
	if not await _drag_capture_is_safe():
		return false
	if not await _keyboard_activate(_mulligan_discard("Swinging Web Kick")):
		return false
	return _mulligan_discard("Avengers Mansion").text == "✓" \
		and _mulligan_discard("Swinging Web Kick").text == "✓"


func _drag_capture_is_safe() -> bool:
	var card := _mulligan_card("Swinging Web Kick")
	if card == null:
		_fail("the opening hand lost Swinging Web Kick before its drag probe")
		return false
	if not await _drag_mulligan_to_discard(card):
		return false
	# The source card owns this press while the pointer enters the destination.
	# One callback changes the unchecked toggle once; two callbacks would return
	# it to unchecked.
	if not _mulligan_discard("Swinging Web Kick").button_pressed:
		_fail("a source-card drag released over discard did not select exactly once")
		return false
	if not await _keyboard_activate(_mulligan_discard("Swinging Web Kick")):
		return false
	card = _mulligan_card("Swinging Web Kick")
	if card == null:
		_fail("the opening hand lost Swinging Web Kick after its choice-sheet probe")
		return false
	if not await _drag_mulligan_outside_discard(card):
		return false
	if _mulligan_discard("Swinging Web Kick").button_pressed:
		_fail("a source-card drag released outside discard changed the opening-hand draft")
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
	var tabletop := _mulligan_discard(title)
	if tabletop == null or tabletop.button_pressed != select:
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


func _mulligan_discard(title: String) -> Button:
	var card := _mulligan_card(title)
	return card.find_child("MulliganDiscard*", true, false) as Button if card != null else null


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
