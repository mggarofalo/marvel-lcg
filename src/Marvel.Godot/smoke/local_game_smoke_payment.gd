extends "res://smoke/local_game_smoke_relationship_checks.gd"


func _payment_modal() -> Control:
	var modal := main.find_child("PaymentWorkspace", true, false) as Control
	return modal if modal != null and modal.is_visible_in_tree() else null


func _payment_button(pattern: String) -> Button:
	var modal := _payment_modal()
	if modal == null: return null
	for candidate in modal.find_children(pattern, "Button", true, false):
		if candidate.is_visible_in_tree(): return candidate as Button
	return null


func _payment_source(anchor: int, keyboard: bool) -> bool:
	var button := _payment_button("Resource%d" % anchor)
	if button == null:
		_fail("payment modal does not offer resource source %d" % anchor)
		return false
	if keyboard:
		if not await _keyboard_activate(button): return false
	elif not await _pointer_activate(button):
		return false
	return await _wait_for(func() -> bool:
		var refreshed := _payment_button("Resource%d" % anchor)
		return refreshed != null and refreshed.button_pressed)


func _payment_modal_is_safe() -> bool:
	var modal := _payment_modal()
	if modal == null:
		_fail("card play did not open its payment modal")
		return false
	if not _payment_symbols_are_readable(modal): return false
	var copy := _visible_text(modal)
	if "Discard cards from hand" not in copy or "Resource abilities" not in copy:
		_fail("payment does not explain its sources and commitment")
		return false
	var played := modal.find_child("InspectPayment*", true, false) as Button
	var table := main.find_child("AstraTableSurface", true, false) as Control
	if played == null or table == null or modal.get_global_rect().intersects(table.get_global_rect()):
		_fail("payment obscures the table or omits inspection of the played card")
		return false
	var overlay := main.find_child("RelationshipOverlay", true, false)
	if overlay != null and overlay.get_child_count() > 0:
		_fail("payment left relationship connectors on the table")
		return false
	var submit := _payment_button("Submit")
	if submit == null or not submit.disabled:
		_fail("the unpaid card can be committed")
		return false
	if not await _payment_inspection_preserves_draft(): return false
	return await _capture_checkpoint("card-payment-workspace")


func _payment_symbols_are_readable(modal: Control) -> bool:
	for source in modal.find_children("Resource*", "Button", true, false):
		if source.icon == null or source.icon.get_image().get_used_rect().has_area() == false:
			_fail("a payment source has no visible resource icons")
			return false
	var cost := modal.find_child("PaymentCost", true, false) as Control
	if cost == null or cost.size.y > 100:
		_fail("the payment cost is missing or wraps into an unreadable column")
		return false
	return true


