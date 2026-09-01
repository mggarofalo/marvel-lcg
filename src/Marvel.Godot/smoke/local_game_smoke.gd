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
	var event_log := _node("Play/Prompt/Margin/Stack/EventLog") as RichTextLabel
	var event_text := event_log.get_parsed_text().strip_edges()
	if event_text.is_empty() or event_text == "No events yet.":
		_fail("the visible event log is empty")
		return

	print("LOCAL_GAME_SMOKE_OK decisions=%d" % decisions)
	quit(0)


func _first_enabled_choice() -> Button:
	for button in _visible_buttons(_decision()):
		if not button.disabled and button.text not in ["Submit decision", "Pass / decline", "+", "−"]:
			return button
	return null


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
