extends "res://smoke/local_game_smoke.gd"

func _run() -> void:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	await _configure_seeded_game()
	_select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Iron Man")
	_select_named_option(_node("Setup/Selections/Fields/Grid/Scenario"), "Ultron")
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _wait_for(func() -> bool: return _task_commit() != null): return
	var cards := main.find_children("ProceduralCard*", "Control", true, false)
	var count := 0
	for card: Control in cards:
		if not card.has_meta("spatial_hand_index"): continue
		if not await _check_preview(card): return
		count += 1
	if count != 6:
		_fail("opening-hand preview probe did not inspect all six cards")
		return
	print("WHOLE_CARD_PREVIEW_OK cards=%s" % count)
	main.queue_free()
	await process_frame
	quit(0)

func _check_preview(card: Control) -> bool:
	var inspector := main.get_node("CardInspector") as Control
	if not await _open_immediate_preview(card, inspector): return false
	card.grab_focus()
	for frame in range(12): await process_frame
	var scroll := inspector.get_node("Frame/Stack/Scroll") as ScrollContainer
	var detail := scroll.get_node("Content").get_child(0) as Control
	if not card.tooltip_text.is_empty():
		_fail("opening hand has an unsolicited tooltip: " + card.tooltip_text)
		return false
	if not inspector.visible or not scroll.visible:
		_fail("opening-hand hover did not show the card")
		return false
	if scroll.get_v_scroll_bar().visible or scroll.get_h_scroll_bar().visible:
		_fail("card preview shows scrollbar chrome")
		return false
	if not scroll.get_global_rect().grow(1).encloses(detail.get_global_rect()):
		_fail("card preview clips its face: scroll=%s detail=%s" % [scroll.get_global_rect(), detail.get_global_rect()])
		return false
	if not preload("res://smoke/card_inspection_protection_checks.gd").verify(self, inspector.get_node("Frame")): return false
	if not await _capture_checkpoint("whole-card-" + str(card.get_meta("spatial_hand_index"))): return false
	return await _staging_preserves_inspection(card)

func _staging_preserves_inspection(card: Control) -> bool:
	var revision = _node("Toolbar/SyncStatus").get_meta("response_revision")
	if not await _keyboard_stage_card(card): return false
	if not card.get_meta("mulligan_staged", false) or not card.tooltip_text.is_empty():
		_fail("staging a redraw must remain operable without a card tooltip")
		return false
	if not "Staged for redraw" in card.accessibility_description:
		_fail("staged redraw lost its accessible instruction")
		return false
	if not await _keyboard_stage_card(card): return false
	if card.get_meta("mulligan_staged", true) or _node("Toolbar/SyncStatus").get_meta("response_revision") != revision:
		_fail("staging and keeping a card changed the authoritative game")
		return false
	await preload("res://smoke/gesture_settlement.gd").hand(self)
	return true
