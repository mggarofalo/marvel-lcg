extends "res://smoke/local_game_smoke_relationship_checks.gd"

const IDENTITY := 1
const BLACK_CAT := 8
const SPIDER_TRACER := 17
const WEB_SHOOTER := 19
const SECOND_WEB_SHOOTER := 20
const DAREDEVIL := 23
const RHINO := 49

func _direct_table_journey_is_operable() -> bool:
	# The two-player setup consumes the seeded stream differently and validates
	# direct controls through the generic visible-decision journey below.
	if OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true":
		return true
	if not await _direct_web_shooter_is_played():
		_fail("the direct Web-Shooter journey ended without a reported interaction failure")
		return false
	if not await _direct_black_cat_is_played():
		_fail("the direct Black Cat journey ended without a reported interaction failure")
		return false
	if not await _direct_change_form_is_played():
		_fail("the direct change-form journey ended without a reported interaction failure")
		return false
	if not await _direct_attacks_are_played():
		_fail("the direct attack journey ended without a reported interaction failure")
		return false
	return true


func _direct_web_shooter_is_played() -> bool:
	var draft := await _draft_web_shooter()
	if draft == null:
		_fail("the Web-Shooter draft did not reach its selected-action relationship probe")
		return false
	# Selecting the exact play affordance rebuilds the board. Probe the current
	# card object, not the pre-draft node that is leaving the scene tree.
	var card := _card_for_anchor(WEB_SHOOTER)
	if card == null:
		_fail("the drafted Web-Shooter has no canonical card surface for relationship inspection")
		return false
	if not await _relationship_path_tracks_table_scrolling(card):
		return false
	return await _complete_web_shooter_play()


func _draft_web_shooter() -> Control:
	var web_shooters := _visible_cards_named("Web-Shooter")
	if web_shooters.is_empty():
		_fail("seed 1 exposed no draggable Web-Shooter hand card")
		return null
	var card := web_shooters.front() as Control
	if not await _body_click_inspects_without_drafting(card):
		_fail("the Web-Shooter body did not support inspection before drafting")
		return null
	if not await _drag_to_prompt_owner_lane(card):
		_fail("the Web-Shooter did not support a pointer drag to its prompt-owner lane")
		return null
	if not await _wait_for_web_shooter_draft(
			"dragging anchor 19 did not prepare its exact Web-Shooter affordance"):
		return null
	return card


func _visible_cards_named(title: String) -> Array[Node]:
	var matches: Array[Node] = []
	for candidate in main.find_children("ProceduralCard*", "", true, false):
		var card := candidate as Control
		if card != null and title in _visible_text(card) and card.is_visible_in_tree():
			matches.append(card)
	return matches


func _card_for_anchor(anchor: int) -> Control:
	var cards := main.find_children("ProceduralCard%d" % anchor, "Control", true, false)
	cards.reverse()
	for candidate in cards:
		if is_instance_valid(candidate) and not candidate.is_queued_for_deletion() \
				and candidate.is_visible_in_tree():
			return candidate as Control
	return null


func _complete_web_shooter_play() -> bool:
	if not await _choose_target(IDENTITY):
		return false
	if not await _choose_cost(0):
		return false
	if not await _activate_decision_resource(IDENTITY):
		return false
	if not await _commit_once("Web-Shooter"):
		return false
	return true


func _direct_change_form_is_played() -> bool:
	var action := _attached(_attached_name(IDENTITY, "Action"))
	if action == null or not await _pointer_activate_attached(action):
		_fail("the identity has no attached action chooser")
		return false
	var chooser := main.find_child("CardActionChoices", true, false) as Control
	if chooser == null:
		if _attached(_attached_name(IDENTITY, "Submit")) == null:
			_fail("the identity action selected neither Change Form nor an anchored chooser")
			return false
		if not await _commit_once("Change Form"):
			return false
		return await _undo_and_replay_change_form()
	var change := _visible_button_beginning(chooser, "◇ Change Form")
	if change == null or not await _pointer_activate(change):
		_fail("the anchored identity chooser cannot select Change Form")
		return false
	if not await _commit_once("Change Form"):
		return false
	return await _undo_and_replay_change_form()


