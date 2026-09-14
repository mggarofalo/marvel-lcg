extends "res://smoke/local_game_smoke_input_support.gd"

const POINTER_OWNERSHIP_ATTEMPTS := 3
const POINTER_ACTIVATION_ATTEMPTS := 3

var render_viewport: Viewport


func _visible_control_rect(control: Control) -> Rect2:
	var visible_rect := control.get_global_rect().intersection(Rect2(Vector2.ZERO, _viewport_size()))
	var ancestor := control.get_parent()
	while ancestor != null:
		if ancestor is ScrollContainer or (ancestor is Control and ancestor.clip_contents):
			visible_rect = visible_rect.intersection(ancestor.get_global_rect())
		ancestor = ancestor.get_parent()
	return visible_rect


func _control_owns_point(control: Control, point: Vector2) -> bool:
	if not _visible_control_rect(control).has_point(point):
		return false
	# A disabled control intentionally does not claim pointer input. It is not an
	# operable hit target even if its painted rectangle is visible.
	if control.mouse_filter == Control.MOUSE_FILTER_IGNORE or control is BaseButton and control.disabled:
		return false
	# A native display server can deliver physical pointer motion between the
	# injected move and the next frame. Resample the same exact point; a clipped
	# or persistently occluded control still cannot satisfy this ownership check.
	for _attempt in POINTER_OWNERSHIP_ATTEMPTS:
		_position_pointer_without_settle(point)
		await process_frame
		if render_viewport.get_mouse_position().is_equal_approx(point) \
				and _hovered_control_owns(control, render_viewport.gui_get_hovered_control()):
			return true
	return false


func _position_pointer_without_settle(point: Vector2) -> void:
	# Smoke geometry is already expressed in this fixed viewport's coordinates.
	# Local input avoids applying the unrelated embedder/window transform.
	var move := InputEventMouseMotion.new()
	move.position = point
	move.global_position = point
	render_viewport.push_input(move, true)


func _hovered_control_owns(control: Control, hovered: Control) -> bool:
	if hovered == control or (hovered != null and control.is_ancestor_of(hovered)):
		return true
	# Containers using Pass may be reported as the hovered owner while delivering
	# the event to an eligible descendant. Follow that actual mouse-filter path;
	# a Stop ancestor is an occluder and must still fail this probe.
	if hovered != null and hovered.is_ancestor_of(control):
		var current: Control = control
		while current != hovered:
			if current.mouse_filter == Control.MOUSE_FILTER_STOP:
				return false
			current = current.get_parent() as Control
		return hovered.mouse_filter == Control.MOUSE_FILTER_PASS
	return false


func _pointer_ownership_probe_is_strict() -> bool:
	var target := Button.new()
	target.name = &"PointerProbeTarget"
	target.position = Vector2(24, 24)
	target.size = Vector2(120, 60)
	target.mouse_filter = Control.MOUSE_FILTER_STOP
	var blocker := Control.new()
	blocker.name = &"PointerProbeBlocker"
	blocker.position = Vector2(74, 24)
	blocker.size = Vector2(20, 60)
	blocker.mouse_filter = Control.MOUSE_FILTER_STOP
	render_viewport.add_child(target)
	render_viewport.add_child(blocker)
	await process_frame
	var rejects_covered := not await _control_owns_point(target, Vector2(84, 54))
	var accepts_exposed := await _control_owns_point(target, Vector2(44, 54))
	render_viewport.remove_child(blocker)
	render_viewport.remove_child(target)
	blocker.queue_free()
	target.queue_free()
	await process_frame
	return rejects_covered and accepts_exposed


func _viewport_size() -> Vector2:
	return Vector2(render_viewport.size)
