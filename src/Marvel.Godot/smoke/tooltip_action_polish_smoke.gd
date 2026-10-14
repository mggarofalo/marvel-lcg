extends "res://smoke/local_game_smoke.gd"

func _run() -> void:
	if not await _tooltip_authoring(): return
	if not await _open_tooltip_game(): return
	if not await _disclosure_and_status(): return
	if not await _action_hint(): return
	if not await _stat_hover(): return
	print("TOOLTIP_ACTION_POLISH_OK clipped=true readable=true icon_hover=true full_no_repeat=true pointer_keyboard=true stable_geometry=true revision_metadata=true")
	if not await _release_table_resources(): return
	quit(0)


func _open_tooltip_game() -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _task_commit() != null): return false
	return true

func _disclosure_and_status() -> bool:
	if (main.find_child("CompleteChoiceSheet", true, false) as Button).text != "All choices":
		_fail("mulligan disclosure is not named All choices")
		return false
	if not await _commit_once("Keep hand"): return false
	if (main.find_child("CompleteChoiceSheet", true, false) as Button).text != "All actions":
		_fail("ordinary turn disclosure is not named All actions")
		return false
	var sync := _node("Toolbar/SyncStatus") as Label
	if sync.text != "Synced" or not sync.tooltip_text.is_empty() or int(sync.get_meta("response_revision", -1)) < 1:
		_fail("sync chrome exposes diagnostics or lacks internal revision evidence")
		return false
	return true

func _tooltip_authoring() -> bool:
	var fixture := (load("res://smoke/TooltipPolicyProbe.cs") as CSharpScript).new() as Control
	render_viewport.add_child(fixture)
	await process_frame
	var clipped := fixture.get_node("ClippedReference") as Button
	var readable := fixture.get_node("ReadableReference") as Button
	var inspection := fixture.get_node("PinInspection") as Button
	clipped.size.x = 120
	await process_frame
	if clipped.tooltip_text != clipped.text or not readable.tooltip_text.is_empty() \
			or inspection.tooltip_text != "Click or press Enter to pin card details." \
			or inspection.accessibility_name != "Inspect Rhino" or readable.accessibility_name.is_empty():
		_fail("authored tooltip policy lost clipping detail, accessibility or pinning distinction: %s / %s / %s / %s / %s" % [clipped.tooltip_text, readable.tooltip_text, inspection.tooltip_text, inspection.accessibility_name, readable.accessibility_name])
		return false
	clipped.size.x = 800
	await process_frame
	if not clipped.tooltip_text.is_empty():
		_fail("full disabled reference caption retained a title-restatement tooltip")
		return false
	fixture.queue_free()
	await process_frame
	return true

func _action_hint() -> bool:
	var action := _attached(_attached_name(IDENTITY, "Action"))
	var hint := action.find_child("ActionVerbHint", false, false) as Control
	var bounds := action.get_global_rect()
	var minimum := action.get_combined_minimum_size()
	var point := bounds.get_center()
	_position_pointer_without_settle(point)
	await process_frame
	if hint == null or not hint.is_visible_in_tree() or action.accessibility_name not in _visible_text(hint):
		_fail("compact action arrow has no immediate contextual action label on hover")
		return false
	if not await _capture_checkpoint("action-verb-hover"): return false
	_position_pointer_without_settle(Vector2(4, 4))
	action.grab_focus()
	await process_frame
	if not hint.is_visible_in_tree() or action.get_global_rect() != bounds or action.get_combined_minimum_size() != minimum:
		_fail("keyboard action hint is absent or changes the compact control's hit geometry")
		return false
	if not await _capture_checkpoint("action-verb-focus"): return false
	action.release_focus()
	await process_frame
	if hint.visible:
		_fail("contextual action label survived pointer and keyboard focus dismissal")
		return false
	return true

func _stat_hover() -> bool:
	var card := _card_for_anchor(IDENTITY)
	var stat := card.find_child("StatREC", true, false) as Control
	if stat == null or stat.tooltip_text.is_empty() or stat.accessibility_name.is_empty():
		_fail("compact stat icon lost its decoding tooltip or accessible name")
		return false
	_position_pointer_without_settle(stat.get_global_rect().get_center())
	await process_frame
	if render_viewport.gui_get_hovered_control() != stat:
		_fail("compact stat decoding tooltip has no native hit area")
		return false
	if not await _pointer_activate_card_body(card): return false
	var inspector := main.get_node("CardInspector") as Control
	var full_stat := inspector.find_child("StatREC", true, false) as Control
	if full_stat == null or not full_stat.tooltip_text.is_empty() or full_stat.accessibility_name.is_empty():
		_fail("full captioned stat repeats its value as a tooltip or lost accessibility")
		return false
	return await _capture_checkpoint("full-stat-tooltip-policy")
