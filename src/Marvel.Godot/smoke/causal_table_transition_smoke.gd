extends "res://smoke/local_game_smoke.gd"

# Seeded two-player visible-control journey; capture real intermediate native surfaces.
var saw_form := false
var arrivals := {}
var sampled := {}
var sample_elapsed := 0.0
var saw_spent_prompt_hidden := false

func _initialize() -> void:
	OS.set_environment("MARVEL_SMOKE_TWO_PLAYER", "true")
	OS.set_environment("MARVEL_SMOKE_MOTION", "enabled")
	super._initialize()

func _process(delta: float) -> bool:
	sample_elapsed += delta
	if sample_elapsed < 0.1 or not is_instance_valid(main): return false
	sample_elapsed = 0.0
	if not _spent_prompt_is_hidden(): return false
	for card in main.find_children("*", "Control", true, false):
		if not card.has_meta("activity_face") and not card.has_meta("activity_table_arrival"): continue
		if not _causal_card_is_native(card): return false
		_capture_causal_card(card)
	return false

func _spent_prompt_is_hidden() -> bool:
	if not bool(main.get_meta("response_staged", false)): return true
	var situation := main.find_child("CausalSituation", true, false)
	if situation == null: return true
	for copy in situation.get_children():
		if copy.name == "SpatialOperationalNotice": continue
		if copy is Control and copy.is_visible_in_tree():
			_fail("spent prompt copy survived into result playback")
			return false
		saw_spent_prompt_hidden = true
	return true

func _causal_card_is_native(card: Control) -> bool:
	if not card.scale.is_equal_approx(Vector2.ONE):
		_fail("causal card transition scaled native text")
		return false
	if card.get_meta("activity_face", "") == "01001a": saw_form = true
	if not card.has_meta("activity_table_arrival"): return true
	var body := card.get_node_or_null("SourceBody")
	if body == null: return _full_arrival_identity_is_intact(card)
	var expected := str(card.get_meta("source_title", ""))
	var title := body.find_child("SourceTitle", true, false) as Label
	if title == null or title.text != expected:
		_fail("host fact refresh changed the arriving attachment's identity")
		return false
	arrivals[expected] = true
	return true

func _full_arrival_identity_is_intact(card: Control) -> bool:
	var title := card.get_node_or_null("CardSurface/CardFace/Title") as Label
	if title == null or title.text != str(card.get_meta("activity_title", "")):
		_fail("arriving full card lost its authorized identity")
		return false
	return true

func _capture_causal_card(card: Control) -> void:
	var key := str(card.get_instance_id())
	if not sampled.has(key):
		sampled[key] = 0
		return
	if sampled[key] != 0: return
	sampled[key] = 1
	var folder := OS.get_environment("MARVEL_SMOKE_CAPTURE_DIR")
	if not folder.is_empty():
		render_viewport.get_texture().get_image().save_png(folder.path_join("causal-transition-%d.png" % sampled.size()))

func _settle_decision(decisions: int, state: Dictionary) -> bool:
	# The generic toggle check deliberately skips the first beat; this probe observes it.
	state.tested_active_motion_toggle = true
	if not await super._settle_decision(decisions, state): return false
	if decisions != 8: return true
	if not saw_spent_prompt_hidden or not saw_form or not arrivals.has("Charge") or not arrivals.has("Armored Rhino Suit"):
		_fail("causal form/attachment surfaces were absent: form=%s arrivals=%s" % [saw_form, arrivals])
		return false
	var heading := main.find_child("ContextualHeading", true, false) as Label
	if heading != null and not heading.text.is_empty() and not heading.is_visible_in_tree():
		_fail("current pending prompt copy was not restored after playback")
		return false
	var pulse := main.find_child("AttackActivityPulse", true, false)
	if pulse != null and int(pulse.get_meta("visible_result_count", 0)) != 0:
		_fail("completed attack remained active after its later encounters")
		return false
	print("CAUSAL_TABLE_TRANSITION_OK form=true attachments=2 native_text_scale=1")
	quit(0)
	return false
