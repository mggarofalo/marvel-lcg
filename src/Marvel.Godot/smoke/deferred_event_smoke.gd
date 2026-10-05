extends "res://smoke/local_game_smoke.gd"


func _run() -> void:
	# Legal Core journey: keep seed 1's Web Kick, change form, stage and cancel its play.
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _wait_for(func() -> bool: return _task_commit() != null):
		_fail("the focused deferred-event journey did not reach opening-hand completion")
		return
	if not await _commit_once("Keep hand"): return
	if not await _direct_change_form_is_played(): return
	if not main.has_meta("deferred_preview_proved"):
		_fail("the kept-hand Core journey never proved the deferred-event drag preview")
		return
	if not await _capture_checkpoint("deferred-event-cancelled"): return
	print("DEFERRED_EVENT_SMOKE_OK")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)
