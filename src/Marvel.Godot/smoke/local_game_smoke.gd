extends SceneTree

const TIMEOUT_MILLISECONDS := 15000
const MAX_DECISIONS := 20

var main: Control
var failed := false


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var packed := load("res://Main.tscn") as PackedScene
	if packed == null:
		_fail("Main.tscn could not be loaded")
		return

	main = packed.instantiate() as Control
	root.add_child(main)
	if not await _wait_for(func() -> bool: return _button_named("Start game") != null):
		_fail("setup never became ready")
		return
	if not await _visual_system_is_resolved():
		return

	_select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Spider-Man")
	_select_named_option(_node("Setup/Selections/Fields/Grid/Scenario"), "Rhino")
	_select_named_option(_node("Setup/Selections/Fields/Grid/Mode"), "Standard")
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "1"
	seed.text_changed.emit(seed.text)
	await process_frame

	var start := _button_named("Start game")
	if start == null or start.disabled:
		_fail("the visible Start game control is unavailable")
		return
	start.pressed.emit()
	if not await _wait_for(func() -> bool: return _play().visible and _decision() != null):
		_fail("the opened table never became visible")
		return

	var saw_mulligan := false
	var saw_pass := false
	var saw_end_phase := false
	var decisions := 0
	while not _is_complete():
		if decisions >= MAX_DECISIONS:
			_fail("the visible-control journey exceeded %d decisions" % MAX_DECISIONS)
			return
		if not _visible_buttons_meet_pointer_floor():
			return

		var decision_text := _visible_text(_decision())
		saw_mulligan = saw_mulligan or "Mulligan" in decision_text
		saw_end_phase = saw_end_phase or "End Phase" in decision_text
		var pass_button := _visible_button(_decision(), "Pass / decline")
		if pass_button != null and not pass_button.disabled:
			saw_pass = true
			pass_button.pressed.emit()
		else:
			var submit := _visible_button(_decision(), "Submit decision")
			if submit == null or submit.disabled:
				var choice := _first_enabled_choice()
				if choice == null:
					_fail("no visible control can advance the current decision")
					return
				choice.pressed.emit()
				await process_frame
				submit = _visible_button(_decision(), "Submit decision")
			if submit == null or submit.disabled:
				_fail("the selected visible decision cannot be submitted")
				return
			submit.pressed.emit()

		decisions += 1
		await process_frame
		if not await _wait_for(func() -> bool:
			return _is_complete() or not _status().text.begins_with("DECISION SENT")):
			_fail("the engine did not reconcile decision %d" % decisions)
			return

	if not saw_mulligan or not saw_pass or not saw_end_phase:
		_fail("the journey missed a required visible decision path")
		return
	if "VILLAIN WINS" not in _status().text:
		_fail("the terminal UI did not report the seeded villain win")
		return
	if _node("Status").theme_type_variation != &"DangerStatusPanel":
		_fail("the villain win did not receive the semantic danger treatment")
		return
	var event_log := _node("Play/Prompt/Margin/Stack/EventLog") as RichTextLabel
	var event_text := event_log.get_parsed_text().strip_edges()
	if event_text.is_empty() or event_text == "No events yet.":
		_fail("the visible event log is empty")
		return

	print("LOCAL_GAME_SMOKE_OK decisions=%d" % decisions)
	quit(0)


