extends "res://smoke/hosted_multiplayer_smoke_client_support.gd"

const MAX_DECISIONS := 600
const GAME_LABEL := "hosted-multiplayer-smoke"

var checkpoint_directory := ""
var upgraded := false


func _ready() -> void:
	print("HOSTED_MULTIPLAYER_SMOKE_DRIVER_READY")
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
	if not _restricted_guest_surface_is_safe():
		return
	if not await _initial_hosted_checkpoint():
		return
	var journey := await _play_hosted_journey()
	if journey.is_empty() or not _hosted_terminal_is_safe(journey):
		return
	print("HOSTED_MULTIPLAYER_SMOKE_OK decisions=%d" % journey.decisions)
	_finish(0)


func _open_host(packed: PackedScene) -> bool:
	host = packed.instantiate() as Control
	host_viewport = _new_client_viewport()
	host_viewport.add_child(host)
	if not await _wait_for(func() -> bool:
		var ready := _button(host, "Start game")
		return ready != null and not ready.disabled):
		var setup_status := _status(host)
		_fail("the host setup never became ready: %s" % setup_status.text)
		return false
	_configure_connection(host)
	_select_option(_node(host, "Setup/Selections/Fields/Grid/Hero"), "Spider-Man")
	_select_option(_node(host, "Setup/Selections/Fields/Grid/SecondHero"), "Captain Marvel")
	_select_option(_node(host, "Setup/Selections/Fields/Grid/Scenario"), "Rhino")
	_select_option(_node(host, "Setup/Selections/Fields/Grid/Mode"), "Standard")
	var seed := _node(host, "Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "7"
	seed.text_changed.emit(seed.text)
	await get_tree().process_frame
	var start := _button(host, "Start game")
	if start == null or start.disabled:
		_fail("the configured two-hero hosted game cannot start")
		return false
	if not await _pointer_activate(start):
		return false
	if not await _wait_for(func() -> bool:
		return _play(host).visible and _button(host, "Copy invitation") != null):
		_fail("the host did not open a two-seat table with an invitation")
		return false
	return true


func _restricted_guest_surface_is_safe() -> bool:
	var host_hand := host.find_child("AstraTableSurface", true, false) as Control
	var guest_hand := guest.find_child("AstraTableSurface", true, false) as Control
	if host_hand == null:
		host_hand = _node(host, "Play/Board/HandShelf") as Control
	if guest_hand == null:
		guest_hand = _node(guest, "Play/Board/HandShelf") as Control
	if host_hand == null or guest_hand == null:
		_fail("the restricted clients do not expose their distinct hand shelves")
		return false
	var host_cards := host_hand.find_children("ProceduralCard*", "", true, false)
	if host_cards.is_empty():
		_fail("the prompt owner has no rendered private hand to protect")
		return false
	var guest_text := _visible_text(guest)
	for node in host_cards:
		var title := (node as Control).find_child("Title", true, false) as Label
		if title != null and not title.text.is_empty() and title.text in guest_text:
			_fail("the restricted guest rendered the prompt owner's private card face: %s" % title.text)
			return false
	if not guest_hand.find_children("MulliganDiscard*", "Button", true, false).is_empty() \
			or guest.find_child("CompleteChoiceSheet", true, false) != null:
		_fail("the restricted guest rendered private opening-hand controls for another seat")
		return false
	return true


func _copy_invitation() -> String:
	DisplayServer.clipboard_set("")
	var copy := _button(host, "Copy invitation")
	if copy == null or not await _pointer_activate(copy):
		return ""
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
	guest_viewport = _new_client_viewport()
	guest_viewport.add_child(guest)
	if not await _wait_for(func() -> bool: return _button(guest, "Join a game") != null):
		_fail("the guest entry screen never became ready")
		return false
	_configure_connection(guest)
	var join_flow := _button(guest, "Join a game")
	if join_flow == null or not await _pointer_activate(join_flow):
		return false
	var field := _node(guest, "Setup/Selections/Fields/JoinFields/Invitation") as LineEdit
	if not field.secret:
		_fail("the guest invitation field is not masked")
		return false
	field.text = invitation
	field.text_changed.emit(invitation)
	await get_tree().process_frame
	var join := _button(guest, "Join game")
	if join == null or join.disabled:
		_fail("the guest cannot redeem the copied invitation")
		return false
	if not await _pointer_activate(join):
		return false
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
	var state := {"host_acted": false, "guest_acted": false, "decisions": 0, "recoveries": 0}
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
		state.recoveries += 1
		if state.recoveries > 20:
			_fail("the hosted clients did not recover an operable prompt")
			return false
		return await _recover_hosted_prompt()
	state.recoveries = 0
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
	var prior_status := _status(main).text
	var attached_decline := _attached(main, "Card*Decline")
	if attached_decline != null and not attached_decline.disabled:
		if not await _pointer_activate(attached_decline):
			return false
	elif _has_table_decision(main):
		if not await _compose_table_decision(main):
			return false
	else:
		return await _answer_fallback_decision(main, prior_status)

	return await _wait_for_hosted_settlement(main, prior_status)


func _answer_fallback_decision(main: Control, prior_status: String) -> bool:
	var decision := _decision(main)
	var decline := _button(decision, "Pass / decline")
	if decline != null and not decline.disabled:
		if not await _pointer_activate(decline):
			return false
	else:
		var submit := _submit_button(decision)
		if submit == null or submit.disabled:
			var choice := _first_enabled_choice(decision)
			if choice == null:
				_fail("the active client has no visible control that can advance its prompt")
				return false
			if not await _pointer_activate(choice):
				return false
			submit = _submit_button(decision)
		if submit == null or submit.disabled:
			_fail("the active client's selected decision cannot be submitted")
			return false
		if not await _pointer_activate(submit):
			return false
	return await _wait_for_hosted_settlement(main, prior_status)


func _wait_for_hosted_settlement(main: Control, prior_status: String) -> bool:
	if not await _wait_for(func() -> bool: return _status(main).text != prior_status):
		_fail("the hosted decision did not start: %s" % prior_status)
		return false
	if not await _wait_for(func() -> bool:
		return not _status(main).text.begins_with("DECISION SENT")):
		_fail("the hosted decision did not settle: %s" % _status(main).text)
		return false
	if _status(main).text.begins_with("MUTATION NOT REPEATED") \
			or _status(main).text.begins_with("DECISION REJECTED"):
		_fail("the hosted decision was not accepted")
		return false
	return true


func _compose_table_decision(main: Control) -> bool:
	for selection in 10:
		var submit := _attached(main, "Card*Submit")
		if submit != null and not submit.disabled:
			return await _pointer_activate(submit)
		var chooser := main.find_child("CardActionChoices", true, false) as Control
		if chooser != null and chooser.is_visible_in_tree():
			var choice := _first_enabled_choice(chooser)
			if choice == null or not await _pointer_activate(choice):
				return false
			await get_tree().process_frame
			continue
		var contextual := main.find_child("ContextAction*", true, false) as Button
		if contextual != null and contextual.is_visible_in_tree() and not contextual.disabled:
			if not await _pointer_activate(contextual):
				return false
			await get_tree().process_frame
			continue
		var control: Button = null
		for pattern in ["Card*Target", "Card*Cost", "Card*Generator", "Card*Action"]:
			control = _attached(main, pattern, true)
			if control != null:
				break
		if control == null or not await _pointer_activate(control):
			_fail("the active hosted client has no card-local control that can advance its prompt")
			return false
		await get_tree().process_frame
	_fail("the active hosted client's card-local draft did not become executable")
	return false


func _synchronize(main: Control) -> bool:
	if _complete(main):
		return true
	var sync := main.find_child("Synchronize", true, false) as Button
	if sync == null or sync.disabled:
		_fail("a waiting client cannot synchronize its hosted table")
		return false
	if not await _pointer_activate(sync):
		return false
	if not await _wait_for(func() -> bool:
		return not _status(main).text.begins_with("SYNCHRONIZING")):
		_fail("a hosted table synchronization did not settle")
		return false
	if _status(main).text.begins_with("SYNC READ FAILED"):
		_fail("a hosted table synchronization failed")
		return false
	return true


func _has_decision(main: Control) -> bool:
	if not _play(main).visible or _complete(main) \
			or "WAITING FOR ANOTHER PLAYER" in _status(main).text:
		return false
	var decision := _decision(main)
	var decline := _button(decision, "Pass / decline")
	var submit := _submit_button(decision)
	return _has_table_decision(main) \
		or decline != null and not decline.disabled \
		or submit != null and not submit.disabled \
		or _first_enabled_choice(decision) != null


func _can_decline(main: Control) -> bool:
	var attached := _attached(main, "Card*Decline")
	if attached != null and not attached.disabled:
		return true
	var decline := _button(_decision(main), "Pass / decline")
	return decline != null and not decline.disabled


func _has_table_decision(main: Control) -> bool:
	for pattern in ["Card*Decline", "Card*Submit", "Card*Target", "Card*Cost", \
			"Card*Generator", "Card*Action"]:
		if _attached(main, pattern) != null:
			return true
	var contextual := main.find_child("ContextAction*", true, false) as Button
	return contextual != null and contextual.is_visible_in_tree() and not contextual.disabled


func _attached(main: Control, pattern: String, skip_selected := false) -> Button:
	for candidate in main.find_children(pattern, "Button", true, false):
		var button := candidate as Button
		if button != null and button.is_visible_in_tree() and not button.disabled \
				and button.has_meta("spatial_card_anchor") \
				and (not skip_selected or not button.text.begins_with("✓")):
			return button
	return null


func _decision_is_terminal(main: Control) -> bool:
	return not _has_decision(main) \
		and "No further decision is waiting" in _visible_text(_decision(main))
