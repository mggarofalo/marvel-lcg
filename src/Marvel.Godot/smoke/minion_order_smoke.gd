extends "res://smoke/boost_choice_smoke.gd"


func _run() -> void:
	if not await _open_android_game(): return
	if not await _choose_setup_upgrade(): return
	if not await _reach_android_boost(): return
	if not await _choose_android_consequence(): return
	if not await _wait_for(func() -> bool: return _boost_option("Engage 1 Drone") != null):
		_fail("Ultron did not offer its post-attack Drone consequence")
		return
	if not await _pointer_activate(_boost_option("Engage 1 Drone")): return
	if not await _commit_once("Engage another Drone"): return
	if not await _wait_for(func() -> bool: return main.find_child("MinionOrderCards", true, false) != null):
		_fail("minion ordering did not open its card chooser")
		return
	await process_frame
	await process_frame
	if not await _order_cards_without_submitting(): return
	print("MINION_ORDER_SMOKE_OK scale=%d" % _scale_percentage())
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _order_cards_without_submitting() -> bool:
	var frame := _search_frame()
	var cards := frame.find_children("MinionOrderCard*", "Control", true, false)
	# The row's name also matches the prefix; only CardControl faces have CardSurface.
	cards = cards.filter(func(card: Node) -> bool: return card.get_node_or_null("CardSurface") != null)
	var targets := frame.find_children("Target*", "Button", true, false)
	if not _ordering_cards_are_visible(frame, cards, targets): return false
	var names: Array[String] = []
	for target in targets: names.append(str(target.name))
	var before: String = _node("Toolbar/SyncStatus").text
	var confirm := _button_named("Confirm minion activation order")
	if confirm == null or not confirm.disabled:
		_fail("opening the chooser preselected a minion order")
		return false
	if not await _stage_revised_minion_order(cards, names, before): return false
	if not await _ordering_pages_keep_keyboard_focus(before): return false
	confirm = _button_named("Confirm minion activation order")
	if confirm.disabled or not _control_is_fully_visible(confirm):
		_fail("complete minion order has no visible commitment")
		return false
	if not await _pointer_activate(confirm): return false
	if not await _wait_for(func() -> bool: return _search_frame() == null and _node("Toolbar/SyncStatus").text != before):
		_fail("confirming the order did not return to the next engine decision")
		return false
	return true


func _ordering_pages_keep_keyboard_focus(revision: String) -> bool:
	var original_size: Vector2i = render_viewport.size
	render_viewport.size = Vector2i(1060, 1080)
	if not await _wait_for(func() -> bool: return main.find_child("NextMinionPage", true, false) != null):
		_fail("narrow ordering gallery did not expose paging")
		return false
	for _frame in 6: await process_frame
	for name in ["NextMinionPage", "PreviousMinionPage"]:
		var button := main.find_child(name, true, false) as Button
		if not await _keyboard_activate(button): return false
		await process_frame
		var expected := "PreviousMinionPage" if name == "NextMinionPage" else "NextMinionPage"
		var focused := render_viewport.gui_get_focus_owner()
		if focused == null or str(focused.name) != expected:
			_fail("ordering page boundary lost its enabled keyboard route")
			return false
	if _node("Toolbar/SyncStatus").text != revision:
		_fail("paging ordering cards changed the authoritative game")
		return false
	render_viewport.size = original_size
	for _frame in 4: await process_frame
	return true


func _ordering_cards_are_visible(frame: Control, cards: Array[Node], targets: Array[Node]) -> bool:
	if cards.size() != 3 or targets.size() != 3:
		_fail("the legal seed did not expose three distinct minion cards")
		return false
	for card in cards:
		if not _control_is_fully_visible(card) or not frame.get_global_rect().encloses(card.get_global_rect()):
			_fail("an ordering card is clipped")
			return false
	for scroll in frame.find_children("*", "ScrollContainer", true, false):
		if scroll.is_visible_in_tree():
			_fail("card ordering introduced a scrolling modal")
			return false
	for target in targets:
		if not _control_is_fully_visible(target):
			_fail("an ordering control is clipped")
			return false
	return true


func _stage_revised_minion_order(cards: Array[Node], names: Array[String], before: String) -> bool:
	if not await _keyboard_activate(cards[2]): return false
	await process_frame
	var focused := render_viewport.gui_get_focus_owner()
	if focused == null or str(focused.name) != names[2]:
		_fail("activating a minion face lost its keyboard focus")
		return false
	if not await _keyboard_activate(_search_frame().find_child(names[0], true, false)): return false
	var confirm := _button_named("Confirm minion activation order")
	if not confirm.disabled:
		_fail("a partial minion order became committable")
		return false
	if not await _keyboard_activate(_search_frame().find_child(names[1], true, false)): return false
	var sequence := main.find_child("MinionActivationSequence", true, false) as Label
	if sequence.text != "C → A → B" or _node("Toolbar/SyncStatus").text != before:
		_fail("ordering cards lost copy identity or changed authoritative state")
		return false
	if not await _keyboard_activate(_search_frame().find_child(names[2], true, false)): return false
	if not await _keyboard_activate(_search_frame().find_child(names[2], true, false)): return false
	sequence = main.find_child("MinionActivationSequence", true, false) as Label
	if sequence.text != "A → B → C" or _node("Toolbar/SyncStatus").text != before:
		_fail("revising the activation order lost its uncommitted draft")
		return false
	return true
