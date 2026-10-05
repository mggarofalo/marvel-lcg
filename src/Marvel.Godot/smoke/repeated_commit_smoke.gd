extends "res://smoke/local_game_smoke.gd"


func _run() -> void:
	# Legal Core journey: a fresh activation must distinguish an attack from ending the turn.
	if not await _stage_core_attack(): return
	if not await _repeat_preserves_next_commitment(): return
	print("REPEATED_COMMIT_SMOKE_OK")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _stage_core_attack() -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _task_commit() != null): return false
	if not await _commit_once("Keep hand"): return false
	if not await _direct_change_form_is_played(): return false
	if not await _set_history_drawer(true): return false
	if not await _select_attached_action(IDENTITY, "Attack"): return false
	if not await _choose_target(RHINO): return false
	var commit := _task_commit()
	if commit == null or commit.text != "Attack Rhino":
		_fail("the repeated-input test did not stage the legal hero attack")
		return false
	return true


func _repeat_preserves_next_commitment() -> bool:
	var commit := _task_commit()
	var attack_point := commit.get_global_rect().get_center()
	if not await _commit_once("Attack Rhino"): return false
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	_inject_repeated_click(attack_point)
	await process_frame
	await process_frame
	if (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("the second click crossed from Attack Rhino into a replacement decision")
		return false
	var end_turn := _button_named("End turn")
	if end_turn == null or end_turn.disabled:
		_fail("the attack did not leave the next turn commitment available")
		return false
	_inject_repeated_click(end_turn.get_global_rect().get_center())
	end_turn.grab_focus()
	var echo := InputEventKey.new()
	echo.keycode = KEY_ENTER
	echo.pressed = true
	echo.echo = true
	render_viewport.push_input(echo)
	var key_release := InputEventKey.new()
	key_release.keycode = KEY_ENTER
	render_viewport.push_input(key_release)
	await process_frame
	await process_frame
	if (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("a repeated activation accepted the replacement End turn decision")
		return false
	if not await _pointer_activate(end_turn):
		_fail("a fresh click could not intentionally end the turn after repeat suppression")
		return false
	if not await _wait_for(func() -> bool:
		return (_node("Toolbar/SyncStatus") as Label).text != revision):
		_fail("the fresh End turn commitment was lost")
		return false
	return true


func _inject_repeated_click(point: Vector2) -> void:
	_position_pointer_without_settle(point)
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.position = point
	press.global_position = point
	press.pressed = true
	press.double_click = true
	render_viewport.push_input(press, true)
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.position = point
	release.global_position = point
	render_viewport.push_input(release, true)
