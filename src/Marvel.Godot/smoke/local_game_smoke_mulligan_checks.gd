extends "res://smoke/local_game_smoke_direct_journey.gd"

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
	var hand_card := (_node("Play/Board/HandShelf") as Control).find_child(
		"ProceduralCard", true, false) as Control
	if hand_card == null:
		_fail("the post-mulligan hand has no card for the pinned-inspector probe")
		return false
	if not await _pinned_inspector_mouse_filter_is_safe(hand_card):
		return false
	if not await _action_card_preview_is_safe():
		return false
	return await _direct_table_journey_is_operable()


func _fallback_mulligan_sheet_is_focus_safe() -> bool:
	var review := main.find_child("CompleteChoiceSheet", true, false) as Button
	if review == null or not await _keyboard_activate(review):
		_fail("the generic opening-choice sheet cannot receive keyboard focus")
		return false
	var target := _first_enabled_choice()
	if target == null or not target.name.begins_with("Target") \
			or render_viewport.gui_get_focus_owner() != target:
		_fail("opening the complete choice sheet did not focus its first canonical target")
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


func _cooperative_seat_switch_is_safe() -> bool:
	var strip := main.find_child("SeatStrip", true, false) as Control
	var switch_two := main.find_child("SeatSwitch1", true, false) as Button
	var switch_one := main.find_child("SeatSwitch0", true, false) as Button
	if strip == null or switch_two == null or switch_one == null \
			or "CAPTAIN MARVEL" not in _visible_text(strip).to_upper():
		_fail("the cooperative opening table has no public second-seat summary")
		return false
	if not await _pointer_activate(switch_two):
		return false
	var expanded := main.find_child("PlayerTable", true, false) as Control
	var heading := _node("Play/Board/HandShelf/Margin/Stack/Heading") as Label
	var destination := main.find_child("MulliganDiscardPile", true, false) as Control
	var cards := (_node("Play/Board/HandShelf") as Control).find_children(
		"ProceduralCard", "PanelContainer", true, false)
	if expanded == null or heading == null or destination == null \
			or "PLAYER 2" not in _visible_text(expanded).to_upper() \
			or not heading.text.begins_with("PLAYER 1 OPENING HAND") \
			or "PLAYER 1" not in _visible_text(destination).to_upper() \
			or cards.size() != 6 or main.find_child("CompleteChoiceSheet", true, false) == null:
		_fail("switching public workspaces changed or ambiguously labeled the prompt owner's hand")
		return false
	switch_one = main.find_child("SeatSwitch0", true, false) as Button
	if switch_one == null or not await _pointer_activate(switch_one):
		return false
	return main.find_child("SeatSwitch0", true, false) is Button \
		and main.find_child("CompleteChoiceSheet", true, false) != null


func _select_mulligan_cards() -> bool:
	if main.find_child("VillainTable", true, false) == null:
		return await _select_mulligan_cards_from_fallback()
	var mansion := _mulligan_discard("Avengers Mansion")
	var aunt := _mulligan_card("Aunt May")
	var kick := _mulligan_card("Swinging Web Kick")
	if mansion == null or aunt == null or kick == null:
		_fail("the opening hand has no explicit discard controls for the seeded cards")
		return false
	if not await _pointer_activate(mansion):
		return false
	if not await _sheet_selection_stays_bound("Aunt May", true):
		return false
	if not await _drag_capture_is_safe():
		return false
	if not await _keyboard_activate(_mulligan_discard("Swinging Web Kick")):
		return false
	return _mulligan_discard("Avengers Mansion").text == "✓ DISCARD" \
		and _mulligan_discard("Swinging Web Kick").text == "✓ DISCARD"


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
	if not await _sheet_selection_stays_bound("Swinging Web Kick", false):
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
	return card.get_parent().find_child("MulliganDiscard*", false, false) as Button if card != null else null


func _mulligan_card(title: String) -> Control:
	for candidate in (_node("Play/Board/HandShelf") as Control).find_children("ProceduralCard", "PanelContainer", true, false):
		var card := candidate as Control
		var name := card.find_child("Title", true, false) as Label
		if name != null and name.text == title:
			return card
	return null