func _payment_cancel_is_safe() -> bool:
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	var modal := _payment_modal()
	if modal == null: return false
	var source := _payment_button("Resource*")
	if source == null or not await _pointer_activate(source): return false
	var cancel := _payment_button("CancelCardPlay")
	if cancel == null or not await _pointer_activate(cancel): return false
	await process_frame
	if _payment_modal() != null or (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("cancelling payment changed the game or retained the modal")
		return false
	return true


func _compose_payment() -> bool:
	for _step in 24:
		var submit := _payment_button("Submit")
		if submit != null and not submit.disabled: return await _pointer_activate(submit)
		var next: Button
		for pattern in ["Target*", "Cost*", "Resource*"]:
			for candidate in _payment_modal().find_children(pattern, "Button", true, false):
				if not candidate.disabled and not candidate.button_pressed:
					next = candidate
					break
			if next != null: break
		if next == null or not await _pointer_activate(next):
			_fail("the payment modal has no choice that advances the draft")
			return false
		await process_frame
	_fail("payment did not become ready after offered sources were selected")
	return false


func _payment_recovery_surface_is_safe() -> bool:
	# Locked submission/recovery must expose the authoritative sync route.
	# This probes presentation state without issuing a game mutation.
	_decision().SetSubmitting(true)
	await process_frame
	if _payment_modal() != null:
		_fail("locked card payment obscures synchronization")
		return false
	var sync := main.find_child("Synchronize", true, false) as Button
	if sync == null or not await _control_has_real_hit_area(sync): return false
	_decision().SetSubmitting(false)
	await process_frame
	return _payment_modal() != null


func _payment_inspection_preserves_draft() -> bool:
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	var resource := _payment_button("Resource*")
	if resource == null: return false
	var resource_name := resource.name
	if not await _payment_source_stays_under_pointer(resource): return false
	var refreshed := _payment_button(resource_name)
	if refreshed == null or not refreshed.button_pressed: return false
	if not await _inspect_payment_source_and_return(resource_name): return false
	refreshed = _payment_button(resource_name)
	if refreshed == null or not refreshed.button_pressed or (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("inspection changed an unpaid draft or committed payment")
		return false
	if not await _payment_history_preserves_draft(resource_name, revision): return false
	if not await _payment_pile_preserves_draft(resource_name, revision): return false
	refreshed = _payment_button(resource_name)
	if not await _keyboard_activate(refreshed): return false
	refreshed = _payment_button(resource_name)
	if refreshed == null or refreshed.button_pressed:
		_fail("the inspected source could not be removed from payment")
		return false
	return true


func _payment_source_stays_under_pointer(resource: Button) -> bool:
	if not await _prepare_activation(resource): return false
	var source_name := resource.name
	var position := resource.get_global_rect().get_center()
	if not _pointer_activate_without_settle(resource): return false
	for _frame in range(6): await process_frame
	var refreshed := _payment_button(source_name)
	if refreshed == null or absf(refreshed.get_global_rect().get_center().y - position.y) > 2.0:
		_fail("payment selection moved the source away from the pointer")
		return false
	return true


func _payment_inspect_label_fits(inspect: Button) -> bool:
	var font := inspect.get_theme_font("font")
	var text_width := font.get_string_size(inspect.text, HORIZONTAL_ALIGNMENT_LEFT, -1,
		inspect.get_theme_font_size("font_size")).x
	var padding := inspect.get_theme_stylebox("normal").get_minimum_size().x
	if inspect.size.x + 1.0 < text_width + padding:
		_fail("the payment Inspect label cannot be read on one line")
		return false
	return true


func _inspect_payment_source_and_return(resource_name: String) -> bool:
	var inspect_name := "InspectPayment%s" % str(resource_name).trim_prefix("Resource")
	var inspect := _payment_button(inspect_name)
	if inspect == null or not _payment_inspect_label_fits(inspect): return false
	if not await _keyboard_activate(inspect): return false
	var inspector := main.get_node("CardInspector") as Control
	if not await _wait_for(func() -> bool: return inspector.visible):
		_fail("a payment source cannot be inspected without discarding it")
		return false
	if int(inspector.get_meta("inspected_card_anchor", -1)) != int(str(resource_name).trim_prefix("Resource")):
		_fail("payment inspection opened a different physical card")
		return false
	var close := inspector.get_node("Frame/Stack/Header/Close") as Button
	if not await _pointer_activate(close): return false
	if not await _wait_for(func() -> bool:
		return not inspector.visible and render_viewport.gui_get_focus_owner() == _payment_button(inspect_name)):
		_fail("inspection did not restore the same payment source focus: visible=%s focus=%s expected=%s" % [inspector.visible, render_viewport.gui_get_focus_owner(), _payment_button(inspect_name)])
		return false
	return true


func _payment_history_preserves_draft(resource_name: String, revision: String) -> bool:
	var history := main.find_child("ToggleHistory", true, false) as Button
	if history == null or not await _pointer_activate(history): return false
	await process_frame
	if not _payment_undo_is_explained(): return false
	var source := _payment_button(resource_name)
	var submit := _payment_button("Submit")
	if source == null or not source.button_pressed or submit == null 			or not _control_is_fully_visible(submit) 			or (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("history obscured commitment or changed the unpaid payment")
		return false
	history = main.find_child("ToggleHistory", true, false) as Button
	if history == null or history.text != "Collapse history" or not await _pointer_activate(history):
		_fail("history could not close while retaining the payment")
		return false
	return true


func _payment_undo_is_explained() -> bool:
	var undo := main.find_child("UndoLast", true, false) as Button
	var reason := main.find_child("UndoReason", true, false) as Label
	if undo == null or not undo.disabled or reason == null \
			or not _control_is_fully_visible(reason) or "payment selection" not in reason.text:
		_fail("the unpaid draft did not visibly explain its temporary undo restriction")
		return false
	return true


func _payment_pile_preserves_draft(resource_name: String, revision: String) -> bool:
	var pile: Button
	for candidate in main.find_children("InspectPile*", "Button", true, false):
		if candidate.is_visible_in_tree() and not candidate.disabled and "Discard" in candidate.text:
			pile = candidate
			break
	if pile == null or not await _pointer_activate(pile):
		_fail("the legal mulligan discard pile cannot be inspected during payment")
		return false
	var popup := root.find_child("PileInspector", true, false) as PopupPanel
	if popup == null or not popup.visible:
		_fail("payment did not leave access to the visible discard pile")
		return false
	var popup_lifetime: WeakRef = weakref(popup)
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	popup.push_input(escape, true)
	if not await _wait_for(func() -> bool: return popup_lifetime.get_ref() == null):
		_fail("pile inspection could not return to the unpaid payment")
		return false
	if not await _wait_for(func() -> bool: return render_viewport.gui_get_focus_owner() == pile):
		_fail("closing the pile did not restore its opener")
		return false
	var source := _payment_button(resource_name)
	if source == null or not source.button_pressed or (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("pile inspection changed the unpaid choices")
		return false
	return true
