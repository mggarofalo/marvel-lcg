extends "res://smoke/hosted_multiplayer_smoke_support.gd"

# The Windows CI runner falls back to software-rendered ANGLE. Socket decisions
# must still complete there, but rendering two live Main scenes can take longer
# than the headless local-game smoke's per-action budget.
const TIMEOUT_MILLISECONDS := 60000


func _complete(main: Control) -> bool:
	return _status(main).text.begins_with("GAME COMPLETE")


func _select_option(node: Node, wanted: String) -> void:
	var option := node as OptionButton
	for index in option.item_count:
		if option.get_item_text(index).begins_with(wanted):
			option.select(index)
			option.item_selected.emit(index)
			return
	_fail("hosted setup option '%s' is unavailable" % wanted)


func _first_enabled_choice(decision: Control) -> Button:
	for button in _visible_buttons(decision):
		if not button.disabled \
				and button.name != "Submit" \
				and button.name != &"Decline":
			return button
	return null


func _submit_button(decision: Control) -> Button:
	var submit := decision.find_child("Submit", true, false) as Button
	return submit if submit != null and submit.is_visible_in_tree() else null


func _button(node: Node, wanted: String) -> Button:
	for button in _visible_buttons(node):
		if button.text == wanted:
			return button
	return null


func _visible_buttons(node: Node) -> Array[Button]:
	var found: Array[Button] = []
	for child in node.get_children():
		if child is Button and child.is_visible_in_tree():
			found.append(child)
		found.append_array(_visible_buttons(child))
	return found


func _visible_text(node: Node) -> String:
	var text := ""
	for child in node.get_children():
		if child is Label and child.is_visible_in_tree():
			text += child.text + "\n"
		elif child is Button and child.is_visible_in_tree():
			text += child.text + "\n"
		text += _visible_text(child)
	return text


func _node(main: Control, relative: String) -> Node:
	return main.get_node("Margin/Shell/Content/" + relative)


func _play(main: Control) -> Control:
	return _node(main, "Play") as Control


func _decision(main: Control) -> Control:
	return main.find_child("Decision", true, false) as Control


func _status(main: Control) -> Label:
	return _node(main, "Status/Text") as Label


func _wait_for(condition: Callable) -> bool:
	var started := Time.get_ticks_msec()
	while Time.get_ticks_msec() - started < TIMEOUT_MILLISECONDS:
		if condition.call():
			return true
		await get_tree().process_frame
	return false


func _compose_hosted_payment(main: Control) -> bool:
	for _selection in 24:
		var payment := main.find_child("PaymentModal", true, false) as Control
		if payment == null or not payment.is_visible_in_tree():
			_fail("the hosted card payment closed before confirmation")
			return false
		var submit := _submit_button(payment)
		if submit != null and not submit.disabled:
			return await _pointer_activate(submit)
		var choice := _hosted_payment_choice(payment)
		if choice == null or not await _pointer_activate(choice):
			_fail("the hosted payment has no choice that advances its draft")
			return false
	_fail("the hosted payment did not become ready")
	return false


func _hosted_payment_choice(payment: Control) -> Button:
	for pattern in ["Target*", "Cost*", "Resource*"]:
		for candidate in payment.find_children(pattern, "Button", true, false):
			if candidate.is_visible_in_tree() and not candidate.disabled and not candidate.button_pressed:
				return candidate as Button
	return null


func _wait_for_hosted_motion(main: Control) -> bool:
	# Result playback can reveal cards by scrolling the compact table.
	# Prove pointer geometry after that public playback state has settled.
	if not await _wait_for(func() -> bool:
		var skip := main.find_child("Skip", true, false) as Button
		return skip != null and skip.disabled):
		_fail("the hosted result animation did not finish")
		return false
	await get_tree().process_frame
	return true