func _undo_and_replay_change_form() -> bool:
	if not await _set_history_drawer(true):
		return false
	var history := _node("Play/Prompt/Margin/Stack/Workbench/History/EventLog") as RichTextLabel
	var undo := main.find_child("UndoLast", true, false) as Button
	if undo.disabled or "Spider-Man changed form." not in history.get_parsed_text():
		_fail("the direct form change has no authoritative history undo")
		return false
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if not await _pointer_activate(undo) or not await _wait_for(func() -> bool:
		return (_node("Toolbar/SyncStatus") as Label).text != revision):
		_fail("the direct form-change undo did not reconcile")
		return false
	if not await _set_history_drawer(false):
		return false
	var action := _attached(_attached_name(IDENTITY, "Action"))
	if action == null or not await _pointer_activate_attached(action):
		_fail("the restored identity cannot repeat Change Form")
		return false
	var chooser := main.find_child("CardActionChoices", true, false) as Control
	if chooser != null:
		var change := _visible_button_beginning(chooser, "◇ Change Form")
		if change == null or not await _pointer_activate(change):
			return false
	if not await _commit_once("Change Form replay"):
		return false
	main.set_meta("smoke_direct_form_undo", true)
	return true


func _set_history_drawer(expanded: bool) -> bool:
	var toggle := main.find_child("ToggleHistory", true, false) as Button
	var log := _node("Play/Prompt/Margin/Stack/Workbench/History/EventLog") as Control
	if toggle == null:
		return false
	if log.visible != expanded and not await _activate_exposed_control_point(toggle):
		return false
	return await _wait_for(func() -> bool: return log.visible == expanded)


func _direct_black_cat_is_played() -> bool:
	var cards := _visible_cards_named("Black Cat")
	var card := cards.front() as Control if not cards.is_empty() else null
	if card == null or not await _drag_to_prompt_owner_lane(card):
		_fail("seed 1 did not expose Black Cat as a draggable table object")
		return false
	if not await _wait_for(func() -> bool:
		return _attached(_attached_name(SPIDER_TRACER, "Generator")) != null \
				and _attached(_attached_name(DAREDEVIL, "Generator")) != null):
		_fail("dragging Black Cat did not prepare its own affordance")
		return false
	if not await _activate_decision_resource(SPIDER_TRACER) \
			or not await _activate_decision_resource_keyboard(DAREDEVIL):
		_fail("Black Cat payment did not expose its exact offered hand-card generators")
		return false
	if not await _commit_once("Black Cat"):
		return false
	return _attached(_attached_name(BLACK_CAT, "Action")) != null


func _direct_attacks_are_played() -> bool:
	if not await _select_attached_action(BLACK_CAT, "Attack"):
		return false
	if not await _choose_target(RHINO):
		return false
	if not await _commit_once("Attack"):
		return false
	if not await _exhausted_card_is_spatial(BLACK_CAT):
		return false
	if not await _select_attached_action(IDENTITY, "Attack"):
		return false
	if not await _choose_target(RHINO) or not await _commit_once("Attack"):
		return false
	return await _exhausted_card_is_spatial(IDENTITY)


func _exhausted_card_is_spatial(anchor: int) -> bool:
	_position_pointer_without_settle(Vector2(4, 4))
	await process_frame
	var found := await _wait_for(func() -> bool:
		var card := _card_for_anchor(anchor)
		var caption := _visible_exhausted_caption(anchor)
		return card != null and card.has_meta("spatial_exhausted") \
				and abs(abs(card.rotation) - PI / 2.0) < 0.01 \
				and caption != null and caption.is_visible_in_tree() \
				and abs(caption.rotation) < 0.01)
	if found:
		return true
	var card := _card_for_anchor(anchor)
	var caption := _visible_exhausted_caption(anchor)
	_fail(("anchor %d did not retain its quarter-turn exhaustion pose " \
			+ "(card=%s meta=%s rotation=%s caption=%s caption_rotation=%s)") % [
		anchor,
		str(card != null),
		str(card != null and card.has_meta("spatial_exhausted")),
		str(card.rotation if card != null else -99.0),
		str(caption != null),
		str(caption.rotation if caption != null else -99.0),
	])
	return false


