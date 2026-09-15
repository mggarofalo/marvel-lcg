extends "res://smoke/local_game_smoke_card_inspector.gd"

func _procedural_cards_are_safe() -> bool:
	if main.find_child("VillainTable", true, false) != null:
		return await _mulligan_cards_are_safe()
	if _fallback_mulligan_is_safe():
		return true
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


func _fallback_mulligan_is_safe() -> bool:
	var sheet := main.find_child("CompleteChoiceSheet", true, false) as Button
	if sheet == null:
		return false
	var header := _node("Play/Prompt/Margin/Stack/PromptHeader") as Control
	var history := _node("Play/Prompt/Margin/Stack/Workbench/History") as Control
	var workbench := _node("Play/Prompt/Margin/Stack/Workbench") as TabContainer
	var scale := _node("Toolbar/ScaleValue") as Label
	if header == null or history == null or workbench == null or scale == null \
			or not header.visible or history.get_parent() != workbench or not workbench.tabs_visible \
			or workbench.get_tab_title(1) != "History" \
			or scale.text != "Scale %s%%" % _scale_percentage():
		_fail("the compact opening-table chrome leaked into the generic mulligan fallback: header=%s history=%s tabs=%s scale=%s" % [
			header.visible if header != null else false,
			history.get_parent() == workbench if history != null and workbench != null else false,
			workbench.tabs_visible if workbench != null else false,
			scale.text if scale != null else "missing",
		])
		return false
	var hand := _node("Play/Board/HandShelf") as Control
	var card := hand.find_child("ProceduralCard", true, false) as Control
	if card == null or card.custom_minimum_size.x < _scaled_metric(172):
		_fail("the generic mulligan fallback did not retain the selected card scale")
		return false
	return true


func _mulligan_cards_are_safe() -> bool:
	var hand := _node("Play/Board/HandShelf") as Control
	var cards := hand.find_children("ProceduralCard", "PanelContainer", true, false)
	var toggles := hand.find_children("MulliganDiscard*", "Button", true, false)
	if cards.size() != 6 or toggles.size() != 6:
		_fail("the opening hand does not expose six readable cards and six discard checkboxes")
		return false
	for toggle_node in toggles:
		var toggle := toggle_node as Button
		var desktop_minimum := 44 if OS.get_environment("MARVEL_SMOKE_VIEWPORT") == "1920x1080" \
			else _scaled_metric(44)
		if not toggle.toggle_mode or toggle.text != "□ DISCARD" \
				or toggle.custom_minimum_size.y < desktop_minimum:
			_fail("a mulligan checkbox is not explicit, keyboard-operable, and generously sized" \
				+ " toggle=%s text=%s minimum=%s expected=%s pressed=%s" % [
					toggle.toggle_mode,
					toggle.text,
					toggle.custom_minimum_size,
					desktop_minimum,
					toggle.button_pressed,
				])
			return false
		if not await _prepare_activation(toggle):
			return false
	return _tabletop_essentials_are_safe()


func _tabletop_essentials_are_safe() -> bool:
	for title in ["Rhino", "Peter Parker", "The Break-In!"]:
		var card := _tabletop_card_named(title)
		if card == null or card.custom_minimum_size.x < 210:
			_fail("the tabletop essential '%s' did not retain readable board geometry" % title)
			return false
	var villain_text := _visible_text(_tabletop_card_named("Rhino"))
	var identity_text := _visible_text(_tabletop_card_named("Peter Parker"))
	var scheme_text := _visible_text(_tabletop_card_named("The Break-In!"))
	if "HP" not in villain_text or "HP" not in identity_text \
			or "THREAT" not in scheme_text:
		_fail("the tabletop omitted a current villain, identity, or scheme value")
		return false
	return true


func _card_controls_are_safe(cards: Array[Node], observed: Dictionary) -> bool:
	var hand_shelf := _node("Play/Board/HandShelf")
	for card_node in cards:
		var card := card_node as Control
		var in_hand := hand_shelf.is_ancestor_of(card)
		var desktop_table := OS.get_environment("MARVEL_SMOKE_VIEWPORT") == "1920x1080"
		var expected_width := (100 if in_hand else 125) \
			if desktop_table else _scaled_metric(100 if in_hand else 125)
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
	if not disclosures.is_empty():
		_fail("upcoming stages permanently occupy the live table")
		return false
	var active_stage: Control = null
	for candidate in main.find_children("ProceduralCard", "PanelContainer", true, false):
		if candidate.find_child("SummaryValuesStage", true, false) != null:
			active_stage = candidate as Control
			break
	if active_stage == null or not await _pointer_activate_card_body(active_stage):
		_fail("the current villain stage cannot open its inspector")
		return false
	await process_frame
	var inspector := main.get_node("CardInspector") as Control
	var next := inspector.find_child("NextStage", true, false) as Button
	var heading := inspector.find_child("Title", true, false) as Label
	if not inspector.visible or next == null or heading == null \
			or heading.text != "STAGE 1 OF 2" or next.disabled:
		_fail("the current villain inspector does not expose bounded stage navigation")
		return false
	if not await _pointer_activate(next):
		return false
	if not await _wait_for(func() -> bool:
		var current := inspector.find_child("Title", true, false) as Label
		return current != null and current.text == "STAGE 2 OF 2"):
		_fail("the villain inspector did not finish navigating to the upcoming stage")
		return false
	heading = inspector.find_child("Title", true, false) as Label
	var previous := inspector.find_child("PreviousStage", true, false) as Button
	if heading == null or heading.text != "STAGE 2 OF 2" \
			or previous == null or previous.disabled:
		_fail("the villain inspector did not navigate to the upcoming stage")
		return false
	if not await _capture_checkpoint("card-inspector-villain-next-stage"):
		return false
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	render_viewport.push_input(escape)
	render_viewport.push_input(InputEventKey.new())
	await process_frame
	if inspector.visible:
		_fail("Escape did not close the stage inspector")
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
