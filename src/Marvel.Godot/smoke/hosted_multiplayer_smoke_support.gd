extends Node

var host: Control
var guest: Control
var host_viewport: SubViewport
var guest_viewport: SubViewport
var failed := false
var finishing := false


func _new_client_viewport() -> SubViewport:
	var viewport := SubViewport.new()
	viewport.size = Vector2i(1280, 720)
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	get_tree().root.add_child(viewport)
	return viewport


func _scroll_control_into_view(control: Control) -> void:
	var scrolls: Array[ScrollContainer] = []
	var ancestor := control.get_parent()
	while ancestor != null:
		if ancestor is ScrollContainer:
			scrolls.append(ancestor)
		ancestor = ancestor.get_parent()
	# The root scroll container follows real keyboard focus. This is the same
	# viewport route a player takes to a prompt action, and lets nested layouts
	# update their visible rect before the pointer proof samples it.
	control.grab_focus()
	for attempt in 12:
		for scroll in scrolls:
			scroll.ensure_control_visible(control)
		await get_tree().process_frame
		if _visible_control_rect(control).size.x >= 4.0 \
				and _visible_control_rect(control).size.y >= 4.0:
			await get_tree().process_frame
			return


func _visible_control_rect(control: Control) -> Rect2:
	var viewport := control.get_viewport()
	var visible_rect := control.get_global_rect().intersection(Rect2(Vector2.ZERO, Vector2(viewport.size)))
	var ancestor := control.get_parent()
	while ancestor != null:
		if ancestor is ScrollContainer or (ancestor is Control and ancestor.clip_contents):
			visible_rect = visible_rect.intersection(ancestor.get_global_rect())
		ancestor = ancestor.get_parent()
	return visible_rect


func _control_owns_point(control: Control, point: Vector2) -> bool:
	if not _visible_control_rect(control).has_point(point):
		return false
	var viewport := control.get_viewport()
	var input_point := _embedder_point(viewport, point)
	var move := InputEventMouseMotion.new()
	move.position = input_point
	move.global_position = input_point
	viewport.push_input(move)
	await get_tree().process_frame
	return _owns_pointer_target(control, viewport.gui_get_hovered_control())


func _control_has_real_hit_area(control: Control) -> bool:
	var rect := _visible_control_rect(control)
	if rect.size.x < 4.0 or rect.size.y < 4.0:
		_fail("hosted control '%s' has no unclipped hit area" % control.name)
		return false
	var inset := minf(2.0, minf(rect.size.x, rect.size.y) / 4.0)
	var points := [
		rect.get_center(),
		rect.position + Vector2(inset, inset),
		Vector2(rect.end.x - inset, rect.position.y + inset),
		Vector2(rect.position.x + inset, rect.end.y - inset),
		rect.end - Vector2(inset, inset),
	]
	for point in points:
		if not await _control_owns_point(control, point):
			_fail("hosted control '%s' loses a center or interior-edge hit" % control.name)
			return false
	return true


func _pointer_activate(control: Control) -> bool:
	if not _is_client_control(control):
		return false
	await _scroll_control_into_view(control)
	if not await _control_has_real_hit_area(control):
		return false
	var viewport := control.get_viewport()
	var point := _visible_control_rect(control).get_center()
	var input_point := _embedder_point(viewport, point)
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = input_point
	press.global_position = input_point
	viewport.push_input(press)
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.position = input_point
	release.global_position = input_point
	viewport.push_input(release)
	await get_tree().process_frame
	return true


func _embedder_point(viewport: Viewport, local_point: Vector2) -> Vector2:
	return viewport.get_final_transform() * local_point


func _is_client_control(control: Control) -> bool:
	if host != null and host.is_ancestor_of(control):
		return true
	if guest != null and guest.is_ancestor_of(control):
		return true
	_fail("a hosted pointer control does not belong to either client")
	return false


func _owns_pointer_target(control: Control, hovered: Control) -> bool:
	if hovered == control or (hovered != null and control.is_ancestor_of(hovered)):
		return true
	if hovered == null or not hovered.is_ancestor_of(control):
		return false
	var current: Control = control
	while current != hovered:
		if current.mouse_filter == Control.MOUSE_FILTER_STOP:
			return false
		current = current.get_parent() as Control
	return hovered.mouse_filter == Control.MOUSE_FILTER_PASS


func _fail(message: String) -> void:
	if failed:
		return
	failed = true
	push_error(message)
	_finish(1)


func _finish(exit_code: int) -> void:
	if finishing:
		return
	finishing = true
	if is_instance_valid(host_viewport):
		host_viewport.queue_free()
	if is_instance_valid(guest_viewport):
		guest_viewport.queue_free()
	_quit_after_client_cleanup.call_deferred(exit_code)


func _quit_after_client_cleanup(exit_code: int) -> void:
	# SubViewports own rendering resources outside this driver's scene. Give
	# queued client trees a frame to release those resources before SceneTree
	# shutdown so successful native runs finish without false leak diagnostics.
	await get_tree().process_frame
	get_tree().quit(exit_code)
