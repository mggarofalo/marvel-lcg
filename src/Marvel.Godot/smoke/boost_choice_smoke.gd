extends "res://smoke/search_choice_smoke.gd"


func _run() -> void:
	if not await _open_android_game(): return
	if not await _choose_setup_upgrade(): return
	if not await _reach_android_boost(): return
	if not await _choose_android_consequence(): return
	if not await _select_ultron_threat(): return
	print("BOOST_CHOICE_SMOKE_OK scale=%d" % _scale_percentage())
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _open_android_game() -> bool:
	var packed := load("res://Main.tscn") as PackedScene
	if not await _open_setup(packed): return false
	await _configure_seeded_game()
	_select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Black Panther")
	_select_named_option(_node("Setup/Selections/Fields/Grid/Scenario"), "Ultron")
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "810709081"
	seed.text_changed.emit(seed.text)
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _button_named("Keep hand") != null):
		_fail("opening hand did not arrive")
		return false
	if not await _pointer_activate(_button_named("Keep hand")): return false
	return await _wait_for(func() -> bool: return _search_frame() != null)


func _choose_setup_upgrade() -> bool:
	for _page in range(4):
		for candidate in _search_frame().find_children("SearchResult*", "Control", true, false):
			if "Energy Daggers" in _visible_text(candidate):
				if not await _keyboard_activate(candidate): return false
				if not await _pointer_activate(_button_named("Add Energy Daggers to your hand")): return false
				return await _wait_for(func() -> bool: return _button_named("End turn") != null)
		var next := main.find_child("NextSearchPage", true, false) as Button
		if next == null or not await _keyboard_activate(next): break
		await process_frame
	_fail("setup search could not select Energy Daggers")
	return false


func _boost_choice_fits() -> bool:
	for title in ["Spend resources", "Engage 1 Drone"]:
		var choice := _boost_option(title)
		if choice == null or not _control_is_fully_visible(choice):
			_fail("required alternative is clipped: %s rect=%s visible=%s" % [title,
				choice.get_global_rect() if choice != null else Rect2(),
				_visible_control_rect(choice) if choice != null else Rect2()])
			return false
	var spend := _boost_option("Spend resources")
	var symbols := spend.find_child("ResourceSymbols", true, false) as TextureRect
	if symbols == null or symbols.texture == null or not _control_is_fully_visible(symbols):
		_fail("typed resource alternative has no canonical icon")
		return false
	return _cause_and_draft_fit()


func _cause_and_draft_fit() -> bool:
	var heading := main.find_child("ContextualHeading", true, false) as Control
	if heading == null or not _control_is_fully_visible(heading):
		_fail("the decision heading is clipped")
		return false
	var context := main.find_child("CausalContext", true, false) as Control
	for label in context.find_children("*", "Label", true, false):
		if label.is_visible_in_tree() and not label.text.is_empty() and not _control_is_fully_visible(label):
			_fail("the attack cause is clipped: %s rect=%s visible=%s" % [label.text,
				label.get_global_rect(), _visible_control_rect(label)])
			return false
	return true


func _boost_option(title: String) -> Button:
	for choice in main.find_children("ContextAction*", "Button", true, false):
		if choice.text == title or title in _visible_text(choice): return choice
	return null


func _end_first_player_turn() -> bool:
	if not await _select_attached_action(1, "Change Form"): return false
	if not await _commit_once("Change Form"): return false
	if not await _turn_end_starts_visible(): return false
	if not await _pointer_activate(_button_named("End turn")): return false
	if not await _wait_for(func() -> bool: return _button_named("Keep hand") != null):
		_fail("ending the turn did not open hand discards")
		return false
	if not await _discard_seeded_resources(): return false
	if not await _wait_for(func() -> bool: return _button_named("Leave attack undefended") != null):
		_fail("the seeded attack did not offer an undefended choice")
		return false
	return true


func _reach_android_boost() -> bool:
	if not await _end_first_player_turn(): return false
	if not await _selected_defense_fits(): return false
	if not await _pointer_activate(_button_named("Leave attack undefended")): return false
	if not await _wait_for(func() -> bool: return _button_named("Engage 1 Drone") != null):
		_fail("the Android Efficiency boost did not offer its consequence")
		return false
	return true


func _choose_android_consequence() -> bool:
	await process_frame
	await process_frame
	if not _boost_choice_fits(): return false
	if not await _pointer_activate(_boost_option("Spend resources")): return false
	await process_frame
	await process_frame
	if not _cause_and_draft_fit(): return false
	var payment_commit := _button_named("Spend resources")
	if payment_commit == null or not _control_is_fully_visible(payment_commit):
		_fail("the typed payment commitment is clipped")
		return false
	if not await _pointer_activate(_button_named("Cancel draft")): return false
	await process_frame
	if not await _pointer_activate(_boost_option("Engage 1 Drone")): return false
	if not await _commit_once("Engage 1 Drone"): return false
	return true


func _select_ultron_threat() -> bool:
	var threat_label := "Place 1 threat on The Crimson Cowl"
	if not await _wait_for(func() -> bool: return _button_named(threat_label) != null):
		_fail("Ultron's follow-on choice did not arrive")
		return false
	if not await _pointer_activate(_button_named(threat_label)): return false
	await process_frame
	await process_frame
	if not _cause_and_draft_fit(): return false
	var summary := main.find_child("DraftProgress", true, false) as Label
	if "1/3" not in summary.text or "Interrupts and prevention" not in summary.text:
		_fail("the selected threat option lost its quantity or uncertainty")
		return false
	return true


func _discard_seeded_resources() -> bool:
	for title in ["Vibranium", "Energy"]:
		var card := _mulligan_card(title)
		var discard := card.find_child("Card*Target", true, false) as Button
		if title == "Energy" and discard != null:
			discard.grab_focus()
			await create_timer(0.12).timeout
			if render_viewport.gui_get_focus_owner() != discard:
				_fail("delayed restoration replaced the newly focused Energy discard")
				return false
		if discard == null or not await _keyboard_activate(discard):
			_fail("cannot stage discard of " + title)
			return false
	if not await _commit_once("Discard two cards"): return false
	return true


func _selected_defense_fits() -> bool:
	if not await _select_attached_action(1, "Defense"): return false
	await process_frame
	await process_frame
	if not _cause_and_draft_fit(): return false
	var summary := main.find_child("DraftProgress", true, false) as Label
	if summary == null or "Exhaust Black Panther" not in summary.text \
			or "DEF 2" not in summary.text or "unresolved" not in summary.text:
		_fail("selected defense lost exhaustion, defense value or boost uncertainty")
		return false
	return await _pointer_activate(_button_named("Cancel draft"))


func _turn_end_starts_visible() -> bool:
	var end_turn := _button_named("End turn")
	await process_frame
	await process_frame
	if end_turn == null or not _control_is_fully_visible(end_turn):
		_fail("End turn must be visible without paging when the turn resumes")
		return false
	return _cause_and_draft_fit()
