extends RefCounted

static func check(main: Node, failures: Array[String]) -> void:
	var samples := main.find_child("UiTypographySamples", true, false)
	if samples == null or samples.get_child_count() != 5:
		failures.append("missing complete packaged typography sample")
		return
	var atlas := Image.create(1200, 5 * 48, false, Image.FORMAT_RGBA8)
	atlas.fill(Color("132532"))
	var row := 0
	for label in samples.get_children():
		_check_label(label, row, atlas, failures)
		row += 1
	var output := OS.get_environment("MARVEL_B1_GLYPH_ATLAS")
	if not output.is_empty() and atlas.save_png(output) != OK:
		failures.append("could not save native glyph raster evidence")

static func _check_label(label: Label, row: int, atlas: Image, failures: Array[String]) -> void:
	var font := label.get_theme_font("font")
	var size := label.get_theme_font_size("font_size")
	if label.get_line_count() != 1 or label.get_visible_line_count() != 1:
		failures.append(str(label.name) + " clips the symbol line")
	var primary: FontFile = font.base_font if font is FontVariation else font
	if primary.allow_system_fallback or primary.fallbacks.size() != 1 or primary.fallbacks[0].allow_system_fallback:
		failures.append(str(label.name) + " depends on an unpinned system fallback")
	var line := TextLine.new()
	line.add_string(label.text, font, size)
	var server := TextServerManager.get_primary_interface()
	var glyphs := server.shaped_text_get_glyphs(line.get_rid())
	var pen := Vector2(12, row * 48 + 30)
	for glyph in glyphs:
		var rid: RID = glyph["font_rid"]
		var index: int = glyph["index"]
		var character := label.text.unicode_at(int(glyph["start"]))
		if not rid.is_valid() or index == 0 or not server.font_has_char(rid, character):
			failures.append(str(label.name) + " missing shaped glyph U+%04X" % character)
			continue
		if character != 32:
			_raster(server, rid, Vector2i(size, 0), index, pen, atlas, failures)
		pen.x += float(glyph["advance"]) * int(glyph["repeat"])
	if pen.x > atlas.get_width(): failures.append("glyph evidence exceeds its width")

static func _raster(server: TextServer, rid: RID, size: Vector2i, index: int,
		pen: Vector2, atlas: Image, failures: Array[String]) -> void:
	server.font_render_glyph(rid, size, index)
	var texture := server.font_get_glyph_texture_idx(rid, size, index)
	var image := server.font_get_texture_image(rid, size, texture)
	var uv := Rect2i(server.font_get_glyph_uv_rect(rid, size, index))
	if image == null or uv.size.x <= 0 or uv.size.y <= 0:
		failures.append("shaped symbol has no rasterized glyph")
		return
	image = image.duplicate()
	image.convert(Image.FORMAT_RGBA8)
	var offset := server.font_get_glyph_offset(rid, size, index)
	atlas.blend_rect(image, uv, Vector2i(pen + offset))
