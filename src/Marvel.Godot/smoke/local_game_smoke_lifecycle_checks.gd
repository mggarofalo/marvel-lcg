extends "res://smoke/local_game_smoke_decision_checks.gd"

func _interaction_lifecycle_is_safe() -> bool:
	var choice := _first_enabled_choice()
	var synchronize := main.find_child("Synchronize", true, false) as Button
	var hand_card := _node("Play/Board/HandShelf").find_child(
		"ProceduralCard", true, false) as Control
	if choice == null or synchronize == null or hand_card == null:
		_fail("the lifecycle probe has no current prompt, synchronization, or card source")
		return false
	# A real pointer selection schedules board reveal and decision focus. Immediately
	# replacing the snapshot exercises the generation fence before those callbacks run.
	if not await _pointer_activate(choice):
		return false
	if not await _keyboard_activate(synchronize):
		return false
	# A pinned inspector schedules focus from a card that the second refresh removes.
	if not await _pointer_activate(hand_card):
		return false
	if not await _pointer_activate(synchronize):
		return false
	for frame in 5:
		await process_frame
	if not main.is_inside_tree() or not _play().is_inside_tree() or _decision() == null:
		_fail("consecutive renders left the current interaction tree unavailable")
		return false
	return true


func _mixed_submit_is_single_shot() -> bool:
	var change_form := _visible_button_beginning(_decision(), "Change Form")
	if change_form == null or change_form.disabled:
		return true
	if not await _pointer_activate(change_form):
		return false
	var submit := _submit_button()
	if submit == null or submit.disabled:
		_fail("the rapid-input probe cannot prepare a submit")
		return false
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	var submit_key := submit.name
	if not await _pointer_activate(submit):
		return false
	# Pointer activation rebuilds the decision tree. Reacquire by the stable
	# control key before checking that queued keyboard repetition cannot reuse it.
	submit = _decision().find_child(submit_key, true, false) as Button
	if submit == null:
		_fail("the submit control was not recreated after pointer activation")
		return false
	if not submit.disabled and not await _keyboard_activate(submit, 2):
		return false
	if not submit.disabled:
		_fail("the submit control did not synchronously lock after its first activation")
		return false
	if not await _wait_for(func() -> bool:
		return _is_complete() or (_node("Toolbar/SyncStatus") as Label).text != revision):
		_fail("the rapid-input probe never received its single authoritative response")
		return false
	return true
