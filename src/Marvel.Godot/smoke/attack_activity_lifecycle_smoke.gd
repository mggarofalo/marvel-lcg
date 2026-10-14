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
	if not _causal_copy_is_readable(): return false
	var meaning := _visible_text(main.find_child("CausalSituation", true, false)).to_lower()
	if "rhino" not in meaning or "spider-man" not in meaning or "boost" not in meaning:
		_fail("attack source, target and unresolved boost missing before defense")
		return false
	if not _attack_cue_is_present("before choosing a defender"): return false
	if not await _capture_checkpoint("attack-01-before-defense"): return false
	if not await _select_attached_action(IDENTITY, "Defend"): return false
	if not _attack_cue_is_present("defender selected but not committed"): return false
	if not await _capture_checkpoint("attack-02-defense-draft"): return false
	attack_revision = str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	if not await _pointer_activate(_task_commit()): return false
	return await _wait_for(func(): return bool(main.get_meta("response_staged", false)) or str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision)

func _causal_copy_is_readable() -> bool:
	var cause := main.find_child("CausalSituation", true, false) as Control
	for label in cause.find_children("*", "Label", true, false):
		if label.get_theme_font_size("font_size") < 15 or not _control_is_fully_visible(label):
			_fail("current attack explanation is too small or clipped")
			return false
	var references := main.find_child("CausalReferenceStack", true, false) as ScrollContainer
	if references != null and references.get_v_scroll_bar().visible:
		_fail("ordinary attack references require a scrolling box")
		return false
	return true

func _attack_cue_is_present(stage: String) -> bool:
	var pulse := main.find_child("AttackActivityPulse", true, false) as Control
	if pulse == null or not pulse.is_visible_in_tree() or int(pulse.get_meta("visible_result_count", 0)) < 2:
		_fail("persistent attack source/target cue missing: %s" % stage)
		return false
	if pulse.mouse_filter != Control.MOUSE_FILTER_IGNORE:
		_fail("persistent attack cue intercepts the decision")
		return false
	return true

func _sync_preserves_initiated_attack() -> bool:
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	var prompt := _logical_prompt_identity()
	if not await _pointer_activate(_node("Toolbar/Synchronize") as Button): return false
	if not await _wait_for(func(): return not (_node("Toolbar/Synchronize") as Button).disabled): return false
	if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision or _logical_prompt_identity() != prompt:
		_fail("synchronizing an initiated attack changed its revision or decision")
		return false
	if bool(main.get_meta("response_staged", false)) or bool(_timeline().get_meta("activity_running", false)):
		_fail("synchronization replayed the attack introduction")
		return false
	if not _attack_cue_is_present("initiated attack after synchronization"): return false
	return await _capture_attack_frame("attack-00-synchronized-initiation")

func _observe_attack_initiation() -> int:
	if not _attack_cue_is_present("Spider-Sense initiation before defense"): return -1
	var meaning := _visible_text(main).to_lower()
	if not saw_attack_intro and ("enemy attack · initiation" not in meaning or "spider-man draws 1 card" not in meaning):
		_fail("attack initiation probe did not reach Spider-Sense")
		return -1
	if not saw_attack_intro and not await _sync_preserves_initiated_attack(): return -1
	saw_attack_intro = true
	if not await _capture_attack_frame("attack-00-spider-sense"): return -1
	return 0

func _watch_completed_probe_attack(defended: bool) -> int:
	if not defended: return await _observe_attack_initiation()
	var captured_consequences := false
	var finished := false
	while bool(main.get_meta("response_staged", false)):
		var activity := main.find_child("EventActivity", true, false)
		var boundary := str(activity.get_meta("activity_attack_boundary", "")) if activity != null else ""
		finished = _boundary_finished(boundary, finished)
		if not _attack_lifetime_matches(finished): return -1
		if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != attack_revision:
			_fail("final table appeared before its accepted consequences")
			return -1
		if not captured_consequences:
			captured_consequences = true
			if not await _capture_attack_frame("attack-03-consequences"): return -1
		await process_frame
	if motion_enabled and not captured_consequences:
		_fail("animated attack probe never observed staged consequences")
		return -1
	return await _verify_settled_attack()

func _boundary_finished(boundary: String, prior: bool) -> bool:
	if boundary == "finished": return true
	if boundary == "started": return false
	return prior

func _attack_lifetime_matches(finished: bool) -> bool:
	if not finished: return _attack_cue_is_present("boost/damage consequence playback")
	var pulse := main.find_child("AttackActivityPulse", true, false)
	if pulse != null and int(pulse.get_meta("visible_result_count", 0)) != 0:
		_fail("attack emphasis outlived its engine-owned final resolution")
		return false
	return true

func _verify_settled_attack() -> int:
	if not saw_attack_intro: return 0
	await process_frame
	var pulse := main.find_child("AttackActivityPulse", true, false) as Control
	if pulse == null or int(pulse.get_meta("visible_result_count", 0)) != 0:
		_fail("completed attack still paints over the next decision")
		return -1
	if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) == attack_revision:
		_fail("attack playback did not apply its accepted revision")
		return -1
	if not await _capture_checkpoint("attack-04-next-decision"): return -1
	return 1

