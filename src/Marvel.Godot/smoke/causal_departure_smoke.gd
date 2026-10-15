extends "res://smoke/staged_response_smoke.gd"

func _run() -> void:
	motion_enabled = false
	if not await _open_window_game(): return
	if not await _start_observed_play(): return
	if not await _observe_payment_departure(): return
	for _frame in range(3): await process_frame
	if bool(main.get_meta("response_staged", false)):
		_fail("motion toggle retained a decorative wait")
		return
	if not await _assert_settled_payment(): return
	print("CAUSAL_DEPARTURE_SMOKE_OK departed_before_prompt=true motion_toggle_immediate=true")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)

func _observe_payment_departure() -> bool:
	var energy := _tabletop_card_named("Energy")
	var emergency := _tabletop_card_named("Emergency")
	var saw_departure := false
	while bool(main.get_meta("response_staged", false)):
		if is_instance_valid(energy) and energy.has_meta("activity_departed"):
			if energy.modulate.a > 0.01:
				_fail("discard receipt left Energy visible in its hand slot")
				return false
			saw_departure = true
		if not _departed_card_is_hidden(emergency): return false
		if saw_departure:
			if not await _capture_checkpoint("causal-payment-departure"): return false
			var motion := _node("Toolbar/Motion") as CheckButton
			motion.button_pressed = false
			motion.toggled.emit(false)
			break
		await process_frame
	if not saw_departure:
		_fail("payment never departed before the final response")
		return false
	return true

func _departed_card_is_hidden(card: Control) -> bool:
	if is_instance_valid(card) and card.has_meta("activity_departed") and card.modulate.a > 0.01:
		_fail("discard receipt left a card visible in its hand slot")
		return false
	return true
