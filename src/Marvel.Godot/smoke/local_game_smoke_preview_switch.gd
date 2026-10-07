extends RefCounted


static func perform(driver, hand: Control, inspector: Control) -> bool:
	var other: Control = null
	for candidate in driver.main.find_children("ProceduralCard*", "Control", true, false):
		if candidate != hand and candidate.is_visible_in_tree() \
				and not candidate.has_meta("spatial_hand_index") \
				and not inspector.is_ancestor_of(candidate):
			other = candidate
			break
	if other == null:
		driver._fail("preview switching has no visible table card")
		return false
	var previous := hand
	for target in [other, hand]:
		var point: Vector2 = await driver._exposed_card_body_point(target)
		if point == Vector2.INF:
			driver._fail("preview switching cannot reach a card body")
			return false
		driver._position_pointer_without_settle(point)
		await driver.process_frame
		# A spatial reordering may deliver the former source's exit after the
		# new source entered. Exercise that delivery order on the real controls.
		previous.emit_signal("mouse_exited")
		driver._position_pointer_without_settle(point)
		await driver.main.get_tree().create_timer(0.38).timeout
		var expected := str(target.name).trim_prefix("ProceduralCard").to_int()
		if not inspector.visible or int(inspector.get_meta("inspected_card_anchor", -1)) != expected:
			driver._fail("leaving the previous card dismissed the current card's preview")
			return false
		previous = target
	return true
