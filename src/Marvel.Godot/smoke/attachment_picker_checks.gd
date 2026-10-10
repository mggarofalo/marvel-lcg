extends RefCounted


static func inspect_played_upgrade(driver) -> bool:
	var tableau := driver.main.find_child("SourceTableau", true, false) as Control
	var card := _installed_card(tableau, "Web-Shooter")
	if tableau == null or card == null or not tableau.is_ancestor_of(card) \
			or not driver._control_is_fully_visible(card):
		driver._fail("the played Web-Shooter has no visible installed tableau tile")
		return false
	var revision: String = (driver._node("Toolbar/SyncStatus") as Label).text
	if not await driver._keyboard_activate(card): return false
	var inspector := driver.main.get_node("CardInspector") as Control
	if not inspector.visible or "Web-Shooter" not in driver._visible_text(inspector) \
			or inspector.find_child("CardFace", true, false) == null:
		driver._fail("the installed source tile did not open its complete card face")
		return false
	if not await driver._close_inspector_with_keyboard(card, inspector): return false
	if (driver._node("Toolbar/SyncStatus") as Label).text != revision:
		driver._fail("source inspection committed a game action")
		return false
	return true


static func _installed_card(tableau: Control, title: String) -> Control:
	if tableau == null: return null
	for candidate in tableau.find_children("*", "Control", true, false):
		if candidate.get_meta("source_title", "") == title:
			return candidate as Control
	return null
