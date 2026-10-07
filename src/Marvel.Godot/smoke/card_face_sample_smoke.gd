extends SceneTree


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	root.size = Vector2i(1920, 1080)
	var main := (load("res://Main.tscn") as PackedScene).instantiate()
	root.add_child(main)
	await process_frame
	await process_frame
	var fixtures := main.find_child("B1FaceFixtures", true, false)
	if fixtures == null or fixtures.get_child_count() < 400:
		push_error("B1_FACES_FAILED missing complete projected fixture set")
		quit(1)
		return
	var failures: Array[String] = []
	for card in fixtures.get_children():
		var identity := str(card.get_meta("fixture_id")) + "/" + str(card.get_meta("fixture_size"))
		_check_text(card, identity, failures)
		_check_identity_lines(card, identity, failures)
		_check_footer(card, identity, failures)
		_check_marks(card, identity, failures)
	if not failures.is_empty():
		for failure in failures: push_error("B1_FACES_FAILED " + failure)
		quit(1)
		return
	print("B1_FACES_OK faces=" + str(fixtures.get_child_count()) + " complete_rules=true bounded_titles=true")
	main.queue_free()
	await process_frame
	quit(0)


func _check_text(card: Control, identity: String, failures: Array[String]) -> void:
	var rules := card.find_child("RulesText", true, false) as RichTextLabel
	var title := card.find_child("Title", true, false) as Label
	if rules == null or rules.scroll_active or rules.get_content_height() > rules.size.y + 1:
		failures.append(identity + " rules overflow")
	if title == null or title.get_line_count() > 2:
		failures.append(identity + " title overflow")
	var cost := str(card.get_meta("expected_cost"))
	if not cost.is_empty():
		var value := card.find_child("PrimaryValueValue", true, false) as Label
		if value == null or value.text != cost:
			failures.append(identity + " missing printed cost including zero")
	for number in card.find_children("SummaryValues*", "Label", true, false):
		if number.text.is_empty() or number.size.y < number.get_theme_font_size("font_size"):
			failures.append(identity + " number clipped " + number.name)


func _check_marks(card: Control, identity: String, failures: Array[String]) -> void:
	var effective: Dictionary = JSON.parse_string(str(card.get_meta("expected_effective")))
	var marks = JSON.parse_string(str(card.get_meta("expected_marks")))
	for mark in marks:
		var attribute: String = mark["Attribute"]
		var cell := card.find_child("Stat" + attribute, true, false) as Control
		if cell == null: continue
		var number := cell.find_child("SummaryValues" + attribute, true, false) as Control
		if number == null:
			failures.append(identity + " missing canonical value " + attribute)
			continue
		var star := cell.find_child("SpecialStar" + attribute, true, false) as Control
		var expects_star: bool = mark["SpecialStar"] and mark["Value"] != "★"
		if (star != null) != expects_star:
			failures.append(identity + " duplicate or missing special star " + attribute)
		if mark["Value"] == "★" and not number is TextureRect:
			failures.append(identity + " bare star is not canonical glyph")
		if star != null and absf(star.position.x - number.get_rect().end.x) > 1.5:
			failures.append(identity + " star detached from numeral " + attribute)
		_check_consequences(card, cell, number, mark, identity, failures)
		_check_modifier(cell, attribute, effective.get(attribute, {}), identity, failures)
	if str(card.get_meta("fixture_id")) == "01121" and card.find_child("BoostSpecialStar", true, false) == null:
		failures.append(identity + " zero-boost special star missing")


func _check_consequences(card: Control, cell: Control, number: Control, mark: Dictionary,
		identity: String, failures: Array[String]) -> void:
	var attribute: String = mark["Attribute"]
	var consequences := cell.find_children("Consequential" + attribute + "*", "TextureRect", false, false)
	if consequences.size() != int(mark["ConsequentialDamage"]):
		failures.append(identity + " wrong consequence count " + attribute)
	if consequences.is_empty(): return
	var band := card.find_child("ProtectedStatsBand", true, false) as Polygon2D
	for damage in consequences:
		var bottom: Vector2 = cell.position + damage.position + Vector2(damage.size.x / 2, damage.size.y - 0.1)
		if band == null or not Geometry2D.is_point_in_polygon(bottom, band.polygon):
			failures.append(identity + " consequence lost its ink contrast field " + attribute)
	var group_center: float = (consequences[0].position.x + consequences[-1].get_rect().end.x) / 2
	if absf(group_center - number.get_rect().get_center().x) > 0.6:
		failures.append(identity + " consequences not centered on numeral " + attribute)


func _check_identity_lines(card: Control, identity: String, failures: Array[String]) -> void:
	for name in ["Traits", "Kind"]:
		var label := card.find_child(name, true, false) as Label
		if label != null and label.get_line_count() > label.get_visible_line_count():
			failures.append(identity + " clipped identity line " + name)


func _check_footer(card: Control, identity: String, failures: Array[String]) -> void:
	var health := card.find_child("ProgressToken", true, false) as Control
	var resources := card.find_child("ResourceIcons", true, false) as Control
	if health != null and resources != null and health.get_rect().intersects(resources.get_rect()):
		failures.append(identity + " resources overlap the health target " + str(resources.get_rect()) + " " + str(health.get_rect()))


func _check_modifier(cell: Control, attribute: String, expected: Dictionary,
		identity: String, failures: Array[String]) -> void:
	var underline := cell.find_child("Modified" + attribute, true, false) as ColorRect
	if (underline != null) != bool(expected.get("IsModified", false)):
		failures.append(identity + " modification indicator did not follow the typed flag")
	if underline == null: return
	for damage in cell.find_children("Consequential" + attribute + "*", "TextureRect", false, false):
		if underline.get_rect().end.y >= damage.position.y:
			failures.append(identity + " modifier underline overlaps consequences")
