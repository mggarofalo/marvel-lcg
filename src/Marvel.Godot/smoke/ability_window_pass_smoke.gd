extends "res://smoke/ability_window_smoke.gd"


func _run() -> void:
	if not await _open_window_game() or not await _play_mockingbird(): return
	var window := _window()
	var pass_button := window.find_child("WindowPass", true, false) as Button
	if pass_button == null or not _control_is_fully_visible(pass_button):
		_fail("the optional window lacks its legitimate Pass")
		return
	var revision := _recipient_revision()
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	render_viewport.push_input(escape)
	escape = InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	render_viewport.push_input(escape)
	if not await _wait_for(func(): return _recipient_revision() > revision): return
	if _recipient_revision() != revision + 1 or _window() != null \
			or not _card_for_anchor(RHINO).find_children("StatusCard*", "Control", true, false).is_empty():
		_fail("Escape did not decline exactly the current optional window")
		return
	print("ABILITY_WINDOW_PASS_SMOKE_OK scale=%d legitimate_pass=true one_command=true" % _scale_percentage())
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)
