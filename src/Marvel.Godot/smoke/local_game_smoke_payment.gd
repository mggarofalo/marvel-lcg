extends "res://smoke/local_game_smoke_relationship_checks.gd"


func _payment_modal() -> Control:
	var modal := main.find_child("PaymentModal", true, false) as Control
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
	if "Discard cards from hand" not in copy or "Resource abilities" not in copy \
			or "Nothing is spent until you confirm" not in copy:
		_fail("payment does not explain its sources and commitment")
		return false
	var stage := modal.find_child("StagedCard", true, false) as Control
	if stage == null or not stage.is_visible_in_tree():
		_fail("card play has no staged card representation")
		return false
	var overlay := main.find_child("RelationshipOverlay", true, false)
	if overlay != null and overlay.get_child_count() > 0:
		_fail("payment left relationship connectors on the table")
		return false
	var submit := _payment_button("Submit")
	if submit == null or not submit.disabled:
		_fail("the unpaid card can be committed")
		return false
	for _step in 12:
		var tab := InputEventKey.new()
		tab.keycode = KEY_TAB
		tab.pressed = true
		render_viewport.push_input(tab, true)
		await process_frame
		var focus := render_viewport.gui_get_focus_owner()
		if focus == null or not modal.is_ancestor_of(focus):
			_fail("keyboard focus escaped the payment modal")
			return false
	return await _capture_checkpoint("card-payment-modal")


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
