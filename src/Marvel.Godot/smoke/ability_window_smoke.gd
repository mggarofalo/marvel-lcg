extends "res://smoke/recipient_commitment_smoke.gd"


func _run() -> void:
	if not await _open_window_game(): return
	if not await _play_mockingbird(): return
	if not await _verify_named_response(): return
	print("ABILITY_WINDOW_SMOKE_OK scale=%d named=true one_command=true stable_table=true" % _scale_percentage())
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _open_window_game() -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "19"
	seed.text_changed.emit(seed.text)
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func(): return _button_named("Keep hand") != null): return false
	if not await _pointer_activate(_button_named("Keep hand")): return false
	return await _wait_for(func(): return _button_named("End turn") != null)


func _play_mockingbird() -> bool:
	if not await _drag_to_prompt_owner_lane(_tabletop_card_named("Mockingbird")): return false
	if not await _wait_for(func(): return _payment_modal() != null): return false
	for title in ["Energy", "Emergency"]:
		var resource := _visible_button_beginning(_payment_modal(), "Discard " + title)
		if resource == null or not await _pointer_activate(resource):
			_fail("legal seed19 payment lacks " + title)
			return false
	if not await _commit_once("Play Mockingbird"): return false
	return await _wait_for(func(): return _window() != null)


func _window() -> Control:
	return preload("res://smoke/ability_window_smoke_support.gd").surface(main)


func _verify_named_response() -> bool:
	await process_frame
	await process_frame
	var window := _window()
	var choice := window.find_child("WindowChoice*", true, false) as Button
	if not _named_response_layout_is_clear(window, choice): return false
	var hand_bounds := (main.find_child("HandDropArea", true, false) as Control).get_global_rect()
	if not await _capture_checkpoint("mockingbird-response"): return false
	var revision := _recipient_revision()
	var stale_commit: Callable = choice.pressed.get_connections()[0]["callable"]
	if not await _pointer_activate(choice): return false
	stale_commit.call()
	if not await _wait_for(func(): return _recipient_revision() > revision): return false
	if _recipient_revision() != revision + 1 or _window() != null \
			or (main.find_child("HandDropArea", true, false) as Control).get_global_rect() != hand_bounds:
		_fail("named response changed layout or needed more than one commitment")
		return false
	var statuses := _card_for_anchor(RHINO).find_children("StatusCard*", "Control", true, false)
	if statuses.is_empty() or statuses[0].accessibility_name != "Stunned":
		_fail("the named response did not visibly stun Rhino")
		return false
	return true


func _window_keyboard_focus_is_contained(choice: Button, pass_button: Button) -> bool:
	choice.grab_focus()
	for expected in [pass_button, choice]:
		var tab := InputEventKey.new()
		tab.keycode = KEY_TAB
		tab.pressed = true
		render_viewport.push_input(tab)
		tab = InputEventKey.new()
		tab.keycode = KEY_TAB
		render_viewport.push_input(tab)
		if render_viewport.gui_get_focus_owner() != expected:
			_fail("Tab left the current ability choices")
			return false
	return true

func _named_response_layout_is_clear(window: Control, choice: Button) -> bool:
	var heading := window.find_child("AbilityWindowHeading", true, false) as Label
	var pass_button := window.find_child("WindowPass", true, false) as Button
	var cause := window.find_child("AbilityWindowCause", true, false) as Label
	if heading == null or heading.text != "Mockingbird Response" or choice == null \
			or choice.text != "Stun Rhino" or pass_button == null or cause == null \
			or "Mockingbird entered play" not in cause.text:
		_fail("the response omitted its named ability, recipient, or Pass: " + _visible_text(window))
		return false
	for control in [heading, cause, choice, pass_button]:
		if not _control_is_fully_visible(control):
			_fail("named response control is clipped: " + str(control.name))
			return false
	if choice.size.y < 44 or pass_button.size.y < 44:
		_fail("response commitments miss44px")
		return false
	var hand := main.find_child("HandDropArea", true, false) as Control
	var hand_bounds := hand.get_global_rect()
	if window.get_global_rect().intersects(hand_bounds):
		_fail("the response covers the physical hand")
		return false
	return _window_keyboard_focus_is_contained(choice, pass_button)
