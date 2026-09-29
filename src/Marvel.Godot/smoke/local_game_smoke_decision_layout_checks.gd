extends "res://smoke/local_game_smoke_board_interaction_checks.gd"


func _prompt_header_is_safe(decision_scroll: ScrollContainer) -> bool:
	var header := _node("Play/Prompt/Margin/Stack/PromptHeader") as Control
	if header == null or decision_scroll == null or decision_scroll.is_ancestor_of(header):
		_fail("the active seat and question are not pinned above the decision body")
		return false
	var readable := _visible_text(header).to_upper()
	if "SPIDER-MAN" not in readable or "OPENING HAND" not in readable:
		_fail("the pinned prompt summary omits its seat or question")
		return false
	if "CHOOSE TO CONTINUE" not in readable and "MAY PASS" not in readable:
		_fail("the pinned prompt summary omits cancellability")
		return false
	return true


func _activate_focused_decision() -> Button:
	var expected := _first_enabled_choice()
	if expected == null:
		_fail("the current prompt has no keyboard-operable action")
		return null
	render_viewport.gui_release_focus()
	await process_frame
	expected.grab_focus()
	await process_frame
	await process_frame
	var focused := render_viewport.gui_get_focus_owner() as Button
	if focused == null or not _decision().is_ancestor_of(focused) or focused.disabled:
		_fail("the current prompt could not focus its keyboard-operable action")
		return null
	var focus_name := focused.name
	var issued_id := focused.get_instance_id()
	_accept_repeats_without_settle()
	if not await _wait_for(func() -> bool:
		var replacement := _decision().find_child(focus_name, true, false) as Button
		return replacement != null and replacement.get_instance_id() != issued_id \
			and replacement.has_focus() and replacement.text.begins_with("✓")):
		_fail("keyboard focus was lost when the selected decision control rebuilt")
		return null
	return _decision().find_child(focus_name, true, false) as Button


func _focused_decision_is_visible(restored: Button, decision_scroll: ScrollContainer) -> bool:
	if not await _wait_for(func() -> bool: return _focused_control_is_visible(restored)):
		var page := main.get_node("Margin") as ScrollContainer
		_fail("keyboard focus moved outside the visible viewport: control=%s decision=%s page=%s root=%s scroll=%d/%d" % [
			restored.get_global_rect(),
			decision_scroll.get_global_rect(),
			page.get_global_rect(),
			_viewport_size(),
			decision_scroll.scroll_vertical,
			page.scroll_vertical,
		])
		return false
	decision_scroll = main.find_child("DecisionBodyScroll", true, false) as ScrollContainer
	if decision_scroll.scroll_horizontal != 0:
		_fail("keyboard focus horizontally clipped the selected decision label")
		return false
	return true


func _prompt_context_is_visible() -> bool:
	for path in [
		"Play/Prompt/Margin/Stack/PromptHeader/Heading",
		"Play/Prompt/Margin/Stack/PromptHeader/Progress",
	]:
		if not _control_text_is_visible(_node(path) as Control):
			_fail("keyboard focus hid active prompt context: %s" % path)
			return false
	return true


func _commit_controls_are_safe(decision_scroll: ScrollContainer) -> bool:
	var summary := main.find_child("ActionSummary", true, false) as Control
	var commit_bar := main.find_child("CommitBar", true, false) as Control
	var submit := main.find_child("Submit", true, false) as Button
	if summary == null or commit_bar == null or submit == null:
		_fail("the selected action has no summary or commitment controls")
		return false
	if decision_scroll.is_ancestor_of(summary) or decision_scroll.is_ancestor_of(commit_bar):
		_fail("the selected action or its commitment moved into the scrolling editor")
		return false
	if not _control_is_fully_visible(submit):
		_fail("the selected action's submit control is clipped or outside the viewport")
		return false
	return true


func _prompt_progress_is_safe() -> bool:
	var progress := _node("Play/Prompt/Margin/Stack/PromptHeader/Progress") as Label
	if progress == null:
		_fail("the pinned prompt has no progress summary")
		return false
	if "READY" not in progress.text and "INCOMPLETE" not in progress.text:
		_fail("the pinned prompt summary omitted readiness progress")
		return false
	if "TARGETS" not in progress.text and "GROUP" not in progress.text \
			and "NO TARGETS" not in progress.text:
		_fail("the pinned prompt summary omitted target progress")
		return false
	return true
