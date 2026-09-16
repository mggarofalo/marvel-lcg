extends "res://smoke/local_game_smoke_lifecycle_checks.gd"

const REDESIGN_GATE_ENV := "MARVEL_REDESIGN_GATE"


func _redesign_gate_enabled() -> bool:
	return OS.get_environment(REDESIGN_GATE_ENV) == "true"


func _redesign_gate_table_interactions_are_safe(operation: Callable) -> bool:
	if not await operation.call():
		return false
	return await _redesign_gate_opening_table_is_safe()


func _redesign_gate_motion_state_is_safe(operation: Callable, state: Dictionary) -> bool:
	if not await operation.call(state):
		return false
	if not _redesign_gate_enabled() or not state.saw_end_phase \
			or state.get("redesign_checked_after_end", false):
		return true
	state.redesign_checked_after_end = true
	return await _redesign_gate_after_decision_is_safe()


func _redesign_gate_opening_table_is_safe() -> bool:
	if not _redesign_gate_enabled():
		return true
	if OS.get_environment("MARVEL_SMOKE_VIEWPORT") != "1920x1080":
		_fail("the redesign gate only admits the 1920x1080 desktop contract")
		return false
	if not await _capture_checkpoint("redesign-opening-mulligan"):
		return false
	if not _redesign_gate_shell_uses_desktop_width("opening mulligan"):
		return false
	if not _redesign_gate_required_mulligan_controls_are_visible():
		return false
	if not _redesign_gate_has_no_scrollbar_chrome("opening mulligan"):
		return false
	return true


func _redesign_gate_post_mulligan_is_safe() -> bool:
	if not _redesign_gate_enabled():
		return true
	if not await _redesign_gate_shell_settles("post-mulligan actions"):
		return false
	if not await _capture_checkpoint("redesign-post-mulligan-actions"):
		return false
	if not _redesign_gate_shell_uses_desktop_width("post-mulligan actions"):
		return false
	if not _redesign_gate_required_action_controls_are_visible():
		return false
	if not _redesign_gate_cards_are_locally_bounded("post-mulligan actions"):
		return false
	if not _redesign_gate_piles_are_compact():
		return false
	if not _redesign_gate_has_no_scrollbar_chrome("post-mulligan actions"):
		return false
	if not await _redesign_gate_inspector_dismissal_is_repeatable(3, true):
		return false
	return true


func _redesign_gate_after_decision_is_safe() -> bool:
	if not await _redesign_gate_shell_settles("after end turn"):
		return false
	if not await _capture_checkpoint("redesign-after-end-turn"):
		return false
	if not _redesign_gate_shell_uses_desktop_width("after end turn"):
		return false
	if not _redesign_gate_has_no_scrollbar_chrome("after end turn"):
		return false
	if not _redesign_gate_cards_are_locally_bounded("after end turn"):
		return false
	if not await _redesign_gate_inspector_dismissal_is_repeatable(2, false):
		return false
	return true


func _redesign_gate_cards_are_locally_bounded(checkpoint: String) -> bool:
	var surface := main.find_child("AstraTableSurface", true, false) as Control
	if surface == null:
		_fail("the %s checkpoint has no Astra table surface" % checkpoint)
		return false
	for node in surface.find_children("ProceduralCard*", "PanelContainer", true, false):
		var card := node as Control
		if card == null or not card.is_visible_in_tree():
			continue
		var maximum_height: float = maxf(300.0, card.custom_minimum_size.y + 4.0)
		if card.size.y > maximum_height:
			var descendants: Array[String] = []
			for child in card.find_children("*", "Control", true, false):
				var control := child as Control
				if control != null and control.get_combined_minimum_size().y > 100.0:
					descendants.append("%s=%s" % [
						control.name,
						control.get_combined_minimum_size(),
					])
			_fail("the %s card %s stretched beyond its local object bounds: size=%s minimum=%s combined=%s" % [
				checkpoint,
				card.name,
				card.size,
				card.custom_minimum_size,
				card.get_combined_minimum_size(),
			] + " descendants=%s" % [descendants])
			return false
	return true


func _redesign_gate_shell_settles(checkpoint: String) -> bool:
	if await _wait_for(func() -> bool:
		var page := main.get_node("Margin") as ScrollContainer
		var shell := main.get_node("Margin/Shell") as Control
		return shell.size.x >= page.size.x - 2.0
	):
		return true
	_fail("the %s shell did not settle to the desktop width" % checkpoint)
	return false


func _redesign_gate_shell_uses_desktop_width(checkpoint: String) -> bool:
	var page := main.get_node("Margin") as ScrollContainer
	var shell := main.get_node("Margin/Shell") as Control
	var page_rect := page.get_global_rect().intersection(Rect2(Vector2.ZERO, _viewport_size()))
	var shell_rect := shell.get_global_rect().intersection(Rect2(Vector2.ZERO, _viewport_size()))
	if shell_rect.size.x >= page_rect.size.x - 2.0:
		return true
	_fail("the %s shell collapsed below the desktop width: shell=%s page=%s minimum=%s" % [
		checkpoint,
		shell_rect,
		page_rect,
		shell.custom_minimum_size,
	])
	return false