func _drag_mulligan_to_discard(card: Control) -> bool:
	# Use the fixed discard destination in the near player area. The compact
	# duplicate inside the horizontally scrolling hand is a click/tap cue, not
	# a reliable cross-scroll drag destination.
	var discard := main.find_child("ExpandedDiscardPile", true, false) as Control
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
	if card == null or not await _prepare_activation(card):
		return false
	var start := _visible_control_rect(card).get_center()
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
		if "DISCARD AND REDRAW" in candidate.text and title in candidate.text:
			return candidate
	return null


func _submit_mulligan() -> bool:
	var submit := _submit_button()
	if submit == null or submit.disabled or "Discard 3 and redraw" not in submit.text:
		_fail("the three-card mulligan cannot be submitted")
		return false
	if not await _pointer_activate(submit):
		return false
	if OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true":
		if not await _wait_for(func() -> bool:
			var heading := _node("Play/Board/HandShelf/Margin/Stack/Heading") as Label
			return heading != null and heading.text.begins_with("PLAYER 2 OPENING HAND") \
				and not (_node("Play/Board/HandShelf") as Control).find_children(
					"MulliganDiscard*", "Button", true, false).is_empty()):
			_fail("the first player's submission did not hand the opening decision to player 2")
			return false
		return true
	if not await _wait_for(func() -> bool:
		return _visible_button_beginning(_decision(), "Change Form") != null \
			and _visible_button_beginning(_decision(), "Play Web-Shooter") != null):
		_fail("the seeded mulligan did not reach the player-action affordances")
		return false
	return true


func _complete_second_opening_hand() -> bool:
	var toggles := (_node("Play/Board/HandShelf") as Control).find_children(
		"MulliganDiscard*", "Button", true, false)
	var second_toggle := toggles[0] as Button if not toggles.is_empty() else null
	if second_toggle == null or not await _pointer_activate(second_toggle):
		_fail("the second player's opening hand has no operable discard target")
		return false
	var submit := _submit_button()
	if submit == null or submit.disabled or "Discard 1 and redraw" not in submit.text:
		_fail("the second player's selected mulligan cannot be submitted once")
		return false
	if not await _pointer_activate(submit):
		return false
	if not await _wait_for(func() -> bool:
		return _visible_button_beginning(_decision(), "Change Form") != null \
			and _visible_button_beginning(_decision(), "Play Web-Shooter") != null):
		_fail("the completed cooperative mulligan did not return to player one's actions")
		return false
	return true


func _dismiss_mulligan_result() -> bool:
	var dismiss := _node(
		"Play/Prompt/Margin/Stack/Workbench/Action/LastResult/Margin/Copy/Header/Dismiss") as Button
	if dismiss == null or not await _pointer_activate(dismiss):
		_fail("the first player's mulligan result cannot be dismissed before the next seat answers")
		return false
	return await _wait_for(func() -> bool:
		return not (_node("Play/Prompt/Margin/Stack/Workbench/Action/LastResult") as Control).visible)


func _mulligan_result_is_operable() -> bool:
	var result := _node("Play/Prompt/Margin/Stack/Workbench/Action/LastResult") as Control
	var summary := _node(
		"Play/Prompt/Margin/Stack/Workbench/Action/LastResult/Margin/Copy/Summary") as Label
	if not result.visible:
		_fail("the mulligan result is not visible")
		return false
	if "Spider-Man discarded Avengers Mansion, Aunt May, and Swinging Web Kick." not in summary.text:
		_fail("the mulligan result omits the discarded cards: %s" % summary.text)
		return false
	if "Spider-Man drew Daredevil, Black Cat, and Jessica Jones." not in summary.text:
		_fail("the mulligan result omits the drawn cards: %s" % summary.text)
		return false
	if not await _capture_checkpoint("mulligan-result"):
		return false
	return await _result_toggle_is_operable(summary)


func _result_toggle_is_operable(summary: Label) -> bool:
	var toggle := _node(
		"Play/Prompt/Margin/Stack/Workbench/Action/LastResult/Margin/Copy/Header/Toggle") as Button
	if not await _pointer_activate(toggle):
		return false
	if summary.visible or toggle.text != "Expand":
		_fail("the transient result cannot be collapsed")
		return false
	if not await _pointer_activate(toggle):
		return false
	if not summary.visible or toggle.text != "Collapse":
		_fail("the transient result cannot be expanded")
		return false
	return true


func _start_web_shooter_draft() -> bool:
	var web_shooter := _web_shooter_action()
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
