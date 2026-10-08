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
		for frame in range(4): await process_frame
		if bool(sample.get_meta("blocked", false)):
			var bounded := sample.find_child("InspectBoundedSources", true, false) as MenuButton
			if bounded == null or sample.find_child("HostSources", true, false).get_child_count() != 0:
				failures.append("occupied table space did not retain a bounded source picker")
			elif bounded.has_focus(): failures.append("automatic source collapse stole focus")
			sample.hide()
			continue
		_check_layout(sample, failures)
		await _check_source_input(sample, fixtures, failures)
		sample.hide()
	if not failures.is_empty():
		for failure in failures: push_error("B1_SOURCES_FAILED " + failure)
		quit(1)
		return
	print("B1_SOURCES_OK host_width=true rail_clearance=true titles_complete=true pointer_keyboard=true duplicate_picker=true")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)

func _check_source_input(sample: Control, fixtures: Control, failures: Array[String]) -> void:
	var source := sample.find_child("SourceLedger", true, false).find_child("ProceduralCard*", true, false) as Control
	await _click(source)
	if int(fixtures.get_meta("inspected_id", -1)) != int(sample.get_meta("source_id")):
		failures.append(str(sample.get_meta("source_title")) + " pointer did not inspect source")
	source.grab_focus()
	await _key(KEY_ENTER)
	if str(fixtures.get_meta("inspected_rules", "")).is_empty():
		failures.append("keyboard source lost complete rules")
	var picker := sample.find_child("SourceLedger", true, false).get_node("SourcePicker") as MenuButton
	if picker.get_popup().item_count != int(sample.get_meta("source_count")): failures.append("source picker lost a physical source")
	await _click(picker)
	await _key(KEY_DOWN)
	await _key(KEY_DOWN)
	await _key(KEY_ENTER)
	for frame in range(3): await process_frame
	var replacement := sample.find_child("SourceLedger", true, false).find_child("ProceduralCard*", true, false) as Control
	if replacement.name != "ProceduralCard" + str(int(sample.get_meta("second_source_id"))):
		failures.append("duplicate title picker lost second physical source: " + str(replacement.name))
	if not picker.has_focus(): failures.append("picker lost focus on source replacement")
	replacement.grab_focus()
	await _key(KEY_ENTER)
	if int(fixtures.get_meta("inspected_id", -1)) != int(sample.get_meta("second_source_id")):
		failures.append("replacement source retained stale identity")

func _check_layout(sample: Control, failures: Array[String]) -> void:
	var host := sample.find_child("SourceHost", true, false) as Control
	var bounds := _bounds(host)
	var tabs := host.find_child("SourceCollection", true, false) as Control
	if abs(tabs.size.x - bounds.size.x) > 0.1:
		failures.append("host tab width " + str(tabs.size.x) + " != " + str(bounds.size.x))
	if abs(tabs.get_global_transform().get_rotation()) > 0.001:
		failures.append("tabs rotated with exhausted host")
	var ledger := sample.find_child("SourceLedger", true, false) as Control
	if abs(ledger.size.x - 250) > 0.1: failures.append("ledger is not 250px: " + str(ledger.size.x))
	_check_readability(sample, failures)

func _check_readability(sample: Control, failures: Array[String]) -> void:
	for title in sample.find_children("SourceTitle", "Label", true, false):
		if title.text != str(sample.get_meta("source_title")) or title.get_line_count() > title.get_visible_line_count():
			failures.append("source title clipped or replaced")
	for strip in sample.find_children("SourceStrip", "MarginContainer", true, false):
		var contents := strip.get_node("SourceContents") as Control
		if contents.position.x < 20: failures.append("content intrudes into angled rail")
		if strip.get_global_rect().end.y > 1000: failures.append("source strip exceeds desktop")
	for meaning in sample.find_children("SourceMeaning*", "RichTextLabel", true, false):
		if meaning.get_content_height() > meaning.size.y + 1: failures.append("source meaning clipped")
	for action in sample.find_children("SourceAction", "Button", true, false):
		var body := action.get_parent().get_parent()
		var strip := body.get_node("SourceStrip") as Control
		if action.size.y < 44 or action.global_position.y < strip.get_global_rect().end.y:
			failures.append("source action obscures its meaning or loses hit area")

func _bounds(control: Control) -> Rect2:
	var pose := control.get_global_transform()
	var points := [pose * Vector2.ZERO, pose * Vector2(control.size.x, 0), pose * control.size, pose * Vector2(0, control.size.y)]
	var bounds := Rect2(points[0], Vector2.ZERO)
	for point in points: bounds = bounds.expand(point)
	return bounds

func _key(code: int) -> void:
	for pressed in [true, false]:
		var key := InputEventKey.new()
		key.keycode = code
		key.pressed = pressed
		Input.parse_input_event(key)
		await process_frame

func _click(control: Control) -> void:
	var point := control.get_global_rect().get_center()
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
