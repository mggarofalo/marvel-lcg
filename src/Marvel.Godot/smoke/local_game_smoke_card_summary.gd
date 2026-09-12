extends "res://smoke/local_game_smoke_layout_checks.gd"

func _card_face_is_safe(card: Control, face: Control, in_hand: bool, observed: Dictionary) -> bool:
	observed.face = true
	if not _compact_card_title_is_safe(card, face, in_hand):
		return false
	if not _compact_card_regions_are_safe(face, observed):
		return false
	if not _compact_card_progress_is_safe(face, observed):
		return false
	if not _compact_card_resources_are_safe(face, observed):
		return false
	return _compact_card_stage_is_safe(face, observed)


func _compact_card_title_is_safe(card: Control, face: Control, in_hand: bool) -> bool:
	var kind := face.find_child("Kind", true, false) as Label
	if in_hand and kind == null:
		_fail("a hand card does not retain subordinate type information")
		return false
	if not in_hand and kind != null:
		_fail("a board card repeats type context already conveyed by its area")
		return false
	var title := face.find_child("Title", true, false) as Label
	if title == null or title.max_lines_visible != -1:
		_fail("a board card title is truncated")
		return false
	if title.text_overrun_behavior == TextServer.OVERRUN_TRIM_ELLIPSIS:
		_fail("a compact card title uses ellipsis instead of wrapping")
		return false
	if title.size.y < title.get_theme_font_size("font_size"):
		_fail("a board card title collapsed out of its card: %s title=%s face=%s card=%s parent=%s" % [
			title.text,
			title.size,
			face.size,
			card.size,
			card.get_parent().size,
		])
		return false
	return true


func _compact_card_regions_are_safe(face: Control, observed: Dictionary) -> bool:
	if face.find_child("RulesText", true, false) != null:
		_fail("a compact board or hand card exposed full rules text")
		return false
	if face.find_child("PrintedValues", true, false) != null:
		_fail("a compact card retained a PRINTED value region")
		return false
	if face.find_child("LiveValues", true, false) != null:
		_fail("a compact card retained a CURRENT value region")
		return false
	if face.find_child("ReadyIndicator", true, false) != null:
		_fail("a compact card retained a standalone READY indicator")
		return false
	var summary := face.find_child("SummaryValues", true, false)
	observed.compact_summary = observed.compact_summary or summary != null
	if summary != null and not _summary_badges_are_safe(summary):
		return false
	return _diagnostic_fields_are_hidden(face)


func _summary_badges_are_safe(summary: Control) -> bool:
	for badge in summary.find_children("*", "Label", true, false):
		if badge.text_overrun_behavior == TextServer.OVERRUN_TRIM_ELLIPSIS:
			_fail("a compact semantic badge uses ellipsis instead of whole-badge wrapping")
			return false
	return true


func _diagnostic_fields_are_hidden(face: Control) -> bool:
	for forbidden_name in [
		"SummaryValuesALLY_LIMIT",
		"SummaryValuesHAND_SIZE",
		"SummaryValuesFIRST_PLAYER_TOKEN",
		"SummaryValuesRESTRICTED_LIMIT",
	]:
		if face.find_child(forbidden_name, true, false) != null:
			_fail("a compact card exposed an unmatched diagnostic field")
			return false
	return true


func _compact_card_progress_is_safe(face: Control, observed: Dictionary) -> bool:
	var health := face.find_child("ProgressValuesHEALTH", true, false) as Label
	if health != null:
		observed.health = true
		if "/" not in health.text:
			_fail("current health is not represented as current/maximum")
			return false
		if face.find_child("SummaryValuesDAMAGE", true, false) != null:
			_fail("a character exposes damage beside current health")
			return false
		if face.find_child("SummaryValuesHP", true, false) != null:
			_fail("a character exposes printed health beside current health")
			return false
	var threat := face.find_child("ProgressValuesTHREAT", true, false) as Label
	if threat != null:
		observed.progress = true
		if threat.text.strip_edges().is_empty():
			_fail("scheme threat progress is empty")
			return false
	return _type_specific_value_was_seen(face, observed)


func _type_specific_value_was_seen(face: Control, observed: Dictionary) -> bool:
	for value_name in [
		"SummaryValuesREC",
		"SummaryValuesTHW",
		"SummaryValuesATK",
		"SummaryValuesDEF",
		"SummaryValuesSCH",
		"SummaryValuesACCELERATION",
		"SummaryValuesAMPLIFY",
		"SummaryValuesCRISIS",
		"SummaryValuesHAZARD",
		"SummaryValuesESCALATION_THREAT",
		"SummaryValuesCOST",
		"SummaryValuesRES",
	]:
		if face.find_child(value_name, true, false) != null:
			observed.type_specific_value = true
			break
	return true


