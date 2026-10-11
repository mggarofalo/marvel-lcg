extends "res://smoke/local_game_smoke.gd"

# Reach the known player discard through accepted legal Core choices.
func _run() -> void:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _wait_for(func(): return _task_commit() != null): return
	if not await _empty_and_hidden_piles_are_private(): return
	if not await _pointer_activate(_task_commit()): return
	if not await _play_with_known_payment(): return
	if not await _known_discard_preview(45): return
	if not await _release_table_resources(): return
	print("DISCARD_PREVIEW_SMOKE_OK player_top=45")
	quit(0)

func _play_with_known_payment() -> bool:
	if not await _wait_for(func(): return _card_for_anchor(19) != null and _attached(_attached_name(19, "Action")) != null): return false
	if not await _drag_to_prompt_owner_lane(_card_for_anchor(19)): return false
	if not await _wait_for(func(): return _payment_modal() != null): return false
	if not await _choose_target(1) or not await _choose_cost(0): return false
	if not await _activate_decision_resource(45): return false
	return await _commit_once("Web-Shooter")

func _empty_and_hidden_piles_are_private() -> bool:
	var inspector: Control = main.get_node("CardInspector")
	var empty_count := 0
	for pile in _hand_surface().find_children("PileEmpty*", "PanelContainer", true, false):
		empty_count += 1
		_position_pointer_without_settle(pile.get_global_rect().get_center())
		await process_frame
		if inspector.visible:
			_fail("empty discard pile exposed a card preview")
			return false
	if empty_count < 2:
		_fail("legal opening fixture lacks both empty discard piles")
		return false
	for pile in _hand_surface().find_children("InspectPile*", "Button", true, false):
		if not pile.disabled: continue
		_position_pointer_without_settle(pile.get_global_rect().get_center())
		await process_frame
		pile.grab_focus()
		await process_frame
		if inspector.visible:
			_fail("empty or concealed pile exposed a card preview")
			return false
	_position_pointer_without_settle(Vector2(4, 4))
	return true

func _known_discard_preview(expected: int) -> bool:
	var discard: Button = null
	for pile in _hand_surface().find_children("InspectPile*", "Button", true, false):
		if "Discard" in pile.text and not pile.disabled:
			discard = pile
			break
	if discard == null:
		_fail("accepted payment created no player discard")
		return false
	var revision: String = _node("Toolbar/SyncStatus").text
	var inspector: Control = main.get_node("CardInspector")
	var focus := render_viewport.gui_get_focus_owner()
	if not await _control_owns_point(discard, discard.get_global_rect().get_center()): return false
	await process_frame
	if not inspector.visible or int(inspector.get_meta("inspected_card_anchor", -1)) != expected \
			or render_viewport.gui_get_focus_owner() != focus:
		_fail("discard hover did not passively preview exact paid instance %d" % expected)
		return false
	_position_pointer_without_settle(Vector2(4, 4))
	await create_timer(0.4).timeout
	if inspector.visible:
		_fail("discard preview remained after the pointer grace period")
		return false
	discard.grab_focus()
	await process_frame
	if not inspector.visible or int(inspector.get_meta("inspected_card_anchor", -1)) != expected \
			or render_viewport.gui_get_focus_owner() != discard:
		_fail("discard keyboard focus lost exact paid identity or stole focus")
		return false
	if _node("Toolbar/SyncStatus").text != revision:
		_fail("discard preview changed the authoritative revision")
		return false
	return true