func _visible_exhausted_caption(anchor: int) -> Label:
	var captions := main.find_children("ExhaustedCaption%d" % anchor, "Label", true, false)
	captions.reverse()
	for candidate in captions:
		if is_instance_valid(candidate) and not candidate.is_queued_for_deletion() \
				and candidate.is_visible_in_tree():
			return candidate as Label
	return null


func _select_attached_action(anchor: int, verb: String) -> bool:
	var action := _attached(_attached_name(anchor, "Action"))
	if action == null or not await _pointer_activate_attached(action):
		_fail("anchor %d has no attached action control" % anchor)
		return false
	var chooser := main.find_child("CardActionChoices", true, false) as Control
	if chooser == null:
		return _attached("Card*Target") != null or _attached(_attached_name(anchor, "Submit")) != null
	var choice := _visible_button_beginning(chooser, "◇ %s" % verb)
	if choice == null or not await _pointer_activate(choice):
		_fail("the attached chooser has no %s action for anchor %d" % [verb, anchor])
		return false
	return true


func _activate_decision_resource(anchor: int) -> bool:
	var resource := _attached(_attached_name(anchor, "Generator"))
	if resource == null:
		_fail("the represented card has no resource choice for anchor %d" % anchor)
		return false
	return await _activate_attached(anchor, "Generator")


func _activate_decision_resource_keyboard(anchor: int) -> bool:
	var resource := _attached(_attached_name(anchor, "Generator"))
	if resource == null or not await _keyboard_activate(resource):
		_fail("the represented card has no keyboard-operable resource choice for anchor %d" % anchor)
		return false
	return await _wait_for(func() -> bool:
		var refreshed := _attached(_attached_name(anchor, "Generator"))
		return refreshed != null and refreshed.text.begins_with("✓"))


func _choose_target(anchor: int) -> bool:
	var target := _attached(_attached_name(anchor, "Target"))
	if target != null:
		return await _pointer_activate(target)
	var card := _card_for_anchor(anchor)
	if card != null and "✓ TARGET" in _visible_text(card):
		return true
	_fail("the prompt did not offer stable target anchor %d" % anchor)
	return false


func _choose_cost(index: int) -> bool:
	var cost := _attached("Card*Cost")
	if cost == null:
		return _attached("Card*Generator") != null or _attached("Card*Submit") != null
	if not await _pointer_activate(cost):
		_fail("the selected action did not expose cost option %d" % index)
		return false
	return true


func _activate_attached(anchor: int, intent: String) -> bool:
	var control := _attached(_attached_name(anchor, intent))
	if control == null:
		_fail("anchor %d has no attached %s control" % [anchor, intent])
		return false
	# The prompt refresh replaces every attached control after the button's
	# Pressed callback. Inject the complete native click before yielding, then
	# observe the replacement by stable name below.
	if not await _pointer_activate_attached(control):
		return false
	if await _wait_for(func() -> bool:
		var refreshed := _attached(_attached_name(anchor, intent))
		return refreshed != null and refreshed.text.begins_with("✓")):
		return true
	_fail("attached %s on anchor %d did not reflect its pointer selection" % [intent, anchor])
	return false


func _pointer_activate_attached(control: Control) -> bool:
	var control_name := String(control.name)
	for frame in 5:
		await process_frame
	var current := _attached(control_name)
	if current != null:
		control = current
	var host := _card_for(control)
	if host != null:
		_position_pointer_without_settle(_card_body_point(host))
		await process_frame
	await _scroll_control_into_view(control)
	if not await _align_attached_control_to_table(control) \
			or _visible_control_rect(control).size.y < 44.0 \
			or not await _activate_exposed_control_point(control):
		_fail(("attached control '%s' has no operable native pointer path " \
				+ "(parent=%s rect=%s visible=%s)") % [control.name,
				control.get_parent().name if control.get_parent() != null else "none",
				_visible_control_rect(control), control.is_visible_in_tree()])
		return false
	await process_frame
	return true


