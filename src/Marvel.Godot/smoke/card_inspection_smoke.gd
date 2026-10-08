extends SceneTree

func _initialize() -> void:
	_run.call_deferred()

func _run() -> void:
	root.size = Vector2i(1920, 1080)
	var main := (load("res://Main.tscn") as PackedScene).instantiate()
	root.add_child(main)
	await process_frame
	var fixtures := main.find_child("B1InspectionFixtures", true, false)
	if fixtures == null or fixtures.get_child_count() < 28:
		push_error("B1_INSPECTION_FAILED missing fixtures")
		quit(1)
		return
	var failures: Array[String] = []
	for sample in fixtures.get_children():
		sample.show()
		await process_frame
		await _inspect(sample, failures)
		sample.hide()
	if not failures.is_empty():
		for failure in failures: push_error("B1_INSPECTION_FAILED " + failure)
		quit(1)
		return
	print("B1_INSPECTION_OK full_text=true edges=true readonly=true keyboard_pointer=true source_focus=true")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)

func _inspect(sample: Control, failures: Array[String]) -> void:
	var source := sample.get_node("InspectionSource") as Control
	await _neighbor(sample, failures)
	var original := source.get_global_transform()
	await _click(source)
	for frame in range(4): await process_frame
	var popup := root.find_child("PileInspector", true, false) as PopupPanel
	if popup == null:
		failures.append("pointer did not open inspection")
		return
	_check(popup, sample, failures)
	await _close(popup, source, failures)
	if source.get_global_transform() != original: failures.append("inspection moved its source")
	source.grab_focus()
	await _key(KEY_ENTER)
	for frame in range(4): await process_frame
	popup = root.find_child("PileInspector", true, false) as PopupPanel
	if popup == null:
		failures.append("keyboard did not open inspection")
		return
	_check(popup, sample, failures)
	var popup_id := popup.get_instance_id()
	await _key(KEY_ESCAPE)
	await _assert_closed(popup_id, source, failures)


func _neighbor(sample: Control, failures: Array[String]) -> void:
	var neighbor := sample.get_node_or_null("InspectionNeighbor") as Button
	if neighbor == null: return
	await _click(neighbor)
	if not sample.get_meta("neighbor_activated", false):
		failures.append("an empty card action area intercepted its neighboring control")
		var popup := root.find_child("PileInspector", true, false) as PopupPanel
		if popup != null: popup.hide()
		await process_frame

func _check(popup: PopupPanel, sample: Control, failures: Array[String]) -> void:
	var bounds := Rect2(Vector2(popup.position), Vector2(popup.size))
	if not Rect2(0, 0, 1920, 1080).encloses(bounds): failures.append("popup exceeds viewport: " + str(bounds))
	var face := popup.find_child("CardFace", true, false)
	var title := face.find_child("Title", true, false) as Label
	if title.text != str(sample.get_meta("title")): failures.append("inspection lost full card identity")
	_check_text(popup, face, failures)
	_check_boundaries(popup, failures)

func _check_text(popup: PopupPanel, face: Control, failures: Array[String]) -> void:
	for label in popup.find_children("*", "Label", true, false):
		if label.name == "InteractionCue": continue
		if label.get_line_count() > label.get_visible_line_count(): failures.append("inspection clips " + str(label.name))
	var rules := face.find_child("RulesText", true, false) as RichTextLabel
	if rules.get_content_height() > rules.size.y + 1: failures.append("complete rules are clipped")
	if rules.get_parsed_text().strip_edges().is_empty(): failures.append("inspection lost rules")

func _check_boundaries(popup: PopupPanel, failures: Array[String]) -> void:
	var count := popup.find_child("PileInspectorPosition", true, false) as Label
	if count.text != "1 / 1": failures.append("concealed cards or false pile order entered inspection")
	for scroll in popup.find_children("*", "ScrollContainer", true, false):
		if scroll.get_v_scroll_bar().visible or scroll.get_h_scroll_bar().visible: failures.append("inspection requires scrolling")
	if not popup.find_children("Card*Action", "Button", true, false).is_empty(): failures.append("read-only inspection offers a game action")

func _close(popup: PopupPanel, source: Control, failures: Array[String]) -> void:
	var popup_id := popup.get_instance_id()
	var close := popup.find_child("ClosePileInspector", true, false) as Button
	# Use the popup's native window coordinates for its close button.
	var point := Vector2(popup.position) + close.get_global_rect().get_center()
	var motion := InputEventMouseMotion.new()
	motion.position = point
	motion.global_position = point
	Input.parse_input_event(motion)
	await process_frame
	for pressed in [true, false]:
		var event := InputEventMouseButton.new()
		event.button_index = MOUSE_BUTTON_LEFT
		event.position = point
		event.global_position = point
		event.pressed = pressed
		Input.parse_input_event(event)
		await process_frame
	await _assert_closed(popup_id, source, failures)

func _assert_closed(popup_id: int, source: Control, failures: Array[String]) -> void:
	for frame in range(3): await process_frame
	if is_instance_id_valid(popup_id):
		var popup := instance_from_id(popup_id) as PopupPanel
		failures.append("close did not dismiss the popup: visible=" + str(popup.visible) + " queued=" + str(popup.is_queued_for_deletion()))
		popup.hide()
		await process_frame
	if not source.has_focus(): failures.append("inspection did not restore its source focus")

func _key(code: int) -> void:
	for pressed in [true, false]:
		var key := InputEventKey.new()
		key.keycode = code
		key.pressed = pressed
		Input.parse_input_event(key)
		await process_frame

func _click(control: Control) -> void:
	var point := control.get_global_transform() * (control.size / 2)
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
