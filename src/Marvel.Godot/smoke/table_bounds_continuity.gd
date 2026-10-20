extends RefCounted


static func fractional_translation(driver, subject: Control) -> bool:
	var surface := driver.main.find_child("AstraTableSurface", true, false) as Control
	var commit: Control = driver._task_commit()
	var focus: Control = driver.render_viewport.gui_get_focus_owner()
	var menu: Node = driver.main.find_child("CardContextMenu", true, false)
	var selected := subject is BaseButton and (subject as BaseButton).button_pressed
	var revision = driver._recipient_revision()
	var origin: Vector2 = driver.main.position
	# Stay within one physical canvas pixel while exercising real transform notifications.
	var fraction := fposmod(surface.global_position.y, 1.0)
	var offset := (1.0 - fraction) * 0.5
	for position in [origin + Vector2(0, offset), origin]:
		driver.main.position = position
		for frame in 5: await driver.process_frame
		if not is_instance_valid(surface) or not is_instance_valid(subject) \
				or (commit != null and not is_instance_valid(commit)):
			var current = driver.main.find_child("AstraTableSurface", true, false)
			print("BOUNDS_AFTER rect=", current.get_global_rect(), " origin=", driver.main.position,
				" surface_valid=", is_instance_valid(surface), " subject_valid=", is_instance_valid(subject))
			driver._fail("subpixel canvas translation replaced active interaction controls")
			return false
		if driver.render_viewport.gui_get_focus_owner() != focus \
				or driver._recipient_revision() != revision \
				or (subject is BaseButton and (subject as BaseButton).button_pressed != selected):
			driver._fail("subpixel canvas translation changed draft, focus or revision")
			return false
		if menu != null and (not is_instance_valid(menu) \
				or driver.main.find_child("CardContextMenu", true, false) != menu):
			driver._fail("subpixel canvas translation dismissed the open source menu")
			return false
	print("TABLE_BOUNDS_CONTINUITY_OK subject=", subject.name)
	return true


static func physical_resize(driver) -> bool:
	var origin: Vector2 = driver.main.position
	for offset in [32.0, 0.0]:
		var before := driver.main.find_child("AstraTableSurface", true, false) as Control
		var previous_id := before.get_instance_id()
		var previous_height := before.size.y
		driver.main.position = origin + Vector2(0, offset)
		if not await driver._wait_for(func() -> bool:
			var current = driver.main.find_child("AstraTableSurface", true, false)
			return current != null and current.get_instance_id() != previous_id and current.size.y != previous_height):
			driver._fail("a physical canvas resize did not update table bounds")
			return false
		var context := driver.main.find_child("ContextualDecision", true, false) as Control
		if not driver._control_is_fully_visible(context):
			driver._fail("a physical canvas resize clipped the current decision")
			return false
	print("TABLE_BOUNDS_RESIZE_OK")
	return await _viewport_resize(driver)


static func _viewport_resize(driver) -> bool:
	var initial: Vector2i = driver.render_viewport.size
	for size in [initial + Vector2i(96, 0), initial]:
		var before := driver.main.find_child("AstraTableSurface", true, false) as Control
		var previous_width := before.size.x
		driver.render_viewport.size = size
		if not await driver._wait_for(func() -> bool:
			var current = driver.main.find_child("AstraTableSurface", true, false)
			return current != null and current.size.x != previous_width):
			driver._fail("viewport width change did not resize the table")
			return false
		var context := driver.main.find_child("ContextualDecision", true, false) as Control
		if not driver._control_is_fully_visible(context):
			driver._fail("viewport width change clipped the current decision")
			return false
	print("TABLE_VIEWPORT_RESIZE_OK")
	return true
