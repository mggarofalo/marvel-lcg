extends "res://smoke/local_game_smoke_card_inspector.gd"

func _procedural_cards_are_safe() -> bool:
	var cards := main.find_children("ProceduralCard", "PanelContainer", true, false)
	if cards.is_empty():
		_fail("the opened table has no procedural card controls")
		return false
	var observed := {
		"face": false,
		"back": false,
		"compact_summary": false,
		"type_specific_value": false,
		"health": false,
		"progress": false,
		"active_villain_stage": false,
	}
	if not _card_controls_are_safe(cards, observed):
		return false
	if not _required_card_kinds_were_observed(observed):
		return false
	if not await _upcoming_stages_are_safe():
		return false
	if not _secondary_areas_are_safe():
		return false
	var hand_card := _node("Play/Board/HandShelf").find_child(
		"ProceduralCard", true, false) as Control
	if hand_card == null:
		_fail("the pinned hand has no readable card to inspect")
		return false
	return await _card_inspector_is_safe(hand_card)


func _card_controls_are_safe(cards: Array[Node], observed: Dictionary) -> bool:
	var hand_shelf := _node("Play/Board/HandShelf")
	for card_node in cards:
		var card := card_node as Control
		var in_hand := hand_shelf.is_ancestor_of(card)
		var expected_width := _scaled_metric(100 if in_hand else 125)
		if card.custom_minimum_size.x < expected_width:
			_fail("a board card does not honor the selected card geometry")
			return false
		var face := card.find_child("CardFace", true, false) as Control
		var back := card.find_child("CardBack", true, false) as Control
		if face != null and not _card_face_is_safe(card, face, in_hand, observed):
			return false
		if face == null and back != null and not _card_back_is_safe(back, observed):
			return false
	return true


func _required_card_kinds_were_observed(observed: Dictionary) -> bool:
	if not observed.face or not observed.back or not observed.compact_summary:
		_fail("the table did not exercise both card faces and compact summaries")
		return false
	if not observed.type_specific_value or not observed.health:
		_fail("the table did not exercise type-specific values and current health")
		return false
	if not observed.progress or not observed.active_villain_stage:
		_fail("the table did not exercise scheme progress and an active villain stage")
		return false
	return true


func _upcoming_stages_are_safe() -> bool:
	var disclosures := main.find_children(
		"UpcomingStagesDisclosure", "Button", true, false)
	if disclosures.is_empty():
		_fail("progressive scenario areas have no upcoming-stages disclosure")
		return false
	for node in disclosures:
		if not await _upcoming_disclosure_is_safe(node as Button):
			return false
	return await _upcoming_disclosures_survive_scale(disclosures.size())


func _upcoming_disclosure_is_safe(disclosure: Button) -> bool:
	if not disclosure.toggle_mode or "Upcoming stages" not in disclosure.text:
		_fail("an upcoming-stages disclosure is not clearly labeled and collapsible")
		return false
	var cards := disclosure.get_parent().get_node("UpcomingStagesList") as VBoxContainer
	disclosure.button_pressed = true
	disclosure.pressed.emit()
	await process_frame
	if not cards.visible:
		_fail("opening upcoming stages did not reveal its compact list")
		return false
	for card in cards.find_children("ProceduralCard", "PanelContainer", true, false):
		if not _upcoming_card_is_safe(card as Control):
			return false
	return true


func _upcoming_card_is_safe(card: Control) -> bool:
	var face := card.find_child("CardFace", true, false)
	var back := card.find_child("CardBack", true, false)
	if face != null and card.focus_mode != Control.FOCUS_ALL:
		_fail("an upcoming stage cannot receive keyboard focus for inspection")
		return false
	if back != null and card.focus_mode != Control.FOCUS_NONE:
		_fail("a concealed upcoming stage gained face-level focus behavior")
		return false
	if card.find_child("ProgressValues", true, false) != null:
		_fail("an upcoming stage competes with the current stage's live progress")
		return false
	return true


func _upcoming_disclosures_survive_scale(expected_count: int) -> bool:
	var slider := _node("Toolbar/InterfaceScale") as HSlider
	var original := slider.value
	for rebuilt_scale in [90.0 if original != 90.0 else 80.0, original]:
		slider.value = rebuilt_scale
		await process_frame
		await process_frame
		var rebuilt := main.find_children(
			"UpcomingStagesDisclosure", "Button", true, false)
		if rebuilt.size() != expected_count:
			_fail("a board rebuild changed the upcoming-stages disclosure set")
			return false
		for node in rebuilt:
			var disclosure := node as Button
			var cards := disclosure.get_parent().get_node(
				"UpcomingStagesList") as VBoxContainer
			if not disclosure.button_pressed or not cards.visible:
				_fail("an open upcoming-stages disclosure collapsed during board rebuild")
				return false
	return true


func _secondary_areas_are_safe() -> bool:
	for secondary in main.find_children("SecondaryAreas", "VBoxContainer", true, false):
		var body := secondary.find_child("SecondaryAreaFlow", false, false)
		if body == null:
			continue
		for area in body.get_children():
			if area.find_child("ProceduralCard", true, false) == null:
				_fail("an empty secondary area rendered individual panel chrome")
				return false
	return true
