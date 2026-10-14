extends RefCounted

# Delivers real viewport pointer events and checks lifted intermediate geometry.
static func perform(driver, card: Control, finish: Vector2, expected_drop_surface: Control = null) -> bool:
	var start: Vector2 = driver._card_body_point(card)
	if not await driver._control_owns_point(card, start):
		driver._fail("card '%s' has no exposed body from which to begin its drag" % card.name)
		return false
	var origin := card.global_position
	var collection := card.find_parent("Continuous*")
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = start
	press.global_position = start
	driver.render_viewport.push_input(press, true)
	await driver.process_frame
	var midpoint := start.lerp(finish, 0.55)
	var move := InputEventMouseMotion.new()
	move.position = midpoint
	move.global_position = midpoint
	driver.render_viewport.push_input(move, true)
	await driver.process_frame
	if card.global_position.distance_to(origin + midpoint - start) > 4.0 or card.z_index < 120:
		driver._fail("dragging '%s' produced no lifted intermediate frame" % card.name)
		return false
	if not card.is_set_as_top_level():
		driver._fail("lifted card still inherits collection clipping")
		return false
	move = InputEventMouseMotion.new()
	move.position = finish
	move.global_position = finish
	driver.render_viewport.push_input(move, true)
	await driver.process_frame
	if expected_drop_surface != null and not expected_drop_surface.get_meta("spatial_drop_active", false):
		driver._fail("the occupied player region did not expose its active play drop affordance")
		return false
	if collection != null and not await driver._capture_checkpoint("ally-lift-outside-collection"):
		return false
	if not driver._drag_preview_is_meaningful(card): return false
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.position = finish
	release.global_position = finish
	driver.render_viewport.push_input(release, true)
	await driver.process_frame
	if is_instance_valid(card) and card.is_inside_tree() and card.global_position.distance_to(origin) > 2.0:
		await driver.main.get_tree().create_timer(0.25).timeout
	return true

static func bounds(card: Control) -> Rect2:
	var pose := card.get_global_transform()
	var result := Rect2(pose * Vector2.ZERO, Vector2.ZERO)
	for point in [Vector2(card.size.x, 0), card.size, Vector2(0, card.size.y)]:
		result = result.expand(pose * point)
	return result


