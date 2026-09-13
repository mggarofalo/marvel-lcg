extends SceneTree

const TIMEOUT_MILLISECONDS := 15000
const MAX_DECISIONS := 20

var main: Control
var failed := false
var motion_enabled := true
var render_viewport: Viewport


func _focused_control_is_visible(control: Control) -> bool:
	var visible_rect := _visible_control_rect(control)
	var expected := 44.0
	return visible_rect.size.x >= expected and visible_rect.size.y >= expected


func _control_text_is_visible(control: Control) -> bool:
	var visible_rect := _visible_control_rect(control)
	return visible_rect.size.x >= minf(100.0, control.size.x) \
		and visible_rect.size.y >= control.size.y - 1.0


func _visible_control_rect(control: Control) -> Rect2:
	var visible_rect := control.get_global_rect().intersection(Rect2(Vector2.ZERO, _viewport_size()))
	var ancestor := control.get_parent()
	while ancestor != null:
		if ancestor is ScrollContainer:
			visible_rect = visible_rect.intersection(ancestor.get_global_rect())
		ancestor = ancestor.get_parent()
	return visible_rect


func _viewport_size() -> Vector2:
	return Vector2(render_viewport.size)


func _focused_board_area_is_visible() -> bool:
	await process_frame
	await process_frame
	var saw_focused_card := false
	var board := _node("Play/Board") as Control
	if board == null:
		_fail("a focused board card has no board viewport")
		return false
	var board_rect: Rect2 = board.get_global_rect()
	for card in main.find_children("ProceduralCard", "PanelContainer", true, false):
		if card.theme_type_variation != &"FocusedCard":
			continue
		saw_focused_card = true
		if not _focused_card_is_visible(card, board, board_rect):
			return false
	if not saw_focused_card:
		_fail("keyboard selection did not highlight its board anchor")
		return false
	return true


func _focused_card_is_visible(card: Control, board: Control, board_rect: Rect2) -> bool:
	var area := _focused_card_area(card)
	if area == null:
		_fail("a focused board card is not contained by the board viewport")
		return false
	if not board_rect.grow(1.0).encloses(area.get_global_rect()):
		_fail("keyboard highlighting clipped the focused board area's heading")
		return false
	var disclosure := area.find_child("Area*Disclosure", true, false) as Control
	var heading := disclosure if disclosure != null else area.find_child("Heading", true, false)
	if heading == null:
		_fail("a focused card has no enclosing area or hand heading")
		return false
	var heading_rect: Rect2 = heading.get_global_rect()
	var card_rect: Rect2 = card.get_global_rect()
	if not board_rect.grow(-1.0).encloses(heading_rect):
		_fail("focused-card alignment clipped its area heading: heading=%s board=%s" % [
			heading_rect,
			board_rect,
		])
		return false
	if not board_rect.grow(-1.0).encloses(card_rect):
		_fail("focused-card alignment clipped its card: card=%s board=%s" % [card_rect, board_rect])
		return false
	return _focused_card_title_is_visible(card, board, board_rect)


func _focused_card_area(card: Control) -> PanelContainer:
	var area := card.get_parent()
	while area != null:
		if area is PanelContainer:
			if area.name.begins_with("Area") or area.name == "HandShelf":
				return area
		area = area.get_parent()
	return null


func _focused_card_title_is_visible(card: Control, board: Control, board_rect: Rect2) -> bool:
	var title := card.find_child("Title", true, false) as Label
	if title == null:
		return true
	var title_rect := title.get_global_rect()
	if title_rect.position.y >= board_rect.position.y - 1.0 \
			and title_rect.end.y <= board_rect.end.y + 1.0:
		return true
	_fail("keyboard highlighting did not reveal the focused card title: title=%s board=%s" % [
		title_rect,
		board_rect,
	])
	return false


func _event_presentation_is_nonblocking() -> bool:
	var cue := _node("Play/Prompt/Margin/Stack/Workbench/History/EventCue") as Control
	var motion := _node("Toolbar/Motion") as CheckButton
	var skip := _node("Play/Prompt/Margin/Stack/Workbench/History/EventHeader/Skip") as Button
	var log := _node("Play/Prompt/Margin/Stack/Workbench/History/EventLog") as RichTextLabel
	var expected_height := 44.0
	if cue == null:
		_fail("event presentation has no cue region")
		return false
	if not _event_controls_are_reachable(motion, skip, expected_height):
		return false
	if motion.button_pressed != motion_enabled:
		_fail("the configured motion preference was not applied before the game opened")
		return false
	if not _current_action_is_available():
		_fail("event presentation blocked the current engine decision")
		return false
	if not await _skip_motion_preserves_history(skip, log):
		return false
	if not _settled_event_status_is_visible(cue):
		return false

	if not motion_enabled and not _disabled_motion_is_settled(skip):
		return false
	return true


func _event_controls_are_reachable(
		motion: CheckButton, skip: Button, expected_height: float) -> bool:
	if motion == null:
		_fail("event presentation has no motion control")
		return false
	if skip == null:
		_fail("event presentation has no skip control")
		return false
	if motion.custom_minimum_size.y < 44.0:
		_fail("motion control misses the pointer-target floor")
		return false
	if skip.custom_minimum_size.y < expected_height:
		_fail("skip control misses the pointer-target floor")
		return false
	return true


func _current_action_is_available() -> bool:
	var action := _first_enabled_choice()
	return action != null and not action.disabled


func _skip_motion_preserves_history(skip: Button, log: RichTextLabel) -> bool:
	var history := log.text
	if motion_enabled and not skip.disabled:
		skip.pressed.emit()
		await process_frame
	if not skip.disabled:
		_fail("settled event playback retained an enabled skip control")
		return false
	if log.text != history:
		_fail("skipping motion changed or cleared event history")
		return false
	return true


func _settled_event_status_is_visible(cue: Control) -> bool:
	var sync_status := _node("Toolbar/SyncStatus") as Label
	if cue.visible:
		_fail("settled motion did not collapse its cue")
		return false
	if sync_status == null or not sync_status.text.begins_with("✓ Synced"):
		_fail("settled motion did not retain the compact sync status")
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
	if checkpoint == "open-table-prompt-dense-concealed" \
			and not await _focused_board_area_is_visible():
		return false
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
	var expected := 44.0
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
	var configured := OS.get_environment("MARVEL_UI_SCALE").strip_edges().to_lower()
	match configured:
		"compact", "":
			return 80
		"standard":
			return 100
		"large":
			return 120
		"extra-large":
			return 150
	return int(configured.trim_suffix("%"))


func _scaled_metric(base: int) -> int:
	return ceili(base * _scale_percentage() / 100.0)


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