func _decline_probe_attack_opportunity() -> bool:
	if not await _wait_for(func(): return _visible_attack_pass() != null):
		_fail("attack has no explicit optional-window pass")
		return false
	var decline := _visible_attack_pass()
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	if not await _pointer_activate(decline): return false
	if not await _wait_for(func(): return bool(main.get_meta("response_staged", false)) or str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision): return false
	while bool(main.get_meta("response_staged", false)):
		if not _attack_cue_is_present("interrupt response consequences"): return false
		await process_frame
	return _attack_cue_is_present("after interrupt and before defense")

func _visible_attack_pass() -> Button:
	for candidate in main.find_children("*", "Button", true, false):
		if candidate.name in ["WindowPass", "ContextualDecline"] \
				and candidate.is_visible_in_tree() and not candidate.is_queued_for_deletion():
			return candidate as Button
	return null

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
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	var initial_health := _basic_attack_target_health()
	if initial_health.is_empty(): return false
	if not await _pointer_activate(_task_commit()): return false
	if not motion_enabled:
		if not await _wait_for(func(): return str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision): return false
		return await _verify_basic_attack_settled(main.find_child("AttackActivityPulse", true, false) as Control)
	if not await _wait_for(func(): return bool(main.get_meta("response_staged", false))): return false
	var pulse := main.find_child("AttackActivityPulse", true, false) as Control
	if not await _await_basic_attack_onset(initial_health, revision): return false
	if not _basic_attack_onset_is_visible(pulse): return false
	if not await _capture_attack_frame("basic-attack-02-introduction"): return false
	if not await _observe_basic_attack_through_finish(initial_health, revision): return false
	return await _verify_basic_attack_settled(pulse)

func _basic_attack_target_health() -> String:
	var value := _card_for_anchor(RHINO).find_child("LiveProgressValue", true, false) as Label
	if value == null:
		_fail("basic attack target has no visible health baseline")
		return ""
	return value.text

func _basic_attack_boundary() -> String:
	return str(main.find_child("EventActivity", true, false).get_meta("activity_attack_boundary", ""))

func _await_basic_attack_onset(initial_health: String, revision: String) -> bool:
	# An offered attack pays exhaustion before its engine-owned initiation boundary.
	# Cost presentation may precede the arrow; target damage may not.
	var started := Time.get_ticks_msec()
	while bool(main.get_meta("response_staged", false)) and Time.get_ticks_msec() - started < TIMEOUT_MILLISECONDS:
		if _basic_attack_target_health() != initial_health:
			_fail("basic attack changed target health before its source/target introduction")
			return false
		if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision:
			_fail("basic attack showed final state before source/target transition")
			return false
		if _basic_attack_boundary() == "started": return true
		if _basic_attack_boundary() == "finished": break
		await process_frame
	_fail("basic attack never exposed its accepted initiation boundary")
	return false

func _basic_attack_onset_is_visible(pulse: Control) -> bool:
	var caption := _timeline().find_child("TimelineCaption", true, false) as Label
	if not caption.text.begins_with("Spider-Man → Rhino") or int(pulse.get_meta("visible_result_count", 0)) < 2:
		_fail("basic attack did not show accepted source and target before damage")
		return false
	if "Spider-Man" not in pulse.accessibility_name or "Rhino" not in pulse.accessibility_name:
		_fail("basic attack introduction names different source or target objects")
		return false
	return true

func _observe_basic_attack_through_finish(initial_health: String, revision: String) -> bool:
	var finished := false
	var saw_damage := false
	var started := Time.get_ticks_msec()
	while bool(main.get_meta("response_staged", false)) and Time.get_ticks_msec() - started < TIMEOUT_MILLISECONDS:
		finished = _boundary_finished(_basic_attack_boundary(), finished)
		if not _attack_lifetime_matches(finished): return false
		if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision:
			_fail("basic attack showed final state before its accepted consequences")
			return false
		if not saw_damage and _basic_attack_target_health() != initial_health:
			saw_damage = true
			if not await _capture_attack_frame("basic-attack-03-damage"): return false
		await process_frame
	return _basic_attack_completion_was_observed(saw_damage, finished)

func _basic_attack_completion_was_observed(saw_damage: bool, finished: bool) -> bool:
	if bool(main.get_meta("response_staged", false)) or not saw_damage or not finished:
		_fail("basic attack never exposed damage followed by its exact final boundary")
		return false
	return true

func _verify_basic_attack_settled(pulse: Control) -> bool:
	if not await _wait_for(func(): return not bool(main.get_meta("response_staged", false))): return false
	await process_frame
	if int(pulse.get_meta("visible_result_count", 0)) != 0:
		_fail("basic attack aftermath remains on next decision")
		return false
	return await _capture_checkpoint("basic-attack-04-next-decision")
