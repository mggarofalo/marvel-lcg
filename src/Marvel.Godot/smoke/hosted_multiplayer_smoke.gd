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
var checkpoint_directory := ""
var upgraded := false


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	checkpoint_directory = OS.get_environment("MARVEL_HOSTED_SMOKE_CHECKPOINT_DIR")
	var packed := load("res://Main.tscn") as PackedScene
	if packed == null:
		_fail("Main.tscn could not be loaded")
		return
	if not await _open_host(packed):
		return
	var invitation := await _copy_invitation()
	if invitation.is_empty():
		return
	var guest_opened := await _open_guest(packed, invitation)
	invitation = ""
	if not guest_opened:
		return
	if not await _initial_hosted_checkpoint():
		return
	var journey := await _play_hosted_journey()
	if journey.is_empty() or not _hosted_terminal_is_safe(journey):
		return
	print("HOSTED_MULTIPLAYER_SMOKE_OK decisions=%d" % journey.decisions)
	quit(0)


func _open_host(packed: PackedScene) -> bool:
	host = packed.instantiate() as Control
	root.add_child(host)
	if not await _wait_for(func() -> bool:
		var ready := _button(host, "Start game")
		return ready != null and not ready.disabled):
		_fail("the host setup never became ready")
		return false
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
		return false
	start.pressed.emit()
	if not await _wait_for(func() -> bool:
		return _play(host).visible and _button(host, "Copy invitation") != null):
		_fail("the host did not open a two-seat table with an invitation")
		return false
	return true


func _copy_invitation() -> String:
	DisplayServer.clipboard_set("")
	var copy := _button(host, "Copy invitation")
	copy.pressed.emit()
	await process_frame
	var invitation := DisplayServer.clipboard_get()
	if invitation.is_empty():
		_fail("copying the one-time invitation did not reach the clipboard")
		return ""
	if invitation in _visible_text(host) or _button(host, "Copy invitation") != null:
		_fail("the host retained or displayed the copied invitation")
		return ""
	return invitation


func _open_guest(packed: PackedScene, invitation: String) -> bool:
	guest = packed.instantiate() as Control
	root.add_child(guest)
	if not await _wait_for(func() -> bool: return _button(guest, "Join a game") != null):
		_fail("the guest entry screen never became ready")
		return false
	_configure_connection(guest)
	_button(guest, "Join a game").pressed.emit()
	await process_frame
	var field := _node(guest, "Setup/Selections/Fields/JoinFields/Invitation") as LineEdit
	if not field.secret:
		_fail("the guest invitation field is not masked")
		return false
	field.text = invitation
	field.text_changed.emit(invitation)
	await process_frame
	var join := _button(guest, "Join game")
	if join == null or join.disabled:
		_fail("the guest cannot redeem the copied invitation")
		return false
	join.pressed.emit()
	invitation = ""
	DisplayServer.clipboard_set("")
	if not await _wait_for(func() -> bool: return _play(guest).visible):
		_fail("the guest did not attach to the hosted table")
		return false
	if not field.text.is_empty():
		_fail("the guest retained the invitation after attach")
		return false
	return true


func _initial_hosted_checkpoint() -> bool:
	if checkpoint_directory.is_empty():
		return true
	if not await _checkpoint("clients-connected", "continue-after-restart"):
		return false
	return await _synchronize(host) and await _synchronize(guest)


func _play_hosted_journey() -> Dictionary:
	var state := {"host_acted": false, "guest_acted": false, "decisions": 0}
	while not _complete(host) or not _complete(guest):
		if not await _play_hosted_decision(state):
			return {}
	return state


