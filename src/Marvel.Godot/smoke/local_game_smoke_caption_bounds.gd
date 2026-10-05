extends RefCounted

# Measures whole words against the actual themed control, not an opening screenshot.
static func problems(main: Control) -> Array[String]:
	var failures: Array[String] = []
	for node in main.find_children("*", "Button", true, false):
		var button := node as Button
		if button.is_visible_in_tree() and _bounded_caption(button):
			failures.append_array(word_problems(button))
	var viewport := main.find_child("ContextualActionScroll", true, false) as Control
	if viewport != null:
		failures.append_array(commitment_problems(viewport))
	return failures


static func word_problems(button: Button) -> Array[String]:
	var failures: Array[String] = []
	var font := button.get_theme_font("font")
	var font_size := button.get_theme_font_size("font_size")
	var available := button.size.x - button.get_theme_stylebox("normal").get_minimum_size().x
	for word in button.text.replace("\n", " ").split(" ", false):
		var width := font.get_string_size(word, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size).x
		if width > available + 0.5:
			failures.append("%s cannot display the whole word %s: %.1f > %.1f" % [
				button.name, word, width, available])
	return failures


static func _bounded_caption(button: Button) -> bool:
	return button.has_meta("spatial_upright_control") \
		or str(button.name).begins_with("InspectPile") \
		or str(button.name).begins_with("InspectAttachments") \
		or str(button.name).begins_with("Contextual") \
		or button.name in [&"Earlier", &"More", &"CopyInvitation", &"CompleteChoiceSheet"]


static func commitment_problems(viewport: Control) -> Array[String]:
	var failures: Array[String] = []
	for node in viewport.find_children("ContextualCommit", "Button", true, false):
		var button := node as Button
		if button.is_visible_in_tree() and button.size.y > viewport.size.y + 0.5:
			failures.append("%s cannot fit a whole commitment in its viewport: %.1f > %.1f" % [
				button.name, button.size.y, viewport.size.y])
	return failures
