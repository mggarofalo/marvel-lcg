extends "res://smoke/local_game_smoke.gd"

var closing := false
var started := 0

func _run() -> void:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _wait_for(func() -> bool: return _task_commit() != null): return
	# Commit and request a normal window close in the same input turn. The host
	# queue must finish the admitted answer before the scene is torn down.
	_task_commit().emit_signal("pressed")
	closing = true
	started = Time.get_ticks_msec()
	main.tree_exiting.connect(func(): print("CLOSE_RESOLUTION_SMOKE_OK"))
	root.emit_signal("close_requested")
	if auto_accept_quit:
		_fail("normal close bypassed the admitted request drain")

func _process(_delta: float) -> bool:
	if closing and Time.get_ticks_msec() - started > 10000:
		_fail("normal close did not complete after request drain")
	return false
