extends "res://smoke/shuri_search_smoke.gd"

const SHUFFLE := "Shuffle selected cards into your deck"

func _run() -> void:
	if not await _open_ancestral_game():
		_fail("Ancestral journey failed at legal opening")
		return
	print("ANCESTRAL_CHECKPOINT legal opening")
	if not await _select_setup_upgrade():
		_fail("Ancestral journey failed at setup upgrade selection")
		return
	print("ANCESTRAL_CHECKPOINT setup upgrade selection")
	if not await _commit_search_choice():
		_fail("Ancestral journey failed at setup upgrade commitment")
		return
	print("ANCESTRAL_CHECKPOINT setup upgrade commitment")
	if not await _play_ancestral_knowledge():
		_fail("Ancestral journey failed at Ancestral Knowledge payment")
		return
	print("ANCESTRAL_CHECKPOINT Ancestral Knowledge payment")
	if not await preload("res://smoke/discard_gallery_checks.gd").perform(self):
		_fail("Ancestral journey failed at discard gallery journey")
		return
	print("ANCESTRAL_CHECKPOINT discard gallery journey")
	if not await _finish_shuffle():
		_fail("Ancestral journey failed at shuffle result")
		return
	print("ANCESTRAL_CHECKPOINT shuffle result")
	print("ANCESTRAL_KNOWLEDGE_SMOKE_OK scale=%d viewport=%s six_candidates=true duplicate_titles_restricted=true selection_preserved=true explicit_commit=true" % [_scale_percentage(), _viewport_size()])
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _open_ancestral_game() -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	_select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Black Panther")
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "4"
	seed.text_changed.emit(seed.text)
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _button_named("Keep hand") != null): return false
	# The maintained managed fixture pins this opening: retain Ancestral Knowledge,
	# replace the other five physical cards, including both copies of Vibranium.
	for id in [13, 31, 16, 17, 38]:
		var toggle := main.find_child("MulliganDiscard%d" % id, true, false) as Button
		if toggle == null or not await _keyboard_activate(toggle):
			_fail("the legal seed lost opening replacement %d" % id)
			return false
	var commit := _task_commit()
	if commit == null or commit.disabled or "5" not in commit.text:
		_fail("five staged mulligan cards lack a count-labelled commitment")
		return false
	if not await _pointer_activate(commit): return false
	return await _wait_for(func() -> bool: return _search_frame() != null)


func _play_ancestral_knowledge() -> bool:
	var source := _tabletop_card_named("Ancestral Knowledge")
	if source == null or not await _drag_to_prompt_owner_lane(source): return false
	if not await _wait_for(func() -> bool: return _payment_modal() != null):
		_fail("Ancestral Knowledge did not open its payment")
		return false
	var resource := _visible_button_beginning(_payment_modal(), "Discard Wakanda Forever!")
	if resource == null or not await _keyboard_activate(resource): return false
	if not await _commit_once("Ancestral Knowledge"): return false
	if not await _wait_for(func() -> bool: return main.find_child("VisibleTargetCards", true, false) != null):
		_fail("paid Ancestral Knowledge did not open the visible target gallery")
		return false
	for frame in range(3): await process_frame
	return true


func _finish_shuffle() -> bool:
	if not await _confirm_shuffle(): return false
	if not await _inspect_remaining_discard(): return false
	return await _capture_checkpoint("ancestral-shuffle-result")


func _confirm_shuffle() -> bool:
	var commit := _button_named(SHUFFLE)
	if commit == null or commit.disabled:
		_fail("the two selected cards lack their named shuffle commitment")
		return false
	var before := (_node("Toolbar/SyncStatus") as Label).text
	if not await _pointer_activate(commit): return false
	if not await _wait_for(func() -> bool:
		return _search_frame() == null and _button_named("End turn") != null): return false
	if (_node("Toolbar/SyncStatus") as Label).text == before:
		_fail("shuffle did not advance the authoritative decision")
		return false
	return true


func _remaining_discard() -> Button:
	# The event joins the four unchosen discards after its effect completes.
	var pile: Button = null
	for button in main.find_children("InspectPile*", "Button", true, false):
		if button.is_visible_in_tree() and not button.disabled and button.text.ends_with("Discard"):
			if pile != null:
				_fail("the opening fixture unexpectedly has several nonempty discard piles")
				return null
			pile = button
	if pile == null or pile.text.split("\n")[1] != "5":
		_fail("shuffle did not leave five cards in the player's discard pile")
		return null
	return pile


func _inspect_remaining_discard() -> bool:
	var pile := _remaining_discard()
	if pile == null or not await _keyboard_activate(pile): return false
	var popup := root.find_child("PileInspector", true, false) as PopupPanel
	if popup == null: return false
	var titles := await _discard_titles(popup)
	if titles.count("Vibranium") != 1 or "Med Team" in titles or "Ancestral Knowledge" not in titles:
		_fail("discard inspection contradicts the selected shuffle: %s" % titles)
		return false
	await preload("res://smoke/search_inspection_checks.gd")._dismiss(self, popup, false)
	return true


func _discard_titles(popup: PopupPanel) -> Array[String]:
	var titles: Array[String] = []
	for index in range(5):
		var title := popup.find_child("Title", true, false) as Label
		if title == null: return []
		titles.append(title.text)
		if index < 4:
			var next := popup.find_child("NextPileCard", true, false) as Button
			if next == null or next.disabled: return []
			next.grab_focus()
			await preload("res://smoke/search_inspection_checks.gd")._key(self, popup, KEY_ENTER)
			await process_frame
	return titles
