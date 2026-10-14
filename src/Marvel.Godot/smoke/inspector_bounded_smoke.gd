extends "res://smoke/local_game_smoke.gd"

func _run() -> void:
	if not await _open_seeded_table(): return
	var coverage := _protected_fixture()
	var card := _mulligan_card("Swinging Web Kick")
	var inspector := main.get_node("CardInspector") as Control
	if not await _bounded_preview(card, inspector): return
	if not await _bounded_pinned(card, inspector): return
	if not await _no_space_reading(card, inspector, coverage): return
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)

func _open_seeded_table() -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return false
	return await _wait_for(func() -> bool: return _task_commit() != null)

func _protected_fixture() -> Panel:
	# Synthetic spatial stress around a legally dealt opening hand; not a played table layout.
	var coverage := _protect("CausalSituation", Rect2(0, 0, 500, 1080))
	_protect("ContextualDecision", Rect2(900, 0, 1020, 1080))
	_protect("EventTimeline", Rect2(500, 400, 400, 680))
	_protect("AbilityWindow", Rect2(500, 0, 400, 100))
	return coverage

func _bounded_preview(card: Control, inspector: Control) -> bool:
	card.grab_focus()
	for count in range(12): await process_frame
	var frame := inspector.get_node("Frame") as Control
	if not inspector.visible or not frame.get_meta("bounded_inspection", false):
		_fail("crowded inspection did not expose its explicit bounded reading surface")
		return false
	if not preload("res://smoke/card_inspection_protection_checks.gd").verify(self, frame): return false
	if not "Pin to read the full card" in _visible_text(inspector):
		_fail("bounded hover does not explain how to read the rest")
		return false
	var scroll := inspector.get_node("Frame/Stack/Scroll") as ScrollContainer
	if not scroll.get_v_scroll_bar().visible:
		_fail("bounded inspection has no overflow affordance")
		return false
	return await _capture_checkpoint("synthetic-bounded-inspection")

func _bounded_pinned(card: Control, inspector: Control) -> bool:
	if not await _pointer_activate_card_body(card): return false
	for count in range(8): await process_frame
	if not "Scroll to read the full card" in _visible_text(inspector):
		_fail("pinned bounded inspection lost its reading instruction")
		return false
	var scroll := inspector.get_node("Frame/Stack/Scroll") as ScrollContainer
	scroll.scroll_vertical = 10000
	await process_frame
	if scroll.scroll_vertical <= 0:
		_fail("pinned bounded face cannot scroll to the remaining content")
		return false
	var frame := inspector.get_node("Frame") as Control
	if not preload("res://smoke/card_inspection_protection_checks.gd").verify(self, frame): return false
	print("BOUNDED_INSPECTION_OK native_scale=%s frame=%s" % [inspector.get_node("Frame/Stack/Scroll/Content").get_child(0).get_meta("inspection_scale"), frame.get_global_rect()])
	return await _close_inspector_with_keyboard(card, inspector)

func _no_space_reading(card: Control, inspector: Control, coverage: Panel) -> bool:
	coverage.size = Vector2(1920, 1080)
	card.release_focus()
	card.grab_focus()
	if not await _no_space_notice(inspector): return false
	if not await _no_space_pinned(card, inspector): return false
	if not await _released_space(inspector, coverage): return false
	if not await _close_inspector_with_keyboard(card, inspector): return false
	print("NO_SPACE_INSPECTION_OK explicit_pin=true close_returns_focus=true")
	return true

func _no_space_notice(inspector: Control) -> bool:
	for count in range(12): await process_frame
	var frame := inspector.get_node("Frame") as Control
	var scroll := inspector.get_node("Frame/Stack/Scroll") as ScrollContainer
	if not frame.get_meta("inspection_overlay", false) or scroll.visible:
		_fail("fully occupied table did not offer an explicit compact reading notice")
		return false
	if not "Pin to read full card" in _visible_text(inspector):
		_fail("no-space preview omitted its reading route")
		return false
	return await _capture_checkpoint("synthetic-no-space-notice")

func _no_space_pinned(card: Control, inspector: Control) -> bool:
	if not await _pointer_activate_card_body(card): return false
	for count in range(12): await process_frame
	var frame := inspector.get_node("Frame") as Control
	var scroll := inspector.get_node("Frame/Stack/Scroll") as ScrollContainer
	if not scroll.visible or not "Full card · Close to return" in _visible_text(inspector):
		_fail("deliberate no-space inspection did not open a readable closeable face")
		return false
	if not Rect2(Vector2.ZERO, main.size).encloses(frame.get_global_rect()):
		_fail("no-space full-card mode leaves the viewport")
		return false
	return await _capture_checkpoint("synthetic-no-space-full-card")

func _released_space(inspector: Control, coverage: Panel) -> bool:
	coverage.size = Vector2(500, 1080)
	for count in range(12): await process_frame
	var frame := inspector.get_node("Frame") as Control
	if frame.get_meta("inspection_overlay", true) or not "Scroll to read the full card" in _visible_text(inspector):
		_fail("released protection did not return inspection to the clear reading space")
		return false
	return preload("res://smoke/card_inspection_protection_checks.gd").verify(self, frame)

func _protect(title: String, bounds: Rect2) -> Panel:
	var panel := Panel.new()
	panel.name = title
	panel.position = bounds.position
	panel.size = bounds.size
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	main.add_child(panel)
	var label := Label.new()
	label.text = "SYNTHETIC PROTECTED REGION"
	panel.add_child(label)
	return panel
