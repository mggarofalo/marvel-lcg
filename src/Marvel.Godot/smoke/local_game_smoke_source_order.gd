extends RefCounted

static func perform(driver, source: Control) -> bool:
	var grip := source.find_child("SourceReorderGrip", true, false) as Button
	if grip == null:
		driver._fail("installed source lacks a distinct rearrangement grip")
		return false
	var revision: String = (driver._node("Toolbar/SyncStatus") as Label).text
	var original := source.get_global_transform()
	var start := grip.get_global_rect().get_center()
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
		driver._fail("source grip did not initiate a physical rearrangement")
		return false
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	driver.render_viewport.push_input(escape, true)
	await driver.process_frame
	if not source.get_global_transform().is_equal_approx(original) or (driver._node("Toolbar/SyncStatus") as Label).text != revision:
		driver._fail("source rearrangement changed game revision or failed to restore its physical slot")
		return false
	return true
