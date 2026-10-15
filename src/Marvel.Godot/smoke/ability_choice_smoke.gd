extends "res://smoke/recipient_commitment_smoke.gd"


func _run() -> void:
	if not await _open_helicarrier_game():
		_fail("Helicarrier opening did not complete")
		return
	if not await _play_helicarrier():
		_fail("Helicarrier play did not complete")
		return
	if not await _activate_helicarrier():
		_fail("Helicarrier action entry did not complete")
		return
	if not await _choose_discount_recipient():
		_fail("Helicarrier recipient commitment did not complete")
		return
	print("ABILITY_CHOICE_SMOKE_OK scale=%d" % _scale_percentage())
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _open_helicarrier_game() -> bool:
	var packed := load("res://Main.tscn") as PackedScene
	if not await _open_setup(packed): return false
	await _configure_seeded_game()
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "31"
	seed.text_changed.emit(seed.text)
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _button_named("Keep hand") != null):
		_fail("opening hand did not arrive")
		return false
	if not await _pointer_activate(_button_named("Keep hand")): return false
	return await _wait_for(func() -> bool: return _button_named("End turn") != null)


func _draft_meaning_fits() -> bool:
	var popup := _recipient_popup()
	if popup == null or not _control_is_fully_visible(popup):
		_fail("Helicarrier's recipient commitment is clipped")
		return false
	var buttons := popup.find_children("Affordance*", "Button", true, false)
	if buttons.size() != 1:
		_fail("Helicarrier omitted its sole legal recipient")
		return false
	var commit := buttons[0] as Button
	for meaning in ["Exhaust Helicarrier", "Spider-Man", "next card −1", "this phase", "Applies once"]:
		if meaning not in commit.text:
			_fail("activation omitted " + meaning)
			return false
	if not _control_is_fully_visible(commit) or commit.get_global_rect().size.y < 44:
		_fail("activation commitment is clipped")
		return false
	return true


func _play_helicarrier() -> bool:
	var card := _tabletop_card_named("Helicarrier")
	if not await _drag_to_prompt_owner_lane(card): return false
	if not await _wait_for(func() -> bool: return _payment_modal() != null):
		_fail("Helicarrier did not open payment")
		return false
	for title in ["For Justice!", "Energy"]:
		var source := _visible_button_beginning(_payment_modal(), "Discard " + title)
		if source == null or not await _keyboard_activate(source):
			_fail("the seeded hand cannot pay using " + title)
			return false
	if not await _commit_once("Play Helicarrier"): return false
	if not await _wait_for(func() -> bool: return _payment_modal() == null): return false
	return true


func _activate_helicarrier() -> bool:
	if not await _use_installed_helicarrier(false): return false
	await process_frame
	await process_frame
	if not _draft_meaning_fits(): return false
	var revision := _recipient_revision()
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	render_viewport.push_input(escape)
	var release := InputEventKey.new()
	release.keycode = KEY_ESCAPE
	render_viewport.push_input(release)
	if not await _wait_for(func() -> bool: return _recipient_popup() == null):
		_fail("Escape did not dismiss the recipient commitment")
		return false
	if _recipient_revision() != revision:
		_fail("cancelling Helicarrier changed the game")
		return false
	if not await _use_installed_helicarrier(true): return false
	return _draft_meaning_fits()


func _use_installed_helicarrier(keyboard: bool) -> bool:
	var tableau := main.find_child("SourceTableau", true, false)
	var action: Button = null
	if tableau != null:
		for tile in tableau.find_children("*", "PanelContainer", true, false):
			if tile.get_meta("source_title", "") == "Helicarrier":
				action = tile.find_child("Card*Action", true, false) as Button
	if action == null or action.disabled or not _control_is_fully_visible(action):
		_fail("installed Helicarrier has no visible, enabled tableau action")
		return false
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	var activated := await _keyboard_activate(action) if keyboard else await _pointer_activate(action)
	if not activated: return false
	await process_frame
	await process_frame
	if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision:
		_fail("selecting Helicarrier's offered action committed before confirmation")
		return false
	return true


func _choose_discount_recipient() -> bool:
	var recipient := _recipient_popup().find_child("Affordance*", true, false) as Button
	var revision := _recipient_revision()
	if not await _pointer_activate(recipient): return false
	if not await _wait_for(func() -> bool: return _recipient_revision() > revision):
		_fail("named recipient did not commit the support activation")
		return false
	if _recipient_revision() != revision + 1 or _recipient_popup() != null:
		_fail("recipient activation did not finish in one revision")
		return false
	if _button_named("End turn") == null or _payment_modal() != null:
		_fail("recipient activation left an extra decision instead of continuing the turn")
		return false
	return true
