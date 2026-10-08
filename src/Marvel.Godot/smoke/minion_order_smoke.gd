extends "res://smoke/boost_choice_smoke.gd"


func _run() -> void:
	if not await _open_android_game(): return
	if not await _choose_setup_upgrade(): return
	if not await _reach_android_boost(): return
	if not await _choose_android_consequence(): return
	if not await _engage_post_attack_drone(): return
	for _frame in 3: await process_frame
	if not await _choose_one_minion(): return
	print("MINION_ORDER_SMOKE_OK scale=%d" % _scale_percentage())
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _engage_post_attack_drone() -> bool:
	if not await _wait_for(func() -> bool: return _boost_option("Engage 1 Drone") != null):
		_fail("Ultron did not offer its post-attack Drone consequence")
		return false
	if not await _pointer_activate(_boost_option("Engage 1 Drone")): return false
	if not await _commit_once("Engage another Drone"): return false
	if not await _wait_for(func() -> bool: return main.find_child("SearchCards", true, false) != null):
		_fail("next-minion decision did not open its card chooser")
		return false
	return true


func _choose_one_minion() -> bool:
	var frame := _search_frame()
	var cards := frame.find_children("SearchResult*", "Control", true, false)
	if not _check_minion_candidates(cards): return false
	if not await preload("res://smoke/search_inspection_checks.gd").perform(self): return false
	var selected_name := "Affordance" + str(cards[2].name).trim_prefix("SearchResult")
	var before: String = _node("Toolbar/SyncStatus").text
	if not await _stage_next_minion(selected_name, before): return false
	if not await preload("res://smoke/search_inspection_checks.gd").perform(self): return false
	if not await _page_next_minions(before): return false
	if not await _pointer_activate(_next_commit()): return false
	if not await _wait_for(func() -> bool: return _node("Toolbar/SyncStatus").text != before):
		_fail("confirmed minion choice did not advance the authoritative decision")
		return false
	if _search_frame() != null:
		_fail("the minion chooser skipped the selected activation's intervening decision")
		return false
	return true


func _check_minion_candidates(cards: Array[Node]) -> bool:
	if cards.size() != 3:
		_fail("the legal seed did not expose three distinct next-minion candidates")
		return false
	for card in cards:
		if not _control_is_fully_visible(card):
			_fail("a next-minion candidate is clipped")
			return false
	if _next_commit() != null:
		_fail("opening the next-minion chooser silently staged an answer")
		return false
	return true


func _stage_next_minion(selected_name: String, before: String) -> bool:
	if not await _keyboard_activate(_search_frame().find_child(selected_name, true, false)): return false
	await process_frame
	if _node("Toolbar/SyncStatus").text != before:
		_fail("choosing a next minion submitted before explicit confirmation")
		return false
	var commit := _next_commit()
	if commit == null or commit.disabled:
		_fail("one selected minion did not produce a named next-activation commitment")
		return false
	return true


func _next_commit() -> Button:
	var submit := main.find_child("Submit", true, false) as Button
	return submit if submit != null and submit.text.begins_with("Activate ") and submit.text.ends_with(" next") else null


func _page_next_minions(revision: String) -> bool:
	var original_size: Vector2i = render_viewport.size
	render_viewport.size = Vector2i(1060, 1080)
	if not await _wait_for(func() -> bool: return main.find_child("NextSearchPage", true, false) != null):
		_fail("narrow next-minion gallery did not expose paging")
		return false
	for _frame in 6: await process_frame
	for name in ["NextSearchPage", "PreviousSearchPage"]:
		if not await _keyboard_activate(main.find_child(name, true, false)): return false
		await process_frame
		var expected := "PreviousSearchPage" if name == "NextSearchPage" else "NextSearchPage"
		if not await _has_focus(expected): return false
		if _next_commit() == null or _node("Toolbar/SyncStatus").text != revision:
			_fail("paging lost the staged next minion or submitted it")
			return false
	render_viewport.size = original_size
	for _frame in 4: await process_frame
	return true
