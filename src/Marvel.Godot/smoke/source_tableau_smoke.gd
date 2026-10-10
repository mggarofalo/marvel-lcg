extends SceneTree

func _initialize() -> void:
	_run.call_deferred()

func _run() -> void:
	root.size = Vector2i(1920, 1080)
	var main := (load("res://Main.tscn") as PackedScene).instantiate()
	root.add_child(main)
	for frame in range(8): await process_frame
	var fixture := main.find_child("TableauFixture", true, false) as Control
	var tableau := main.find_child("SourceTableau", true, false) as Control
	var failures: Array[String] = []
	if fixture == null or tableau == null:
		push_error("SOURCE_TABLEAU_FAILED missing fixture")
		quit(1)
		return
	var viewport := tableau.find_child("SourceTableauViewport", true, false) as ScrollContainer
	var narrow := bool(fixture.get_meta("narrow", false))
	_check_table_bounds(fixture, tableau, narrow, failures)
	var copies := await _check_cards(tableau, viewport, narrow, failures)
	await _check_copy_input(copies, viewport, fixture, failures)
	await _capture()
	for failure in failures: push_error("SOURCE_TABLEAU_FAILED " + failure)
	if failures.is_empty(): print("SOURCE_TABLEAU_OK sixteen_reachable=true duplicates=true pointer_inspection=true keyboard=true")
	quit(0 if failures.is_empty() else 1)

func _check_table_bounds(fixture: Control, tableau: Control, narrow: bool, failures: Array[String]) -> void:
	if narrow and tableau.get_global_rect().end.x > fixture.global_position.x + 1320:
		failures.append("source viewport clipped by payment table")
	for hand in fixture.find_children("ProceduralCard*", "Control", true, false):
		if hand.has_meta("spatial_hand_index") and _bounds(hand).intersects(tableau.get_global_rect()):
			failures.append("rotated hand obscures source tableau")

func _check_cards(tableau: Control, viewport: ScrollContainer, narrow: bool, failures: Array[String]) -> Array[Control]:
	var cards := tableau.find_children("ProceduralCard*", "Control", true, false)
	if cards.size() != 16: failures.append("lost physical sources")
	var copies: Array[Control] = []
	for card in cards:
		if narrow:
			viewport.ensure_control_visible(card)
			for frame in range(3): await process_frame
		if not viewport.get_global_rect().encloses(card.get_global_rect()):
			failures.append("source hidden by scrolling: " + str(card.name) + " " + str(card.get_global_rect()))
		_check_overlap(card, cards, failures)
		_check_card_readability(card, failures)
		var title := card.find_child("SourceTitle", true, false) as Label
		if title.text == "Powered Gauntlets": copies.append(card)
	return copies

func _check_overlap(card: Control, cards: Array[Node], failures: Array[String]) -> void:
	for other in cards:
		if card != other and card.get_global_rect().intersects(other.get_global_rect()):
			failures.append("source tiles overlap")

func _check_card_readability(card: Control, failures: Array[String]) -> void:
	var title := card.find_child("SourceTitle", true, false) as Label
	if title.get_line_count() > title.get_visible_line_count(): failures.append("source title clipped")
	var button := card.find_child("Card*Action", true, false) as Button
	if button != null and (button.size.y < 44 or not card.get_global_rect().encloses(button.get_global_rect())):
		failures.append("source action exceeds tile")
	if card.find_child("SourceState", true, false) == null: failures.append("missing readiness")

func _check_copy_input(copies: Array[Control], viewport: ScrollContainer, fixture: Control, failures: Array[String]) -> void:
	if copies.size() != 2:
		failures.append("duplicate gauntlets coalesced")
		return
	for copy in copies:
		viewport.ensure_control_visible(copy)
		for frame in range(3): await process_frame
		await _click(copy.get_global_rect().position + Vector2(12, 12))
		var identity := int(str(copy.name).trim_prefix("ProceduralCard"))
		if int(fixture.get_meta("inspected_id", -1)) != identity:
			failures.append("body inspection lost physical source")
		var action := copy.find_child("Card*Action", true, false) as Button
		action.grab_focus()
		await _activate_key()
		if int(fixture.get_meta("selected_id", -1)) != identity:
			failures.append("keyboard selected wrong physical source")

func _activate_key() -> void:
	for pressed in [true, false]:
		var key := InputEventKey.new()
		key.keycode = KEY_ENTER
		key.pressed = pressed
		Input.parse_input_event(key)
		await process_frame

func _capture() -> void:
	var capture := OS.get_environment("MARVEL_TABLEAU_CAPTURE")
	if not capture.is_empty():
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png(capture)

func _click(point: Vector2) -> void:
	var motion := InputEventMouseMotion.new()
	motion.position = point
	Input.parse_input_event(motion)
	await process_frame
	for pressed in [true, false]:
		var event := InputEventMouseButton.new()
		event.position = point
		event.button_index = MOUSE_BUTTON_LEFT
		event.pressed = pressed
		Input.parse_input_event(event)
		await process_frame

func _bounds(control: Control) -> Rect2:
	var pose := control.get_global_transform()
	var bounds := Rect2(pose * Vector2.ZERO, Vector2.ZERO)
	for point in [Vector2(control.size.x, 0), control.size, Vector2(0, control.size.y)]:
		bounds = bounds.expand(pose * point)
	return bounds
