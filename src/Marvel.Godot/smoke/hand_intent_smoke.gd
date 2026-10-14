extends "res://smoke/local_game_smoke.gd"

func _run() -> void:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	if not await _pointer_ownership_probe_is_strict():
		_fail("pointer ownership did not preserve transformed shape, occlusion and clipping")
		return
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _wait_for(func() -> bool: return _task_commit() != null): return
	if not await _local_hand_gestures(): return
	if not await _clearing_mulligan_replacements_preserves_task(): return
	if not await _select_mulligan_cards(): return
	if _task_commit().text != "Redraw 3 cards":
		_fail("three staged cards lack one Redraw commitment")
		return
	if not await _release_table_resources(): return
	print("HAND_INTENT_SMOKE_OK")
	quit(0)

func _local_hand_gestures() -> bool:
	if not await _hand_insertion_is_local(): return false
	if not await _middle_mulligan_closes_gap(): return false
	if not await preload("res://smoke/mulligan_order_checks.gd").perform(self): return false
	return await _drag_capture_is_safe()


func _hand_insertion_is_local() -> bool:
	var hand := main.find_child("HandDropArea", true, false) as Control
	var cards: Array[Control] = []
	for candidate in _hand_surface().find_children("ProceduralCard*", "", true, false):
		if candidate.has_meta("spatial_hand_index"): cards.append(candidate as Control)
	if hand == null or cards.size() != 6:
		_fail("hand insertion lacks six physical cards")
		return false
	cards.sort_custom(func(a: Control, b: Control) -> bool: return a.global_position.x < b.global_position.x)
	var source := cards[0]
	var neighbor := cards[1]
	var old_neighbor := neighbor.position
	var old_source := source.position
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	var start := await _exposed_card_body_point(source)
	if start == Vector2.INF: return false
	var finish := Vector2(hand.get_global_rect().end.x - 15, hand.get_global_rect().get_center().y)
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = start
	press.global_position = start
	render_viewport.push_input(press, true)
	var move := InputEventMouseMotion.new()
	move.position = finish
	move.global_position = finish
	render_viewport.push_input(move, true)
	await create_timer(0.16).timeout
	if neighbor.position.distance_to(old_neighbor) < 10:
		_fail("neighbors did not preview the insertion before release")
		return false
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.position = finish
	release.global_position = finish
	render_viewport.push_input(release, true)
	await create_timer(0.22).timeout
	if source.position.x <= old_source.x + 100 or (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("hand reorder did not remain a local presentation change")
		return false
	return true

func _middle_mulligan_closes_gap() -> bool:
	var cards: Array[Control] = []
	for candidate in _hand_surface().find_children("ProceduralCard*", "", true, false):
		if candidate.has_meta("spatial_hand_index"): cards.append(candidate as Control)
	cards.sort_custom(func(a: Control, b: Control) -> bool: return a.position.x < b.position.x)
	var middle := cards[2]
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	var original := cards.map(func(card: Control) -> Vector2: return card.position)
	middle.grab_focus()
	await _press_key(KEY_SPACE)
	await create_timer(0.16).timeout
	if not middle.get_meta("mulligan_staged", false):
		_fail("middle mulligan card did not stage")
		return false
	var kept := cards.filter(func(card: Control) -> bool: return card != middle)
	var step: float = kept[1].position.x - kept[0].position.x
	for index in range(1, kept.size()):
		if abs(kept[index].position.x - kept[index - 1].position.x - step) > 1:
			_fail("staging the middle card left a gap in the kept hand")
			return false
	middle.grab_focus()
	await _press_key(KEY_SPACE)
	await create_timer(0.16).timeout
	for index in range(cards.size()):
		if cards[index].position.distance_to(original[index]) > 1:
			_fail("unstaging did not restore local hand order and spacing")
			return false
	return (_node("Toolbar/SyncStatus") as Label).text == revision

func _press_key(code: Key) -> void:
	for pressed in [true, false]:
		var event := InputEventKey.new()
		event.keycode = code
		event.pressed = pressed
		render_viewport.push_input(event, true)
		await process_frame
