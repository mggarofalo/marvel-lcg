extends RefCounted

# Delivers real viewport pointer events and checks lifted intermediate geometry.
static func perform(driver, card: Control, finish: Vector2, expected_drop_surface: Control = null) -> bool:
	var start := await _settled_start(driver, card)
	if start == Vector2.INF: return false
	card.force_update_transform()
	var original := card.get_global_transform()
	var pose_snapshot := preload("res://smoke/player_card_pose.gd").capture(driver, card)
	var origin := card.global_position
	var collection := card.find_parent("Continuous*")
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = start
	press.global_position = start
	driver.render_viewport.push_input(press, true)
	await driver.process_frame
	var pressed_origin := card.global_position
	if pressed_origin.distance_to(origin) > 1.0:
		driver._fail("pressing '%s' displaced it on screen: %s -> %s" % [card.name, origin, pressed_origin])
		return false
	var midpoint := start.lerp(finish, 0.55)
	var move := InputEventMouseMotion.new()
	move.position = midpoint
	move.global_position = midpoint
	driver.render_viewport.push_input(move, true)
	var immediate_origin := card.global_position
	await driver.process_frame
	if card.global_position.distance_to(origin + midpoint - start) > 4.0 or card.z_index < 120:
		driver._fail("dragging '%s' produced no lifted intermediate frame: %s" % [card.name, _lift_diagnostic(driver, card, origin, pressed_origin, immediate_origin, start, midpoint)])
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
	if not await preload("res://smoke/player_card_pose.gd").settled(driver, card, pose_snapshot, true):
		driver._fail("released card did not finish returning or retire within 750 ms: " + _return_diagnostic(card, original))
		return false
	return true

# The resting pose must be authored, still on screen and exposed at the grab point.
static func _settled_start(driver, card: Control) -> Vector2:
	if not await preload("res://smoke/gesture_settlement.gd").authored_pose(driver, card):
		driver._fail("card drag could not establish its exact resting baseline")
		return Vector2.INF
	if not await preload("res://smoke/gesture_settlement.gd").stable_global_pose(driver, card):
		driver._fail("card '%s' kept moving on screen; its drag has no stable baseline" % card.name)
		return Vector2.INF
	var settled := card.global_position
	var start: Vector2 = driver._card_body_point(card)
	if not await driver._control_owns_point(card, start):
		driver._fail("card '%s' has no exposed body from which to begin its drag: %s" % [card.name, _exposure_diagnostic(driver, card, settled, start)])
		return Vector2.INF
	return start

static func bounds(card: Control) -> Rect2:
	var pose := card.get_global_transform()
	var result := Rect2(pose * Vector2.ZERO, Vector2.ZERO)
	for point in [Vector2(card.size.x, 0), card.size, Vector2(0, card.size.y)]:
		result = result.expand(pose * point)
	return result



# Distinguishes a card that moved after its baseline from one another control covers.
static func _exposure_diagnostic(driver, card: Control, settled: Vector2, point: Vector2) -> String:
	if not is_instance_valid(card): return "retired"
	var hovered: Control = driver.render_viewport.gui_get_hovered_control()
	return "point=%s settled=%s current=%s hovered=%s" % [
		point, settled, card.global_position, hovered.get_path() if hovered != null else "none"]


static func _return_diagnostic(card: Control, expected: Transform2D) -> String:
	if not is_instance_valid(card): return "retired"
	return "%s expected=%s actual=%s local=%s resting=%s returning=%s lifted=%s tint=%s" % [card.name, expected, card.get_global_transform(), card.position, card.get_meta("spatial_resting_position", Vector2.INF), card.get_meta("card_return_active", false), card.is_set_as_top_level(), card.modulate]


static func _lift_diagnostic(driver, card: Control, origin: Vector2, pressed: Vector2, immediate: Vector2, start: Vector2, midpoint: Vector2) -> String:
	var expected := origin + midpoint - start
	var hovered: Control = driver.render_viewport.gui_get_hovered_control()
	return "origin=%s after_press=%s after_motion=%s expected=%s actual=%s error=%s z=%s top_level=%s pointer=%s hovered=%s resting=%s" % [
		origin, pressed, immediate, expected, card.global_position,
		card.global_position.distance_to(expected), card.z_index, card.is_set_as_top_level(),
		driver.render_viewport.get_mouse_position(), hovered.get_path() if hovered != null else "none",
		card.get_meta("spatial_resting_position", Vector2.INF)]