func _redesign_gate_required_mulligan_controls_are_visible() -> bool:
	var submit := _attached(_attached_name(IDENTITY, "Submit"))
	var toggles := _hand_surface().find_children(
		"MulliganDiscard*", "Button", true, false)
	if submit == null or submit.disabled or toggles.size() != 6:
		_fail("the opening tabletop does not expose all six mulligan choices and its commit action")
		return false
	for toggle in toggles:
		if not _control_is_fully_visible(toggle as Control):
			_fail("a mulligan choice is outside the fixed desktop viewport: %s" % toggle.name)
			return false
	if not _control_is_fully_visible(submit):
		_fail("the opening-hand card-local execute action is outside the fixed desktop viewport")
		return false
	return true


func _redesign_gate_required_action_controls_are_visible() -> bool:
	var identity_action := _attached(_attached_name(IDENTITY, "Action"))
	var hand_actions := main.find_children("Card*Action", "Button", true, false)
	if identity_action == null or hand_actions.is_empty():
		_fail("the post-mulligan desktop does not expose actions on its card objects")
		return false
	for control in [identity_action, hand_actions.front()]:
		if control == null or not _control_is_fully_visible(control):
			_fail("a required post-mulligan action affordance is outside the fixed desktop viewport")
			return false
	return true


func _redesign_gate_has_no_scrollbar_chrome(checkpoint: String) -> bool:
	for node in main.find_children("*", "ScrollContainer", true, false):
		var scroll := node as ScrollContainer
		if scroll == null or not scroll.is_visible_in_tree():
			continue
		var horizontal := scroll.get_h_scroll_bar()
		var vertical := scroll.get_v_scroll_bar()
		if horizontal != null and horizontal.is_visible_in_tree():
			_fail("the %s surface exposes horizontal scrollbar chrome at %s" % [
				checkpoint,
				scroll.get_path(),
			])
			return false
		if vertical != null and vertical.is_visible_in_tree():
			_fail("the %s surface exposes vertical scrollbar chrome at %s" % [
				checkpoint,
				scroll.get_path(),
			])
			return false
	return true


func _redesign_gate_piles_are_compact() -> bool:
	var saw_discard := false
	for node in main.find_children("Pile*", "PanelContainer", true, false):
		var area := node as Control
		if area == null or "DISCARD" not in _visible_text(area).to_upper():
			continue
		saw_discard = true
		var visible_cards := 0
		for candidate in area.find_children("ProceduralCard*", "", true, false):
			if (candidate as Control).is_visible_in_tree():
				visible_cards += 1
		if visible_cards > 1:
			_fail("discard pile %s expanded %d individual cards into the table" % [
				area.name,
				visible_cards,
			])
			return false
		var inspector_action := area.find_child("InspectPile*", true, false) as Button
		if "EMPTY" in _visible_text(area).to_upper():
			continue
		if inspector_action == null or inspector_action.disabled:
			_fail("discard pile %s has no explicit collection-inspector action" % area.name)
			return false
	if not saw_discard:
		_fail("the post-mulligan table has no recognizable discard pile surface")
		return false
	return true


func _redesign_gate_inspector_dismissal_is_repeatable(
		attempts: int, require_change_form: bool) -> bool:
	for attempt in attempts:
		var hand_card := _current_hand_card()
		if hand_card == null:
			_fail("inspector dismissal attempt %d has no visible hand card" % (attempt + 1))
			return false
		var inspector := await _open_card_inspector(hand_card)
		if inspector == null:
			return false
		var action := _attached(_attached_name(IDENTITY, "Action"))
		if action == null:
			for candidate in main.find_children("Card*Action", "Button", true, false):
				if (candidate as Button).is_visible_in_tree() \
						and not candidate.is_queued_for_deletion() \
						and not inspector.is_ancestor_of(candidate):
					action = candidate as Button
					break
		if action == null:
			_fail("inspector dismissal attempt %d has no current background action" % (attempt + 1))
			return false
		var action_id := action.get_instance_id()
		var backdrop := inspector.get_node("Backdrop") as Control
		var click := InputEventMouseButton.new()
		click.button_index = MOUSE_BUTTON_LEFT
		click.pressed = true
		click.position = backdrop.get_global_rect().position + Vector2(20, 20)
		render_viewport.push_input(click, true)
		var release := InputEventMouseButton.new()
		release.button_index = MOUSE_BUTTON_LEFT
		release.position = click.position
		render_viewport.push_input(release, true)
		await process_frame
		await process_frame
		if inspector.visible:
			_fail("outside click %d did not dismiss the pinned card inspector" % (attempt + 1))
			return false
		var action_replaced := not is_instance_valid(action) \
				or action.get_instance_id() != action_id
		var choices_opened := main.find_child("CardActionChoices", true, false) != null
		if action_replaced or choices_opened:
			_fail(("inspector dismissal attempt %d activated its underlying action " \
					+ "(replaced=%s choices=%s)") % [
					attempt + 1, action_replaced, choices_opened])
			return false
	return true
