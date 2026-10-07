extends SceneTree


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	root.size = Vector2i(1920, 1080)
	var main := (load("res://Main.tscn") as PackedScene).instantiate()
	root.add_child(main)
	await process_frame
	await process_frame
	var cards := main.find_child("B1Cards", true, false) as HBoxContainer
	if cards == null or main.is_processing_input():
		_fail("the specimen did not isolate its GUI from uninitialized game input")
		return
	var larger := main.find_child("B1Scale150", true, false) as Button
	if larger == null or not larger.is_visible_in_tree():
		_fail("the specimen has no visible 150% pointer control")
		return
	var point := larger.get_global_rect().get_center()
	for pressed in [true, false]:
		var event := InputEventMouseButton.new()
		event.position = point
		event.global_position = point
		event.button_index = MOUSE_BUTTON_LEFT
		event.pressed = pressed
		Input.parse_input_event(event)
		await process_frame
		await process_frame
	if not _profile_is_ready(cards, 150): return
	var smaller := main.find_child("B1Scale50", true, false) as Button
	smaller.grab_focus()
	await process_frame
	for pressed in [true, false]:
		var event := InputEventKey.new()
		event.keycode = KEY_ENTER
		event.physical_keycode = KEY_ENTER
		event.pressed = pressed
		Input.parse_input_event(event)
		await process_frame
		await process_frame
	if not _profile_is_ready(cards, 50): return
	print("B1_PRIMITIVES_INPUT_OK pointer=150 keyboard=50 synthetic=true")
	main.queue_free()
	await process_frame
	quit(0)


func _profile_is_ready(cards: HBoxContainer, percentage: int) -> bool:
	if cards.get_meta("sample_scale") != percentage or cards.get_child_count() != 5:
		_fail("native input did not replace the complete frame-family specimen")
		return false
	for card in cards.get_children():
		if not card.is_visible_in_tree() or card.size.x <= 0 or card.size.y <= 0:
			_fail("a frame family lost its visible native bounds after scale input")
			return false
	return true


func _fail(message: String) -> void:
	push_error("B1_PRIMITIVES_INPUT_FAILED " + message)
	quit(1)
