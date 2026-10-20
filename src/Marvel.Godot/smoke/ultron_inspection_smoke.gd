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
	var inspector := main.find_child("CardInspector", true, false) as Control
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	var prompt := _logical_prompt_identity()
	if card == null:
		_fail("Ultron has no readable top-row face")
		return false
	if not await _hover_and_pin_ultron(card, inspector): return false
	if not await _bounded_expansion(card, inspector): return false
	if not await _upcoming_stages_are_safe(): return false
	if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision or _logical_prompt_identity() != prompt:
		_fail("inspection changed the current revision or prompt draft")
		return false
	return true

func _bounded_expansion(card: Control, inspector: Control) -> bool:
	var motion := _node("Toolbar/Motion") as CheckButton
	var preference := motion.button_pressed
	var physical := preload("res://smoke/player_card_pose.gd").capture(self, card, "VillainTable")
	var physical_size := card.size
	for enabled in [true, false]:
		motion.button_pressed = enabled
		motion.toggled.emit(enabled)
		if not await _keyboard_activate(card): return false
		var frame := inspector.get_node("Frame") as Control
		if (inspector.get_node("Backdrop") as Control).visible:
			_fail("bounded inspection dims the entire table")
			return false
		if not frame.scale.is_equal_approx(Vector2.ONE):
			_fail("inspector resizes already rasterized text during reveal")
			return false
		if not enabled and not is_equal_approx(frame.modulate.a, 1.0):
			_fail("reduced motion retained an inspector transition")
			return false
		if not await _wait_for(func() -> bool: return is_equal_approx(frame.modulate.a, 1.0)):
			_fail("pinned inspection did not settle at its measured size")
			return false
		if card.size != physical_size or not card.get_global_transform().is_equal_approx(preload("res://smoke/player_card_pose.gd").expected(physical)):
			_fail("inspection expansion moved its physical source")
			return false
		if not await _close_inspector_with_keyboard(card, inspector): return false
	motion.button_pressed = preference
	motion.toggled.emit(preference)
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


func _hover_and_pin_ultron(card: Control, inspector: Control) -> bool:
	if not await _open_immediate_preview(card, inspector): return false
	await process_frame
	await process_frame
	if not _complete_ultron(inspector): return false
	if not await _bridge_and_dismiss_preview(inspector): return false
	if not await _keyboard_activate(card): return false
	await process_frame
	if not _complete_ultron(inspector): return false
	if not await _close_inspector_with_keyboard(card, inspector): return false
	return true
