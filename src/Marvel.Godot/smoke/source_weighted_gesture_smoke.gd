extends "res://smoke/source_tableau_smoke.gd"

# Synthetic sixteen-source fixture; presentation identity and native frame geometry, not legality.
func _check_intermediate_swap(tableau: Control, source: Control, destination: Control, failures: Array[String]) -> void:
	var grip := source.find_child("SourceReorderGrip", true, false) as Button
	var displaced := destination.find_child("ProceduralCard*", true, false) as Control
	var target_bounds := destination.get_global_rect()
	var original_slot := source.get_parent() as Control
	var original_bounds := original_slot.get_global_rect()
	var finish := target_bounds.get_center()
	await _pointer(grip.get_global_rect().position + Vector2(16, 16), true)
	await _motion(finish)
	if not original_slot.get_global_rect().is_equal_approx(target_bounds):
		failures.append("source slot geometry did not immediately reach the exact destination")
	if not destination.get_global_rect().is_equal_approx(original_bounds):
		failures.append("displaced slot geometry did not immediately reach the exact origin")
	if displaced != null: _check_neighbor_frame(displaced, failures)
	if source.mouse_default_cursor_shape != Control.CURSOR_CAN_DROP:
		failures.append("valid source slot does not expose its legal drop cursor")
	await _capture_neighbor("-neighbor-moving")
	if displaced != null: await _wait_for_source_slot(displaced, failures)
	if displaced != null and not displaced.position.is_equal_approx(Vector2.ZERO):
		failures.append("displaced source did not settle exactly inside its destination slot")
	await _motion(Vector2(8, 8))
	if source.mouse_default_cursor_shape != Control.CURSOR_FORBIDDEN:
		failures.append("off-grid source does not expose a forbidden destination")
	await _source_key(KEY_ESCAPE)
	await _pointer(Vector2(8, 8), false)
	await _wait_for_source_slot(source, failures)
	if grip.mouse_default_cursor_shape != Control.CURSOR_MOVE:
		failures.append("source cancellation did not restore its pickup cursor")
	await super._check_intermediate_swap(tableau, source, destination, failures)

func _check_neighbor_frame(displaced: Control, failures: Array[String]) -> void:
	if OS.get_environment("MARVEL_SMOKE_MOTION") == "disabled":
		if not displaced.position.is_equal_approx(Vector2.ZERO):
			failures.append("reduced-motion displaced source did not settle immediately")
	elif displaced.position.is_equal_approx(Vector2.ZERO):
		failures.append("source swap popped the displaced visual without intermediate movement")

func _capture_neighbor(suffix: String) -> void:
	var path := OS.get_environment("MARVEL_TABLEAU_CAPTURE")
	if path.is_empty(): return
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png(path.trim_suffix(".png") + suffix + ".png")
