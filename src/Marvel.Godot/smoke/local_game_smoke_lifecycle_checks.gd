extends "res://smoke/local_game_smoke_decision_checks.gd"

func _interaction_lifecycle_is_safe() -> bool:
	var choice := _first_enabled_choice()
	var synchronize := main.find_child("Synchronize", true, false) as Button
	if choice == null or synchronize == null:
		_fail("the lifecycle probe has no current prompt or synchronization control")
		return false
	# A real pointer selection schedules board reveal and decision focus. Immediately
	# replacing the snapshot exercises the generation fence before those callbacks run.
	if not await _selection_and_sync_overlap(choice, synchronize):
		return false
	for frame in 2:
		await process_frame
	# A pinned inspector schedules focus from a card that the second refresh removes.
	if not await _inspector_and_sync_overlap():
		return false
	for frame in 5:
		await process_frame
	return _current_interaction_tree_is_available()


func _selection_and_sync_overlap(choice: Button, synchronize: Button) -> bool:
	if not await _prepare_activation(choice):
		return false
	if not await _prepare_activation(synchronize):
		return false
	if not _pointer_activate_without_settle(choice):
		return false
	return _keyboard_activate_without_settle(synchronize)


func _inspector_and_sync_overlap() -> bool:
	var hand_card := _hand_surface().find_child(
		"ProceduralCard", true, false) as Control
	var synchronize := main.find_child("Synchronize", true, false) as Button
	if hand_card == null or synchronize == null:
		_fail("the synchronized lifecycle probe has no current card source or sync control")
		return false
	if not await _prepare_activation(hand_card):
		return false
	if not await _prepare_activation(synchronize):
		return false
	return _pointer_activate_without_settle(hand_card) \
		and _pointer_activate_without_settle(synchronize)


func _current_interaction_tree_is_available() -> bool:
	if not main.is_inside_tree() or not _play().is_inside_tree() or _decision() == null:
		_fail("consecutive renders left the current interaction tree unavailable")
		return false
	return true


func _mixed_submit_is_single_shot() -> bool:
	var change_form := _visible_button_beginning(_decision(), "Change Form")
	if change_form == null or change_form.disabled:
		return true
	var revision_before_selection := (_node("Toolbar/SyncStatus") as Label).text
	if not await _pointer_activate(change_form):
		return false
	var submit := _submit_button()
	if submit == null or submit.disabled:
		if (_node("Toolbar/SyncStatus") as Label).text != revision_before_selection:
			return true
		_fail("the rapid-input probe cannot prepare a submit")
		return false
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	var prompt := _logical_prompt_identity()
	if not await _prepare_activation(submit):
		return false
	submit.grab_focus()
	if render_viewport.gui_get_focus_owner() != submit:
		_fail("the original prompt submit cannot receive keyboard focus")
		return false
	if not _pointer_activate_without_settle(submit):
		return false
	# Never reacquire a generic Submit: a quick authoritative response may already
	# have rendered a distinct prompt with the same node name. The queued keys
	# belong only to the focused original revision and logical prompt.
	if (_node("Toolbar/SyncStatus") as Label).text == revision \
			and _logical_prompt_identity() == prompt:
		_accept_repeats_without_settle(2)
	if not await _wait_for(func() -> bool:
		return _is_complete() or (_node("Toolbar/SyncStatus") as Label).text != revision):
		_fail("the rapid-input probe never received its single authoritative response")
		return false
	return true


func _logical_prompt_identity() -> String:
	var heading := _node("Play/Prompt/Margin/Stack/PromptHeader/Heading") as Label
	return heading.text + "\n" + _visible_text(_decision())
