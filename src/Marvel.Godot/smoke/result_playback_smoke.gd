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
	var play := main.find_child("PlayResults", true, false) as Button
	var next := main.find_child("NextResult", true, false) as Button
	var previous := main.find_child("PreviousResult", true, false) as Button
	var cue := main.find_child("ResultCue", true, false) as Label
	var position := main.find_child("ResultPosition", true, false) as Label
	if play == null or cue == null or not play.is_visible_in_tree():
		_fail("the completed attack has no visible result navigator")
		return false
	if play.text == "Ⅱ" and not await _pointer_activate(play): return false
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if not await _pointer_activate(play): return false
	if not await _wait_for(func() -> bool: return play.text == "▶"): return false
	if "Rhino" not in cue.text or "14 → 12" not in cue.text:
		_fail("settled attack playback did not retain Rhino's health consequence: " + cue.text)
		return false
	if not await _walk_attack_results(previous, next, cue, position): return false
	if (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("visual result navigation changed the game revision")
		return false
	if not cue.get_global_rect().encloses(Rect2(cue.global_position,
			Vector2(cue.size.x, cue.get_line_count() * cue.get_line_height()))):
		_fail("the result caption is clipped")
		return false
	return true


func _walk_attack_results(previous: Button, next: Button, cue: Label, position: Label) -> bool:
	if not await _pointer_activate(previous): return false
	if position.text != "1/2" or "exhaust" not in cue.text.to_lower():
		_fail("previous result did not recover the attack's exhaustion beat")
		return false
	if not await _pointer_activate(next): return false
	if position.text != "2/2" or "14 → 12" not in cue.text:
		_fail("next result did not recover the ordered health change")
		return false
	return true


func _undo_clears_results() -> bool:
	var undo := main.find_child("UndoLast", true, false) as Button
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if undo == null or undo.disabled or not await _pointer_activate(undo): return false
	if not await _wait_for(func() -> bool:
		return (_node("Toolbar/SyncStatus") as Label).text != revision): return false
	var navigator := main.find_child("EventPlayback", true, false) as Control
	var cue := main.find_child("ResultCue", true, false) as Label
	if navigator.is_visible_in_tree() or not cue.text.is_empty():
		_fail("undo retained the reverted attack in result playback")
		return false
	return true
