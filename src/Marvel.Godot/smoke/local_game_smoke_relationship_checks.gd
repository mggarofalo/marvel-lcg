extends "res://smoke/local_game_smoke_card_checks.gd"

const RELATIONSHIP_IDENTITY := 1


func _relationship_path_tracks_table_scrolling(card: Control) -> bool:
	var overlay := main.get_node_or_null("RelationshipOverlay") as Control
	var table := _node("Play/Board/TableScroll") as ScrollContainer
	var identity_action := _attached(_attached_name(RELATIONSHIP_IDENTITY, "Action"))
	var target := _card_for(identity_action) if identity_action != null else null
	if overlay == null or table == null or target == null:
		_fail("the selected relationship has no overlay or table target")
		return false
	var original_scroll := table.scroll_vertical
	var original_follow_focus := table.follow_focus
	table.follow_focus = false
	render_viewport.gui_release_focus()
	for frame in 5:
		await process_frame
	var original_target := target.get_global_rect().get_center()
	if not await _wait_for(func() -> bool:
			return _relationship_endpoint_for(overlay, card) != Vector2.INF):
		_fail("the selected Web-Shooter relationship has no visible path before scrolling")
		return false
	var scroll_limit := int(table.get_v_scroll_bar().max_value - table.get_v_scroll_bar().page)
	if scroll_limit < original_scroll + 50:
		table.follow_focus = original_follow_focus
		return true
	table.scroll_vertical = original_scroll + 50
	if not await _wait_for(func() -> bool:
			return target.get_global_rect().get_center().distance_to(
					original_target - Vector2(0.0, 50.0)) < 1.0):
		_fail("the linked table target did not move by the exact scroll offset")
		return false
	if not await _wait_for(func() -> bool:
			return _relationship_endpoint_for(overlay, target) != Vector2.INF \
				or not table.get_global_rect().has_point(target.get_global_rect().get_center())):
		_fail("table scrolling neither moved nor clipped the relationship endpoint")
		return false
	table.scroll_vertical = scroll_limit
	if not await _wait_for(func() -> bool:
			return not table.get_global_rect().has_point(target.get_global_rect().get_center())):
		_fail("the relationship probe did not clip its linked table target")
		return false
	if not await _wait_for(func() -> bool:
			return _relationship_endpoint_for(overlay, target) == Vector2.INF):
		_fail("a clipped relationship endpoint remained drawn")
		return false
	table.scroll_vertical = original_scroll
	table.follow_focus = original_follow_focus
	await process_frame
	await process_frame
	return true


func _relationship_line_at(overlay: Control, point: Vector2) -> Line2D:
	for child in overlay.get_children():
		if child is Line2D and _line_has_endpoint(child as Line2D, overlay, point):
			return child
	return null


func _relationship_endpoint_for(overlay: Control, card: Control) -> Vector2:
	var rect := card.get_global_rect()
	for child in overlay.get_children():
		if not child is Line2D:
			continue
		var line := child as Line2D
		for endpoint in [line.points[0], line.points[line.points.size() - 1]]:
			var point: Vector2 = endpoint + overlay.get_global_rect().position
			if rect.grow(1.0).has_point(point) and not rect.grow(-1.0).has_point(point):
				return point
	return Vector2.INF
func _line_has_endpoint(line: Line2D, overlay: Control, point: Vector2) -> bool:
	var local := point - overlay.get_global_rect().position
	return line.points.size() > 0 and (line.points[0].distance_to(local) < 1.0 \
			or line.points[line.points.size() - 1].distance_to(local) < 1.0)


func _other_endpoint(line: Line2D, overlay: Control, source: Vector2) -> Vector2:
	var overlay_origin := overlay.get_global_rect().position
	var first := line.points[0] + overlay_origin
	var last := line.points[line.points.size() - 1] + overlay_origin
	return last if first.distance_to(source) < 1.0 else first


func _attached(name: String) -> Button:
	for candidate in main.find_children(name, "Button", true, false):
		var control := candidate as Button
		if control != null and control.is_visible_in_tree() \
				and control.get_parent() != null and control.get_parent().name == "DirectControls":
			return control
	return null


func _attached_name(anchor: int, intent: String) -> String:
	return "Card%d%s" % [anchor, intent]


func _card_for(control: Control) -> Control:
	var candidate: Node = control
	while candidate != null:
		if candidate is PanelContainer:
			return candidate as Control
		candidate = candidate.get_parent()
	return null
