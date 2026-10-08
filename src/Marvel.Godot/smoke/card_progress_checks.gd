extends RefCounted

static func check(card: Control, identity: String, failures: Array[String]) -> void:
	var health := card.find_child("ProgressToken", true, false) as Control
	var resources := card.find_child("ResourceIcons", true, false) as Control
	for value in card.find_children("ProgressValues*", "Label", true, false):
		if value.text.is_empty() or value.get_line_count() != 1 or value.get_visible_line_count() != 1:
			failures.append(identity + " progress value is clipped: " + value.text)
		if not Rect2(Vector2.ZERO, health.size).grow(0.1).encloses(value.get_rect()):
			failures.append(identity + " progress value exceeds footer: " + str(value.get_rect()) + " / " + str(health.size))
		for index in value.text.length():
			if value.get_character_bounds(index).size == Vector2.ZERO:
				failures.append(identity + " progress character is not rendered: " + value.text[index])
		var font: Font = value.get_theme_font("font")
		var font_size: int = value.get_theme_font_size("font_size")
		if font.get_string_size(value.text, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size).x > value.size.x + 0.1:
			failures.append(identity + " progress value overflows its width: " + value.text)
	if health != null and resources != null and health.get_rect().intersects(resources.get_rect()):
		failures.append(identity + " resources overlap the health target " + str(resources.get_rect()) + " " + str(health.get_rect()))
