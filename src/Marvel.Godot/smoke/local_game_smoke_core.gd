extends "res://smoke/local_game_smoke_pointer_support.gd"

const SmokeScale = preload("res://smoke/local_game_smoke_scale.gd")
const TIMEOUT_MILLISECONDS := 15000
const MAX_DECISIONS := 80

var main: Control
var failed := false
var motion_enabled := true


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
