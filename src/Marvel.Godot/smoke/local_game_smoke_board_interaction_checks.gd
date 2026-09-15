extends "res://smoke/local_game_smoke_support.gd"


func _focused_board_area_is_visible() -> bool:
	if main.find_child("VillainTable", true, false) != null:
		return await _tabletop_board_area_is_visible()
	return await _standard_board_area_is_visible()


func _tabletop_board_area_is_visible() -> bool:
	var villain := main.find_child("VillainTable", true, false) as Control
	var player := main.find_child("PlayerTable", true, false) as Control
	var board := _node("Play/Board/TableScroll") as ScrollContainer
	var scheme := _tabletop_card_named("The Break-In!")
	var standard_scale := int(OS.get_environment("MARVEL_UI_SCALE")) <= 100
	var board_scroll_is_bounded := board != null \
		and board.vertical_scroll_mode == ScrollContainer.SCROLL_MODE_DISABLED
	if villain == null or player == null or board == null or scheme == null \
			or not board_scroll_is_bounded \
			or standard_scale and (not _control_is_fully_visible(villain) \
				or not _control_is_fully_visible(player)):
		_fail("the tabletop did not keep its far and near board areas visibly focusable")
		return false
	await _scroll_control_into_view(scheme)
	scheme.grab_focus()
	await process_frame
	if render_viewport.gui_get_focus_owner() != scheme \
			or not await _control_owns_point(scheme, _card_body_point(scheme)):
		_fail("the tabletop main scheme cannot receive visible body focus")
		return false
	if not await _pointer_activate_card_body(scheme):
		return false
	var inspector := main.get_node("CardInspector") as Control
	if inspector == null or not inspector.visible \
			or inspector.find_child("CardFace", true, false) == null:
		_fail("clicking the tabletop main scheme did not inspect its current card")
		return false
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	render_viewport.push_input(escape)
	await process_frame
	return not inspector.visible


func _align_attached_control_to_table(control: Control) -> bool:
	await _scroll_control_into_view(control)
	var table := _node("Play/Board/TableScroll") as ScrollContainer
	if table == null:
		_fail("the table viewport is unavailable for an attached control")
		return false
	var status := main.get_node("StatusBar") as Control
	var page := main.get_node("Margin") as ScrollContainer
	var rect := control.get_global_rect()
	if status != null and page != null and rect.position.y < status.get_global_rect().end.y:
		page.scroll_vertical = maxi(
			0,
			page.scroll_vertical - ceili(status.get_global_rect().end.y - rect.position.y + 4.0))
		await process_frame
		await process_frame
	return true


func _pointer_activate_card_body(card: Control) -> bool:
	await _scroll_control_into_view(card)
	var point := _card_body_point(card)
	if not await _control_owns_point(card, point):
		_fail("card '%s' has no inspection-only body hit area" % card.name)
		return false
	if render_viewport.gui_get_hovered_control() is BaseButton:
		_fail("card '%s' body probe landed on an attached control" % card.name)
		return false
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = point
	press.global_position = point
	render_viewport.push_input(press, true)
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.position = point
	release.global_position = point
	render_viewport.push_input(release, true)
	await process_frame
	return true


func _card_body_point(card: Control) -> Vector2:
	var rect := _visible_control_rect(card)
	return Vector2(
		rect.position.x + minf(24.0, rect.size.x * 0.2),
		rect.position.y + minf(24.0, rect.size.y * 0.15))
