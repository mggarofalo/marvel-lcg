extends "res://smoke/local_game_smoke.gd"


func _run() -> void:
	if OS.get_environment("MARVEL_ACTIVITY_ATTACK_ONLY") == "true":
		if await _legal_enemy_attack_is_causal():
			print("ACTIVITY_ATTACK_PROBE_OK")
			quit(0)
		return
	for animated in [true, false]:
		if not await _open_phase_activity(animated): return
		if not await _verify_phase_activity(animated): return
		if not await _complete_history_is_reachable(): return
		if not await _historical_encounter_face_is_inspectable(): return
		if not await _sync_clears_activity(): return
		main.queue_free()
		await process_frame
		await process_frame
	if not await _legal_enemy_attack_is_causal(): return
	print("EVENT_ACTIVITY_SMOKE_OK ordered=true decision_preserved=true reduced_motion=true sync_clears=true")
	quit(0)


func _open_phase_activity(animated: bool) -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	var motion := _node("Toolbar/Motion") as CheckButton
	motion.button_pressed = animated
	motion.toggled.emit(animated)
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _task_commit() != null): return false
	if not await _commit_once("Keep hand"): return false
	if not await _pointer_activate(_button_named("End turn")): return false
	if not await _wait_for(func(): return _button_named("Keep hand") != null):
		_fail("End turn did not offer its required end-of-phase discard commitment")
		return false
	var prior := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	main.set_meta("phase_prior_revision", prior)
	main.set_meta("phase_expected_revision", int(prior) + 1)
	if not await _pointer_activate(_button_named("Keep hand")): return false
	if await _wait_for(func() -> bool:
		var activity := _timeline()
		return activity != null and int(activity.get_meta("timeline_count", 0)) > 1 and activity.is_visible_in_tree()): return true
	_fail("accepted villain-phase response has no visible chronological playback")
	return false


func _activity() -> Control:
	return main.find_child("EventActivity", true, false) as Control


func _verify_phase_activity(animated: bool) -> bool:
	var activity := _timeline()
	var settled_revision := int(main.get_meta("phase_expected_revision", -1))
	var account := _timeline().find_child("TimelineCaption", true, false) as Label
	if not _control_is_fully_visible(_timeline()) or account.text.is_empty():
		_fail("the villain phase has no readable bounded activity account")
		return false
	if animated:
		if not await _capture_checkpoint("phase-activity-first-beat"): return false
		if not await _ordered_beats_remain_interactive(activity): return false
	else:
		if not _static_results_are_visible(activity): return false
	if not await _wait_for(func(): return not bool(main.get_meta("response_staged", false))):
		_fail("accepted phase consequences never settled")
		return false
	if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != str(settled_revision):
		_fail("presentation did not settle on exactly the accepted revision")
		return false
	return await _capture_checkpoint("phase-activity-%s" % ("animated" if animated else "reduced"))


func _ordered_beats_remain_interactive(activity: Control) -> bool:
	if not bool(activity.get_meta("activity_running", false)):
		_fail("the accepted phase batch skipped directly to its final result")
		return false
	if not bool(main.get_meta("response_staged", false)) or _button_named("End turn") != null:
		_fail("activity exposed the next decision before its consequences finished")
		return false
	while bool(activity.get_meta("activity_running", false)):
		if not _prior_phase_is_retained(): return false
		var index := int(activity.get_meta("activity_index"))
		var result := _timeline().find_child("TimelineCaption", true, false) as Label
		if result == null or result.text.is_empty() or not _control_is_fully_visible(result):
			_fail("a chronological beat has no visible result")
			return false
		if not await _capture_checkpoint("phase-beat-%d" % index): return false
		if not await _wait_for(func() -> bool:
			return int(activity.get_meta("activity_index")) != index or not bool(activity.get_meta("activity_running", false))): return false
		var next := int(activity.get_meta("activity_index"))
		if next != index + 1 and bool(activity.get_meta("activity_running", false)):
			_fail("activity skipped a committed occurrence")
			return false
	var pulse := main.find_child("EventActivityPulse", true, false) as Control
	if pulse == null or pulse.mouse_filter != Control.MOUSE_FILTER_IGNORE:
		_fail("activity cues intercept table input")
		return false
	return true


func _prior_phase_is_retained() -> bool:
	if not bool(main.get_meta("response_staged", false)) or _button_named("End turn") != null:
		_fail("the next phase decision appeared during prior consequences")
		return false
	if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != str(main.get_meta("phase_prior_revision")):
		_fail("the prior authorized table revision changed during a phase beat")
		return false
	return true


