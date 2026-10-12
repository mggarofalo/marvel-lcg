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
	var progress := modal.find_child("PaymentProgress", true, false) as Label
	if progress == null or ("more resource" not in progress.text or "needed" not in progress.text):
		_fail("unpaid card does not explain the remaining resource cost")
		return false
	if not _payment_symbols_are_readable(modal): return false
	if not _payment_choices_are_comparable(modal): return false
	var copy := _visible_text(modal)
	if "Discard cards from hand" not in copy or "Resource abilities" not in copy:
		_fail("payment does not explain its sources and commitment")
		return false
	if not _payment_table_layout_is_safe(modal): return false
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


func _payment_table_layout_is_safe(modal: Control) -> bool:
	var played := modal.find_child("PaymentCardPreview", true, false) as Control
	var table := main.find_child("AstraTableSurface", true, false) as Control
	if played == null or table == null or modal.get_global_rect().intersects(table.get_global_rect()) \
			or played.get_global_rect().end.x > modal.get_global_rect().position.x:
		_fail("payment preview layout: played=%s modal=%s table=%s" % [played.get_global_rect() if played else "missing", modal.get_global_rect(), table.get_global_rect() if table else "missing"])
		return false
	return true


func _payment_choices_are_comparable(modal: Control) -> bool:
	var scroll := modal.find_child("DecisionBodyScroll", true, false) as Control
	if scroll == null:
		_fail("payment does not expose its source choices")
		return false
	var sources := modal.find_children("Resource*", "Button", true, false)
	var complete := 0
	for source in sources:
		if source.is_visible_in_tree() and scroll.get_global_rect().encloses(source.get_global_rect()):
			complete += 1
	if complete < mini(3, sources.size()):
		_fail("payment cannot compare three complete resource choices without scrolling")
		return false
	return true


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


func _inspect_payment_source_and_return(resource_name: String) -> bool:
	var resource := _payment_button(resource_name)
	if resource == null: return false
	resource.grab_focus()
	await process_frame
	var preview := _payment_modal().find_child("PaymentCardPreview", true, false) as Control
	if preview == null or int(preview.get_meta("previewed_card_anchor", -1)) != int(resource_name.trim_prefix("Resource")):
		_fail("payment focus did not preview the exact source")
		return false
	if preview.get_global_rect().end.x > _payment_modal().get_global_rect().position.x:
		_fail("payment source preview covers payment choices")
		return false
	var toggle := preview.find_child("TogglePlayedCardPreview", true, false) as Button
	toggle.grab_focus()
	await process_frame
	if int(preview.get_meta("previewed_card_anchor", -1)) == int(resource_name.trim_prefix("Resource")):
		_fail("played card preview did not return after leaving the payment source")
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
	if history == null or history.text != ">>" or not await _pointer_activate(history):
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
	if not await _close_payment_pile(popup): return false
	if not await _wait_for(func() -> bool: return render_viewport.gui_get_focus_owner() == pile):
		_fail("closing the pile did not restore its opener")
		return false
	var source := _payment_button(resource_name)
	if source == null or not source.button_pressed or (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("pile inspection changed the unpaid choices")
		return false
	return true


func _close_payment_pile(popup: PopupPanel) -> bool:
	if popup.theme == null:
		_fail("pile inspection has no installed theme")
		return false
	var theme_lifetime: WeakRef = weakref(popup.theme)
	var popup_lifetime: WeakRef = weakref(popup)
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	popup.push_input(escape, true)
	if not await _wait_for(func() -> bool: return popup_lifetime.get_ref() == null):
		_fail("pile inspection could not return to the unpaid payment")
		return false
	if theme_lifetime.get_ref() != null:
		_fail("the closed pile inspector retained its owned theme")
		return false
	return true


func _close_settled_payment_inspector(inspector: Control) -> bool:
	var frame := inspector.get_node("Frame") as Control
	if not await _wait_for(func() -> bool: return is_equal_approx(frame.modulate.a, 1.0)):
		_fail("payment inspector expansion did not settle")
		return false
	return await _pointer_activate(inspector.get_node("Frame/Stack/Header/Close") as Button)
