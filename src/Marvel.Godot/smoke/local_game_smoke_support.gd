extends "res://smoke/local_game_smoke_pointer_support.gd"

const SmokeScale = preload("res://smoke/local_game_smoke_scale.gd")
const TIMEOUT_MILLISECONDS := 15000
const MAX_DECISIONS := 80

var main: Control
var failed := false
var motion_enabled := true


func _redesign_gate_post_mulligan_is_safe() -> bool:
	return true


func _focused_control_is_visible(control: Control) -> bool:
	var visible_rect := _visible_control_rect(control)
	var expected := _scaled_metric(44)
	return visible_rect.size.x >= expected and visible_rect.size.y >= expected


func _control_text_is_visible(control: Control) -> bool:
	var visible_rect := _visible_control_rect(control)
	return visible_rect.size.x >= minf(100.0, control.size.x) \
		and visible_rect.size.y >= control.size.y - 1.0


func _control_has_real_hit_area(control: Control) -> bool:
	for _attempt in CONTROL_HIT_AREA_ATTEMPTS:
		var rect := _visible_control_rect(control)
		var global_rect := control.get_global_rect()
		if not _control_is_fully_visible(control) or rect.size.x < 4.0 or rect.size.y < 4.0:
			# A decision rebuild can change a scroll range after the caller's first
			# reveal. Reapply that same reveal before resnapshotting the geometry.
			await _scroll_control_into_view(control)
			continue
		var inset := minf(2.0, minf(rect.size.x, rect.size.y) / 4.0)
		var points := [
			rect.get_center(),
			rect.position + Vector2(inset, inset),
			Vector2(rect.end.x - inset, rect.position.y + inset),
			Vector2(rect.position.x + inset, rect.end.y - inset),
			rect.end - Vector2(inset, inset),
		]
		var proof_is_stable := true
		for point in points:
			if not control.get_global_rect().is_equal_approx(global_rect) \
					or not _visible_control_rect(control).is_equal_approx(rect) \
					or not await _control_owns_point(control, point):
				proof_is_stable = false
				break
		if proof_is_stable and control.get_global_rect().is_equal_approx(global_rect) \
				and _visible_control_rect(control).is_equal_approx(rect):
			return true
		await process_frame
	_fail("control '%s' has no stable unclipped and unobscured hit area: visible %s of %s" % [
		control.name,
		_visible_control_rect(control),
		control.get_global_rect(),
	])
	return false


func _scroll_control_into_view(control: Control) -> void:
	var scrolls: Array[ScrollContainer] = []
	var ancestor := control.get_parent()
	while ancestor != null:
		if ancestor is ScrollContainer:
			scrolls.append(ancestor)
		ancestor = ancestor.get_parent()
	for scroll in scrolls:
		if scroll.horizontal_scroll_mode != ScrollContainer.SCROLL_MODE_DISABLED \
				or scroll.vertical_scroll_mode != ScrollContainer.SCROLL_MODE_DISABLED:
			scroll.ensure_control_visible(control)
		await process_frame
	var page := main.get_node_or_null("Margin") as ScrollContainer
	var status := main.get_node_or_null("StatusBar") as Control
	if page != null and page.vertical_scroll_mode != ScrollContainer.SCROLL_MODE_DISABLED:
		var rect := control.get_global_rect()
		var page_rect := page.get_global_rect()
		var safe_top := maxf(page_rect.position.y, status.get_global_rect().end.y + 4.0 \
			if status != null else page_rect.position.y)
		var safe_bottom := page_rect.end.y - 20.0
		if rect.position.y < safe_top:
			page.scroll_vertical = maxi(
				0, page.scroll_vertical - ceili(safe_top - rect.position.y))
		elif rect.end.y > safe_bottom:
			page.scroll_vertical += ceili(rect.end.y - safe_bottom)
		await process_frame
	await process_frame


func _prepare_activation(control: Control) -> bool:
	# Setup actions can begin below the page fold. Move the real scroll viewport
	# first, then prove that the control owns an unclipped input area.
	await _scroll_control_into_view(control)
	return await _control_has_real_hit_area(control)


