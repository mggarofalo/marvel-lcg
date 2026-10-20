extends RefCounted

static func perform(driver, source: Control) -> bool:
	for region in ["SourceReorderGrip", "SourceType", "SourceState", "SourceMeaning", "Card*Action"]:
		var surface := source.find_child(region, true, false) as Control
		if surface == null or not surface.is_visible_in_tree(): continue
		if not await preload("res://smoke/gesture_settlement.gd").authored_pose(driver, source):
			driver._fail("source arrangement baseline did not settle")
			return false
		if not await _region(driver, source, surface): return false
	return true

static func _region(driver, source: Control, surface: Control) -> bool:
	var revision: String = str((driver._node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	source.force_update_transform()
	var pose := preload("res://smoke/player_card_pose.gd").capture(driver, source)
	var start := surface.get_global_rect().get_center()
	var hover := InputEventMouseMotion.new()
	hover.position = start
	hover.global_position = start
	driver.render_viewport.push_input(hover, true)
	await driver.process_frame
	var press := InputEventMouseButton.new()
	press.position = start
	press.global_position = start
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	driver.render_viewport.push_input(press, true)
	await driver.process_frame
	var move := InputEventMouseMotion.new()
	move.position = start + Vector2(-70, 60)
	move.global_position = move.position
	driver.render_viewport.push_input(move, true)
	await driver.process_frame
	if not source.is_set_as_top_level():
		driver._fail("source region did not initiate arrangement: " + str(surface.name))
		return false
	for slot in source.get_parent().get_parent().get_children():
		var outline := slot.find_child("SourceSwapOutline", false, false) as Control
		if outline == null or not outline.is_visible_in_tree():
			driver._fail("source drag did not reveal every destination")
			return false
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	driver.render_viewport.push_input(escape, true)
	await driver.process_frame
	press.pressed = false
	press.position = move.position
	press.global_position = move.position
	driver.render_viewport.push_input(press, true)
	await driver.process_frame
	if not await preload("res://smoke/player_card_pose.gd").settled(driver, source, pose):
		driver._fail("cancelled source did not settle: region=%s expected=%s actual=%s local=%s slot=%s" % [surface.name, preload("res://smoke/player_card_pose.gd").expected(pose), source.get_global_transform(), source.position, source.get_parent().get_global_rect()])
		return false
	if not source.get_global_transform().is_equal_approx(preload("res://smoke/player_card_pose.gd").expected(pose)) or str((driver._node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision:
		driver._fail("source rearrangement changed game revision or failed to restore its physical slot")
		return false
	return true
