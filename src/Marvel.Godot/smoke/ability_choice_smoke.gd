extends "res://smoke/local_game_smoke.gd"


func _run() -> void:
	if not await _open_helicarrier_game(): return
	if not await _play_helicarrier(): return
	if not await _activate_helicarrier(): return
	if not await _choose_discount_recipient(): return
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
	var summary := main.find_child("DraftProgress", true, false) as Label
	if summary == null or not _control_is_fully_visible(summary):
		_fail("Helicarrier's activation meaning is clipped")
		return false
	for meaning in ["Exhaust Helicarrier", "Choose a player", "next card", "1 fewer"]:
		if meaning not in summary.text:
			_fail("activation omitted " + meaning)
			return false
	var commit := _button_named("Use Helicarrier")
	if commit == null or not _control_is_fully_visible(commit):
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
	var card := _tabletop_card_named("Helicarrier")
	var identity := _tabletop_card_named("Peter Parker")
	if not await _drag(card, identity.get_global_rect().get_center()): return false
	await process_frame
	await process_frame
	if not _draft_meaning_fits(): return false
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if not await _pointer_activate(_button_named("Cancel draft")): return false
	if (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("cancelling Helicarrier changed the game")
		return false
	card = _tabletop_card_named("Helicarrier")
	identity = _tabletop_card_named("Peter Parker")
	if not await _drag(card, identity.get_global_rect().get_center()): return false
	if not _draft_meaning_fits(): return false
	if not await _commit_once("Use Helicarrier"): return false
	if not await _wait_for(func() -> bool: return _button_named("Spider-Man: next card −1") != null):
		_fail("activation did not expose the named player discount")
		return false
	return true


func _choose_discount_recipient() -> bool:
	var recipient := _button_named("Spider-Man: next card −1")
	if not _control_is_fully_visible(recipient):
		_fail("the recipient choice is clipped")
		return false
	if not await _pointer_activate(recipient): return false
	await process_frame
	await process_frame
	var description := main.find_child("DraftProgress", true, false) as Label
	if description == null or not _control_is_fully_visible(description) \
			or "Applies once" not in description.text:
		_fail("the selected recipient's once-only discount is clipped or missing")
		return false
	return true