func _play_hosted_decision(state: Dictionary) -> bool:
	if state.decisions >= MAX_DECISIONS:
		_fail("the hosted game is still playing after %d decisions" % state.decisions)
		return false
	if _complete(host) != _complete(guest):
		return await _synchronize(guest if _complete(host) else host)
	var active := _active_hosted_client()
	if active == null:
		return await _recover_hosted_prompt()
	state.host_acted = state.host_acted or active == host
	state.guest_acted = state.guest_acted or active == guest
	if not await _answer_visible_decision(active):
		return false
	state.decisions += 1
	if not await _refresh_hosted_peer(active):
		return false
	return await _upgrade_checkpoint_if_needed()


func _active_hosted_client() -> Control:
	if _has_decision(host) and _has_decision(guest):
		# A root player-turn menu is cancellable; the simultaneous off-turn
		# Action menu is not. Keep the deterministic policy on the active player.
		return host if _can_decline(host) else guest
	if _has_decision(host):
		return host
	if _has_decision(guest):
		return guest
	return null


func _recover_hosted_prompt() -> bool:
	if not await _synchronize(host):
		return false
	if not _has_decision(host) and not _complete(host):
		return await _synchronize(guest)
	return true


func _refresh_hosted_peer(active: Control) -> bool:
	var other := guest if active == host else host
	# Every accepted command advances the shared revision, so the peer's menu
	# is stale even when it still renders a simultaneous action.
	if not _complete(other):
		return await _synchronize(other)
	return true


func _upgrade_checkpoint_if_needed() -> bool:
	if checkpoint_directory.is_empty() or upgraded:
		return true
	if not await _checkpoint("upgrade-ready", "continue-after-upgrade"):
		return false
	if not await _synchronize(host) or not await _synchronize(guest):
		return false
	upgraded = true
	return true


func _hosted_terminal_is_safe(state: Dictionary) -> bool:
	if not state.host_acted or not state.guest_acted:
		_fail("both independently authorized clients did not answer a decision")
		return false
	if "VILLAIN WINS" not in _status(host).text or "VILLAIN WINS" not in _status(guest).text:
		_fail("the two clients did not converge on the deterministic villain win")
		return false
	if not _decision_is_terminal(host) or not _decision_is_terminal(guest):
		_fail("a terminal client still exposes an operable decision")
		return false
	if not checkpoint_directory.is_empty():
		_write_checkpoint("journey-complete")
	return true


func _checkpoint(ready_name: String, continue_name: String) -> bool:
	_write_checkpoint(ready_name)
	if failed:
		return false
	if not await _wait_for(func() -> bool:
		return FileAccess.file_exists(checkpoint_directory.path_join(continue_name))):
		_fail("the hosted release checkpoint '%s' was not continued" % ready_name)
		return false
	return true


func _write_checkpoint(name: String) -> void:
	var marker := FileAccess.open(
		checkpoint_directory.path_join(name), FileAccess.WRITE)
	if marker == null:
		_fail("the hosted release checkpoint '%s' could not be written" % name)
		return
	marker.store_line("ready")
	marker.close()


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
		var submit := _submit_button(decision)
		if submit == null or submit.disabled:
			var choice := _first_enabled_choice(decision)
			if choice == null:
				_fail("the active client has no visible control that can advance its prompt")
				return false
			choice.pressed.emit()
			await process_frame
			submit = _submit_button(decision)
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
	var sync := main.find_child("Synchronize", true, false) as Button
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
	var submit := _submit_button(decision)
	return decline != null and not decline.disabled \
		or submit != null and not submit.disabled \
		or _first_enabled_choice(decision) != null


func _can_decline(main: Control) -> bool:
	var decline := _button(_decision(main), "Pass / decline")
	return decline != null and not decline.disabled


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
				and button.name != "Submit" \
				and button.text != "Pass / decline":
			return button
	return null


func _submit_button(decision: Control) -> Button:
	var submit := decision.find_child("Submit", true, false) as Button
	return submit if submit != null and submit.is_visible_in_tree() else null


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
	return _node(main, "Play/Prompt/Margin/Stack/Workbench/Action/Decision") as Control


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