func _static_results_are_visible(activity: Control) -> bool:
	var caption := _timeline().find_child("TimelineCaption", true, false) as Label
	if caption == null or caption.text.is_empty() or not _control_is_fully_visible(caption):
		_fail("reduced motion lost the persistent timeline result")
		return false
	if bool(activity.get_meta("activity_running", true)):
		_fail("reduced motion delayed accepted consequences")
		return false
	var pulse := main.find_child("EventActivityPulse", true, false) as Control
	if pulse == null or int(pulse.get_meta("visible_result_count", 0)) != 0:
		_fail("reduced motion retained old consequences over the next decision")
		return false
	return true


func _timeline() -> Control:
	return main.find_child("EventTimeline", true, false) as Control


func _sync_clears_activity() -> bool:
	var sync := _node("Toolbar/Synchronize") as Button
	if not await _pointer_activate(sync): return false
	if not await _wait_for(func() -> bool:
		var activity := _activity()
		return activity == null or not activity.is_visible_in_tree()):
		_fail("synchronization replayed or retained stale activity")
		return false
	return true


func _complete_history_is_reachable() -> bool:
	var latest := main.find_child("LatestResult", true, false) as Label
	if latest == null or latest.is_visible_in_tree():
		_fail("collapsed history duplicates the live activity feed")
		return false
	if not await _set_history_drawer(true): return false
	var log := _node("Play/Prompt/Margin/Stack/Workbench/History/HistoryOccurrences") as Control
	var account := str(log.get_meta("chronology", ""))
	if not log.is_visible_in_tree() or "Bomb Scare" not in account or "Hard to Keep Down" not in account or "threat" not in account:
		_fail("history lost the ordered encounter/boost/consequence account")
		return false
	return await _set_history_drawer(false)


func _legal_enemy_attack_is_causal() -> bool:
	if not await _open_hero_attack_probe(): return false
	if not await _end_hero_turn_for_attack(): return false
	var defended := false
	for step in range(8):
		if _attack_defense_prompt_is_visible():
			if not await _declare_probe_defender(): return false
			defended = true
		var result := await _watch_completed_probe_attack(defended)
		if result < 0: return false
		if result > 0:
			main.queue_free()
			await process_frame
			return true
		if not await _decline_probe_attack_opportunity(): return false
	_fail("legal attack probe did not reach its completed attack")
	return false


