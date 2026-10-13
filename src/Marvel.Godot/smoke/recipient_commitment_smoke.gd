extends "res://smoke/local_game_smoke.gd"

var recipient_title := "Avengers Mansion"
var recipient_anchor := -1

func _run() -> void:
	if OS.get_environment("MARVEL_RECIPIENT_FACE") == "01092": recipient_title = "Helicarrier"
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	await _configure_seeded_game()
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = OS.get_environment("MARVEL_RECIPIENT_SEED")
	seed.text_changed.emit(seed.text)
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _keep_recipient_opening(): return
	if not await _play_recipient_source(): return
	if not await _recipient_cancel_and_commit(): return
	if not await _release_table_resources(): return
	print("RECIPIENT_COMMITMENT_SMOKE_OK title=%s players=%s" % [recipient_title, OS.get_environment("MARVEL_SMOKE_TWO_PLAYER")])
	quit(0)

func _keep_recipient_opening() -> bool:
	var players := 2 if OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true" else 1
	for seat in players:
		if not await _wait_for(func() -> bool: return _button_named("Keep hand") != null):
			_fail("recipient opening mulligan did not arrive")
			return false
		if not await _pointer_activate(_button_named("Keep hand")): return false
		await create_timer(0.15).timeout
	return await _wait_for(func() -> bool: return _button_named("End turn") != null)

func _play_recipient_source() -> bool:
	var matches := _visible_cards_named(recipient_title)
	if matches.is_empty():
		_fail("seed did not supply the recipient support in the legal opening hand")
		return false
	var card := matches[0] as Control
	recipient_anchor = int(str(card.name).trim_prefix("ProceduralCard"))
	if not await _drag_to_prompt_owner_lane(card): return false
	if not await _wait_for(func() -> bool: return _payment_modal() != null):
		_fail("support drag did not open its legal payment")
		return false
	if not await _compose_payment(): return false
	return await _wait_for(func() -> bool: return _recipient_entry() != null)

func _recipient_entry() -> Button:
	return _attached(_attached_name(recipient_anchor, "Action"))

func _recipient_popup() -> Control:
	return main.find_child("RecipientCommitments", true, false) as Control

func _recipient_cancel_and_commit() -> bool:
	var before := _recipient_revision()
	if not await _pointer_activate(_recipient_entry()): return false
	if not await _wait_for(func() -> bool: return _recipient_popup() != null):
		_fail("source action did not open explicit recipient commitments")
		return false
	if not await _recipient_choices_are_readable(): return false
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	render_viewport.push_input(escape)
	var release := InputEventKey.new()
	release.keycode = KEY_ESCAPE
	render_viewport.push_input(release)
	if not await _wait_for(func() -> bool: return _recipient_popup() == null):
		_fail("Escape did not cancel the recipient commitment")
		return false
	if _recipient_revision() != before:
		_fail("opening/cancelling a recipient choice committed gameplay")
		return false
	return await _commit_recipient(before)

func _recipient_choices_are_readable() -> bool:
	var popup := _recipient_popup()
	var choices := popup.find_children("Affordance*", "Button", true, false)
	var count := 2 if OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true" else 1
	if choices.size() != count:
		_fail("recipient popup lost an engine-offered player")
		return false
	for button in choices:
		if button.get_global_rect().size.y < 44:
			_fail("recipient commitment is shorter than its deliberate 44px target")
			return false
		if not "Exhaust" in button.text or not ("Spider-Man" in button.text or "Captain Marvel" in button.text):
			_fail("recipient commitment hid its exact cost or player: %s" % button.text)
			return false
		if not await _control_owns_point(button, button.get_global_rect().get_center()): return false
	return await _capture_checkpoint("recipient-named-commitments")

func _commit_recipient(before: int) -> bool:
	if not await _keyboard_activate(_recipient_entry()): return false
	if not await _wait_for(func() -> bool: return _recipient_popup() != null): return false
	var choices := _recipient_popup().find_children("Affordance*", "Button", true, false)
	if not await _keyboard_activate(choices[-1], 3): return false
	if not await _wait_for(func() -> bool: return _recipient_revision() > before):
		_fail("named recipient did not commit its activation")
		return false
	await create_timer(0.2).timeout
	if _recipient_revision() != before + 1 or _recipient_popup() != null:
		_fail("recipient activation repeated or retained a future confirmation")
		return false
	return true

func _recipient_revision() -> int:
	var status := _node("Toolbar/SyncStatus") as Label
	var regex := RegEx.new()
	regex.compile("r([0-9]+)")
	var found := regex.search(status.text)
	return int(found.get_string(1)) if found != null else -1
