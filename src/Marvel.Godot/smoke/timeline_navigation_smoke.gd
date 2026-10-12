extends "res://smoke/event_activity_smoke.gd"


func _run() -> void:
	if not await _open_phase_activity(false): return
	var timeline := _timeline()
	var rail := timeline.find_child("TimelineRail", true, false) as Control
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if not await _zoom_and_preview(timeline, rail): return
	if not await _keyboard_navigation(timeline, rail): return
	if not await _history_edge():
		_fail("history edge navigation failed")
		return
	if (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("timeline navigation submitted a game decision")
		return
	if not await _capture_checkpoint("timeline-navigation"): return
	print("TIMELINE_NAVIGATION_SMOKE_OK zoom=true preview=true keyboard=true hidden_history=true")
	quit(0)


func _zoom_and_preview(timeline: Control, rail: Control) -> bool:
	var before := float(timeline.get_meta("timeline_span"))
	var point := rail.get_global_rect().get_center()
	_position_pointer_without_settle(point)
	var wheel := InputEventMouseButton.new()
	wheel.position = point
	wheel.global_position = point
	wheel.button_index = MOUSE_BUTTON_WHEEL_UP
	wheel.pressed = true
	render_viewport.push_input(wheel, true)
	wheel = wheel.duplicate() as InputEventMouseButton
	wheel.pressed = false
	render_viewport.push_input(wheel, true)
	await process_frame
	if float(timeline.get_meta("timeline_span")) >= before:
		_fail("hovered timeline did not zoom")
		return false
	_position_pointer_without_settle(point + Vector2(3, 0))
	await process_frame
	var preview := main.find_child("TimelinePreview", true, false) as Control
	if preview == null or not _control_is_fully_visible(preview) or _visible_text(preview).is_empty():
		_fail("timeline node has no readable hover preview")
		return false
	return true


func _keyboard_navigation(timeline: Control, rail: Control) -> bool:
	rail.grab_focus()
	var key := InputEventKey.new()
	key.keycode = KEY_HOME
	key.pressed = true
	render_viewport.push_input(key, true)
	key = key.duplicate() as InputEventKey
	key.pressed = false
	render_viewport.push_input(key, true)
	await process_frame
	if int(timeline.get_meta("timeline_selected")) != 0:
		_fail("keyboard cannot reach the first timeline occurrence")
		return false
	key = InputEventKey.new()
	key.keycode = KEY_END
	key.pressed = true
	render_viewport.push_input(key, true)
	key = key.duplicate() as InputEventKey
	key.pressed = false
	render_viewport.push_input(key, true)
	await process_frame
	if not bool(timeline.get_meta("timeline_following")):
		_fail("Latest did not resume following accepted events")
		return false
	return true


func _history_edge() -> bool:
	if not await _set_history_drawer(false): return false
	var drawer := _node("Play/Prompt") as Control
	var toggle := main.find_child("ToggleHistory", true, false) as Button
	if drawer.is_visible_in_tree() or toggle.text != "<<" or not _control_is_fully_visible(toggle):
		_fail("collapsed History still reserves a visible panel")
		return false
	if not await _set_history_drawer(true): return false
	if toggle.text != ">>" or not drawer.is_visible_in_tree():
		_fail("History edge cannot reveal its details")
		return false
	return await _set_history_drawer(false)
