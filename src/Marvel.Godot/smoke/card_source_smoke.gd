extends SceneTree

func _initialize() -> void:
	_run.call_deferred()

func _run() -> void:
	root.size = Vector2i(1920, 1080)
	var main := (load("res://Main.tscn") as PackedScene).instantiate()
	root.add_child(main)
	await process_frame
	var fixtures := main.find_child("B1SourceFixtures", true, false)
	var failures: Array[String] = []
	if fixtures == null or fixtures.get_child_count() < 9:
		push_error("B1_SOURCES_FAILED missing fixtures")
		quit(1)
		return
	for sample in fixtures.get_children():
		sample.show()
		for frame in range(6): await process_frame
		await _check_sample(sample, fixtures, failures)
		sample.hide()
	for failure in failures: push_error("B1_SOURCES_FAILED " + failure)
	if failures.is_empty(): print("B1_SOURCES_OK continuous_copies=true titles_bounded=true full_identity_accessible=true complete_rules=true pointer_keyboard=true")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0 if failures.is_empty() else 1)

func _check_sample(sample: Control, fixtures: Control, failures: Array[String]) -> void:
	if not sample.find_children("*", "MenuButton", true, false).is_empty():
		failures.append("source collection uses a dropdown")
	var ledger := sample.find_child("SourceLedger", true, false) as Control
	if abs(ledger.size.x - 250) > 0.1: failures.append("ledger is not bounded to 250px")
	await _check_sources(ledger, sample, fixtures, failures)
	var attached := sample.find_child("SourceHost", true, false).find_child("SourceCollection", true, false) as Control
	if attached == null:
		failures.append("missing host attachment collection")
	else:
		if abs(attached.get_global_transform().get_rotation()) > 0.001:
			failures.append("attachments rotated with exhausted host")
		if attached.find_children("ProceduralCard*", "Control", true, false).size() != int(sample.get_meta("source_count")):
			failures.append("host attachment collection lost physical copies")
		await _check_sources(attached, sample, fixtures, failures)

func _check_sources(ledger: Control, sample: Control, fixtures: Control, failures: Array[String]) -> void:
	var cards := ledger.find_children("ProceduralCard*", "Control", true, false)
	var scroll := ledger.find_child("AttachedSourceScroll", true, false) as ScrollContainer
	if scroll != null: failures.append("host attachment creates nested scrolling")
	if cards.size() != int(sample.get_meta("source_count")):
		failures.append("continuous collection lost a physical source")
	for card in cards:
		await _check_copy(card, scroll, fixtures, failures)

func _check_copy(card: Control, scroll: ScrollContainer, fixtures: Control, failures: Array[String]) -> void:
	if scroll != null: scroll.ensure_control_visible(card)
	for frame in range(4): await process_frame
	var title := card.find_child("SourceTitle", true, false) as Label
	_scroll_ancestors(card, title)
	for frame in range(3): await process_frame
	_check_title(title, card, failures)
	var action := card.find_child("SourceAction", true, false) as Button
	_check_action(action, card, failures)
	await _check_copy_input(card, title, fixtures, failures)

func _check_title(title: Label, card: Control, failures: Array[String]) -> void:
	if title == null or title.text.is_empty():
		failures.append("source title clipped or missing")
		return
	if title.get_line_count() > title.get_visible_line_count():
		failures.append("source title clipped or missing")
	if title.get_line_count() > 2:
		failures.append("source title exceeds two compact lines")
	if not card.get_global_rect().encloses(title.get_global_rect()):
		failures.append("source title exceeds its compact host-local tile")
	if title.accessibility_name != str(card.get_meta("source_title")):
		failures.append("source title loses full accessible identity")
	var measured := title.get_theme_font("font").get_string_size(title.text.to_upper(), HORIZONTAL_ALIGNMENT_LEFT, -1, title.get_theme_font_size("font_size"))
	if title.autowrap_mode == TextServer.AUTOWRAP_OFF and measured.x > title.size.x and title.text_overrun_behavior != TextServer.OVERRUN_TRIM_ELLIPSIS:
		failures.append("bounded source title hides words without ellipsis")

func _check_copy_input(card: Control, title: Label, fixtures: Control, failures: Array[String]) -> void:
	var identity := int(str(card.name).trim_prefix("ProceduralCard"))
	var original: Vector2 = card.position
	# Preview the title, leaving the source-integrated action hit area independent.
	var visible_title := _visible_rect(title)
	if not visible_title.has_area(): failures.append("host attachment title cannot be reached")
	await _click(visible_title.get_center())
	if int(fixtures.get_meta("previewed_id", -1)) != identity:
		failures.append("pointer preview lost physical source " + str(identity))
	if card.position.distance_to(original) > 1: failures.append("hover reordered a physical source")
	card.grab_focus()
	await _key(KEY_ENTER)
	if int(fixtures.get_meta("inspected_id", -1)) != identity or str(fixtures.get_meta("inspected_rules", "")).is_empty():
		failures.append("keyboard inspection lost identity or complete source rules")

func _check_action(action: Button, card: Control, failures: Array[String]) -> void:
	if action == null:
		failures.append("missing source action")
		return
	var paper := (action.get_theme_stylebox("normal") as StyleBoxFlat).bg_color
	var ink := action.get_theme_color("font_color")
	ink = paper.lerp(ink, ink.a * action.self_modulate.a)
	var light := maxf(paper.srgb_to_linear().get_luminance(), ink.srgb_to_linear().get_luminance())
	var dark := minf(paper.srgb_to_linear().get_luminance(), ink.srgb_to_linear().get_luminance())
	if (light + 0.05) / (dark + 0.05) < 4.5: failures.append("source action lacks paper contrast")
	if action.text.is_empty(): failures.append("source action lost its name")
	if action.size.y < 28 or not card.get_global_rect().encloses(action.get_global_rect()):
		failures.append("source action is clipped or loses hit area")

func _key(code: int) -> void:
	for pressed in [true, false]:
		var key := InputEventKey.new()
		key.keycode = code
		key.pressed = pressed
		Input.parse_input_event(key)
		await process_frame

func _click(point: Vector2) -> void:
	var motion := InputEventMouseMotion.new()
	motion.position = point
	Input.parse_input_event(motion)
	await process_frame
	for pressed in [true, false]:
		var click := InputEventMouseButton.new()
		click.position = point
		click.button_index = MOUSE_BUTTON_LEFT
		click.pressed = pressed
		Input.parse_input_event(click)
		await process_frame

func _scroll_ancestors(scroll: Control, title: Control) -> void:
	var parent := scroll.get_parent()
	while parent != null:
		if parent is ScrollContainer: parent.ensure_control_visible(title)
		parent = parent.get_parent()

func _visible_rect(control: Control) -> Rect2:
	var bounds := control.get_global_rect()
	var parent := control.get_parent()
	while parent != null:
		if parent is ScrollContainer: bounds = bounds.intersection(parent.get_global_rect())
		parent = parent.get_parent()
	return bounds
