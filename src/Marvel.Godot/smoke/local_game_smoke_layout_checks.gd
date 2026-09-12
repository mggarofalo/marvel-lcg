extends "res://smoke/local_game_smoke_support.gd"

func _board_layout_is_resolved() -> bool:
	var lanes := _board_lanes()
	if lanes.is_empty():
		return false
	if not _areas_are_unique():
		return false
	if not await _area_disclosures_are_safe():
		return false
	if not _secondary_disclosures_are_safe():
		return false
	if not _overflow_rails_are_safe(lanes.scenario):
		return false
	await process_frame
	await process_frame
	if not _responsive_layout_is_safe():
		return false
	return _hand_is_pinned()


func _board_lanes() -> Dictionary:
	var areas := _node("Play/Board/TableScroll/Margin/Areas")
	var scenario := areas.find_child("ScenarioLane", true, false) as Control
	var player := areas.find_child("PlayerLane0", true, false) as Control
	if scenario == null or player == null:
		_fail("the opened table does not expose scenario and player lanes")
		return {}
	return {"scenario": scenario, "player": player}


func _areas_are_unique() -> bool:
	var areas := main.find_children("Area*", "PanelContainer", true, false)
	if areas.is_empty():
		_fail("the opened table has no rendered areas")
		return false
	var area_ids: Dictionary = {}
	for area in areas:
		if area.name in area_ids:
			_fail("a board area was rendered more than once: %s" % area.name)
			return false
		area_ids[area.name] = true
	return true


func _area_disclosures_are_safe() -> bool:
	var toggled := false
	for node in main.find_children("Area*Disclosure", "Button", true, false):
		var disclosure := node as Button
		if not disclosure.toggle_mode:
			_fail("a table section is not collapsible: %s" % disclosure.name)
			return false
		if disclosure.text.ends_with("·  0"):
			_fail("an empty table section rendered individual disclosure chrome")
			return false
		if not toggled:
			if not await _toggle_area_disclosure(disclosure):
				return false
			toggled = true
	if not toggled:
		_fail("the table has no populated collapsible area")
		return false
	return true


func _toggle_area_disclosure(disclosure: Button) -> bool:
	var body := disclosure.get_parent().get_node("Body") as Control
	disclosure.button_pressed = false
	disclosure.pressed.emit()
	await process_frame
	if body.visible:
		_fail("collapsing a populated table section left its cards visible")
		return false
	disclosure.button_pressed = true
	disclosure.pressed.emit()
	return true


func _secondary_disclosures_are_safe() -> bool:
	var saw_populated := false
	for flow in main.find_children("SecondaryAreaFlow", "HFlowContainer", true, false):
		for area in flow.get_children():
			if area.find_child("Area*Disclosure", true, false) == null:
				_fail("a populated secondary section has no disclosure")
				return false
			saw_populated = true
	var aggregate_populated := false
	var aggregate_empty := false
	for node in main.find_children("SecondaryAreasDisclosure", "Button", true, false):
		var text := (node as Button).text
		aggregate_populated = aggregate_populated or "with cards" in text
		aggregate_empty = aggregate_empty or "empty" in text
	if saw_populated and aggregate_populated and aggregate_empty:
		return true
	_fail("secondary disclosures did not preserve populated panels and aggregate empty counts")
	return false


func _overflow_rails_are_safe(scenario_lane: Control) -> bool:
	var area_flow := scenario_lane.find_child("LiveAreaFlow", true, false) as HFlowContainer
	var card_scroll := main.find_child("CARDSScroll", true, false) as ScrollContainer
	var decision_scroll := main.find_child("DecisionBodyScroll", true, false) as ScrollContainer
	if area_flow == null or card_scroll == null or decision_scroll == null:
		_fail("the table is missing its wrapped areas or bounded overflow rails")
		return false
	if card_scroll.horizontal_scroll_mode == ScrollContainer.SCROLL_MODE_DISABLED:
		_fail("a dense card rail cannot reach its overflow")
		return false
	if decision_scroll.vertical_scroll_mode == ScrollContainer.SCROLL_MODE_DISABLED:
		_fail("a dense prompt rail cannot reach its overflow")
		return false
	var commit_bar := main.find_child("CommitBar", true, false) as Control
	if commit_bar == null or decision_scroll.is_ancestor_of(commit_bar):
		_fail("the decision commitment is not fixed outside the scrolling editor")
		return false
	if scenario_lane.find_child("AreaScroll", true, false) != null:
		_fail("the finite area layout still requires its own scrollbar")
		return false
	return true


