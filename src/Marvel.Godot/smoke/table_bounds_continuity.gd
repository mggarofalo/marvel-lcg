extends RefCounted


static func fractional_translation(driver, subject: Control) -> bool:
	var surface := driver.main.find_child("AstraTableSurface", true, false) as Control
	var commit: Control = driver._task_commit()
	var decline: Control = driver.main.find_child("ContextualDecline", true, false)
	var focus: Control = driver.render_viewport.gui_get_focus_owner()
	var menu: Node = driver.main.find_child("CardContextMenu", true, false)
	var selected := subject is BaseButton and (subject as BaseButton).button_pressed
	var revision = driver._recipient_revision()
	var origin: Vector2 = driver.main.position
	# Stay within one physical canvas pixel while exercising real transform notifications.
	var fraction := fposmod(surface.global_position.y, 1.0)
	var offset := (1.0 - fraction) * 0.5
	for position in [origin + Vector2(0, offset), origin + Vector2(0, 24), origin]:
		driver.main.position = position
		for frame in 5: await driver.process_frame
		if not is_instance_valid(surface) or not is_instance_valid(subject) \
				or (commit != null and not is_instance_valid(commit)) \
				or (decline != null and not is_instance_valid(decline)):
			var current = driver.main.find_child("AstraTableSurface", true, false)
			print("BOUNDS_AFTER rect=", current.get_global_rect(), " origin=", driver.main.position,
				" surface_valid=", is_instance_valid(surface), " subject_valid=", is_instance_valid(subject))
			driver._fail("canvas height reflow replaced active interaction controls")
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
	await _settle_layout(driver)
	var origin: Vector2 = driver.main.position
	var sources := driver.main.find_child("SourceTableau", true, false) as Control
	var source_position := sources.position if sources != null else Vector2.ZERO
	var observed_positions: Dictionary = {}
	var initial_surface := driver.main.find_child("AstraTableSurface", true, false) as Control
	observed_positions[initial_surface.size.y] = source_position
	for offset in [1.0, 3.0, 32.0, 3.0, 1.0, 0.0]:
		await _settle_layout(driver)
		var before := driver.main.find_child("AstraTableSurface", true, false) as Control
		var previous_id := before.get_instance_id()
		var previous_height := before.size.y
		driver.main.position = origin + Vector2(0, offset)
		if not await _height_changed(driver, previous_height): return false
		await _settle_layout(driver)
		if not is_instance_valid(before) or before.get_instance_id() != previous_id:
			driver._fail("height-only resize replaced the current interaction surface")
			return false
		var context := driver.main.find_child("ContextualDecision", true, false) as Control
		if not driver._control_is_fully_visible(context):
			driver._fail("a physical canvas resize clipped the current decision")
			return false
		if not _hand_clearance(driver, before, context): return false
		if not _source_pose(driver, sources, source_position, before.size.y, observed_positions): return false
		if offset != 0 and not await driver._capture_checkpoint("table-height-shrink-%d" % int(offset)): return false
	print("TABLE_BOUNDS_RESIZE_OK")
	return await _viewport_resize(driver)


static func _settle_layout(driver) -> void:
	for frame in 5: await driver.process_frame


static func _height_changed(driver, previous_height: float) -> bool:
	if await driver._wait_for(func() -> bool:
		var current = driver.main.find_child("AstraTableSurface", true, false)
		return current != null and current.size.y != previous_height): return true
	driver._fail("a physical canvas resize did not update table bounds")
	return false


static func _source_pose(driver, sources: Control, original: Vector2, height: float, observed: Dictionary) -> bool:
	if sources == null: return true
	var shift := sources.position.y - original.y
	if not is_equal_approx(shift, roundf(shift)) \
			or (observed.has(height) and sources.position != observed[height]):
		driver._fail("odd canvas reflow blurred or drifted the source viewport")
		return false
	observed[height] = sources.position
	return true


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


static func _hand_clearance(driver, surface: Control, context: Control) -> bool:
	for card in surface.get_children():
		if not card is Control or not card.has_meta("spatial_hand_index"): continue
		var face := _footprint(card)
		if not driver.render_viewport.get_visible_rect().encloses(face) \
				or face.intersects(context.get_global_rect()):
			driver._fail("height reflow clipped a hand card or overlapped its decision dock")
			return false
		for other in surface.get_children():
			if other is Control and str(other.name).begins_with("ProceduralCard") \
					and not other.has_meta("spatial_hand_index") and face.intersects(_footprint(other)):
				driver._fail("height reflow overlapped the hand with the installed tableau")
				return false
	return true


static func _footprint(control: Control) -> Rect2:
	var transform := control.get_global_transform()
	var bounds := Rect2(transform * Vector2.ZERO, Vector2.ZERO)
	for corner in [Vector2(control.size.x, 0), control.size, Vector2(0, control.size.y)]:
		bounds = bounds.expand(transform * corner)
	return bounds
