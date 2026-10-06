extends "res://smoke/local_game_smoke.gd"


func _run() -> void:
	# Reach seed 1's ordinary turn through the real opening-hand decision.
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _wait_for(func() -> bool: return _task_commit() != null):
		_fail("payment comparison did not reach the opening hand")
		return
	if not await _select_mulligan_cards() or not await _submit_mulligan(): return
	if OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true" \
			and not await _complete_second_opening_hand(): return
	if not await _direct_web_shooter_is_played(): return
	print("PAYMENT_COMPARISON_SMOKE_OK")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)
