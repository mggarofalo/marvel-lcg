extends "res://smoke/repeated_commit_smoke.gd"

# Legal seeded Core setup; compare native intermediate frames and uncommitted choices.
func _run() -> void:
	if not await _open_loop_game(): return
	if not await _mulligan_loop_checks(): return
	if not await _hand_loop_checks(): return
	if not await _attack_loop_checks(): return
	print("LOOP_DRAG_SMOKE_OK mulligan=true hand=true draft=true exhausted=true next_click=true motion=" + str(motion_enabled))
	if not await _release_table_resources(): return
	quit(0)


func _open_loop_game() -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _task_commit() != null): return false
	return true

func _mulligan_loop_checks() -> bool:
	var staged := _mulligan_card("Swinging Web Kick")
	staged.grab_focus()
	await _loop_key(KEY_SPACE)
	await preload("res://smoke/gesture_settlement.gd").hand(self)
	if not await _loop_returns(_mulligan_card("Avengers Mansion"), "mulligan"): return false
	if not bool(staged.get_meta("mulligan_staged", false)):
		_fail("looped mulligan drag erased a separately staged card")
		return false
	staged.grab_focus()
	await _loop_key(KEY_SPACE)
	if not await _commit_once("Keep hand"): return false
	return true

func _hand_loop_checks() -> bool:
	if not await _loop_returns(_card_for_anchor(19), "hand"): return false
	if not await _loop_returns(_card_for_anchor(19), "hand near-origin release", Vector2.INF, Vector2(9, 0)): return false
	var identity := _card_for_anchor(IDENTITY)
	var stat := identity.find_child("StatREC", true, false) as Control
	if stat == null or not await _loop_returns(identity, "stat-origin drag and click", stat.get_global_rect().get_center()): return false
	return true

func _attack_loop_checks() -> bool:
	if not await _direct_change_form_is_played(): return false
	if not await _select_attached_action(IDENTITY, "Attack"): return false
	if not await _choose_target(RHINO): return false
	if not await _loop_returns(_card_for_anchor(IDENTITY), "attack draft"): return false
	if not await _commit_once("Attack Rhino"): return false
	var exhausted := _card_for_anchor(IDENTITY)
	if not exhausted.has_meta("spatial_exhausted"):
		_fail("looped drag test did not reach a legally exhausted hero")
		return false
	if not await _loop_returns(exhausted, "exhausted hero"): return false
	return true

func _loop_returns(card: Control, context: String, start_override: Vector2 = Vector2.INF, release_offset: Vector2 = Vector2.ZERO) -> bool:
	if not await preload("res://smoke/gesture_settlement.gd").authored_pose(self, card): return false
	var start := start_override if start_override != Vector2.INF else await _exposed_card_body_point(card)
	if start == Vector2.INF: return false
	if not await _loop_pose_ready(card, context): return false
	card.force_update_transform()
	var original := card.get_global_transform()
	var pose_snapshot := preload("res://smoke/player_card_pose.gd").capture(self, card)
	var original_scale := card.scale
	var original_pivot := card.pivot_offset
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	var commitment := _task_commit().text if _task_commit() != null else ""
	var order := _hand_order()
	if not await _follow_loop(card, start, original.origin, release_offset, context): return false
	_loop_pointer(start + release_offset, false)
	await process_frame
	if not await preload("res://smoke/player_card_pose.gd").settled(self, card, pose_snapshot) \
			or card.scale != original_scale or card.pivot_offset != original_pivot:
		_fail(context + ": looped drag did not restore its exact authored pose and original visual location")
		return false
	if not await preload("res://smoke/player_card_pose.gd").reflow_round_trip(self, card, pose_snapshot):
		_fail(context + ": chrome reflow changed the card pose relative to its table region")
		return false
	if not await _loop_state_is_clean(card, revision, commitment, order, context): return false
	start += preload("res://smoke/player_card_pose.gd").expected(pose_snapshot).origin - original.origin
	return await _next_inspection(start, revision, context)


func _loop_state_is_clean(card: Control, revision: String, commitment: String, order: Array, context: String) -> bool:
	var inspector := main.find_child("CardInspector", true, false) as Control
	if card.find_child("DragContactShadow", true, false) != null or card.is_set_as_top_level() \
			or card.modulate != Color.WHITE or card.mouse_default_cursor_shape == Control.CURSOR_FORBIDDEN \
			or (inspector.visible and inspector.get_node("Frame/Stack/Header/Close").visible):
		_fail(context + ": looped release retained drag presentation or pinned inspection")
		return false
	if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision \
			or (_task_commit().text if _task_commit() != null else "") != commitment or _hand_order() != order:
		_fail(context + ": looped release changed authoritative revision, draft commitment or hand order")
		return false
	for surface in main.find_children("*", "Control", true, false):
		if bool(surface.get_meta("spatial_drop_active", false)):
			_fail(context + ": looped release retained a live drop surface")
			return false
	return true

func _hand_order() -> Array:
	var cards: Array[Control] = []
	for candidate in _hand_surface().find_children("ProceduralCard*", "Control", true, false):
		if candidate.has_meta("spatial_hand_index") and not bool(candidate.get_meta("mulligan_staged", false)):
			cards.append(candidate)
	cards.sort_custom(func(a: Control, b: Control) -> bool: return a.position.x < b.position.x)
	return cards.map(func(card: Control) -> String: return str(card.name))

func _loop_motion(point: Vector2) -> void:
	var motion := InputEventMouseMotion.new()
	motion.position = point
	motion.global_position = point
	render_viewport.push_input(motion, true)

func _loop_pointer(point: Vector2, pressed: bool) -> void:
	var event := InputEventMouseButton.new()
	event.button_index = MOUSE_BUTTON_LEFT
	event.pressed = pressed
	event.position = point
	event.global_position = point
	render_viewport.push_input(event, true)

func _loop_key(code: Key) -> void:
	for pressed in [true, false]:
		var event := InputEventKey.new()
		event.keycode = code
		event.pressed = pressed
		render_viewport.push_input(event, true)
		await process_frame

func _follow_loop(card: Control, start: Vector2, origin: Vector2, release_offset: Vector2, context: String) -> bool:
	_loop_pointer(start, true)
	await process_frame
	for position in [start + Vector2(160, 0), start + Vector2(180, -350), start + Vector2(5, -2), start + release_offset]:
		_loop_motion(position)
		for frame in range(8): await process_frame
		if not card.is_set_as_top_level() or card.global_position.distance_to(origin + position - start) > 4:
			_fail(context + ": recognized drag stopped following the pointer near its origin")
			return false
	return true

func _loop_pose_ready(card: Control, context: String) -> bool:
	if not await preload("res://smoke/gesture_settlement.gd").wait_for(self, func() -> bool:
		return card.position == card.get_meta("spatial_resting_position", card.position) \
			and card.rotation == card.get_meta("spatial_resting_rotation", card.rotation)):
		_fail(context + ": exact authored baseline did not finish settling")
		return false
	return true

func _next_inspection(start: Vector2, revision: String, context: String) -> bool:
	# A body click keeps hover inspection transient after the consumed drag release.
	_loop_motion(start)
	_loop_pointer(start, true)
	await process_frame
	_loop_pointer(start, false)
	await process_frame
	var inspector := main.find_child("CardInspector", true, false) as Control
	if not inspector.visible or inspector.get_node("Frame/Stack/Header/Close").visible:
		_fail(context + ": the next ordinary card click did not retain transient inspection")
		return false
	await _loop_key(KEY_ESCAPE)
	return str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) == revision
