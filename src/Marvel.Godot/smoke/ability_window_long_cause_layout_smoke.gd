extends "res://smoke/ability_window_smoke.gd"

const STAMPEDE_CAUSE := "Cause: Stampede. Enemy attack · Initiation · Interrupt window · Rhino is initiating an attack against Spider-Man. Target: Spider-Man. ATK 2 before boost icons and defense."


func _run() -> void:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	var sample = (load("res://AbilityWindowLayoutSample.cs") as Script).new()
	main.add_child(sample)
	for current_choices in [1, 2]:
		if not await _verify_sample(sample, current_choices): return
	print("ABILITY_WINDOW_LONG_CAUSE_LAYOUT_OK scale=%d synthetic_layout=true choices=1,2 whole_choice=true no_scroll=true" % _scale_percentage())
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _verify_sample(sample: Node, current_choices: int) -> bool:
	sample.Present(main, "Spider-Man Interrupt", STAMPEDE_CAUSE, "Spider-Man draws 1 card", current_choices)
	await process_frame
	await process_frame
	await process_frame
	var window := _window()
	if not _all_current_controls_fit(window, current_choices): return false
	if not await _capture_checkpoint("stampede-cause-layout-%d-choices" % current_choices): return false
	var choice := window.find_child("WindowChoiceSample", true, false) as Button
	if not await _pointer_activate(choice): return false
	if not sample.get_meta("choice_committed", false):
		_fail("the fully visible current commitment cannot receive a native pointer")
		return false
	return true

func _all_current_controls_fit(window: Control, current_choices: int) -> bool:
	var controls := ["AbilityWindowHeading", "AbilityWindowAnswerer", "AbilityWindowCause", "WindowChoiceSample", "WindowPass"]
	if current_choices == 2: controls.append("WindowChoiceSecond")
	for name in controls:
		var control := window.find_child(name, true, false) as Control
		if control == null or not _control_is_fully_visible(control):
			_fail("long authorized cause clips " + name)
			return false
		if control is Button and control.size.y < 44:
			_fail("long cause shrinks its current commitment")
			return false
	var scroll := window.find_child("AbilityWindowViewport", true, false) as ScrollContainer
	if scroll.get_v_scroll_bar().is_visible_in_tree():
		_fail("complete current choices need a scrollbar")
		return false
	if window.get_global_rect().position.y < 478 or window.get_global_rect().end.y > 776:
		_fail("the modal overlaps chronology or the hand: " + str(window.get_global_rect()))
		return false
	return true
