extends RefCounted

# Checks the actual primary table surfaces without using concealed game state.
static func restricted_guest_is_safe(driver: Node) -> bool:
	var host_hand := driver.host.find_child("AstraTableSurface", true, false) as Control
	var guest_hand := driver.guest.find_child("AstraTableSurface", true, false) as Control
	if host_hand == null:
		host_hand = driver._node(driver.host, "Play/Board/HandShelf") as Control
	if guest_hand == null:
		guest_hand = driver._node(driver.guest, "Play/Board/HandShelf") as Control
	if host_hand == null or guest_hand == null:
		driver._fail("the restricted clients do not expose their distinct hand shelves")
		return false
	var host_cards := host_hand.find_children("ProceduralCard*", "", true, false)
	if host_hand.name == &"AstraTableSurface":
		host_cards = host_cards.filter(func(card: Node) -> bool:
			return card.has_meta("spatial_hand_index"))
	if host_cards.is_empty():
		driver._fail("the prompt owner has no rendered private hand to protect")
		return false
	var guest_text: String = driver._visible_text(driver.guest)
	for node in host_cards:
		var title := (node as Control).find_child("Title", true, false) as Label
		if title != null and not title.text.is_empty() and title.text in guest_text:
			driver._fail("the restricted guest rendered the prompt owner's private card face: %s" % title.text)
			return false
	if not guest_hand.find_children("MulliganDiscard*", "Button", true, false).is_empty() \
			or driver.guest.find_child("CompleteChoiceSheetOverlay", true, false) != null:
		driver._fail("the restricted guest rendered private opening-hand controls for another seat")
		return false
	return true



static func terminal_is_safe(driver: Node, main: Control) -> bool:
	var entry := main.find_child("CompleteChoiceSheet", true, false) as Button
	var cause := main.find_child("CausalContext", true, false) as Control
	return not driver._has_table_decision(main) \
		and (entry == null or entry.disabled) \
		and driver._first_enabled_choice(driver._decision(main)) == null \
		and cause != null and "Defeat" in driver._visible_text(cause) \
		and "The players lost." in driver._visible_text(cause)
