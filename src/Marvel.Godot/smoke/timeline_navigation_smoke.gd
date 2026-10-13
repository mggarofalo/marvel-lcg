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
	if not await _history_details_keep_selected(): return false
	return await _set_history_drawer(false)


func _history_details_keep_selected() -> bool:
	var history := _node("Play/Prompt/Margin/Stack/Workbench/History/HistoryOccurrences") as Control
	var scroll := history.find_child("OccurrenceScroll", true, false) as ScrollContainer
	var selected := int(_timeline().get_meta("timeline_selected"))
	var expanded := _button_named("▾ Details")
	if expanded != null:
		if not await _pointer_activate(expanded): return false
		for _frame in 4: await process_frame
	var toggle := _button_named("▸ Details")
	if toggle == null or not await _pointer_activate(toggle):
		_fail("the selected occurrence has no inspectable Details commitment")
		return false
	for _frame in 4: await process_frame
	if not _control_is_fully_visible(history) or not _selected_history_row_is_visible(history, selected):
		_fail("expanding Details clips the history viewport or hides its selected occurrence")
		return false
	if not await _capture_checkpoint("history-details-selected-row"): return false
	scroll.scroll_vertical = 0
	for _frame in 4: await process_frame
	if scroll.scroll_vertical != 0:
		_fail("manual history scrolling was overridden by selected-row pinning")
		return false
	return true


func _selected_history_row_is_visible(history: Control, selected: int) -> bool:
	for row in history.find_children("*", "Button", true, false):
		if row.get_meta("occurrence_index", -1) == selected and row.get_meta("selected_occurrence", false):
			return _control_is_fully_visible(row)
	return false