func _pointer_activate(control: Control) -> bool:
	if not await _prepare_activation(control):
		return false
	if not _pointer_activate_without_settle(control):
		return false
	await process_frame
	return true


func _pointer_activate_without_settle(control: Control) -> bool:
	var point := _visible_control_rect(control).get_center()
	if not control is BaseButton:
		_inject_pointer_click(point)
		return true
	var button := control as BaseButton
	for _attempt in POINTER_ACTIVATION_ATTEMPTS:
		var observed := [false]
		var observe := func() -> void: observed[0] = true
		button.pressed.connect(observe)
		_inject_pointer_click(point)
		if button.pressed.is_connected(observe):
			button.pressed.disconnect(observe)
		if observed[0]:
			return true
	return false


func _inject_pointer_click(point: Vector2) -> void:
	_position_pointer_without_settle(point)
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


func _keyboard_activate(control: Control, repeats := 1) -> bool:
	control.grab_focus()
	await process_frame
	if render_viewport.gui_get_focus_owner() != control:
		_fail("keyboard activation could not focus '%s'" % control.name)
		return false
	_accept_repeats_without_settle(repeats)
	await process_frame
	return true


func _keyboard_activate_without_settle(control: Control, repeats := 1) -> bool:
	control.grab_focus()
	if render_viewport.gui_get_focus_owner() != control:
		_fail("keyboard activation could not focus '%s'" % control.name)
		return false
	_accept_repeats_without_settle(repeats)
	return true


func _accept_repeats_without_settle(repeats := 1) -> void:
	for index in repeats:
		var press := InputEventKey.new()
		press.keycode = KEY_ENTER
		press.pressed = true
		render_viewport.push_input(press)
		var release := InputEventKey.new()
		release.keycode = KEY_ENTER
		render_viewport.push_input(release)


func _standard_board_area_is_visible() -> bool:
	if not await _wait_for(func() -> bool: return not _focused_board_cards().is_empty()):
		_fail("keyboard selection did not highlight its board anchor")
		return false
	for card in _focused_board_cards():
		var area := card.get_parent()
		while area != null and not (area is PanelContainer and area.name.begins_with("Area")):
			area = area.get_parent()
		var board := _node("Play/Board/TableScroll") as ScrollContainer
		if area == null or board == null:
			_fail("a focused board card is not contained by the board viewport")
			return false
		var area_rect: Rect2 = area.get_global_rect()
		var board_rect: Rect2 = board.get_global_rect()
		if area_rect.position.x < board_rect.position.x - 1.0 \
				or area_rect.end.x > board_rect.end.x + 1.0:
			_fail("keyboard highlighting clipped the focused board area's heading")
			return false
		var disclosure := area.find_child("Area*Disclosure", true, false) as Control
		var card_rect: Rect2 = card.get_global_rect()
		if disclosure == null:
			_fail("a focused board card has no enclosing area disclosure")
			return false
		var disclosure_rect: Rect2 = disclosure.get_global_rect()
		if disclosure_rect.position.y < board_rect.position.y + 8.0 \
				or disclosure_rect.end.y > board_rect.end.y - 1.0 \
				or card_rect.position.y < board_rect.position.y + 8.0 \
				or card_rect.end.y > board_rect.end.y - 1.0:
			_fail("focused-card alignment clipped its area disclosure or full frame")
			return false
		if not _focused_card_title_is_visible(card, board, board_rect):
			return false
	return true


func _tabletop_card_named(title: String) -> Control:
	return BOARD_HELPERS.tabletop_card_named(main, title)


func _focused_board_cards() -> Array[Control]:
	return BOARD_HELPERS.focused_cards(main)


func _focused_card_title_is_visible(card: Control, board: ScrollContainer, board_rect: Rect2) -> bool:
	var title := card.find_child("Title", true, false) as Label
	if title == null:
		return true
	var title_rect := title.get_global_rect()
	if title_rect.position.y >= board_rect.position.y - 1.0 \
			and title_rect.end.y <= board_rect.end.y + 1.0:
		return true
	_fail("keyboard highlighting did not reveal the focused card title: title=%s board=%s scroll=%d/%d" % [
		title_rect,
		board_rect,
		board.scroll_vertical,
		board.get_v_scroll_bar().max_value,
	])
	return false


