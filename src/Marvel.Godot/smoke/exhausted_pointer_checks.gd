extends RefCounted

static func perform(driver, card: Control, anchor: int) -> bool:
	if not await preload("res://smoke/gesture_settlement.gd").authored_pose(driver, card):
		driver._fail("exhausted pointer check could not establish its exact resting baseline")
		return false
	card.force_update_transform()
	var pose := card.get_global_transform()
	var revision: String = str(driver._node("Toolbar/SyncStatus").get_meta("response_revision", -1))
	var commit: Button = driver._task_commit()
	var commitment: String = commit.text if commit != null else ""
	if not await driver._pointer_activate_card_body(card): return false
	var inspector: Control = driver.main.get_node("CardInspector")
	if not inspector.visible or int(inspector.get_meta("inspected_card_anchor", -1)) != anchor:
		driver._fail("exhausted card body helper did not inspect its exact instance")
		return false
	for pressed in [true, false]:
		var escape := InputEventKey.new()
		escape.keycode = KEY_ESCAPE
		escape.pressed = pressed
		driver.render_viewport.push_input(escape, true)
		await driver.process_frame
	if not card.get_global_transform().is_equal_approx(pose) or inspector.visible:
		driver._fail("exhausted inspection dismissal changed the physical pose")
		return false
	if not await _invalid_drops(driver, card, pose, revision, commitment): return false
	print("EXHAUSTED_POINTER_HELPERS_OK anchor=%d" % anchor)
	return true

static func _invalid_drops(driver, card: Control, pose: Transform2D, revision: String, commitment: String) -> bool:
	var enemy: Control = driver._card_for_anchor(driver.RHINO)
	for finish in [Vector2(4, 4), enemy.get_global_rect().get_center()]:
		if not await driver.CardDrag.perform(driver, card, finish): return false
		if not await preload("res://smoke/gesture_settlement.gd").global_pose(driver, card, pose):
			driver._fail("invalid exhausted drop did not settle at its saved physical pose")
			return false
		# global_pose already established the authored local pose and bounded global
		# transform above. No frame elapses before these independent state checks.
		if card.is_set_as_top_level():
			driver._fail("invalid exhausted drop retained its lifted presentation")
			return false
		var actual_revision := str(driver._node("Toolbar/SyncStatus").get_meta("response_revision", -1))
		if actual_revision != revision:
			driver._fail("invalid exhausted drop changed revision: expected=%s actual=%s" % [revision, actual_revision])
			return false
		var commit: Button = driver._task_commit()
		var actual_commitment: String = commit.text if commit != null else ""
		if actual_commitment != commitment:
			driver._fail("invalid exhausted drop changed commitment: expected=%s actual=%s" % [commitment, actual_commitment])
			return false
	return true
