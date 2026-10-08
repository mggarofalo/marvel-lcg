extends RefCounted


static func inspect_played_upgrade(driver) -> bool:
	var ledger := driver.main.find_child("ControlledSourceLedger", true, false) as Control
	var drawer := ledger.find_child("SourcePicker", true, false) as MenuButton if ledger != null else null
	if drawer == null or not drawer.is_visible_in_tree() or drawer.disabled:
		driver._fail("the played Web-Shooter has no controlled-upgrade source picker")
		return false
	var revision: String = (driver._node("Toolbar/SyncStatus") as Label).text
	if not await _select_public_upgrade(driver, drawer): return false
	var card := ledger.find_child("ProceduralCard*", true, false) as Control
	if card == null or not await driver._keyboard_activate(card): return false
	var inspector := driver.main.get_node("CardInspector") as Control
	if not inspector.visible or "Web-Shooter" not in driver._visible_text(inspector) \
			or inspector.find_child("CardFace", true, false) == null:
		driver._fail("the source strip did not open its complete B1 card face")
		return false
	if not await driver._close_inspector_with_keyboard(card, inspector): return false
	if not await driver._keyboard_activate(drawer) or not drawer.get_popup().visible:
		driver._fail("the controlled source picker did not reopen with keyboard input")
		return false
	drawer.get_popup().hide()
	if (driver._node("Toolbar/SyncStatus") as Label).text != revision:
		driver._fail("source inspection committed a game action")
		return false
	return true


static func _select_public_upgrade(driver, drawer: MenuButton) -> bool:
	if not await driver._keyboard_activate(drawer): return false
	var menu := drawer.get_popup()
	if not menu.visible or menu.item_count != 1 or "Web-Shooter" not in menu.get_item_text(0):
		driver._fail("the source picker did not list the visible upgrade")
		return false
	if menu.get_focused_item() < 0: await _key(driver, KEY_DOWN)
	await _key(driver, KEY_ENTER)
	if menu.visible or driver.render_viewport.gui_get_focus_owner() != drawer:
		driver._fail("source selection did not return focus to its title picker: visible=%s focus=%s" % [menu.visible, driver.render_viewport.gui_get_focus_owner()])
		return false
	return true


static func _key(driver, code: int) -> void:
	for pressed in [true, false]:
		var key := InputEventKey.new()
		key.keycode = code
		key.pressed = pressed
		driver.render_viewport.push_input(key, true)
		await driver.process_frame
