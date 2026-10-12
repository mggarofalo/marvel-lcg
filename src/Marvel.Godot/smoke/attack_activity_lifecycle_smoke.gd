extends "res://smoke/event_activity_smoke.gd"

var saw_attack_intro := false
var attack_revision := ""

func _run() -> void:
	if not await _legal_enemy_attack_is_causal(): return
	if not saw_attack_intro:
		_fail("no source/target introduction before attack consequences")
		return
	if not await _basic_attack_stages(): return
	print("ATTACK_ACTIVITY_LIFECYCLE_OK before=true during=true next_decision_clear=true")
	quit(0)

func _declare_probe_defender() -> bool:
	var meaning := _visible_text(main.find_child("CausalSituation", true, false)).to_lower()
	if "rhino" not in meaning or "spider-man" not in meaning or "boost" not in meaning:
		_fail("attack source, target and unresolved boost missing before defense")
		return false
	if not await _capture_checkpoint("attack-01-before-defense"): return false
	if not await _select_attached_action(IDENTITY, "Defend"): return false
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if not await _pointer_activate(_task_commit()): return false
	return await _wait_for(func(): return bool(main.get_meta("response_staged", false)) or (_node("Toolbar/SyncStatus") as Label).text != revision)

func _watch_completed_probe_attack(defended: bool) -> int:
	if not defended: return 0
	var pulse := main.find_child("EventActivityPulse", true, false) as Control
	var captured_damage := false
	while bool(main.get_meta("response_staged", false)):
		var caption := _timeline().find_child("TimelineCaption", true, false) as Label
		if not saw_attack_intro and caption != null and caption.text == "Rhino → Spider-Man":
			if int(pulse.get_meta("visible_result_count", 0)) < 2:
				_fail("attack introduction omitted board source/target cues")
				return -1
			saw_attack_intro = true
			attack_revision = (_node("Toolbar/SyncStatus") as Label).text
			if not await _capture_attack_frame("attack-02-introduction"): return -1
		elif saw_attack_intro and not captured_damage and int(pulse.get_meta("visible_result_count", 0)) > 0:
			if (_node("Toolbar/SyncStatus") as Label).text != attack_revision:
				_fail("final table appeared before its accepted consequences")
				return -1
			captured_damage = true
			if not await _capture_attack_frame("attack-03-consequences"): return -1
		await process_frame
	return await _verify_settled_attack(pulse)

func _verify_settled_attack(pulse: Control) -> int:
	if not saw_attack_intro: return 0
	await process_frame
	if int(pulse.get_meta("visible_result_count", 0)) != 0:
		_fail("completed attack still paints over the next decision")
		return -1
	if (_node("Toolbar/SyncStatus") as Label).text == attack_revision:
		_fail("attack playback did not apply its accepted revision")
		return -1
	if not await _capture_checkpoint("attack-04-next-decision"): return -1
	return 1

func _decline_probe_attack_opportunity() -> bool:
	var decline := main.find_child("WindowPass", true, false) as Button
	if decline == null: decline = main.find_child("ContextualDecline", true, false) as Button
	if decline == null or not decline.is_visible_in_tree():
		_fail("attack has no explicit optional-window pass")
		return false
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if not await _pointer_activate(decline): return false
	return await _wait_for(func(): return bool(main.get_meta("response_staged", false)) or (_node("Toolbar/SyncStatus") as Label).text != revision)

func _capture_attack_frame(checkpoint: String) -> bool:
	var folder := OS.get_environment("MARVEL_SMOKE_CAPTURE_DIR")
	if folder.is_empty(): return true
	await RenderingServer.frame_post_draw
	return _save_checkpoint_image(checkpoint, folder, render_viewport.get_texture().get_image())

func _basic_attack_stages() -> bool:
	if not await _prepare_basic_attack(): return false
	return await _observe_basic_attack()

func _prepare_basic_attack() -> bool:
	if not await _open_hero_attack_probe(): return false
	if not await _select_attached_action(IDENTITY, "Attack"): return false
	if not await _choose_target(RHINO): return false
	if not await _capture_checkpoint("basic-attack-01-before"): return false
	return true

func _observe_basic_attack() -> bool:
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if not await _pointer_activate(_task_commit()): return false
	if not await _wait_for(func(): return bool(main.get_meta("response_staged", false))): return false
	var caption := _timeline().find_child("TimelineCaption", true, false) as Label
	var pulse := main.find_child("EventActivityPulse", true, false) as Control
	if caption.text != "Spider-Man → Rhino" or int(pulse.get_meta("visible_result_count", 0)) < 2:
		_fail("basic attack did not show accepted source and target before damage")
		return false
	if (_node("Toolbar/SyncStatus") as Label).text != revision:
		_fail("basic attack showed final state before source/target transition")
		return false
	if not await _capture_attack_frame("basic-attack-02-introduction"): return false
	if not await _wait_for(func(): return caption.text != "Spider-Man → Rhino"): return false
	if not await _capture_attack_frame("basic-attack-03-damage"): return false
	return await _verify_basic_attack_settled(pulse)

func _verify_basic_attack_settled(pulse: Control) -> bool:
	if not await _wait_for(func(): return not bool(main.get_meta("response_staged", false))): return false
	await process_frame
	if int(pulse.get_meta("visible_result_count", 0)) != 0:
		_fail("basic attack aftermath remains on next decision")
		return false
	return await _capture_checkpoint("basic-attack-04-next-decision")
