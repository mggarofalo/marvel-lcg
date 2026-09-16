extends "res://smoke/local_game_smoke_terminal_checks.gd"


func _active_resolution_is_safe(state: Dictionary) -> bool:
	var active := _node("Play/Prompt/Margin/Stack/ActiveResolution") as Control
	if not active.visible:
		return await _hidden_resolution_is_safe(state)
	var text := _visible_text(active).to_lower()
	if text.strip_edges() == "current resolution":
		_fail("the empty current-resolution panel is visible")
		return false
	if "resolving card" in text:
		return await _card_resolution_is_safe(active, text)
	if "enemy attack" not in text or "interrupt window" not in text:
		return true
	return await _attack_resolution_is_safe(state, text)


func _hidden_resolution_is_safe(state: Dictionary) -> bool:
	var contextual := main.find_child("ContextualDecision", true, false) as Control
	var text := _visible_text(contextual).to_lower() if contextual != null else ""
	if "enemy attack" not in text and "defend" not in text and "defense" not in text:
		return true
	state.saw_attack_resolution = true
	if not await _capture_checkpoint("attack-interrupt"): return false
	return await _capture_villain_phase_once(state)


func _card_resolution_is_safe(active: Control, text: String) -> bool:
	var context_cards := active.find_children("ProceduralCard*", "", true, false)
	if context_cards.is_empty() or "click the card to inspect" not in text:
		_fail("the card resolution does not keep an inspectable causal card visible")
		return false
	return await _capture_checkpoint("card-interrupt")


func _attack_resolution_is_safe(state: Dictionary, text: String) -> bool:
	state.saw_attack_resolution = true
	if "rhino" not in text or "spider-man" not in text:
		_fail("the attack resolution does not name its actor and target")
		return false
	if not await _capture_checkpoint("attack-interrupt"): return false
	return await _capture_villain_phase_once(state)


func _capture_villain_phase_once(state: Dictionary) -> bool:
	if state.captured_villain_phase: return true
	if not await _capture_checkpoint("villain-phase"): return false
	state.captured_villain_phase = true
	return true


func _villain_history_checkpoint_is_safe(state: Dictionary) -> bool:
	if state.captured_villain_phase or _is_complete():
		return true
	var history := (_node(
		"Play/Prompt/Margin/Stack/Workbench/History/EventLog") as RichTextLabel) \
		.get_parsed_text().to_lower()
	if "villain phase" not in history:
		return true
	if not await _capture_checkpoint("villain-phase"):
		return false
	state.captured_villain_phase = true
	return true