func _event_presentation_is_nonblocking() -> bool:
	var cue := _node("Play/Prompt/Margin/Stack/Workbench/History/EventCue") as Control
	var motion := _node("Toolbar/Motion") as CheckButton
	var skip := _node("Play/Prompt/Margin/Stack/Workbench/History/EventHeader/Skip") as Button
	var log := _node("Play/Prompt/Margin/Stack/Workbench/History/EventLog") as RichTextLabel
	var expected_height := _scaled_metric(44)
	if cue == null:
		_fail("event presentation has no cue region")
		return false
	if motion == null or skip == null \
			or motion.custom_minimum_size.y < 44.0 \
			or skip.custom_minimum_size.y < expected_height:
		_fail("event presentation controls miss the pointer-target floor")
		return false
	if motion.button_pressed != motion_enabled:
		_fail("the configured motion preference was not applied before the game opened")
		return false
	var action := _first_enabled_choice()
	if action == null or action.disabled:
		_fail("event presentation blocked the current engine decision")
		return false

	var history := log.text
	if motion_enabled and not skip.disabled:
		if not await _pointer_activate(skip):
			return false
	if not skip.disabled or log.text != history:
		_fail("skipping motion changed or cleared event history")
		return false
	var sync_status := _node("Toolbar/SyncStatus") as Label
	if cue.visible or sync_status == null or not sync_status.text.begins_with("✓ Synced"):
		_fail("settled motion did not collapse its cue or retain the compact sync status")
		return false

	if not motion_enabled and not _disabled_motion_is_settled(skip):
		return false
	return true


func _disabled_motion_is_settled(skip: Button) -> bool:
	if not skip.disabled:
		_fail("motion-disabled presentation left playback active")
		return false
	var cue := _node("Play/Prompt/Margin/Stack/Workbench/History/EventCue") as Control
	var sync_status := _node("Toolbar/SyncStatus") as Label
	if cue.visible or sync_status == null or not sync_status.text.begins_with("✓ Synced"):
		_fail("motion-disabled presentation did not retain the compact sync status")
		return false
	return true


func _capture_checkpoint(checkpoint: String) -> bool:
	var capture_dir := OS.get_environment("MARVEL_SMOKE_CAPTURE_DIR")
	if capture_dir.is_empty():
		return true
	await process_frame
	await process_frame
	var image := render_viewport.get_texture().get_image()
	if image == null or image.is_empty():
		_fail("visual checkpoint '%s' needs a non-headless rendering driver" % checkpoint)
		return false
	var requested_viewport := OS.get_environment("MARVEL_SMOKE_VIEWPORT").split("x")
	if requested_viewport.size() == 2 \
			and image.get_size() != Vector2i(
				int(requested_viewport[0]), int(requested_viewport[1])):
		_fail("visual checkpoint '%s' has size %s instead of %s" % [
			checkpoint,
			image.get_size(),
			OS.get_environment("MARVEL_SMOKE_VIEWPORT"),
		])
		return false

	if not _checkpoint_image_is_rendered(checkpoint, image):
		return false
	return _save_checkpoint_image(checkpoint, capture_dir, image)


func _checkpoint_image_is_rendered(checkpoint: String, image: Image) -> bool:
	var colors: Dictionary = {}
	var sample := image.duplicate()
	sample.resize(32, 18, Image.INTERPOLATE_NEAREST)
	for x_step in sample.get_width():
		for y_step in sample.get_height():
			var pixel: Color = sample.get_pixel(x_step, y_step)
			colors[pixel.to_html()] = true
	if colors.size() >= 6:
		return true
	_fail("visual checkpoint '%s' is blank or materially unrendered" % checkpoint)
	return false


