extends SceneTree

var main: Control
var sample: ColorRect
var baseline := PackedByteArray()
var rendered := false

func _initialize() -> void:
	rendered = DisplayServer.get_name() != "headless"
	root.gui_embed_subwindows = true
	_run.call_deferred()

func _run() -> void:
	main = load("res://Main.tscn").instantiate()
	root.add_child(main)
	root.size = Vector2i(1920, 1080)
	if await preload("res://smoke/setup_smoke_readiness.gd").wait_for_start(main) == null:
		_fail("setup did not become ready")
		return
	_make_sample()
	for size in [Vector2i(1920, 1080), Vector2i(2560, 1440), Vector2i(3840, 2160), Vector2i(2303, 1297), Vector2i(1920, 1080)]:
		if not await _check_resolution(size): return
	sample.hide()
	if not await _check_deck_window(): return
	if not await _activate(_button(main, "Start game")): return
	if not await _wait_for_table(): return
	for size in [Vector2i(1920, 1080), Vector2i(2560, 1440), Vector2i(3840, 2160)]:
		root.size = size
		await process_frame
		await process_frame
		if not _native_pixels(root): return
		await _capture("table-" + str(size.x))
	main.queue_free()
	await process_frame
	print("TEXT_RESOLUTION_OK rendered=%s ui_scale=%s" % [rendered, OS.get_environment("MARVEL_UI_SCALE")])
	quit(0)

func _check_deck_window() -> bool:
	if not await _activate(_button(main, "Deck library · browse, build, import")): return false
	for attempt in range(400):
		if main.has_node("DeckLibrary"): break
		await create_timer(0.025).timeout
	if not main.has_node("DeckLibrary"):
		_fail("deck library did not open")
		return false
	var library := main.get_node("DeckLibrary") as Window
	await process_frame
	await process_frame
	if not _native_pixels(library): return false
	await _capture("deck")
	library.hide()
	return true

func _make_sample() -> void:
	# Uses the real client's inherited font/theme on a fixed opaque backdrop.
	# Comparing a native crop across resize catches resampling without a machine-specific golden image.
	sample = ColorRect.new()
	sample.position = Vector2(32, 32)
	sample.size = Vector2(720, 60)
	sample.color = Color(0.035, 0.055, 0.082, 1)
	sample.z_index = 250
	sample.mouse_filter = Control.MOUSE_FILTER_IGNORE
	main.add_child(sample)
	var label := Label.new()
	label.text = "Helicarrier — Exhaust to reduce the cost by 1. 0123456789"
	label.position = Vector2(8, 8)
	sample.add_child(label)

func _check_resolution(size: Vector2i) -> bool:
	root.size = size
	await process_frame
	await process_frame
	if not _native_pixels(root): return false
	if main.size != Vector2(size):
		_fail("responsive root does not use the physical window area")
		return false
	if rendered:
		await RenderingServer.frame_post_draw
		var pixels := root.get_texture().get_image().get_region(Rect2i(sample.position, sample.size)).get_data()
		if baseline.is_empty(): baseline = pixels
		elif pixels != baseline:
			_fail("the same text changed rasterization when only the window resolution changed")
			return false
	print("TEXT_RESOLUTION_GEOMETRY size=%s transform=%s dpi=%s screen_scale=%s" % [size, root.get_final_transform(), DisplayServer.screen_get_dpi(), DisplayServer.screen_get_scale()])
	sample.hide()
	await _capture("setup-" + str(size.x))
	sample.show()
	return true

func _native_pixels(window: Window) -> bool:
	if window.get_final_transform() != Transform2D.IDENTITY or window.get_visible_rect().size != Vector2(window.size):
		_fail("text viewport is stretched instead of using physical pixels: " + str(window.get_final_transform()))
		return false
	return true

func _wait_for_table() -> bool:
	for attempt in range(200):
		var play := main.find_child("Play", true, false) as Control
		if play != null and play.visible: return true
		await create_timer(0.025).timeout
	_fail("Start game did not open the table")
	return false

func _capture(label: String) -> void:
	var directory := OS.get_environment("MARVEL_SMOKE_CAPTURE_DIR")
	if not rendered or directory.is_empty(): return
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png(directory.path_join("text-" + OS.get_environment("MARVEL_UI_SCALE") + "-" + label + ".png"))

func _activate(button: Button) -> bool:
	if button == null or button.disabled:
		_fail("required control is unavailable")
		return false
	button.grab_focus()
	for pressed in [true, false]:
		var event := InputEventKey.new()
		event.keycode = KEY_ENTER
		event.pressed = pressed
		root.get_last_exclusive_window().push_input(event)
		await process_frame

	return true

func _button(node: Node, text: String) -> Button:
	if node is Button and node.text == text and node.is_visible_in_tree(): return node
	for child in node.get_children():
		var found := _button(child, text)
		if found != null: return found
	return null

func _fail(message: String) -> void:
	push_error("TEXT_RESOLUTION_FAIL " + message)
	quit(1)
