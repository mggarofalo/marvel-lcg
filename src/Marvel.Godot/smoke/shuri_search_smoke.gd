extends "res://smoke/search_choice_smoke.gd"

const SEARCH := "Search your deck for an upgrade and add it to your hand"

func _run() -> void:
	if not await _open_shuri_game(): return
	if not await _select_setup_upgrade(): return
	if not await _commit_search_choice(): return
	if not await _play_shuri(): return
	if not await _accept_search(): return
	if not await preload("res://smoke/search_inspection_checks.gd").perform(self): return
	if not await _select_and_finish(): return
	print("SHURI_SEARCH_SMOKE_OK committed_privacy=true inspected_without_selection=true required_choice=true")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)

func _open_shuri_game() -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	_select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Black Panther")
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "0"
	seed.text_changed.emit(seed.text)
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _button_named("Keep hand") != null): return false
	if not await _pointer_activate(_button_named("Keep hand")): return false
	return await _wait_for(func() -> bool: return _search_frame() != null)

func _select_setup_upgrade() -> bool:
	for face in _search_frame().find_children("SearchResult*", "Control", true, false):
		if "Tactical Genius" in _visible_text(face):
			var badge := face.find_child("Affordance*", true, false) as Button
			return await _keyboard_activate(badge)
	_fail("seeded setup search lost Tactical Genius")
	return false

func _play_shuri() -> bool:
	var shuri := _tabletop_card_named("Shuri")
	if shuri == null or not await _drag_to_prompt_owner_lane(shuri): return false
	if not await _wait_for(func() -> bool: return _payment_modal() != null):
		_fail("Shuri did not open her payment")
		return false
	var resource := _visible_button_beginning(_payment_modal(), "Discard Vibranium")
	if resource == null or not await _keyboard_activate(resource): return false
	if not await _commit_once("Play Shuri"): return false
	if not await _wait_for(func() -> bool: return _visible_button_beginning(_play(), SEARCH) != null):
		_fail("Shuri response did not arrive: " + _visible_text(_play()))
		return false
	return true

func _accept_search() -> bool:
	if _search_frame() != null or not main.find_children("SearchResult*", "Control", true, false).is_empty():
		_fail("Shuri exposed candidate faces before search commitment")
		return false
	var pass_option := _button_named("Pass this opportunity")
	if pass_option == null:
		_fail("Shuri's optional response lost its pass choice")
		return false
	if not await _pointer_activate(_visible_button_beginning(_play(), SEARCH)): return false
	if not await _commit_once(SEARCH): return false
	if not await _wait_for(func() -> bool: return _search_frame() != null):
		_fail("committed Shuri search did not reveal authorized upgrade choices")
		return false
	if _button_named("Cancel draft") != null:
		_fail("committed search acquired a refunding cancel")
		return false
	return _search_fits()

func _select_and_finish() -> bool:
	var face := _search_frame().find_child("SearchResult*", true, false) as Control
	var badge := face.find_child("Affordance*", true, false) as Button
	if not await _pointer_activate(badge): return false
	await process_frame
	var commit := _visible_button_beginning(_search_frame(), "Add ")
	if commit == null or commit.disabled:
		_fail("Shuri selection did not retain a named next commitment")
		return false
	if not await _pointer_activate(commit): return false
	if not await _wait_for(func() -> bool: return _search_frame() == null and _button_named("End turn") != null):
		_fail("Shuri search did not complete and return to the player turn")
		return false
	return true
