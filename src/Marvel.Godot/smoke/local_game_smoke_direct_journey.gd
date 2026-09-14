extends "res://smoke/local_game_smoke_card_checks.gd"

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
		return false
	if not await _direct_black_cat_is_played():
		return false
	if not await _direct_change_form_is_played():
		return false
	if not await _direct_attacks_are_played():
		return false
	return true


func _direct_web_shooter_is_played() -> bool:
	var action := _attached(_attached_name(WEB_SHOOTER, "Action"))
	var duplicate := _attached(_attached_name(SECOND_WEB_SHOOTER, "Action"))
	if action == null or duplicate == null:
		_fail("seed 1 did not expose both stable Web-Shooter action anchors")
		return false
	var card := _card_for(action)
	if not await _body_click_inspects_without_drafting(card):
		return false
	if not await _drag_to_prompt_owner_lane(card):
		return false
	if not await _wait_for_web_shooter_draft(
			"dragging anchor 19 did not prepare its exact Web-Shooter affordance"):
		return false
	if not await _choose_target(IDENTITY):
		return false
	if not await _choose_cost(0):
		return false
	if not await _activate_attached(IDENTITY, "Generator"):
		return false
	if not await _commit_once("Web-Shooter"):
		return false
	if _attached(_attached_name(SECOND_WEB_SHOOTER, "Action")) == null:
		_fail("the unplayed duplicate Web-Shooter left the visible hand")
		return false
	return true


func _direct_change_form_is_played() -> bool:
	var action := _attached(_attached_name(IDENTITY, "Action"))
	if action == null or not await _pointer_activate_attached(action):
		_fail("the identity has no attached action chooser")
		return false
	var chooser := main.find_child("CardActionChoices", true, false) as Control
	if chooser == null:
		if not _selected_action_is("Change Form"):
			_fail("the identity action selected neither Change Form nor an anchored chooser")
			return false
		return await _commit_once("Change Form")
	var change := _visible_button_beginning(chooser, "◇ Change Form")
	if change == null or not await _pointer_activate(change):
		_fail("the anchored identity chooser cannot select Change Form")
		return false
	return await _commit_once("Change Form")


func _direct_black_cat_is_played() -> bool:
	var action := _attached(_attached_name(BLACK_CAT, "Action"))
	var card := _card_for(action) if action != null else null
	if action == null or not await _drag_to_prompt_owner_lane(card):
		_fail("seed 1 did not expose Black Cat for a live-lane play")
		return false
	if not await _wait_for(func() -> bool: return _selected_action_is("Play Black Cat")):
		_fail("dragging Black Cat did not prepare its own affordance")
		return false
	if not await _activate_attached(SPIDER_TRACER, "Generator") \
			or not await _activate_attached(DAREDEVIL, "Generator"):
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
	if not await _select_attached_action(IDENTITY, "Attack"):
		return false
	return await _choose_target(RHINO) and await _commit_once("Attack")


func _select_attached_action(anchor: int, verb: String) -> bool:
	var action := _attached(_attached_name(anchor, "Action"))
	if action == null or not await _pointer_activate_attached(action):
		_fail("anchor %d has no attached action control" % anchor)
		return false
	var chooser := main.find_child("CardActionChoices", true, false) as Control
	if chooser == null:
		return _selected_action_is(verb)
	var choice := _visible_button_beginning(chooser, "◇ %s" % verb)
	if choice == null or not await _pointer_activate(choice):
		_fail("the attached chooser has no %s action for anchor %d" % [verb, anchor])
		return false
	return true


func _choose_target(anchor: int) -> bool:
	var target := _attached(_attached_name(anchor, "Target"))
	if target != null:
		return await _pointer_activate(target)
	var decision_text := _visible_text(_decision()).to_lower()
	var automatic_name := "peter parker" if anchor == IDENTITY else "rhino" if anchor == RHINO else ""
	if not automatic_name.is_empty() and automatic_name in decision_text and "automatic" in decision_text:
		return true
	_fail("the prompt did not offer stable target anchor %d" % anchor)
	return false


func _choose_cost(index: int) -> bool:
	var cost := _decision().find_child("Cost%d" % index, true, false) as Button
	if cost == null or not await _pointer_activate(cost):
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
	for frame in 5:
		await process_frame
	await _scroll_control_into_view(control)
	if not await _align_attached_control_to_table(control) \
			or not await _control_has_real_hit_area(control) \
			or not _pointer_activate_without_settle(control):
		return false
	await process_frame
	return true


func _commit_once(expected: String) -> bool:
	var submit := _submit_button()
	if submit == null or submit.disabled or expected.to_lower() not in submit.text.to_lower():
		_fail("%s has no ready explicit Commit control" % expected)
		return false
	var revision := (_node("Toolbar/SyncStatus") as Label).text
	if not await _pointer_activate(submit):
		return false
	return await _wait_for(func() -> bool:
		return _is_complete() or (_node("Toolbar/SyncStatus") as Label).text != revision)


func _drag_to_prompt_owner_lane(card: Control) -> bool:
	var areas := _node("Play/Board/TableScroll/Margin/Areas") as Control
	var lane := areas.find_child("PlayerLane0", true, false) as Control
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
	var table := _node("Play/Board/TableScroll") as Control
	if not await _drag(card, _visible_control_rect(table).position + Vector2(4, 4)):
		return false
	return not _web_shooter_draft_is_prepared()


func _drag(card: Control, finish: Vector2) -> bool:
	var rect := _visible_control_rect(card)
	var start := Vector2(
		rect.position.x + minf(24.0, rect.size.x * 0.2),
		rect.end.y - minf(24.0, rect.size.y * 0.15))
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = start
	press.global_position = start
	render_viewport.push_input(press)
	var move := InputEventMouseMotion.new()
	move.position = finish
	move.global_position = finish
	render_viewport.push_input(move)
	var release := InputEventMouseButton.new()
	release.button_index = MOUSE_BUTTON_LEFT
	release.position = finish
	release.global_position = finish
	render_viewport.push_input(release)
	await process_frame
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


func _selected_action_is(text: String) -> bool:
	var summary := _decision().find_child("ActionSummary", true, false) as Control
	return summary != null and text.to_lower() in _visible_text(summary).to_lower()


func _attached(name: String) -> Button:
	var control := main.find_child(name, true, false) as Button
	return control if control != null and control.is_visible_in_tree() else null


func _attached_name(anchor: int, intent: String) -> String:
	return "Card%d%s" % [anchor, intent]


func _card_for(control: Control) -> Control:
	return control.get_parent().get_parent() as Control
