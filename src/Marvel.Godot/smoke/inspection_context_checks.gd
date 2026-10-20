extends RefCounted

static func defend(driver) -> bool:
	var cause: Control = driver.main.find_child("CausalSituation", true, false)
	var allies: Control = driver.main.find_child("CollectionAlliesArea", true, false)
	if cause != null and allies != null and cause.get_global_rect().intersects(allies.get_global_rect()):
		driver._fail("defense situation overlaps the allies collection")
		return false
	var card: Control = driver._tabletop_card_named("Spider-Man")
	if card == null:
		driver._fail("defense inspection has no visible identity")
		return false
	card.grab_focus()
	for frame in range(4): await driver.process_frame
	if not _clear(driver): return false
	if not await driver._pointer_activate_card_body(card): return false
	for frame in range(4): await driver.process_frame
	if not _clear(driver): return false
	if not await driver._capture_checkpoint("defense-inspection-clear"): return false
	return await driver._close_inspector_with_keyboard(card, driver.main.find_child("CardInspector", true, false))

static func _clear(driver) -> bool:
	var inspector: Control = driver.main.find_child("CardInspector", true, false)
	if not inspector.is_visible_in_tree():
		driver._fail("defender focus did not expose the full card")
		return false
	var bounds: Rect2 = inspector.get_node("Frame").get_global_rect()
	if not preload("res://smoke/card_inspection_protection_checks.gd").verify(driver, inspector.get_node("Frame")): return false
	for name in ["CausalSituation", "ContextualDecision", "EventTimeline"]:
		var region: Control = driver.main.find_child(name, true, false)
		if region != null and region.is_visible_in_tree() and bounds.intersects(region.get_global_rect()):
			driver._fail("full defender inspection obscures " + name)
			return false
	return true
