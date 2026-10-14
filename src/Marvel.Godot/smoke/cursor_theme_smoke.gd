extends SceneTree

const ARTWORK := ["arrow", "point", "grab", "drag", "drop", "forbidden", "text"]
var failures: Array[String] = []

func _initialize() -> void:
	_run.call_deferred()

func _run() -> void:
	root.size = Vector2i(1120, 480)
	var specimen := Control.new()
	root.add_child(specimen)
	for row in range(3):
		var pixels: int = [32, 40, 48][row]
		for column in range(ARTWORK.size()):
			_add_cursor(specimen, ARTWORK[column], pixels, Vector2(24 + column * 154, 20 + row * 150))
	for frame in range(3): await process_frame
	var directory := OS.get_environment("MARVEL_CURSOR_CAPTURE")
	if not directory.is_empty() and DisplayServer.get_name() != "headless":
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png(directory.path_join("cursor-specimen.png"))
	for failure in failures: push_error("CURSOR_THEME_FAILED " + failure)
	if failures.is_empty(): print("CURSOR_THEME_OK shapes=7 raster_sizes=3")
	quit(0 if failures.is_empty() else 1)

func _add_cursor(owner: Control, name: String, pixels: int, position: Vector2) -> void:
	var image := Image.new()
	var svg := FileAccess.get_file_as_string("res://assets/cursors/%s.svg" % name)
	if image.load_svg_from_string(svg, pixels / 32.0) != OK:
		failures.append("invalid vector artwork: " + name)
		return
	if image.get_size() != Vector2i(pixels, pixels) or image.is_invisible():
		failures.append("cursor raster is empty or incorrectly sized: " + name)
	for background in range(2):
		var tile := ColorRect.new()
		tile.color = Color("101f2a") if background == 0 else Color("f2edd9")
		tile.position = position + Vector2(background * 68, 0)
		tile.size = Vector2(64, 80)
		owner.add_child(tile)
		var cursor := TextureRect.new()
		cursor.texture = ImageTexture.create_from_image(image)
		cursor.position = (tile.size - Vector2(pixels, pixels)) / 2
		tile.add_child(cursor)
	var label := Label.new()
	label.text = "%s · %dpx" % [name, pixels]
	label.position = position + Vector2(0, 84)
	label.add_theme_font_size_override("font_size", 15)
	owner.add_child(label)
