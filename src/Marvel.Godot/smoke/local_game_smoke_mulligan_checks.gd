extends "res://smoke/local_game_smoke_card_checks.gd"

func _mulligan_result_and_payment_are_operable() -> bool:
	if not await _select_mulligan_cards():
		return false
	if not await _submit_mulligan():
		return false
	if not await _mulligan_result_is_operable():
		return false
	if not await _start_web_shooter_draft():
		return false
	return await _payment_is_keyboard_operable()


func _select_mulligan_cards() -> bool:
	if main.find_child("VillainTable", true, false) == null:
		return await _select_mulligan_cards_from_fallback()
	var mansion := _mulligan_discard("Avengers Mansion")
	var aunt := _mulligan_card("Aunt May")
	var kick := _mulligan_discard("Swinging Web Kick")
	if mansion == null or aunt == null or kick == null:
		_fail("the opening hand has no explicit discard controls for the seeded cards")
		return false
	if not await _pointer_activate(mansion):
		return false
	if not await _drag_mulligan_to_discard(aunt):
		return false
	if not await _keyboard_activate(kick):
		return false
	return mansion.text == "✓ DISCARD" and kick.text == "✓ DISCARD"


func _select_mulligan_cards_from_fallback() -> bool:
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
	var discard := main.find_child("MulliganDiscardPile", true, false) as Control
	if discard == null:
		# A populated pile retains its normal area identity.
		for area_node in main.find_children("Area*", "PanelContainer", true, false):
			var area := area_node as Control
			if "DISCARD PILE" in _visible_text(area):
				discard = area
				break
	if discard == null or not await _prepare_activation(card) or not await _prepare_activation(discard):
		_fail("the player discard place is not a reachable mulligan drop destination")
		return false
	var start := _visible_control_rect(card).get_center()
	var finish := _visible_control_rect(discard).get_center()
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = start
	press.global_position = start
	render_viewport.push_input(press)
	var move := InputEventMouseMotion.new()
	move.position = finish
	move.global_position = finish
	render_viewport.push_input(move)
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.position = finish
	release.global_position = finish
	render_viewport.push_input(release)
	await process_frame
	return true


func _mulligan_target(title: String) -> Button:
	for candidate in _visible_buttons(_decision()):
		if candidate.text.begins_with("◇ DISCARD AND REDRAW") and title in candidate.text:
			return candidate
	return null


func _submit_mulligan() -> bool:
	var submit := _submit_button()
	if submit == null or submit.disabled or "Discard 3 and redraw" not in submit.text:
		_fail("the three-card mulligan cannot be submitted")
		return false
	if not await _pointer_activate(submit):
		return false
	if not await _wait_for(func() -> bool:
		return _visible_button_beginning(_decision(), "Change Form") != null \
			and _visible_button_beginning(_decision(), "Play Web-Shooter") != null):
		_fail("the seeded mulligan did not reach the player-action affordances")
		return false
	return true


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
	var web_shooter := _visible_button_beginning(_decision(), "Play Web-Shooter")
	if web_shooter == null or web_shooter.disabled:
		_fail("Web-Shooter is not playable after the mulligan")
		return false
	if not await _pointer_activate(web_shooter):
		return false
	await process_frame
	await process_frame
	var result := _node("Play/Prompt/Margin/Stack/Workbench/Action/LastResult") as Control
	if result.visible:
		_fail("opening a new draft did not clear the transient result")
		return false
	return true


func _payment_is_keyboard_operable() -> bool:
	var generators := _decision().find_children("Resource*", "Button", true, false)
	if generators.is_empty():
		_fail("Web-Shooter exposes no post-mulligan payment generators")
		return false
	var generator := generators[0] as Button
	generator.grab_focus()
	await process_frame
	await process_frame
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
	var progress := _node("Play/Prompt/Margin/Stack/PromptHeader/Progress") as Label
	if "PAYMENT 1/1 ICONS" not in progress.text or "READY" not in progress.text:
		_fail("Peter Parker's post-mulligan resource cannot complete Web-Shooter's payment")
		return false
	return await _capture_checkpoint("post-mulligan-payment")



