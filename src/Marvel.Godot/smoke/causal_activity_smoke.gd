extends "res://smoke/repeated_commit_smoke.gd"

var impact_sampled := false
var motion_sampled := false

func _run() -> void:
	if not await _stage_core_attack(): return
	var rhino := _tabletop_card_named("Rhino")
	var origin := rhino.position
	if not await _pointer_activate(_task_commit()): return
	if not await _wait_for(func(): return bool(main.get_meta("response_staged", false))): return
	if not await _inspect_and_dismiss_during_activity(rhino): return
	if not await _observe_impact(rhino, origin): return
	if not impact_sampled or not motion_sampled:
		_fail("attack lacked an exact HP impact or native card motion")
		return
	if not await _capture_checkpoint("causal-settled"): return
	print("CAUSAL_ACTIVITY_SMOKE_OK impact_hp=true native_motion=true next_prompt_gated=true inspector_backdrop=true")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)

func _inspect_and_dismiss_during_activity(rhino: Control) -> bool:
	rhino.grab_focus()
	var inspect := InputEventKey.new()
	inspect.keycode = KEY_ENTER
	inspect.pressed = true
	render_viewport.push_input(inspect)
	await process_frame
	var inspector := main.find_child("CardInspector", true, false) as Control
	if not inspector.visible:
		_fail("read-only card inspection was blocked during accepted activity")
		return false
	# The table background must pass the complete dismissal gesture to the modal owner.
	var point := rhino.get_global_rect().get_center()
	var frame := inspector.get_node("Frame") as Control
	if frame.get_global_rect().has_point(point):
		_fail("inspector covered its source; no board-owned backdrop target")
		return false
	_position_pointer_without_settle(point)
	await process_frame
	for pressed in [true, false]:
		var click := InputEventMouseButton.new()
		click.button_index = MOUSE_BUTTON_LEFT
		click.pressed = pressed
		click.position = point
		click.global_position = point
		render_viewport.push_input(click, true)
	for _frame in range(3): await process_frame
	if inspector.visible:
		_fail("pinned inspector backdrop dismissal was blocked during accepted activity")
		return false
	if not bool(main.get_meta("response_staged", false)):
		_fail("inspector dismissal check missed the active presentation")
		return false
	return true

func _observe_impact(rhino: Control, origin: Vector2) -> bool:
	while bool(main.get_meta("response_staged", false)):
		if is_instance_valid(rhino):
			motion_sampled = motion_sampled or rhino.position.distance_to(origin) > 0.2 or rhino.scale.x > 1.001
			if str(rhino.get_meta("activity_health", "")) == "12/14":
				if not await _check_impact_value(rhino): return false
		if _button_named("End turn") != null:
			_fail("next decision exposed before motion settled")
			return false
		await process_frame
	return true

func _check_impact_value(rhino: Control) -> bool:
	var value := rhino.find_child("LiveProgressValue", true, false) as Label
	if value == null or value.text != "12/14":
		_fail("impact badge contradicted the live HP value")
		return false
	if not impact_sampled:
		impact_sampled = true
		if not await _capture_checkpoint("causal-impact"): return false
	return true