func _activate_exposed_control_point(control: Control) -> bool:
	var control_name := control.name
	var rect := _visible_control_rect(control)
	for y_fraction in [0.2, 0.5, 0.8]:
		for x_fraction in [0.1, 0.3, 0.5, 0.7, 0.9]:
			if not is_instance_valid(control):
				control = main.find_child(control_name, true, false) as Control
				if control == null or control.is_queued_for_deletion():
					return false
				rect = _visible_control_rect(control)
			var point := rect.position + Vector2(
				rect.size.x * x_fraction, rect.size.y * y_fraction)
			if not await _control_owns_point(control, point):
				continue
			# Hover can lift and straighten a fanned host. Re-sample the same
			# relative point from the settled control before injecting the click.
			rect = _visible_control_rect(control)
			point = rect.position + Vector2(
				rect.size.x * x_fraction, rect.size.y * y_fraction)
			if not await _control_owns_point(control, point):
				continue
			var press := InputEventMouseButton.new()
			press.button_index = MOUSE_BUTTON_LEFT
			press.pressed = true
			press.position = point
			press.global_position = point
			render_viewport.push_input(press, true)
			var release := InputEventMouseButton.new()
			release.button_index = MOUSE_BUTTON_LEFT
			release.position = point
			release.global_position = point
			render_viewport.push_input(release, true)
			return true
	return false


func _commit_once(expected: String) -> bool:
	var submit := _attached("Card*Submit")
	if submit == null or submit.disabled:
		_fail("%s has no ready card-local execute control" % expected)
		return false
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if not await _pointer_activate(submit):
		return false
	return await _wait_for(func() -> bool:
		return _is_complete() or (_node("Toolbar/SyncStatus") as Label).text != revision)


func _drag_to_prompt_owner_lane(card: Control) -> bool:
	var lane := main.find_child("PlayerTable", true, false) as Control
	if lane == null:
		var areas := _node("Play/Board/TableScroll/Margin/Areas") as Control
		lane = areas.find_child("PlayerLane0", true, false) as Control
	if card == null or lane == null:
		_fail("the prompt owner's live lane is not available as a hand-card drop target")
		return false
	await _scroll_control_into_view(lane)
	var finish := _visible_control_rect(lane).get_center()
	if _visible_control_rect(lane).size == Vector2.ZERO:
		_fail("the prompt-owner live lane has no visible drop area")
		return false
	return await _drag(card, finish)


func _outside_drag_keeps_draft_empty(card: Control) -> bool:
	if card == null:
		return false
	var origin := card.global_position
	var rotation := card.rotation
	var table := _node("Play/Board/TableScroll") as Control
	if not await _drag(card, _visible_control_rect(table).position + Vector2(4, 4)):
		return false
	if not await _wait_for(func() -> bool:
		return is_instance_valid(card) and card.global_position.distance_to(origin) < 2.0 \
				and abs(card.rotation - rotation) < 0.01):
		_fail("an invalid drag did not visibly return the card to its fanned origin")
		return false
	return not _web_shooter_draft_is_prepared()


func _drag(card: Control, finish: Vector2) -> bool:
	var start := _card_body_point(card)
	if not await _control_owns_point(card, start):
		_fail("card '%s' has no exposed body from which to begin its drag" % card.name)
		return false
	var origin := card.global_position
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = start
	press.global_position = start
	render_viewport.push_input(press, true)
	await process_frame
	var midpoint := start.lerp(finish, 0.55)
	var move := InputEventMouseMotion.new()
	move.position = midpoint
	move.global_position = midpoint
	render_viewport.push_input(move, true)
	await process_frame
	if card.get_global_rect().get_center().distance_to(midpoint) > 4.0 or card.z_index < 120:
		_fail("dragging '%s' produced no lifted intermediate frame" % card.name)
		return false
	move = InputEventMouseMotion.new()
	move.position = finish
	move.global_position = finish
	render_viewport.push_input(move, true)
	await process_frame
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.position = finish
	release.global_position = finish
	render_viewport.push_input(release, true)
	await process_frame
	if card.is_inside_tree() and card.global_position.distance_to(origin) > 2.0:
		await main.get_tree().create_timer(0.25).timeout
	return true


func _body_click_inspects_without_drafting(card: Control) -> bool:
	if card == null or not await _pointer_activate_card_body(card):
		return false
	var inspector := main.get_node("CardInspector") as Control
	if inspector == null or not inspector.visible or _web_shooter_draft_is_prepared():
		_fail("a Web-Shooter body click did not remain an inspection-only action")
		return false
	var escape := InputEventKey.new()
	escape.keycode = KEY_ESCAPE
	escape.pressed = true
	render_viewport.push_input(escape)
	await process_frame
	return not inspector.visible
