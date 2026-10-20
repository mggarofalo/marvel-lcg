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
	if not await _staged_choice_once(_boost_option("Engage 1 Drone")): return false
	if not await _wait_for(func() -> bool: return _activation_buttons().size() == 3):
		_fail("next-minion decision did not expose three direct Activate controls")
		return false
	return true


func _activation_buttons() -> Array[Button]:
	var result: Array[Button] = []
	for candidate in main.find_children("Card*Action", "Button", true, false):
		if (candidate as Button).text == "Activate" and candidate.is_visible_in_tree():
			result.append(candidate as Button)
	return result


func _choose_one_minion() -> bool:
	var buttons := _activation_buttons()
	if buttons.size() != 3 or main.find_child("NextSearchPage", true, false) != null:
		_fail("minion ordering must expose all physical minions without a paging chooser")
		return false
	var before: String = str(_node("Toolbar/SyncStatus").get_meta("response_revision", -1))
	if _task_commit() != null:
		_fail("opening minion order silently selected an answer")
		return false
	if not await _keyboard_activate(buttons[2]): return false
	if not await _wait_for(func() -> bool: return str(_node("Toolbar/SyncStatus").get_meta("response_revision", -1)) != before):
		_fail("one explicit Activate did not commit that exact minion")
		return false
	if not _activation_buttons().is_empty():
		_fail("minion ordering skipped the chosen activation's intervening decision")
		return false
	return true
