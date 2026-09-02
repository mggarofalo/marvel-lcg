extends SceneTree

# The Windows CI runner falls back to software-rendered ANGLE. Socket decisions
# must still complete there, but rendering two live Main scenes can take longer
# than the headless local-game smoke's per-action budget.
const TIMEOUT_MILLISECONDS := 60000
const MAX_DECISIONS := 600
const GAME_LABEL := "hosted-multiplayer-smoke"

var host: Control
var guest: Control
var failed := false


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	var packed := load("res://Main.tscn") as PackedScene
	if packed == null:
		_fail("Main.tscn could not be loaded")
		return

	host = packed.instantiate() as Control
	root.add_child(host)
	if not await _wait_for(func() -> bool:
		var ready := _button(host, "Start game")
		return ready != null and not ready.disabled):
		_fail("the host setup never became ready")
		return
	_configure_connection(host)
	_select_option(_node(host, "Setup/Selections/Fields/Grid/Hero"), "Spider-Man")
	_select_option(_node(host, "Setup/Selections/Fields/Grid/SecondHero"), "Captain Marvel")
	_select_option(_node(host, "Setup/Selections/Fields/Grid/Scenario"), "Rhino")
	_select_option(_node(host, "Setup/Selections/Fields/Grid/Mode"), "Standard")
	var seed := _node(host, "Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "7"
	seed.text_changed.emit(seed.text)
	await process_frame

	var start := _button(host, "Start game")
	if start == null or start.disabled:
		_fail("the configured two-hero hosted game cannot start")
		return
	start.pressed.emit()
	if not await _wait_for(func() -> bool:
		return _play(host).visible and _button(host, "Copy invitation") != null):
		_fail("the host did not open a two-seat table with an invitation")
		return

	DisplayServer.clipboard_set("")
	var copy := _button(host, "Copy invitation")
	copy.pressed.emit()
	await process_frame
	var invitation := DisplayServer.clipboard_get()
	if invitation.is_empty():
		_fail("copying the one-time invitation did not reach the clipboard")
		return
	if invitation in _visible_text(host) or _button(host, "Copy invitation") != null:
		_fail("the host retained or displayed the copied invitation")
		return

	guest = packed.instantiate() as Control
	root.add_child(guest)
	if not await _wait_for(func() -> bool: return _button(guest, "Join a game") != null):
		_fail("the guest entry screen never became ready")
		return
	_configure_connection(guest)
	var join_flow := _button(guest, "Join a game")
	join_flow.pressed.emit()
	await process_frame
	var invitation_field := _node(
		guest, "Setup/Selections/Fields/JoinFields/Invitation") as LineEdit
	if not invitation_field.secret:
		_fail("the guest invitation field is not masked")
		return
	invitation_field.text = invitation
	invitation_field.text_changed.emit(invitation)
	await process_frame
	var join := _button(guest, "Join game")
	if join == null or join.disabled:
		_fail("the guest cannot redeem the copied invitation")
		return
	join.pressed.emit()
	invitation = ""
	DisplayServer.clipboard_set("")
	if not await _wait_for(func() -> bool: return _play(guest).visible):
		_fail("the guest did not attach to the hosted table")
		return
	if not invitation_field.text.is_empty():
		_fail("the guest retained the invitation after attach")
		return

	var host_acted := false
	var guest_acted := false
	var decisions := 0
	while not _complete(host) or not _complete(guest):
		if decisions >= MAX_DECISIONS:
			_fail("the hosted game is still playing after %d decisions" % decisions)
			return

		if _complete(host) != _complete(guest):
			var unfinished := guest if _complete(host) else host
			if not await _synchronize(unfinished):
				return
			continue

		var active: Control = host if _has_decision(host) else guest if _has_decision(guest) else null
		if active == null:
			if not await _synchronize(host):
				return
			if not _has_decision(host) and not _complete(host):
				if not await _synchronize(guest):
					return
			continue

		if active == host:
			host_acted = true
		else:
			guest_acted = true
		if not await _answer_visible_decision(active):
			return
		decisions += 1

		var other := guest if active == host else host
		if not _complete(active) and not _has_decision(active):
			if not await _synchronize(other):
				return

	if not host_acted or not guest_acted:
		_fail("both independently authorized clients did not answer a decision")
		return
	if "VILLAIN WINS" not in _status(host).text or "VILLAIN WINS" not in _status(guest).text:
		_fail("the two clients did not converge on the deterministic villain win")
		return
	if not _decision_is_terminal(host) or not _decision_is_terminal(guest):
		_fail("a terminal client still exposes an operable decision")
		return

	print("HOSTED_MULTIPLAYER_SMOKE_OK decisions=%d" % decisions)
	quit(0)


func _configure_connection(main: Control) -> void:
	var game_id := _node(main, "Setup/Selections/Fields/ConnectionGrid/GameId") as LineEdit
	game_id.text = GAME_LABEL
	game_id.text_changed.emit(game_id.text)


func _answer_visible_decision(main: Control) -> bool:
	var decision := _decision(main)
	var decline := _button(decision, "Pass / decline")
	if decline != null and not decline.disabled:
		decline.pressed.emit()
	else:
		var submit := _button(decision, "Submit decision")
		if submit == null or submit.disabled:
			var choice := _first_enabled_choice(decision)
			if choice == null:
				_fail("the active client has no visible control that can advance its prompt")
				return false
			choice.pressed.emit()
			await process_frame
			submit = _button(decision, "Submit decision")
		if submit == null or submit.disabled:
			_fail("the active client's selected decision cannot be submitted")
			return false
		submit.pressed.emit()

	if not await _wait_for(func() -> bool:
		return not _status(main).text.begins_with("DECISION SENT")):
		_fail("the hosted decision did not settle: %s" % _status(main).text)
		return false
	if _status(main).text.begins_with("MUTATION NOT REPEATED") \
			or _status(main).text.begins_with("DECISION REJECTED"):
		_fail("the hosted decision was not accepted")
		return false
	return true


func _synchronize(main: Control) -> bool:
	if _complete(main):
		return true
	var sync := _button(main, "Synchronize table")
	if sync == null:
		sync = _button(main, "Reconnect table")
	if sync == null or sync.disabled:
		_fail("a waiting client cannot synchronize its hosted table")
		return false
	sync.pressed.emit()
	if not await _wait_for(func() -> bool:
		return not _status(main).text.begins_with("SYNCHRONIZING")):
		_fail("a hosted table synchronization did not settle")
		return false
	if _status(main).text.begins_with("SYNC READ FAILED"):
		_fail("a hosted table synchronization failed")
		return false
	return true


func _has_decision(main: Control) -> bool:
	if not _play(main).visible or _complete(main):
		return false
	var decision := _decision(main)
	var decline := _button(decision, "Pass / decline")
	var submit := _button(decision, "Submit decision")
	return decline != null and not decline.disabled \
		or submit != null and not submit.disabled \
		or _first_enabled_choice(decision) != null


func _decision_is_terminal(main: Control) -> bool:
	return not _has_decision(main) and "No further decision is waiting" in _visible_text(_decision(main))


func _complete(main: Control) -> bool:
	return _status(main).text.begins_with("GAME COMPLETE")


func _select_option(node: Node, wanted: String) -> void:
	var option := node as OptionButton
	for index in option.item_count:
		if option.get_item_text(index).begins_with(wanted):
			option.select(index)
			option.item_selected.emit(index)
			return
	_fail("hosted setup option '%s' is unavailable" % wanted)


func _first_enabled_choice(decision: Control) -> Button:
	for button in _visible_buttons(decision):
		if not button.disabled \
				and button.text != "Submit decision" \
				and button.text != "Pass / decline":
			return button
	return null


func _button(node: Node, wanted: String) -> Button:
	for button in _visible_buttons(node):
		if button.text == wanted:
			return button
	return null


func _visible_buttons(node: Node) -> Array[Button]:
	var found: Array[Button] = []
	for child in node.get_children():
		if child is Button and child.is_visible_in_tree():
			found.append(child)
		found.append_array(_visible_buttons(child))
	return found


func _visible_text(node: Node) -> String:
	var text := ""
	for child in node.get_children():
		if child is Label and child.is_visible_in_tree():
			text += child.text + "\n"
		elif child is Button and child.is_visible_in_tree():
			text += child.text + "\n"
		text += _visible_text(child)
	return text


func _node(main: Control, relative: String) -> Node:
	return main.get_node("Margin/Shell/Content/" + relative)


func _play(main: Control) -> Control:
	return _node(main, "Play") as Control


func _decision(main: Control) -> Control:
	return _node(main, "Play/Prompt/Margin/Stack/DecisionScroll/Decision") as Control


func _status(main: Control) -> Label:
	return _node(main, "Status/Text") as Label


func _wait_for(condition: Callable) -> bool:
	var started := Time.get_ticks_msec()
	while Time.get_ticks_msec() - started < TIMEOUT_MILLISECONDS:
		if condition.call():
			return true
		await process_frame
	return false


func _fail(message: String) -> void:
	if failed:
		return
	failed = true
	push_error(message)
	quit(1)
