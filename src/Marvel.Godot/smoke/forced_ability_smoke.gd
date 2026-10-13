extends "res://smoke/ability_window_smoke.gd"


func _run() -> void:
	if not await _open_forced_game(): return
	if not await _play_black_cat(): return
	if not await _verify_forced_acknowledgment(): return
	print("FORCED_ABILITY_SMOKE_OK scale=%d persists=true ack_local=true engine_prompt_retained=true" % _scale_percentage())
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _open_forced_game() -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "3"
	seed.text_changed.emit(seed.text)
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func(): return _button_named("Keep hand") != null): return false
	if not await _pointer_activate(_button_named("Keep hand")): return false
	return await _wait_for(func(): return _button_named("End turn") != null)


func _play_black_cat() -> bool:
	if not await _drag_to_prompt_owner_lane(_tabletop_card_named("Black Cat")): return false
	if not await _wait_for(func(): return _payment_modal() != null): return false
	for title in ["Interrogation Room", "Great Responsibility"]:
		var resource := _visible_button_beginning(_payment_modal(), "Discard " + title)
		if resource == null or not await _pointer_activate(resource):
			_fail("legal seed3 payment lacks " + title)
			return false
	if not await _commit_once("Play Black Cat"): return false
	return await _wait_for(func(): return _window() != null)


func _verify_forced_acknowledgment() -> bool:
	await process_frame
	await process_frame
	if not _forced_notification_layout_is_clear(_window()): return false
	if not await _inspect_forced_source(): return false
	var revision := _recipient_revision()
	if not await _forced_receipt_survives_escape_and_sync(revision): return false
	var acknowledge := _window().find_child("AcknowledgeForcedAbility", true, false) as Button
	if not await _capture_checkpoint("black-cat-forced-response"): return false
	var stale_ack: Callable = acknowledge.pressed.get_connections()[0]["callable"]
	if not await _pointer_activate(acknowledge): return false
	stale_ack.call()
	await process_frame
	await process_frame
	if _window() != null or _recipient_revision() != revision or _button_named("End turn") == null:
		_fail("acknowledgment sent an engine answer or lost its pending action")
		return false
	return true


func _inspect_forced_source() -> bool:
	var summary := _window().find_child("ForcedAbilitySummary", true, false) as Label
	if summary.text != "Spider-Man discarded Spider-Tracer and Haymaker.":
		_fail("Black Cat receipt does not identify its actual discarded cards: " + summary.text)
		return false
	var heading := _window().find_child("ForcedAbilityHeading", true, false) as Control
	if not await _keyboard_activate(heading): return false
	var inspector := main.get_node("CardInspector") as Control
	if not inspector.visible or "Black Cat" not in _visible_text(inspector):
		_fail("forced source title does not inspect the exact printed card")
		return false
	return await _close_inspector_with_keyboard(heading, inspector)


func _forced_notification_layout_is_clear(window: Control) -> bool:
	var heading := window.find_child("ForcedAbilityHeading", true, false) as Label
	var acknowledge := window.find_child("AcknowledgeForcedAbility", true, false) as Button
	if heading == null or heading.text != "Black Cat — Forced Response" or acknowledge == null \
			or window.find_child("WindowPass", true, false) != null:
		_fail("forced effect lacks distinct acknowledgment: " + _visible_text(window))
		return false
	for control in [heading, window.find_child("ForcedAbilitySummary", true, false), acknowledge]:
		if not _control_is_fully_visible(control):
			_fail("forced notification control is clipped")
			return false
	if acknowledge.size.y < 44:
		_fail("forced acknowledgment misses44px")
		return false
	return true


func _forced_receipt_survives_escape_and_sync(revision: int) -> bool:
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	render_viewport.push_input(escape)
	escape = InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	render_viewport.push_input(escape)
	await process_frame
	if _window() == null or _recipient_revision() != revision:
		_fail("Escape bypassed forced acknowledgment")
		return false
	var sync := main.find_child("Synchronize", true, false) as Button
	if sync == null or not await _pointer_activate(sync): return false
	if not await _wait_for(func(): return not _status().text.begins_with("SYNCHRONIZING")): return false
	if _window() == null or _recipient_revision() != revision:
		_fail("synchronization consumed the unacknowledged forced receipt")
		return false
	return true