func _visual_system_is_resolved() -> bool:
	if main.theme == null:
		_fail("the root scene has no reusable client theme")
		return false

	var start := _button_named("Start game")
	var scale := OS.get_environment("MARVEL_UI_SCALE")
	var expected_height := 66 if scale == "extra-large" else 55 if scale == "large" else 44
	var expected_body := 24 if scale == "extra-large" else 20 if scale == "large" else 16
	var expected_focus := 5 if scale == "extra-large" else 4 if scale == "large" else 3
	if start.theme_type_variation != &"PrimaryButton":
		_fail("the primary action does not use its semantic theme role")
		return false
	if start.custom_minimum_size.y < expected_height:
		_fail("the primary action is smaller than the pointer-target floor")
		return false
	if start.get_theme_font_size("font_size") < expected_body:
		_fail("the primary action did not adopt the selected type scale")
		return false

	var focus := start.get_theme_stylebox("focus") as StyleBoxFlat
	var normal := start.get_theme_stylebox("normal") as StyleBoxFlat
	var hover := start.get_theme_stylebox("hover") as StyleBoxFlat
	var disabled := start.get_theme_stylebox("disabled") as StyleBoxFlat
	if focus == null or normal == null or hover == null or disabled == null:
		_fail("the primary action is missing a required interaction style")
		return false
	if focus.border_width_left < expected_focus or focus.expand_margin_left < expected_focus:
		_fail("keyboard focus has no structural focus ring")
		return false
	if hover.border_width_bottom == normal.border_width_bottom:
		_fail("pointer hover differs from rest by color alone")
		return false
	if disabled.border_width_bottom == normal.border_width_bottom:
		_fail("unavailable actions differ from rest by color alone")
		return false

	var theme := main.theme
	var legal := theme.get_stylebox("normal", &"LegalTargetButton") as StyleBoxFlat
	var selected := theme.get_stylebox("normal", &"SelectedTargetButton") as StyleBoxFlat
	var unavailable := theme.get_stylebox("normal", &"UnavailableButton") as StyleBoxFlat
	if legal == null or selected == null or unavailable == null:
		_fail("the theme does not define every semantic action state")
		return false
	if legal.border_width_left == selected.border_width_left:
		_fail("legal and selected targets differ by color alone")
		return false
	if unavailable.border_width_left == legal.border_width_left:
		_fail("unavailable and legal actions differ by color alone")
		return false

	var page_scroll := main.get_node("Margin") as ScrollContainer
	if page_scroll == null:
		_fail("the scaled page has no outer scroll container")
		return false
	for path in [
		"Setup/Selections/Fields/Grid/Hero",
		"Setup/Selections/Fields/Grid/Scenario",
		"Setup/Selections/Fields/Grid/Mode",
		"Setup/Selections/Fields/Grid/Modular",
		"Setup/Selections/Fields/Grid/Seed",
	]:
		var control := _node(path) as Control
		if control.custom_minimum_size.y < expected_height:
			_fail("setup control '%s' is smaller than the pointer-target floor" % path)
			return false
		control.grab_focus()
		await process_frame
		await process_frame
		var page_rect := page_scroll.get_global_rect().intersection(
			Rect2(Vector2.ZERO, Vector2(root.size)))
		var control_rect := control.get_global_rect()
		if control_rect.end.y > page_rect.end.y:
			page_scroll.scroll_vertical += ceili(control_rect.end.y - page_rect.end.y)
		elif control_rect.position.y < page_rect.position.y:
			page_scroll.scroll_vertical -= ceili(page_rect.position.y - control_rect.position.y)
		await process_frame
		var visible_rect := control.get_global_rect().intersection(
			page_rect)
		if visible_rect.size.x < expected_height or visible_rect.size.y < expected_height:
			_fail("setup control '%s' cannot be brought into the viewport: control=%s visible=%s scroll=%s/%s" % [
				path,
				control.get_global_rect(),
				visible_rect,
				page_scroll.scroll_vertical,
				page_scroll.get_v_scroll_bar().max_value,
			])
			return false

	return true


func _first_enabled_choice() -> Button:
	for button in _visible_buttons(_decision()):
		if not button.disabled and button.text not in ["Submit decision", "Pass / decline", "+", "−"]:
			return button
	return null


func _visible_buttons_meet_pointer_floor() -> bool:
	var scale := OS.get_environment("MARVEL_UI_SCALE")
	var expected := 66 if scale == "extra-large" else 55 if scale == "large" else 44
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


func _button_named(wanted: String) -> Button:
	return _visible_button(main, wanted)


func _visible_button(node: Node, wanted: String) -> Button:
	for button in _visible_buttons(node):
		if button.text == wanted:
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
	return main.get_node("Margin/Shell/Content/" + relative)


func _play() -> Control:
	return _node("Play") as Control


func _decision() -> Control:
	return _node("Play/Prompt/Margin/Stack/DecisionScroll/Decision") as Control


func _status() -> Label:
	return _node("Status/Text") as Label


func _fail(message: String) -> void:
	if failed:
		return
	failed = true
	push_error(message + "\nVisible UI:\n" + (_visible_text(main) if main != null else "<none>"))
	quit(1)
