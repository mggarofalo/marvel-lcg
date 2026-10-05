extends "res://smoke/local_game_smoke_direct_journey.gd"


func _direct_change_form_is_played() -> bool:
	if not await super._direct_change_form_is_played():
		return false
	if not await _hero_target_drag_preserves_commitment(): return false
	if not _active_cues_leave_printed_values_readable(): return false
	return await _deferred_preview_when_offered()


func _hero_target_drag_preserves_commitment() -> bool:
	var source := _card_for_anchor(IDENTITY)
	var target := _card_for_anchor(RHINO)
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if source == null or target == null or not await _drag(source, target.get_global_rect().get_center()):
		_fail("the ready hero could not be dragged onto the offered enemy")
		return false
	var commit := _task_commit()
	if commit == null or commit.text != "Attack Rhino" or commit.disabled:
		_fail("the hero drop did not stage its offered attack and exact enemy")
		return false
	if (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("a source-target drop committed an answer without confirmation")
		return false
	var history := main.find_child("ToggleHistory", true, false) as Button
	if history == null or not history.is_visible_in_tree():
		_fail("starting an attack draft concealed history")
		return false
	if not _active_cues_leave_printed_values_readable(): return false
	var cancel := main.find_child("ContextualCancelDraft", true, false) as Button
	if cancel == null or not await _pointer_activate(cancel):
		_fail("the attack drag draft cannot be cancelled locally")
		return false
	await process_frame
	return (_node("Toolbar/SyncStatus") as Label).text == revision and _task_commit() == null


func _direct_black_cat_is_played() -> bool:
	if not await super._direct_black_cat_is_played():
		return false
	var entry := _attached(_attached_name(BLACK_CAT, "Action"))
	if entry == null or not entry.text.begins_with("Actions"):
		print("SOURCE_CHOOSER_FOCUS_PENDING: no ambiguous ally action in this legal state")
		return true
	return await _source_chooser_restores_keyboard_focus()


func _source_chooser_restores_keyboard_focus() -> bool:
	var entry := _attached(_attached_name(BLACK_CAT, "Action"))
	if entry == null or not await _pointer_activate_attached(entry):
		return false
	var chooser := main.find_child("CardActionChoices", true, false) as Control
	if chooser == null:
		_fail("the ally's multiple actions did not open a local chooser")
		return false
	if not await _dismiss_source_chooser(chooser): return false
	if not await _wait_for(func() -> bool:
		var current := _attached(_attached_name(BLACK_CAT, "Action"))
		return current != null and current.has_focus()):
		_fail("dismissing the source chooser did not restore its action entry focus")
		return false
	entry = _attached(_attached_name(BLACK_CAT, "Action"))
	if not await _keyboard_activate(entry):
		return false
	chooser = main.find_child("CardActionChoices", true, false) as Control
	if chooser == null:
		_fail("the restored source focus could not reopen its chooser with the keyboard")
		return false
	if not await _dismiss_source_chooser(chooser): return false
	return _task_commit() == null


func _dismiss_source_chooser(chooser: Control) -> bool:
	var chooser_id := chooser.get_instance_id()
	if not await _wait_for(func() -> bool:
		var current := main.find_child("CardActionChoices", true, false) as Control
		var focused := render_viewport.gui_get_focus_owner()
		return current != null and current.get_instance_id() == chooser_id \
			and focused != null and current.is_ancestor_of(focused)):
		_fail("the source chooser did not receive keyboard focus")
		return false
	# Observe containment beyond the table's 50 ms deferred-focus confirmation.
	var started := Time.get_ticks_msec()
	while Time.get_ticks_msec() - started < 100:
		var focused := render_viewport.gui_get_focus_owner()
		if focused == null or not chooser.is_ancestor_of(focused):
			_fail("the source chooser lost keyboard focus while open")
			return false
		await process_frame
	# Synthetic passive-preview visibility exercises overlay input priority;
	# the surrounding chooser and gameplay still come from the legal Core game.
	var preview := main.get_node("CardInspector") as Control
	var preview_was_visible := preview.visible
	preview.visible = true
	print("SOURCE_CHOOSER_PASSIVE_PREVIEW_ESCAPE_CHECK")
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	render_viewport.push_input(escape)
	var release := InputEventKey.new()
	release.keycode = KEY_ESCAPE
	render_viewport.push_input(release)
	if not await _wait_for(func() -> bool:
		var current := main.find_child("CardActionChoices", true, false) as Control
		return current == null or current.get_instance_id() != chooser_id \
			or not current.is_visible_in_tree()):
		_fail("Escape did not dismiss the focused source chooser")
		return false
	preview.visible = preview_was_visible
	return true


func _source_chooser_focus_when_offered() -> bool:
	if main.has_meta("source_chooser_focus_proved"):
		return true
	var entry := _attached(_attached_name(BLACK_CAT, "Action"))
	if entry == null or not entry.text.begins_with("Actions"):
		return true
	if not await _source_chooser_restores_keyboard_focus():
		return false
	main.set_meta("source_chooser_focus_proved", true)
	return true


func _deferred_preview_when_offered() -> bool:
	if main.has_meta("deferred_preview_proved"):
		return true
	var cards := _visible_cards_named("Swinging Web Kick")
	if cards.is_empty(): return true
	var card := cards.front() as Control
	var anchor := String(card.name).trim_prefix("ProceduralCard").to_int()
	var action := _attached(_attached_name(anchor, "Action"))
	if action == null or action.disabled: return true
	var target := _card_for_anchor(RHINO)
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	main.set_meta("checking_deferred_preview", true)
	if target == null or not await _drag(card, target.get_global_rect().get_center()):
		_fail("the deferred event could not be dragged onto its visible enemy")
		return false
	main.remove_meta("checking_deferred_preview")
	var modal := _payment_modal()
	if modal == null or "Payment commits now" not in _visible_text(modal):
		_fail("the deferred drop did not open a payment commitment with later targeting explained")
		return false
	if not await _payment_cancel_is_safe(): return false
	if (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("the deferred drag preview or cancelled payment committed an answer")
		return false
	main.set_meta("deferred_preview_proved", true)
	return true


func _drag_preview_is_meaningful(card: Control) -> bool:
	if not main.has_meta("checking_deferred_preview"):
		return true
	var context := main.find_child("ContextualDecision", true, false) as Control
	var text := _visible_text(context)
	if "choose a target after payment" not in text or "No offered action" in text:
		_fail("the accepted deferred event drag preview denied or concealed its later target choice: %s" % text)
		return false
	return "Swinging Web Kick" in _visible_text(card)


func _active_cues_leave_printed_values_readable() -> bool:
	for card in main.find_children("ProceduralCard*", "PanelContainer", true, false):
		var cue := card.find_child("InteractionCue", true, false) as Label
		if cue == null or cue.text.is_empty() or not cue.is_visible_in_tree(): continue
		var content := card.find_child("CardContent", true, false)
		if content == null: continue
		for node in content.find_children("*", "Label", true, false):
			var printed := node as Label
			if printed.text.is_empty() or not printed.is_visible_in_tree(): continue
			if cue.get_global_rect().grow(-1).intersects(printed.get_global_rect().grow(-1)):
				_fail("an interaction cue covers printed card information: %s / %s" % [cue.text, printed.text])
				return false
	return true
