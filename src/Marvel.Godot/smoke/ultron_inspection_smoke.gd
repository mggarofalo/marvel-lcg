extends "res://smoke/local_game_smoke.gd"

func _run() -> void:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	await _configure_seeded_game()
	_select_named_option(_node("Setup/Selections/Fields/Grid/Scenario"), "Ultron")
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _wait_for(func() -> bool: return _button_named("Keep hand") != null):
		_fail("Ultron opening hand did not arrive")
		return
	if not await _pointer_activate(_button_named("Keep hand")): return
	if not await _wait_for(func() -> bool: return _button_named("End turn") != null):
		_fail("Ultron player turn did not arrive")
		return
	if not await _inspect_ultron(): return
	print("ULTRON_INSPECTION_OK complete_rules=true preview_grace=true pinned=true draft_unchanged=true")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)

func _inspect_ultron() -> bool:
	var card := _tabletop_card_named("Ultron")
	var inspector := main.get_node("CardInspector") as Control
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	var prompt := _logical_prompt_identity()
	if card == null:
		_fail("Ultron has no readable top-row face")
		return false
	if not await _open_immediate_preview(card, inspector): return false
	await process_frame
	await process_frame
	if not _complete_ultron(inspector): return false
	if not await _bridge_and_dismiss_preview(inspector): return false
	if not await _pointer_activate_card_body(card): return false
	await process_frame
	if not _complete_ultron(inspector): return false
	if not await _close_inspector_with_keyboard(card, inspector): return false
	if (_node("Toolbar/SyncStatus") as Label).text != revision or _logical_prompt_identity() != prompt:
		_fail("inspection changed the current revision or prompt draft")
		return false
	return true

func _complete_ultron(inspector: Control) -> bool:
	var frame := inspector.get_node("Frame") as Control
	if not inspector.visible or not _control_is_fully_visible(frame):
		_fail("Ultron inspection is missing or exceeds the viewport")
		return false
	var rules := frame.find_child("RulesText", true, false) as RichTextLabel
	if rules == null or "Drone" not in rules.get_parsed_text() \
			or rules.get_content_height() > rules.size.y + 1:
		_fail("Ultron inspection clips or loses its canonical drone rules")
		return false
	for scroll in frame.find_children("*", "ScrollContainer", true, false):
		if scroll.get_v_scroll_bar().visible or scroll.get_h_scroll_bar().visible:
			_fail("Ultron inspection requires a scrollbar")
			return false
	return true
