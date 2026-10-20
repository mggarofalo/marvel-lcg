extends RefCounted


static func perform(driver) -> bool:
	var card: Control = driver._mulligan_card("Swinging Web Kick")
	var surface := driver.main.find_child("AstraTableSurface", true, false) as Control
	var commit: Control = driver._task_commit()
	var origin: Vector2 = driver.main.position
	var height := surface.size.y
	var resting := card.position
	var rotation := card.rotation
	var revision = driver._node("Toolbar/SyncStatus").get_meta("response_revision")
	var start: Vector2 = await driver._exposed_card_body_point(card)
	if start == Vector2.INF: return false
	driver._send_pointer(start, true)
	await driver.process_frame
	var move := InputEventMouseMotion.new()
	move.position = Vector2(570, 450)
	move.global_position = move.position
	driver.render_viewport.push_input(move, true)
	await driver.process_frame
	driver.main.position = origin + Vector2(0, 24)
	for frame in 5: await driver.process_frame
	if not is_instance_valid(surface) or not is_instance_valid(commit) \
			or surface.size.y != height or not card.is_set_as_top_level():
		driver._fail("chrome reflow interrupted the held card or replaced its decision")
		return false
	driver._send_pointer(move.position, false)
	await driver.process_frame
	# Resolving the invalid drop refreshes its feedback; subsequent geometry must retain that control.
	commit = driver._task_commit()
	if bool(card.get_meta("card_return_active", false)) and surface.size.y != height:
		driver._fail("chrome reflow raced an active card return")
		return false
	if not await driver._wait_for(func() -> bool:
		return not bool(card.get_meta("card_return_active", false)) and surface.size.y != height):
		driver._fail("chrome reflow did not resume after the card settled")
		return false
	await driver.main.get_tree().create_timer(0.35).timeout
	var expected := resting + Vector2(0, surface.size.y - height)
	if not card.position.is_equal_approx(expected) or not is_equal_approx(card.rotation, rotation):
		print("REFLOW_POSE expected=", expected, " actual=", card.position, " original=", resting, " height=", height, " current=", surface.size.y, " rotation=", rotation, " actualrotation=", card.rotation)
		driver._fail("a return tween overwrote the reflowed hand slot")
		return false
	var shrunken_height := surface.size.y
	driver.main.position = origin
	if not await driver._wait_for(func() -> bool: return surface.size.y != shrunken_height):
		driver._fail("restoring chrome did not grow the table")
		return false
	for frame in 5: await driver.process_frame
	expected = resting + Vector2(0, surface.size.y - height)
	if not card.position.is_equal_approx(expected) or not is_instance_valid(commit) \
			or driver._node("Toolbar/SyncStatus").get_meta("response_revision") != revision:
		print("RESTORED expected=", expected, " actual=", card.position, " height=", surface.size.y)
		driver._fail("capture/reflow changed the authoritative draft or failed to restore the hand")
		return false
	print("TABLE_BOUNDS_CAPTURE_OK")
	return true
