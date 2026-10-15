extends SceneTree

var modified_checks := 0

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
		if sample.get_meta("navigation", false): await _navigation(sample, failures)
		else: await _inspect(sample, failures)
		sample.hide()
	if modified_checks == 0: failures.append("no modified-value disclosure was exercised")
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
	source.grab_focus()
	await _key(KEY_ENTER)
	for frame in range(4): await process_frame
	var popup := root.find_child("PileInspector", true, false) as PopupPanel
	if popup == null:
		failures.append("Enter did not open inspection")
		return
	_check(popup, sample, failures)
	await _late_minimum_growth(popup, failures)
	await _modified_value(popup, failures)
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

func _navigation(sample: Control, failures: Array[String]) -> void:
	var source := sample.get_node("InspectionSource") as Control
	source.grab_focus()
	await _key(KEY_ENTER)
	for frame in range(4): await process_frame
	var popup := root.find_child("PileInspector", true, false) as PopupPanel
	if popup == null:
		failures.append("navigation pile did not open")
		return
	var next := popup.find_child("NextPileCard", true, false) as Button
	var previous := popup.find_child("PreviousPileCard", true, false) as Button
	var next_rect := next.get_global_rect()
	var previous_rect := previous.get_global_rect()
	var popup_rect := Rect2(Vector2(popup.position), Vector2(popup.size))
	var fixed_point := Vector2(popup.position) + next_rect.get_center()
	var orientations: Dictionary = {}
	for index in range(6):
		_check_popup_bounds(popup, failures)
		var face := popup.find_child("CardFace", true, false) as Control
		orientations[face.size.x > face.size.y] = true
		if next.get_global_rect() != next_rect or previous.get_global_rect() != previous_rect or Rect2(Vector2(popup.position), Vector2(popup.size)) != popup_rect:
			failures.append("pile navigation moved beneath the pointer")
		if face.scale != Vector2.ONE: failures.append("inspector scaled rendered card fonts")
		if next.disabled: break
		await _popup_click(fixed_point)
		for frame in range(4): await process_frame
		var position := popup.find_child("PileInspectorPosition", true, false) as Label
		if not position.text.begins_with(str(index + 2) + " /"):
			failures.append("fixed pointer did not advance pile card")
	if orientations.size() != 2: failures.append("navigation did not cover portrait and landscape cards")
	await _close(popup, source, failures)

func _popup_click(point: Vector2) -> void:
	var motion := InputEventMouseMotion.new()
	motion.position = point
	motion.global_position = point
	Input.parse_input_event(motion)
	for pressed in [true, false]:
		var click := InputEventMouseButton.new()
		click.position = point
		click.global_position = point
		click.button_index = MOUSE_BUTTON_LEFT
		click.pressed = pressed
		Input.parse_input_event(click)
		await process_frame


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
	_check_popup_bounds(popup, failures)
	var face := popup.find_child("CardFace", true, false)
	var title := face.find_child("Title", true, false) as Label
	if title.text != str(sample.get_meta("title")): failures.append("inspection lost full card identity")
	_check_text(popup, face, failures)
	_check_boundaries(popup, failures)

func _check_popup_bounds(popup: PopupPanel, failures: Array[String]) -> void:
	var bounds := Rect2(Vector2(popup.position), Vector2(popup.size))
	if not Rect2(0, 0, 1920, 1080).encloses(bounds): failures.append("popup exceeds viewport: " + str(bounds))

func _late_minimum_growth(popup: PopupPanel, failures: Array[String]) -> void:
	if OS.get_environment("MARVEL_UI_SCALE") != "50": return
	# Synthetic layout stress: late native minimum growth must reposition
	# the window while source geometry and dismissal remain usable.
	popup.min_size = popup.size + Vector2i(0, 16)
	for frame in range(3): await process_frame
	_check_popup_bounds(popup, failures)

func _check_text(popup: PopupPanel, face: Control, failures: Array[String]) -> void:
	for label in popup.find_children("*", "Label", true, false):
		if not label.is_visible_in_tree(): continue
		if label.name == "InteractionCue": continue
		if label.get_line_count() > label.get_visible_line_count(): failures.append("inspection clips " + str(label.name))
	var rules := face.find_child("RulesText", true, false) as RichTextLabel
	if rules.get_content_height() > rules.size.y + 1: failures.append("complete rules are clipped")
	if rules.get_parsed_text().strip_edges().is_empty(): failures.append("inspection lost rules")

func _modified_value(popup: PopupPanel, failures: Array[String]) -> void:
	if popup.find_child("LiveProgress", true, false) != null:
		failures.append("inspection repeats the face's HP or threat value")
	var detail := popup.find_child("ValueSourceDetail", true, false) as Control
	if detail == null: return
	if detail.visible: failures.append("modified-value explanation opens without intent")
	var mark := popup.find_child("ModifiedHealthMaximum", true, false) as Control
	if mark == null: return
	var value := mark.get_parent() as Control
	modified_checks += 1
	await _popup_click(Vector2(popup.position) + value.get_global_rect().get_center())
	await process_frame
	if not detail.is_visible_in_tree(): failures.append("underlined HP does not disclose its calculation")
	var source := detail.find_child("InspectValueSource*", true, false) as Button
	if source == null or not source.is_visible_in_tree(): failures.append("modified HP hides its contributing source")
	value.grab_focus()
	await _key(KEY_ENTER)
	if detail.visible: failures.append("keyboard cannot collapse the modified-value explanation")

func _check_boundaries(popup: PopupPanel, failures: Array[String]) -> void:
	var count := popup.find_child("PileInspectorPosition", true, false) as Label
	if count.text != "1 / 1": failures.append("concealed cards or false pile order entered inspection")
	for scroll in popup.find_children("*", "ScrollContainer", true, false):
		if scroll.get_v_scroll_bar().visible or scroll.get_h_scroll_bar().visible: failures.append("inspection requires scrolling " + str(popup.size) + " scroll=" + str(scroll.size) + " min=" + str(scroll.get_child(0).get_combined_minimum_size()))
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
