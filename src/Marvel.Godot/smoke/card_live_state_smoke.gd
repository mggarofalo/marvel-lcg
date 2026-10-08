extends SceneTree


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	root.size = Vector2i(1920, 1080)
	var main := (load("res://Main.tscn") as PackedScene).instantiate()
	root.add_child(main)
	await process_frame
	var fixtures := main.find_child("B1StateFixtures", true, false)
	if fixtures == null or fixtures.get_child_count() < 15:
		push_error("B1_STATE_FAILED missing specimens")
		quit(1)
		return
	var failures: Array[String] = []
	var last_card: Control
	for sample in fixtures.get_children():
		sample.show()
		await process_frame
		await process_frame
		if sample.has_meta("state_card"):
			_check_card(sample, failures)
			last_card = sample
		else:
			_check_waiting(sample, failures)
		sample.hide()
	last_card.show()
	await process_frame
	await _check_source_input(last_card, fixtures, failures)
	if not failures.is_empty():
		for failure in failures: push_error("B1_STATE_FAILED " + failure)
		quit(1)
		return
	print("B1_STATE_OK upright_state=true complete_rules=true source_pointer_keyboard=true waiting_two_seats=true")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _check_card(sample: Control, failures: Array[String]) -> void:
	var card := sample.find_child("StateCard", true, false) as Control
	var state := card.get_node("SpatialOverlay/LiveState") as Control
	var identity := str(sample.get_meta("title"))
	if abs(state.get_global_transform().get_rotation()) > 0.001:
		failures.append(identity + " state rotates with face")
	var face_bounds := _bounds(card)
	if state.global_position.x < face_bounds.end.x + 7:
		failures.append(identity + " state overlaps its face")
	if state.size.y > max(card.size.x, card.size.y) + 0.1:
		failures.append(identity + " state exceeds reserved host height: " + str(state.size.y))
	var actions := card.get_node("SpatialOverlay/SpatialControls") as Control
	if actions.size.y < 44:
		failures.append(identity + " essential state leaves no visible action: state=" + str(state.size) + " actions=" + str(actions.size))
	if state.find_children("LiveStatus*", "Label", true, false).size() != int(sample.get_meta("expected_entries")):
		failures.append(identity + " lost state entry")
	if bool(sample.get_meta("concealed")) and state.get_child_count() != 0:
		failures.append("concealed card exposes live facts")
	var maximum_mark := card.find_child("ModifiedHealthMaximum", true, false)
	if (maximum_mark != null) != bool(sample.get_meta("modified_maximum")):
		failures.append(identity + " maximum modification differs from supplied flag")
	_check_labels(state, identity, failures)
	_check_rules(card, identity, failures)
	preload("res://smoke/card_progress_checks.gd").check(card, identity, failures)
	var detail := sample.find_child("StateInspection", true, false) as Control
	_check_labels(detail, identity + " inspection", failures)
	_check_rules(detail, identity + " inspection", failures)
	preload("res://smoke/card_progress_checks.gd").check(detail, identity + " inspection", failures)
	if detail.get_global_rect().end.y > 1080:
		failures.append(identity + " inspection exceeds desktop height")


func _check_rules(control: Control, identity: String, failures: Array[String]) -> void:
	var rules := control.find_child("RulesText", true, false) as RichTextLabel
	if rules != null and rules.get_content_height() > rules.size.y + 1:
		failures.append(identity + " clips rules " + str(rules.get_content_height()) + "/" + str(rules.size.y))


func _check_labels(control: Control, identity: String, failures: Array[String]) -> void:
	for label in control.find_children("*", "Label", true, false):
		if label.name == "InteractionCue": continue
		if label.get_line_count() > label.get_visible_line_count():
			failures.append(identity + " clips " + str(label.name) + " size=" + str(label.size) + " lines=" + str(label.get_line_count()) + "/" + str(label.get_visible_line_count()) + " lineheight=" + str(label.get_line_height()) + " spacing=" + str(label.get_theme_constant("line_spacing")))


func _bounds(control: Control) -> Rect2:
	var pose := control.get_global_transform()
	var points := [pose * Vector2.ZERO, pose * Vector2(control.size.x, 0), pose * control.size, pose * Vector2(0, control.size.y)]
	var bounds := Rect2(points[0], Vector2.ZERO)
	for point in points: bounds = bounds.expand(point)
	return bounds


func _check_waiting(sample: Control, failures: Array[String]) -> void:
	var count := int(sample.get_meta("waiting_count"))
	var indicator := sample.find_child("PendingEncounterCards", true, false)
	if count == 0:
		if indicator != null: failures.append("zero waiting count retains a card")
		return
	if indicator == null or indicator.find_children("EncounterBack*", "Panel", false, false).size() != min(count, 3):
		failures.append("waiting card silhouette count incorrect")
		return
	var caption := indicator.find_child("PendingEncounterCount", true, false) as Label
	if str(count) not in caption.text or "Waiting to reveal" not in caption.text:
		failures.append("waiting card is not distinguished from a minion")


func _check_source_input(sample: Control, fixtures: Control, failures: Array[String]) -> void:
	var detail := sample.find_child("StateInspection", true, false)
	var source := detail.find_child("InspectValueSource", true, false) as Button
	var next := detail.find_child("NextValueSource", true, false) as Button
	if source == null or next == null:
		failures.append("missing supplied HP source access")
		return
	await _click(source)
	if int(fixtures.get_meta("opened_source_target", -1)) != 900:
		failures.append("pointer source access lost authorized current copy")
	await _click(next)
	source = detail.find_child("InspectValueSource", true, false) as Button
	source.grab_focus()
	await process_frame
	for pressed in [true, false]:
		var key := InputEventKey.new()
		key.keycode = KEY_ENTER
		key.pressed = pressed
		Input.parse_input_event(key)
		await process_frame
	if str(fixtures.get_meta("opened_source_title", "")) != "Duplicate source" or int(fixtures.get_meta("opened_source_target", 0)) != -1:
		failures.append("historical source keyboard access retained live target or wrong title")


func _click(button: Button) -> void:
	var point := button.get_global_rect().get_center()
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
