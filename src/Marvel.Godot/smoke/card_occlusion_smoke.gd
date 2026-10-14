extends "res://smoke/motion_finish_smoke.gd"

# Synthetic geometry probe: move an authorized visible card through causal cues.
# Game state and engine offers stay unchanged; this checks overlay pixels only.

func _run() -> void:
	await _legal_enemy_attack_is_causal()

func _declare_probe_defender() -> bool:
	var source := _tabletop_card_named("Rhino")
	var target := _tabletop_card_named("Spider-Man")
	var neighbor: Control
	for candidate in main.find_children("ProceduralCard*", "Control", true, false):
		if candidate.is_visible_in_tree() and candidate.global_position.y > 700:
			neighbor = candidate
			break
	if source == null or target == null or neighbor == null:
		_fail("occlusion probe requires source, target, and authorized neighboring card")
		return false
	var pulse := main.find_child("AttackActivityPulse", true, false) as Control
	var parent := neighbor.get_parent()
	var index := neighbor.get_index()
	var pose := neighbor.position
	var z := neighbor.z_index
	var midpoint := source.get_global_rect().get_center().lerp(target.get_global_rect().get_center(), .34)
	var connector_region := Rect2(midpoint - Vector2(16, 16), Vector2(32, 32))
	var badge_region := Rect2(source.get_global_rect().position + Vector2(4, -27), Vector2(96, 21))
	if not await _compare_region(pulse, connector_region, "unobstructed-connector", false): return false
	if not await _compare_region(pulse, badge_region, "unobstructed-badge", false): return false
	neighbor.reparent(main)
	neighbor.z_index = 1000
	neighbor.global_position = midpoint - neighbor.size / 2
	if not await _compare_region(pulse, connector_region, "neighbor-connector"): return false
	neighbor.global_position = source.get_global_rect().position + Vector2(-20, -70)
	if not await _compare_region(pulse, badge_region, "neighbor-badge"): return false
	neighbor.reparent(parent)
	parent.move_child(neighbor, index)
	neighbor.position = pose
	neighbor.z_index = z
	print("CARD_OCCLUSION_NATIVE_OK connector=true badge=true synthetic_geometry=true")
	quit(0)
	return false

func _compare_region(pulse: Control, region: Rect2, label: String, covered: bool = true) -> bool:
	await process_frame
	pulse.hide()
	await process_frame
	await RenderingServer.frame_post_draw
	var plain := render_viewport.get_texture().get_image()
	pulse.show()
	await process_frame
	await RenderingServer.frame_post_draw
	var overlay := render_viewport.get_texture().get_image()
	var changed := 0
	for x in range(int(region.position.x), int(region.end.x)):
		for y in range(int(region.position.y), int(region.end.y)):
			if plain.get_pixel(x, y) != overlay.get_pixel(x, y): changed += 1
	print("OCCLUSION_PIXELS ", label, " ", changed, " rect=", region)
	_save_checkpoint_image(label, OS.get_environment("MARVEL_SMOKE_CAPTURE_DIR"), overlay)
	if (changed > 0) == covered:
		_fail("causal overlay visibility does not respect the neighboring card: " + label)
		return false
	return true
