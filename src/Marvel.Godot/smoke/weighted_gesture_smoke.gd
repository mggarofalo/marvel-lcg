extends "res://smoke/local_game_smoke.gd"

# Real seeded mulligan; native pointer events interrupt an invalid return before it settles.
func _run() -> void:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _wait_for(func() -> bool: return _task_commit() != null): return
	if not await _return_click_restores_pose(): return
	await _press_key(KEY_ESCAPE)
	if not await _check_cancel_paths(): return
	if not await preload("res://smoke/table_bounds_capture.gd").perform(self): return
	if not await _pending_return_retires(): return
	print("WEIGHTED_GESTURE_OK returning_click_restores=true inspection=true unchanged_revision=true escape=true focus_loss=true")
	quit(0)

func _return_click_restores_pose() -> bool:
	var card := _mulligan_card("Swinging Web Kick")
	if card == null: return false
	var position := card.position
	var canvas_height := _canvas_height()
	var rotation := card.rotation
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	var start := await _exposed_card_body_point(card)
	if start == Vector2.INF: return false
	var finish := Vector2(570, 450)
	_send_pointer(start, true)
	await process_frame
	var move := InputEventMouseMotion.new()
	move.position = finish
	move.global_position = finish
	render_viewport.push_input(move, true)
	await process_frame
	if _rejection_feedback(card, finish) == null: return false
	if not await _capture_checkpoint("weighted-invalid-held"): return false
	_send_pointer(finish, false)
	await process_frame
	if not _return_state_matches(card, motion_enabled): return false
	if motion_enabled and card.position.is_equal_approx(position):
		_fail("return-click regression did not interrupt an intermediate returning card")
		return false
	# Move and press in one frame so the returning card cannot escape the click.
	var click := card.get_global_transform() * Vector2(card.size.x * 0.5, card.size.y * 0.78)
	move = InputEventMouseMotion.new()
	move.position = click
	move.global_position = click
	render_viewport.push_input(move, true)
	_send_pointer(click, true)
	await process_frame
	_send_pointer(click, false)
	await process_frame
	await preload("res://smoke/gesture_settlement.gd").authored_pose(self, card)
	position += Vector2(0, _canvas_height() - canvas_height)
	if not _return_state_matches(card, false): return false
	if not card.position.is_equal_approx(position) or not is_equal_approx(card.rotation, rotation) \
			or card.modulate != Color.WHITE or card.is_set_as_top_level():
		_fail("clicking a returning card failed to restore its exact resting pose and white tint")
		return false
	if not _return_click_inspection_safe(card, revision): return false
	return await _capture_checkpoint("weighted-return-click-settled")

func _return_click_inspection_safe(card: Control, revision: String) -> bool:
	var inspector := main.find_child("CardInspector", true, false) as Control
	if not inspector.visible or inspector.get_node("Backdrop").visible \
			or inspector.get_node("Frame/Stack/Header/Close").visible:
		_fail("returning-card click did not retain transient inspection semantics")
		return false
	if bool(card.get_meta("mulligan_staged", false)) or str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision:
		_fail("invalid drop or returning-card click changed the draft or authoritative revision")
		return false
	return true

func _send_pointer(point: Vector2, pressed: bool) -> void:
	var event := InputEventMouseButton.new()
	event.button_index = MOUSE_BUTTON_LEFT
	event.pressed = pressed
	event.position = point
	event.global_position = point
	render_viewport.push_input(event, true)

func _check_cancel_paths() -> bool:
	var card := _mulligan_card("Swinging Web Kick")
	var resting := card.position
	var rotation := card.rotation
	for focus_loss in [false, true]:
		var canvas_height := _canvas_height()
		var start := await _exposed_card_body_point(card)
		if start == Vector2.INF: return false
		_send_pointer(start, true)
		await process_frame
		var finish := Vector2(570, 450)
		var move := InputEventMouseMotion.new()
		move.position = finish
		move.global_position = finish
		render_viewport.push_input(move, true)
		await process_frame
		if focus_loss:
			main.get_window().emit_signal("focus_exited")
		else:
			await _press_key(KEY_ESCAPE)
		_send_pointer(finish, false)
		await preload("res://smoke/gesture_settlement.gd").authored_pose(self, card)
		resting += Vector2(0, _canvas_height() - canvas_height)
		if not _cancelled_pose_is_safe(card, resting, rotation): return false
	return true

func _canvas_height() -> float:
	return (main.find_child("AstraTableSurface", true, false) as Control).custom_minimum_size.y

func _cancelled_pose_is_safe(card: Control, resting: Vector2, rotation: float) -> bool:
	if not _return_state_matches(card, false): return false
	if card.is_set_as_top_level() or not card.position.is_equal_approx(resting) \
			or not is_equal_approx(card.rotation, rotation) or card.modulate != Color.WHITE:
		_fail("cancelled drag failed to restore exact pose")
		return false
	if card.mouse_default_cursor_shape == Control.CURSOR_FORBIDDEN:
		_fail("cancelled drag retained forbidden cursor")
		return false
	return true

func _return_state_matches(card: Control, active: bool) -> bool:
	if bool(card.get_meta("card_return_active", false)) != active:
		_fail("card return lifecycle disagrees with its active or completed gesture")
		return false
	return true

func _pending_return_retires() -> bool:
	var card := _mulligan_card("Swinging Web Kick")
	var tracked: WeakRef = weakref(card)
	var observed := {"seen": false, "valid": false, "active": true}
	card.tree_exiting.connect(func() -> void:
		var current := tracked.get_ref() as Control
		observed.seen = true
		observed.valid = current != null
		observed.active = bool(current.get_meta("card_return_active", false)) if current != null else true)
	if not await _start_pending_return(card): return false
	if not await _release_table_resources(): return false
	if not observed.seen or not observed.valid or observed.active:
		_fail("scene retirement retained an active return or missed its live tree-exit cleanup")
		return false
	return true

func _start_pending_return(card: Control) -> bool:
	var start := await _exposed_card_body_point(card)
	if start == Vector2.INF: return false
	_send_pointer(start, true)
	await process_frame
	var move := InputEventMouseMotion.new()
	move.position = Vector2(570, 450)
	move.global_position = move.position
	render_viewport.push_input(move, true)
	await process_frame
	_send_pointer(move.position, false)
	await process_frame
	return _return_state_matches(card, motion_enabled)

func _press_key(code: Key) -> void:
	for pressed in [true, false]:
		var key := InputEventKey.new()
		key.keycode = code
		key.pressed = pressed
		render_viewport.push_input(key, true)
		await process_frame

func _local_feedback_is_readable(cue: Label) -> bool:
	if cue.get_line_count() != 1 or cue.size.y > 40 or cue.size.x < 120:
		_fail("drag feedback is not a legible bounded single-line cue")
		return false
	return true

func _rejection_feedback(card: Control, finish: Vector2) -> Label:
	if card.mouse_default_cursor_shape != Control.CURSOR_FORBIDDEN:
		_fail("rejected destination did not expose forbidden cursor")
		return null
	var cue := card.find_child("DragLocalFeedback", true, false) as Label
	if cue == null or not cue.visible or cue.global_position.distance_to(finish) > 90:
		_fail("rejected destination lacks a bounded pointer-local cue")
		return null
	if not _local_feedback_is_readable(cue): return null
	return cue