func _save_checkpoint_image(checkpoint: String, capture_dir: String, image: Image) -> bool:
	var absolute_dir := ProjectSettings.globalize_path(capture_dir)
	var error := DirAccess.make_dir_recursive_absolute(absolute_dir)
	if error != OK:
		_fail("visual checkpoint directory could not be created: %s" % absolute_dir)
		return false
	var viewport := OS.get_environment("MARVEL_SMOKE_VIEWPORT")
	var scale := OS.get_environment("MARVEL_UI_SCALE")
	var motion := "motion" if motion_enabled else "reduced-motion"
	var path := absolute_dir.path_join("%s-%s-%s-%s.png" % [
		viewport,
		scale,
		motion,
		checkpoint,
	])
	if image.save_png(path) != OK:
		_fail("visual checkpoint could not be saved: %s" % path)
		return false
	return true


func _first_enabled_choice() -> Button:
	for button in _visible_buttons(_decision()):
		if not button.disabled and button.name != "Submit" \
				and button.text not in ["Pass / decline", "+", "−"]:
			return button
	return null


func _first_enabled_target() -> Button:
	for button in _visible_buttons(_decision()):
		if not button.disabled and button.text.begins_with("◇"):
			return button
	return null


func _submit_button() -> Button:
	var submit := _decision().find_child("Submit", true, false) as Button
	return submit if submit != null and submit.is_visible_in_tree() else null


func _visible_buttons_meet_pointer_floor() -> bool:
	var expected: float = 44.0 if OS.get_environment("MARVEL_SMOKE_VIEWPORT") == "1920x1080" \
		else _scaled_metric(44)
	for button in _visible_buttons(_decision()):
		if button.size.x < expected or button.size.y < expected:
			_fail("visible decision control '%s' misses the pointer-target floor" % button.text)
			return false
	return true


func _select_named_option(option: OptionButton, wanted: String) -> void:
	for index in option.item_count:
		if option.get_item_text(index).begins_with(wanted):
			option.select(index)
			option.item_selected.emit(index)
			return
	_fail("visible option '%s' is unavailable" % wanted)


func _scale_percentage() -> int:
	return SmokeScale.percentage(OS.get_environment("MARVEL_UI_SCALE"))


func _scaled_metric(base: int) -> int:
	return SmokeScale.metric(base, OS.get_environment("MARVEL_UI_SCALE"))


func _button_named(wanted: String) -> Button:
	return _visible_button(main, wanted)


func _visible_button(node: Node, wanted: String) -> Button:
	for button in _visible_buttons(node):
		if button.text == wanted:
			return button
	return null


func _visible_button_beginning(node: Node, wanted: String) -> Button:
	for button in _visible_buttons(node):
		if button.text.begins_with(wanted):
			return button
	return null


func _visible_buttons(node: Node) -> Array[Button]:
	var found: Array[Button] = []
	for child in node.get_children():
		if child is Button and child.is_visible_in_tree():
			found.append(child)
		found.append_array(_visible_buttons(child))
	return found


func _visible_text(node: Node) -> String:
	var text := ""
	for child in node.get_children():
		if child is Label and child.is_visible_in_tree():
			text += child.text + "\n"
		elif child is Button and child.is_visible_in_tree():
			text += child.text + "\n"
		text += _visible_text(child)
	return text


func _wait_for(condition: Callable) -> bool:
	var started := Time.get_ticks_msec()
	while Time.get_ticks_msec() - started < TIMEOUT_MILLISECONDS:
		if condition.call():
			return true
		await process_frame
	return false


func _is_complete() -> bool:
	return _status().text.begins_with("GAME COMPLETE")


func _node(relative: String) -> Node:
	if relative.begins_with("Toolbar/"):
		return main.get_node("StatusBar/" + relative.trim_prefix("Toolbar/"))
	return main.get_node("Margin/Shell/Content/" + relative)


func _play() -> Control:
	return _node("Play") as Control


func _decision() -> Control:
	return _node("Play/Prompt/Margin/Stack/Workbench/Action/Decision") as Control


func _status() -> Label:
	return _node("Status/Text") as Label


func _fail(message: String) -> void:
	if failed:
		return
	failed = true
	push_error(message + "\nVisible UI:\n" + (_visible_text(main) if main != null else "<none>"))
	quit(1)
