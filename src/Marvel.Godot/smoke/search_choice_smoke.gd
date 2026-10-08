extends "res://smoke/local_game_smoke.gd"


func _run() -> void:
	if not await _open_required_search(): return
	await process_frame
	await process_frame
	if not _search_fits(): return
	if not await _automatic_search_restores_focus(): return
	if not await _compare_and_stage_search(): return
	if not await _paging_preserves_selection(): return
	if not await _dismissal_preserves_selection(): return
	if not _search_fits(): return
	if not await _commit_search_choice(): return
	print("SEARCH_CHOICE_SMOKE_OK scale=%d viewport=%s" % [_scale_percentage(), _viewport_size()])
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _compare_and_stage_search() -> bool:
	if not await preload("res://smoke/search_inspection_checks.gd").perform(self): return false
	if not await _stage_search_choice(): return false
	return await preload("res://smoke/search_inspection_checks.gd").perform(self)


func _open_required_search() -> bool:
	var packed := load("res://Main.tscn") as PackedScene
	if not await _open_setup(packed): return false
	await _configure_seeded_game()
	_select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Black Panther")
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "31"
	seed.text_changed.emit(seed.text)
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _button_named("Keep hand") != null):
		_fail("opening hand did not arrive")
		return false
	if not await _pointer_activate(_button_named("Keep hand")): return false
	if not await _wait_for(func() -> bool: return _search_frame() != null):
		_fail("the authorized setup search did not open its card choices")
		return false
	return true


func _stage_search_choice() -> bool:
	var choices := _search_frame().find_children("SearchResult*", "Control", true, false)
	if choices.is_empty():
		_fail("search has no card selection")
		return false
	var expected_focus := "Affordance" + str(choices[0].name).trim_prefix("SearchResult")
	var select := _search_frame().find_child(expected_focus, true, false) as Button
	if not await _keyboard_activate(select): return false
	if not await _has_focus(expected_focus): return false
	await process_frame
	var submit := _button_named("Add Tactical Genius to your hand")
	if submit == null or submit.disabled:
		_fail("selecting a card did not stage a named commitment")
		return false
	return true


func _commit_search_choice() -> bool:
	if not await _pointer_activate(_button_named("Add Tactical Genius to your hand")): return false
	if not await _wait_for(func() -> bool: return _search_frame() == null):
		_fail("committed search stayed open")
		return false
	if not await _wait_for(func() -> bool: return _button_named("End turn") != null):
		_fail("setup search did not advance to the player turn")
		return false
	return true


func _search_frame() -> Control:
	var frame := main.find_child("CompleteChoicesFrame", true, false) as Control
	return frame if frame != null and frame.is_visible_in_tree() and not frame.is_queued_for_deletion() else null


func _search_fits() -> bool:
	var frame := _search_frame()
	if not _control_is_fully_visible(frame):
		_fail("search frame %s extends beyond viewport %s (minimum %s)" % [
			frame.get_global_rect(), _viewport_size(), frame.get_combined_minimum_size()])
		return false
	if not frame.find_children("*", "ScrollContainer", true, false).is_empty():
		_fail("search choices introduced a scrollbar container")
		return false
	var cards := frame.find_children("SearchResult*", "Control", true, false)
	if cards.size() < 2:
		_fail("search did not show multiple complete cards")
		return false
	for card in cards:
		if not _control_is_fully_visible(card as Control):
			_fail("a search card is clipped")
			return false
	return true


func _paging_preserves_selection() -> bool:
	var original_size: Vector2i = render_viewport.size
	if not await preload("res://smoke/search_inspection_checks.gd").replace_surface(self, Vector2i(1280, 900)): return false
	await process_frame
	await process_frame
	await process_frame
	var next := main.find_child("NextSearchPage", true, false) as Button
	if next == null:
		_fail("resizing the search did not page the cards")
		return false
	if not await _wait_for(func() -> bool: return _control_is_fully_visible(_search_frame())):
		return _search_fits()
	if not _search_fits(): return false
	if not await _keyboard_activate(next): return false
	if not await _has_focus("PreviousSearchPage"): return false
	await process_frame
	if _button_named("Add Tactical Genius to your hand") == null:
		_fail("paging lost the staged choice")
		return false
	var previous := main.find_child("PreviousSearchPage", true, false) as Button
	if not await _keyboard_activate(previous): return false
	if not await _has_focus("NextSearchPage"): return false
	await process_frame
	render_viewport.size = original_size
	await process_frame
	await process_frame
	return true


func _dismissal_preserves_selection() -> bool:
	if not await _pointer_activate(_button_named("Return to table")): return false
	await process_frame
	if _search_frame() != null:
		_fail("return to table did not dismiss search choices")
		return false
	var reopen := main.find_child("CompleteChoiceSheet", true, false) as Button
	if reopen == null:
		_fail("dismissed search has no way to reopen")
		return false
	if not await _pointer_activate(reopen): return false
	await process_frame
	await process_frame
	if _button_named("Add Tactical Genius to your hand") == null:
		_fail("reopening lost the staged search choice")
		return false
	return true


func _automatic_search_restores_focus() -> bool:
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	render_viewport.push_input(escape)
	var release := InputEventKey.new()
	release.keycode = KEY_ESCAPE
	render_viewport.push_input(release)
	if not await _has_focus("CompleteChoiceSheet"): return false
	_accept_repeats_without_settle()
	if not await _wait_for(func() -> bool: return _search_frame() != null):
		_fail("keyboard dismissal could not reopen the automatic search")
		return false
	await process_frame
	await process_frame
	return true


func _has_focus(expected: String) -> bool:
	if await _wait_for(func() -> bool:
		var focused := render_viewport.gui_get_focus_owner()
		return focused != null and focused.name == expected): return true
	_fail("search focus should be %s, actual %s" % [expected, render_viewport.gui_get_focus_owner()])
	return false
