extends RefCounted

# Exact instance identities in the legal seeded Core opening hand.
static func perform(driver) -> bool:
	var original := _kept(driver)
	if original.size() != 6:
		driver._fail("reserved-order fixture lacks six exact opening instances")
		return false
	var revision: String = driver._node("Toolbar/SyncStatus").text
	var expected := await _single_reserved_slot(driver, original)
	if expected.is_empty(): return false
	var after := await _two_reserved_slots(driver, expected)
	if after.is_empty(): return false
	if not await _round_trip_and_cancel(driver, after): return false
	if not await _repeat_drag_return(driver, original[2], after): return false
	if driver._node("Toolbar/SyncStatus").text != revision:
		driver._fail("reversible hand ordering changed the authoritative revision")
		return false
	print("MULLIGAN_RESERVED_ORDER_OK ", after.map(func(card: Control): return card.name))
	return true

static func _single_reserved_slot(driver, original: Array) -> Array:
	var middle: Control = original[2]
	await _toggle(driver, middle)
	if driver._task_commit().text != "Redraw 1 card":
		driver._fail("staged middle card lacks its one-card commitment")
		return []
	if not await _move_last(driver, original[0]): return []
	await _toggle(driver, middle)
	var expected: Array = [original[1], original[3], middle, original[4], original[5], original[0]]
	if not _matches(driver, expected, "single reserved slot"): return []
	return expected

static func _two_reserved_slots(driver, expected: Array) -> Array:
	# Two reserved slots survive a second kept-card permutation and either return order.
	await _toggle(driver, expected[1])
	await _toggle(driver, expected[4])
	if not await _move_last(driver, expected[0]): return []
	var after: Array = [expected[2], expected[1], expected[3], expected[5], expected[4], expected[0]]
	await _toggle(driver, expected[4])
	await _toggle(driver, expected[1])
	if not _matches(driver, after, "two reserved slots"): return []
	return after

static func _round_trip_and_cancel(driver, after: Array) -> bool:
	for card in after: await _toggle(driver, card)
	if not _kept(driver).is_empty() or driver._task_commit().text != "Redraw 6 cards":
		driver._fail("all staged hand lacks its six-card commitment")
		return false
	for card in after: await _toggle(driver, card)
	if not _matches(driver, after, "all staged round trip"): return false
	await _toggle(driver, after[1])
	await _toggle(driver, after[4])
	if not await _cancel_insertion(driver, after[0]): return false
	var clear: Button = driver.main.find_child("ContextualCancelDraft", true, false)
	if not await driver._pointer_activate(clear): return false
	if not _matches(driver, after, "cancel insertion and clear replacements"): return false
	return true

static func _repeat_drag_return(driver, middle: Control, after: Array) -> bool:
	for _repeat in range(2):
		if not await driver._drag_mulligan_to_discard(middle): return false
		if not await driver._drag_mulligan_to_discard(middle): return false
		var hand: Control = driver.main.find_child("HandDropArea", true, false)
		if not await driver._drag_mulligan(middle, hand.get_global_rect().get_center()): return false
		await driver.create_timer(0.22).timeout
		if not _matches(driver, after, "repeated stage and drag return"): return false
	return true

static func _toggle(driver, card: Control) -> void:
	card.grab_focus()
	await driver._press_key(KEY_SPACE)
	await driver.process_frame

static func _move_last(driver, card: Control) -> bool:
	var hand: Control = driver.main.find_child("HandDropArea", true, false)
	if not await driver._drag_mulligan(card, Vector2(hand.get_global_rect().end.x - 15, hand.get_global_rect().get_center().y)):
		return false
	await driver.create_timer(0.22).timeout
	return true

static func _kept(driver) -> Array:
	var cards: Array = []
	for card in driver._hand_surface().find_children("ProceduralCard*", "", true, false):
		if card.has_meta("spatial_hand_index") and not card.get_meta("mulligan_staged", false): cards.append(card)
	cards.sort_custom(func(a: Control, b: Control): return a.position.x < b.position.x)
	return cards

static func _matches(driver, expected: Array, context: String) -> bool:
	var actual := _kept(driver)
	if actual != expected:
		driver._fail("%s changed exact instance order: %s expected %s" % [context, actual.map(func(card): return card.name), expected.map(func(card): return card.name)])
		return false
	var step: float = actual[1].position.x - actual[0].position.x
	for index in range(1, actual.size()):
		if abs(actual[index].position.x - actual[index - 1].position.x - step) > 1:
			driver._fail("%s left a gap in the returned hand" % context)
			return false
	if driver._task_commit().text != "Keep hand":
		driver._fail("%s left an unexpected staged selection" % context)
		return false
	return true

static func _cancel_insertion(driver, source: Control) -> bool:
	var start: Vector2 = await driver._exposed_card_body_point(source)
	if start == Vector2.INF: return false
	var hand: Control = driver.main.find_child("HandDropArea", true, false)
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = start
	press.global_position = start
	driver.render_viewport.push_input(press, true)
	var move := InputEventMouseMotion.new()
	move.position = Vector2(hand.get_global_rect().end.x - 15, hand.get_global_rect().get_center().y)
	move.global_position = move.position
	driver.render_viewport.push_input(move, true)
	await driver.process_frame
	await driver._press_key(KEY_ESCAPE)
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.position = Vector2(4, 4)
	release.global_position = release.position
	driver.render_viewport.push_input(release, true)
	await driver.create_timer(0.22).timeout
	return not source.is_set_as_top_level() and driver._task_commit().text == "Redraw 2 cards"