func _open_hero_attack_probe() -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _task_commit() != null): return false
	if not await _commit_once("Keep hand"): return false
	if not await _wait_for(func(): return int((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) == 1 and not bool(main.get_meta("response_staged", false))):
		_fail("initial Keep hand did not settle before form-change drafting")
		return false
	if not await _select_attached_action(IDENTITY, "Change Form"): return false
	await process_frame
	await process_frame
	if not await _commit_once("Change Form"): return false
	if not await _wait_for(func(): return not bool(main.get_meta("response_staged", false)) and _button_named("End turn") != null):
		_fail("the exact offered form-change commitment did not settle")
		return false
	return true


func _end_hero_turn_for_attack() -> bool:
	if not await _wait_for(func(): return _button_named("End turn") != null and not bool(main.get_meta("response_staged", false))):
		_fail("hero form did not settle into its next ordinary action prompt")
		return false
	if not await _pointer_activate(_button_named("End turn")): return false
	if not await _wait_for(func(): return _button_named("Keep hand") != null):
		_fail("the hero attack journey lacks its required end-phase choice")
		return false
	if not await _stage_required_hero_discard(): return false
	return await _wait_for(func(): return not bool(main.get_meta("response_staged", false)) and not _status().text.begins_with("DECISION SENT"))

func _stage_required_hero_discard() -> bool:
	var before := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	var card := _mulligan_card("Avengers Mansion")
	if card == null:
		_fail("the seeded hero end-phase choice lacks Avengers Mansion")
		return false
	var anchor := int(str(card.name).trim_prefix("ProceduralCard"))
	if not await _choose_target(anchor):
		_fail("the seeded hero end-phase choice lacks offered Avengers Mansion")
		return false
	var submit := _task_commit()
	if submit == null or submit.disabled or submit.text != "Discard 1":
		_fail("the required hero discard did not stage exactly one offered card")
		return false
	if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != before:
		_fail("staging the required discard changed the authoritative revision")
		return false
	return await _pointer_activate(submit)

func _attack_defense_prompt_is_visible() -> bool:
	var heading := main.find_child("ContextualHeading", true, false) as Label
	return heading != null and "defender" in heading.text.to_lower()


func _declare_probe_defender() -> bool:
	var situation := main.find_child("CausalSituation", true, false) as Control
	var meaning := _visible_text(situation).to_lower()
	if "rhino" not in meaning or "spider-man" not in meaning or "boost" not in meaning:
		_fail("defense lost source, target or unresolved boost")
		return false
	if not await _capture_checkpoint("activity-defense-context"): return false
	if not await preload("res://smoke/inspection_context_checks.gd").defend(self): return false
	if not await _select_attached_action(IDENTITY, "Defend"): return false
	if not await _commit_once("Defend"): return false
	return true


func _watch_completed_probe_attack(defended: bool) -> int:
	var activity := _activity()
	if activity == null: return 0
	while true:
		var tile := _probe_attack_result_tile(activity)
		if tile != null:
			if not _completed_probe_attack_roles_are_valid(tile, defended): return -1
			if not await _capture_checkpoint("activity-enemy-attack-result"): return -1
			return 1
		if not bool(activity.get_meta("activity_running", false)):
			var retained := await _retained_probe_attack()
			if retained == null: return 0
			if not _completed_probe_attack_roles_are_valid(retained, defended): return -1
			if not await _capture_checkpoint("activity-enemy-attack-retained"): return -1
			return 1
		await process_frame
	return 0


func _probe_attack_result_tile(_activity_host: Control) -> Control:
	var caption := _timeline().find_child("TimelineCaption", true, false) as Label
	if caption != null and "Rhino's attack on Spider-Man ended." in caption.text:
		return caption
	return null


func _retained_probe_attack() -> Control:
	if not await _expand_activity_timeline(): return null
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	var prompt := _logical_prompt_identity()
	var timeline := _timeline()
	if not await _pointer_activate(_timeline_button("Latest event")): return null
	for _event in range(int(timeline.get_meta("timeline_count", 0))):
		var caption := _probe_attack_result_tile(_activity())
		if caption != null:
			if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision or _logical_prompt_identity() != prompt:
				_fail("timeline navigation changed the engine decision")
				return null
			return caption
		if int(timeline.get_meta("timeline_selected", 0)) == 0: break
		if not await _pointer_activate(_timeline_button("Previous event")): return null
	if not await _pointer_activate(_timeline_button("Latest event")): return null
	return null


func _timeline_button(action: String) -> Button:
	for button in _timeline().find_children("*", "Button", true, false):
		if button.tooltip_text == action: return button as Button
	return null


func _completed_probe_attack_roles_are_valid(caption: Control, defended: bool) -> bool:
	var text := (caption as Label).text
	if not defended or "Spider-Man defended." not in text or "Attack damage before prevention:" not in text:
		_fail("completed attack lost its attacker or affected character")
		return false
	return true


func _decline_probe_attack_opportunity() -> bool:
	var decline := main.find_child("WindowPass", true, false) as Button
	if decline == null: decline = main.find_child("ContextualDecline", true, false) as Button
	if decline == null or not decline.is_visible_in_tree():
		await _capture_checkpoint("activity-enemy-pending")
		print("ATTACK_PROBE_CONTEXT ", _visible_text(main.find_child("CausalSituation", true, false)), " choices=", _visible_buttons(main).map(func(button): return button.text))
		_fail("legal attack probe has no explicit optional response")
		return false
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	if not await _pointer_activate(decline): return false
	if not await _wait_for(func() -> bool: return str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision): return false
	return true


func _historical_encounter_face_is_inspectable() -> bool:
	if not await _expand_activity_timeline(): return false
	if not await _pointer_activate(_timeline_button("Latest event")): return false
	for index in range(int(_timeline().get_meta("timeline_count", 0))):
		var references := _timeline().find_child("ActivityReference", true, false)
		for chip in references.get_children():
			if chip is Button and "Hard to Keep Down" in chip.text:
				chip.grab_focus()
				await process_frame
				await process_frame
				var inspector := main.find_child("CardInspector", true, false) as Control
				if not inspector.is_visible_in_tree() or "Hard to Keep Down" not in _visible_text(inspector):
					_fail("discarded boost face cannot be inspected from its occurrence")
					return false
				if not await _capture_checkpoint("historical-boost-reference"): return false
				chip.release_focus()
				return await _set_history_drawer(false)
		if not await _pointer_activate(_timeline_button("Previous event")): return false
	_fail("chronology has no authorized reference for the resolved boost")
	return false


func _expand_activity_timeline() -> bool:
	var disclosure := _timeline().find_child("TimelineDisclosure", true, false) as Button
	if disclosure == null or bool(_timeline().get_meta("timeline_expanded", false)): return true
	return await _pointer_activate(disclosure)
