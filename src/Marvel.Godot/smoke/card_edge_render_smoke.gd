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
	var passed := columns > 10 and covered_columns > columns / 2
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
