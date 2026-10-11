extends "res://smoke/local_game_smoke.gd"

func _run() -> void:
	if not await _open_iron_man_turn(): return
	if not await _inspect_complete_catalogue(): return
	if not await _reach_obligation(): return
	if not await _check_obligation_context(): return
	print("DECISION_OBLIGATION_OK")
	quit(0)


func _open_iron_man_turn() -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	_select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Iron Man")
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "7"
	seed.text_changed.emit(seed.text)
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _button_named("Keep hand") != null):
		_fail("Iron Man opening did not arrive")
		return false
	if not await _pointer_activate(_button_named("Keep hand")): return false
	if not await _wait_for(func() -> bool: return _button_named("End turn") != null):
		_fail("Iron Man turn did not arrive")
		return false
	return true


func _inspect_complete_catalogue() -> bool:
	var complete := main.find_child("CompleteChoiceSheet", true, false) as Button
	if not await _pointer_activate(complete): return false
	if not await _wait_for(func() -> bool: return main.find_child("CompleteChoicesFrame", true, false) != null):
		_fail("Complete Choices did not open")
		return false
	if _decision().find_child("ChoiceGroup1", true, false) == null or _decision().find_child("ChoiceGroup3", true, false) == null:
		_fail("the catalogue did not group identity and hand offers")
		return false
	await _capture_checkpoint("complete-choices-iron-man")
	return await _pointer_activate(main.find_child("CloseChoiceSheet", true, false) as Button)


func _reach_obligation() -> bool:
	for index in range(120):
		if _is_complete():
			_fail("seed 7 ended before its expected obligation")
			return false
		await process_frame
		var context := main.find_child("CausalSituation", true, false) as Control
		if "Business Problems" in _visible_text(context): return true
		if not await _advance_to_obligation(): return false
		await create_timer(0.1).timeout
	_fail("Business Problems was not reached in this seeded run")
	return false


func _advance_to_obligation() -> bool:
	var decline := main.find_child("ContextualDecline", true, false) as Button
	if decline != null and decline.is_visible_in_tree() and not decline.disabled:
		return await _pointer_activate(decline)
	return await _compose_table_decision()


func _check_obligation_context() -> bool:
	if not await _obligation_face_visible():
		_fail("Business Problems has no visible contextual face")
		return false
	await _capture_checkpoint("business-problems-context")
	if not await _compose_table_decision(): return false
	if not await _wait_for(func() -> bool: return "Exhaust Tony Stark" in _visible_text(main)):
		_fail("the obligation consequences did not arrive")
		return false
	await _capture_checkpoint("business-problems-consequences")
	if not await _obligation_face_visible():
		_fail("the obligation disappeared during its next choice")
		return false
	return true


func _obligation_face_visible() -> bool:
	var references: Array[Control] = []
	for card in main.find_children("CausalReference*", "Control", true, false):
		if card.has_meta("causal_reference") and "Business Problems" in _visible_text(card):
			references.append(card)
	if references.size() != 1: return false
	var reference := references[0]
	if not _control_is_fully_visible(reference): return false
	# The causal copy belongs to its seam, never another physical player slot.
	for physical in main.find_children("ProceduralCard*", "Control", true, false):
		if "Business Problems" in _visible_text(physical): return false
	var inspector := main.get_node("CardInspector") as Control
	var hover := InputEventMouseMotion.new()
	hover.position = reference.get_global_rect().get_center()
	hover.global_position = hover.position
	render_viewport.push_input(hover, true)
	await process_frame
	if not inspector.visible: return false
	if "Business Problems" not in _visible_text(inspector): return false
	var leave := InputEventMouseMotion.new()
	leave.position = Vector2(4, 4)
	leave.global_position = leave.position
	render_viewport.push_input(leave, true)
	await process_frame
	return true
