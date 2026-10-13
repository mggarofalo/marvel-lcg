extends RefCounted

static func perform(driver, card: Control, anchor: int) -> bool:
	var pose := card.get_global_transform()
	var revision: String = driver._node("Toolbar/SyncStatus").text
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
	var enemy: Control = driver._card_for_anchor(driver.RHINO)
	for finish in [Vector2(4, 4), enemy.get_global_rect().get_center()]:
		if not await driver.CardDrag.perform(driver, card, finish): return false
		await driver.create_timer(0.25).timeout
		commit = driver._task_commit()
		if card.is_set_as_top_level() or not card.get_global_transform().is_equal_approx(pose) \
				or driver._node("Toolbar/SyncStatus").text != revision \
				or (commit.text if commit != null else "") != commitment:
			driver._fail("invalid exhausted drop changed pose, revision or commitment")
			return false
	print("EXHAUSTED_POINTER_HELPERS_OK anchor=%d" % anchor)
	return true