func _responsive_layout_is_safe() -> bool:
	var board := _node("Play/Board") as Control
	var prompt := _node("Play/Prompt") as Control
	if not _fixed_header_is_visible():
		return false
	if board.size.x < 480.0 or prompt.size.x < 330.0 or prompt.size.x > board.size.x:
		_fail("the responsive table did not preserve usable board and prompt widths: %s/%s" % [
			board.size.x,
			prompt.size.x,
		])
		return false
	if not _wide_prompt_has_expected_width(prompt):
		return false
	if board.get_global_rect().intersects(prompt.get_global_rect()):
		_fail("the prompt rail overlaps the board")
		return false
	return true


func _fixed_header_is_visible() -> bool:
	if _scale_percentage() > 100:
		return true
	var page := main.get_node("Margin") as ScrollContainer
	if page.scroll_vertical == 0 \
			and _control_text_is_visible(_node("Eyebrow") as Control) \
			and _control_text_is_visible(_node("Title") as Control) \
			and _control_text_is_visible(_node("Description") as Control):
		return true
	_fail("the play layout moved its fixed header outside the viewport: page=%s scroll=%d" % [
		page.get_global_rect(),
		page.scroll_vertical,
	])
	return false


func _wide_prompt_has_expected_width(prompt: Control) -> bool:
	var viewport := OS.get_environment("MARVEL_SMOKE_VIEWPORT")
	var scale := OS.get_environment("MARVEL_UI_SCALE")
	if viewport != "1600x900" or scale != "standard":
		return true
	if prompt.size.x >= 595.0 and prompt.size.x <= 605.0:
		return true
	_fail("the wide desktop prompt did not grow to its 600px workbench width: %s" % prompt.size.x)
	return false


func _hand_is_pinned() -> bool:
	var hand := _node("Play/Board/HandShelf") as Control
	if hand != null and hand.visible and "HAND" in _visible_text(hand):
		return true
	_fail("the player's hand is not pinned to the bottom of the table viewport")
	return false


func _keyboard_selection_is_operable() -> bool:
	var decision_scroll := main.find_child("DecisionBodyScroll", true, false) as ScrollContainer
	if not _prompt_header_is_safe(decision_scroll):
		return false
	var restored := await _activate_focused_decision()
	if restored == null:
		return false
	decision_scroll = main.find_child("DecisionBodyScroll", true, false) as ScrollContainer
	if not await _focused_decision_is_visible(restored, decision_scroll):
		return false
	if not _prompt_context_is_visible():
		return false
	if not _commit_controls_are_safe(decision_scroll):
		return false
	if not _prompt_progress_is_safe():
		return false
	if not await _focused_board_area_is_visible():
		return false
	return await _capture_checkpoint("action-composition")


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
	await process_frame
	await process_frame
	var focused := render_viewport.gui_get_focus_owner() as Button
	if focused == null or not _decision().is_ancestor_of(focused) or focused.disabled:
		_fail("a fresh prompt did not focus its first keyboard-operable action")
		return null
	var focus_name := focused.name
	var press := InputEventAction.new()
	press.action = &"ui_accept"
	press.pressed = true
	render_viewport.push_input(press)
	await process_frame
	press.pressed = false
	render_viewport.push_input(press)
	await process_frame
	await process_frame
	var restored := render_viewport.gui_get_focus_owner() as Button
	if restored == null or restored.name != focus_name or not _decision().is_ancestor_of(restored):
		_fail("keyboard focus was lost when the selected decision control rebuilt")
		return null
	if not restored.text.begins_with("✓"):
		_fail("ui_accept did not select the focused decision action")
		return null
	return restored


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
		"Play/Prompt/Margin/Stack/PromptHeader/Eyebrow",
		"Play/Prompt/Margin/Stack/PromptHeader/Heading",
		"Play/Prompt/Margin/Stack/PromptHeader/Context",
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
	if not _focused_control_is_visible(submit):
		_fail("the selected action's submit control is outside the viewport")
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
