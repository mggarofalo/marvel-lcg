extends "res://smoke/repeated_commit_smoke.gd"


func _run() -> void:
	if not await _stage_core_attack(): return
	if not await _commit_once("Attack Rhino"): return
	if not await _review_attack_results(): return
	if not await _undo_clears_results(): return
	print("RESULT_PLAYBACK_SMOKE_OK")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _review_attack_results() -> bool:
	if not _unpaged_result_account(): return false
	if not await _set_history_drawer(false): return false
	var latest := main.find_child("LatestResult", true, false) as Label
	var toggle := main.find_child("ToggleHistory", true, false) as Button
	var rows := _node("Play/Prompt/Margin/Stack/Workbench/History/HistoryOccurrences") as Control
	if latest == null or toggle == null or not toggle.is_visible_in_tree():
		_fail("the completed attack has no durable result or detail disclosure")
		return false
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	if not await _review_attack_disclosure(toggle, rows, latest): return false
	if not await _rapid_details_work(toggle, rows): return false
	if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision:
		_fail("read-only chronology changed the game revision")
		return false
	return true


func _review_attack_disclosure(toggle: Button, rows: Control, latest: Label) -> bool:
	if not await _pointer_activate(toggle): return false
	if not _attack_consequences_visible(rows): return false
	if not await _native_occurrence_details(rows): return false
	if not await _capture_checkpoint("history-attack-expanded"): return false
	if not await _pointer_activate(toggle): return false
	if rows.is_visible_in_tree() or latest.text.is_empty():
		_fail("collapsing details erased the latest result or left details open")
		return false
	return true


func _undo_clears_results() -> bool:
	var history := main.find_child("ToggleHistory", true, false) as Button
	if not (_node("Play/Prompt/Margin/Stack/Workbench/History/HistoryOccurrences") as Control).is_visible_in_tree():
		if history == null or not await _pointer_activate(history): return false
	var undo := main.find_child("UndoLast", true, false) as Button
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	if undo == null or undo.disabled or not await _pointer_activate(undo): return false
	if not await _wait_for(func() -> bool:
		return str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision): return false
	var account := main.find_child("EventActivity", true, false) as Control
	if account != null and account.is_visible_in_tree():
		_fail("undo retained reverted attack details")
		return false
	return true


func _unpaged_result_account() -> bool:
	for name in ["PlayResults", "NextResult", "PreviousResult", "EventPlayback"]:
		if main.find_child(name, true, false) != null:
			_fail("history still exposes timed or paged result navigation")
			return false
	return true


func _attack_consequences_visible(rows: Control) -> bool:
	if not rows.is_visible_in_tree():
		_fail("one click did not expose the complete history")
		return false
	var latest := main.find_child("LatestResult", true, false) as Label
	var text := latest.text.to_lower()
	if "exhaust" not in text or "14 to 12" not in text:
		_fail("the attack account lost authoritative exhaustion/damage")
		return false
	if text.find("exhaust") > text.find("14 to 12"):
		_fail("the attack account lost authoritative order")
		return false
	return true


func _rapid_details_work(toggle: Button, rows: Control) -> bool:
	_inject_repeated_click(toggle.get_global_rect().get_center())
	await process_frame
	if not rows.is_visible_in_tree():
		_fail("rapid read-only detail expansion was suppressed as a game commitment")
		return false
	if not await _pointer_activate(toggle): return false
	return true


func _native_occurrence_details(rows: Control) -> bool:
	var cards := rows.find_child("Occurrences", true, false) as Control
	if cards == null or cards.get_child_count() == 0 or cards.get_child_count() > 16:
		_fail("history has no bounded pool of native occurrence cards")
		return false
	var selected: Button = null
	for card in cards.get_children():
		if card is Button and card.is_visible_in_tree() and _control_is_fully_visible(card):
			selected = card
	if selected == null or not await _pointer_activate(selected): return false
	var details := rows.find_child("OccurrenceDetails", true, false)
	var toggle := details.get_child(0) as Button
	if not (details.get_child(1) as Control).visible and not await _pointer_activate(toggle): return false
	if not (details.get_child(1) as Control).is_visible_in_tree():
		_fail("native occurrence details did not expand")
		return false
	if main.find_child("EventLog", true, false).is_visible_in_tree():
		_fail("native chronology is duplicated by a text account")
		return false
	return true