func _compact_card_resources_are_safe(face: Control, observed: Dictionary) -> bool:
	var resource := face.find_child("SummaryValuesRES", true, false) as HFlowContainer
	if resource == null:
		return true
	observed.type_specific_value = true
	if not FileAccess.file_exists("res://assets/fonts/ChampionsIcons.ttf"):
		_fail("the pinned Champions icon font is outside the project resource root")
		return false
	var tokens := resource.find_children("ResourceToken*", "HBoxContainer", false, false)
	if tokens.is_empty():
		_fail("a compact resource has no icon token")
		return false
	var slot_size := Vector2.ZERO
	for token_node in tokens:
		var slot := token_node.find_child("ResourceIconSlot*", true, false) as Label
		if not _resource_token_structure_is_safe(resource, token_node, slot):
			return false
		if slot_size == Vector2.ZERO:
			slot_size = slot.custom_minimum_size
		if not _resource_slot_is_safe(slot, slot_size):
			return false
	return true


func _resource_token_structure_is_safe(resource: HFlowContainer, token: Control, slot: Label) -> bool:
	if resource.size_flags_horizontal != Control.SIZE_SHRINK_BEGIN:
		_fail("a compact printed resource row does not shrink to its icons")
		return false
	if slot == null or token.find_child("ResourceName*", true, false) != null:
		_fail("a compact printed resource is not an icon-only row")
		return false
	if not token.tooltip_text.is_empty() or not slot.tooltip_text.is_empty():
		_fail("a compact printed resource icon exposes redundant tooltip text")
		return false
	return true


func _resource_slot_is_safe(slot: Label, expected_size: Vector2) -> bool:
	if slot.custom_minimum_size.x != slot.custom_minimum_size.y:
		_fail("a resource glyph does not use a square slot")
		return false
	if slot.custom_minimum_size != expected_size:
		_fail("resource glyph slots do not share one fixed size")
		return false
	if slot.horizontal_alignment != HORIZONTAL_ALIGNMENT_CENTER:
		_fail("a resource glyph is not horizontally centered")
		return false
	if slot.vertical_alignment != VERTICAL_ALIGNMENT_CENTER:
		_fail("a resource glyph is not vertically centered")
		return false
	if slot.text not in ["P", "M", "E", "W"]:
		_fail("a resource glyph does not use the Champions icon alphabet")
		return false
	if not slot.has_theme_font_override("font"):
		_fail("a resource glyph does not use the pinned icon font")
		return false
	if slot.get_theme_font("font").resource_path != "res://assets/fonts/ChampionsIcons.runtime.tres":
		_fail("a resource glyph resolved an unexpected icon font")
		return false
	return true


func _compact_card_stage_is_safe(face: Control, observed: Dictionary) -> bool:
	var stage := face.find_child("SummaryValuesStage", true, false)
	var health := face.find_child("ProgressValuesHEALTH", true, false)
	var threat := face.find_child("ProgressValuesTHREAT", true, false)
	if stage == null:
		return true
	if health != null:
		if face.find_child("SummaryValuesSCH", true, false) == null:
			_fail("an active villain stage is missing scheme beside live health")
			return false
		if face.find_child("SummaryValuesATK", true, false) == null:
			_fail("an active villain stage is missing attack beside live health")
			return false
		if face.find_children("SummaryValuesStage", "Label", true, false).size() != 1:
			_fail("an active villain stage is repeated beside live stats")
			return false
		observed.active_villain_stage = true
		return true
	if threat != null or face.find_child("SummaryValuesHP", true, false) != null:
		_fail("a stored stage competes with active threat or printed health")
		return false
	return true


func _card_back_is_safe(back: Control, observed: Dictionary) -> bool:
	observed.back = true
	if back.find_child("Title", true, false) != null:
		_fail("a concealed card back contains a title")
		return false
	if back.find_child("RulesText", true, false) != null:
		_fail("a concealed card back contains rules text")
		return false
	if back.find_child("Illustration", true, false) != null:
		_fail("a concealed card consulted the face-art path")
		return false
	var back_text := _visible_text(back).to_lower()
	if "secret" in back_text or "face-" in back_text:
		_fail("a concealed card back leaked an identity")
		return false
	return true
