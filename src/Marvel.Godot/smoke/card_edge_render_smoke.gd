extends SceneTree

func _initialize() -> void:
	_run.call_deferred()

func _run() -> void:
	root.size = Vector2i(1920, 1080)
	var main := (load("res://Main.tscn") as PackedScene).instantiate()
	root.add_child(main)
	for frame in range(8): await process_frame
	# Isolate the actual card plane so text/art cannot masquerade as edge coverage.
	var source := main.find_child("InkPlane", true, false) as Polygon2D
	if source == null:
		push_error("CARD_EDGE_RENDER_FAILED missing card plane")
		quit(1)
		return
	var plane := source.duplicate() as Polygon2D
	main.hide()
	var paper := ColorRect.new()
	paper.color = Color.WHITE
	paper.size = Vector2(1920, 1080)
	root.add_child(paper)
	root.add_child(plane)
	plane.position = Vector2(80, 80)
	await process_frame
	await RenderingServer.frame_post_draw
	var rendered := root.get_texture().get_image()
	var counts := _plane_coverage(rendered, plane)
	var covered_columns := counts.x
	var columns := counts.y
	var passed := columns > 10 and covered_columns > columns / 2
	var card := source.get_parent()
	while card != null and not card is PanelContainer: card = card.get_parent()
	if card == null:
		push_error("CARD_EDGE_RENDER_FAILED missing actual card shell")
		quit(1)
		return
	card.reparent(root)
	for child in card.get_children():
		if child is CanvasItem: child.hide()
	card.position = Vector2(580, 180)
	card.rotation = 0.19
	card.show()
	var connector := (load("res://SmoothRelationshipLine.cs") as Script).new() as Line2D
	connector.points = PackedVector2Array([Vector2(850, 100), Vector2(1100, 157)])
	connector.width = 3
	connector.default_color = Color("132532")
	connector.antialiased = true
	root.add_child(connector)
	await process_frame
	await RenderingServer.frame_post_draw
	rendered = root.get_texture().get_image()
	var pose: Transform2D = card.get_global_transform()
	var frame_coverage := _coverage(rendered, pose * Vector2(15, 0), pose * Vector2(card.size.x - 15, 0), Color("132532"))
	var line_coverage := _coverage(rendered, Vector2(860, 100.74), Vector2(1090, 153.18), connector.default_color)
	passed = passed and frame_coverage > 0.5 and line_coverage > 0.5
	print("ROTATED_FRAME_COVERAGE ", frame_coverage, " CONNECTOR_COVERAGE ", line_coverage)
	card.queue_free()
	connector.queue_free()
	if passed:
		print("CARD_EDGE_RENDER_OK coverage_columns=", covered_columns, "/", columns)
	else:
		push_error("CARD_EDGE_RENDER_FAILED hard staircase edge: %s/%s" % [covered_columns, columns])
	var capture := OS.get_environment("MARVEL_SMOKE_CAPTURE_DIR")
	if not capture.is_empty(): rendered.save_png(capture.path_join("card-edge-coverage.png"))
	plane.queue_free()
	paper.queue_free()
	main.queue_free()
	await process_frame
	await process_frame
	quit(0 if passed else 1)


func _coverage(rendered: Image, left: Vector2, right: Vector2, ink: Color) -> float:
	var covered := 0
	var total := 0
	for x in range(int(left.x), int(right.x)):
		var edge_y := lerpf(left.y, right.y, (x - left.x) / (right.x - left.x))
		total += 1
		for y in range(int(edge_y) - 3, int(edge_y) + 4):
			var pixel := rendered.get_pixel(x, y)
			if pixel.r > ink.r + 0.03 and pixel.r < 0.97:
				covered += 1
				break
	return float(covered) / max(1, total)


func _plane_coverage(rendered: Image, plane: Polygon2D) -> Vector2i:
	var left := plane.polygon[3] + plane.position
	var right := plane.polygon[2] + plane.position
	var covered_columns := 0
	var columns := 0
	for x in range(int(left.x + 10), int(right.x - 10)):
		var edge_y := lerpf(left.y, right.y, (x - left.x) / (right.x - left.x))
		columns += 1
		for y in range(int(edge_y) - 2, int(edge_y) + 3):
			var pixel := rendered.get_pixel(x, y)
			if pixel.r > plane.color.r + 0.03 and pixel.r < 0.97:
				covered_columns += 1
				break
	return Vector2i(covered_columns, columns)
