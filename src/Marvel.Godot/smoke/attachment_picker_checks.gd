extends RefCounted


static func inspect_played_upgrade(driver) -> bool:
	var drawer := driver.main.find_child("InspectAttachments1", true, false) as MenuButton
	if drawer == null or not drawer.is_visible_in_tree() or drawer.disabled:
		driver._fail("the played Web-Shooter has no attachment menu beside its identity")
		return false
	var revision: String = (driver._node("Toolbar/SyncStatus") as Label).text
	var popup := await _select_public_upgrade(driver, drawer)
	if popup == null: return false
	if not await _dismiss_and_reopen(driver, drawer, popup): return false
	if (driver._node("Toolbar/SyncStatus") as Label).text != revision:
		driver._fail("attachment inspection committed a game action")
		return false
	return true


static func _select_public_upgrade(driver, drawer: MenuButton) -> PopupPanel:
	if not await driver._keyboard_activate(drawer): return null
	var menu := drawer.get_popup()
	if not menu.visible or menu.item_count != 1 or "Web-Shooter" not in menu.get_item_text(0):
		driver._fail("the attachment menu did not list the visible upgrade")
		return null
	# Exercise the selection callback; real menu input is reviewed in the client.
	menu.hide()
	menu.id_pressed.emit(menu.get_item_id(0))
	await driver.process_frame
	var popup := driver.root.find_child("PileInspector", true, false) as PopupPanel
	if popup == null or not popup.visible or "Web-Shooter" not in driver._visible_text(popup):
		driver._fail("choosing an attachment did not open its card inspector")
		return null
	return popup


static func _dismiss_and_reopen(driver, drawer: MenuButton, popup: PopupPanel) -> bool:
	var lifetime: WeakRef = weakref(popup)
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	popup.push_input(escape, true)
	if not await driver._wait_for(func() -> bool: return lifetime.get_ref() == null):
		driver._fail("attachment inspection did not dismiss with Escape")
		return false
	if not await driver._wait_for(func() -> bool:
		return driver.render_viewport.gui_get_focus_owner() == drawer):
		driver._fail("closing attachment inspection did not restore menu focus")
		return false
	if not await driver._keyboard_activate(drawer) or not drawer.get_popup().visible:
		driver._fail("the attachment menu did not reopen from its restored keyboard focus")
		return false
	drawer.get_popup().hide()
	return true
