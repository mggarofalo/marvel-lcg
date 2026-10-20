extends "res://smoke/local_game_smoke.gd"

func _run() -> void:
	var probe = load("res://smoke/CardFaceReuseNativeProbe.cs").new()
	root.add_child(probe)
	var failure: String = probe.VerifyBoundaries()
	if not failure.is_empty():
		_fail(failure)
		return
	if not await _open_reconciliation_game(): return
	var observed := await _unpaid_face_continuity(probe)
	if observed.is_empty(): return
	if not await _committed_faces_retire(observed): return
	if not await _scale_and_scene_retire(probe): return
	print("CARD_FACE_RECONCILIATION_OK identity=true stale_state=true scale=true lifetime=true")
	quit(0)

func _open_reconciliation_game() -> bool:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return false
	if not await _wait_for(func() -> bool: return _task_commit() != null): return false
	if not await _select_mulligan_cards() or not await _submit_mulligan(): return false
	return true

func _unpaid_face_continuity(probe: Node) -> Dictionary:
	if not await _begin_face_continuity(probe): return {}
	# The resize probe may change physical-card fitting. Observe the settled
	# faces before checking unchanged draft and accepted response boundaries.
	var rhino := _face(RHINO)
	var observed := {"rhino_id": rhino.get_instance_id(), "rhino_children": rhino.get_child_count(),
		IDENTITY: weakref(_face(IDENTITY)), WEB_SHOOTER: weakref(_face(WEB_SHOOTER)),
		SECOND_WEB_SHOOTER: weakref(_face(SECOND_WEB_SHOOTER))}
	if not await _cancel_and_recompose(observed): return {}
	return observed

func _begin_face_continuity(probe: Node) -> bool:
	var rhino := _face(RHINO)
	if rhino == null or _face(IDENTITY) == null or _face(WEB_SHOOTER) == null:
		_fail("face continuity probe lacks the real seeded table")
		return false
	var rhino_id := rhino.get_instance_id()
	var count := rhino.get_child_count()
	if await _draft_web_shooter() == null: return false
	if not _same_face(RHINO, rhino_id, count): return false
	return await _payment_wait_geometry_is_safe(probe)

func _cancel_and_recompose(observed: Dictionary) -> bool:
	if not await _payment_cancel_is_safe(): return false
	if not _same_face(RHINO, observed.rhino_id, observed.rhino_children): return false
	if await _draft_web_shooter() == null: return false
	if not await _payment_modal_is_safe(): return false
	return _same_face(RHINO, observed.rhino_id, observed.rhino_children)

func _committed_faces_retire(observed: Dictionary) -> bool:
	if not await _complete_web_shooter_play(): return false
	if not _same_face(RHINO, observed.rhino_id, observed.rhino_children): return false
	for frame in 3: await process_frame
	if (observed[WEB_SHOOTER].get_ref() == null) == (observed[SECOND_WEB_SHOOTER].get_ref() == null):
		_fail("the committed copy's face must retire while the other copy remains in hand")
		return false
	if not await _direct_change_form_is_played(): return false
	for frame in 3: await process_frame
	if observed[IDENTITY].get_ref() != null:
		_fail("changed live identity retains its prior face")
		return false
	return true

func _scale_and_scene_retire(probe: Node) -> bool:
	if not await _local_inspector_lifetime_is_safe(): return false
	var old_face: WeakRef = weakref(_face(RHINO))
	var scale := main.get_node("StatusBar/InterfaceScale") as HSlider
	# Physical cards retain their readable table scale independently of the interface.
	# 50% changes the physical face geometry at either desktop profile.
	scale.value = 50
	for frame in 3: await process_frame
	if old_face.get_ref() != null:
		_fail("physical face with replaced reading geometry remains owned")
		return false
	var current_face: WeakRef = weakref(_face(RHINO))
	var current_inspector: WeakRef = weakref(main.find_child("CardInspector", true, false))
	main.queue_free()
	probe.queue_free()
	for frame in 3: await process_frame
	if current_face.get_ref() != null:
		_fail("last table retains a procedural face after closing")
		return false
	if current_inspector.get_ref() != null:
		_fail("closing the local decision scene leaked its shared inspector")
		return false
	return true

func _local_inspector_lifetime_is_safe() -> bool:
	var local := main.find_child("LocalDecisionWorkspace", true, false)
	var inspector := main.find_child("CardInspector", true, false)
	if local == null or inspector.get_parent() != local.get_parent():
		_fail("the staged hero attack did not own inspection in its local layer")
		return false
	var original_id := inspector.get_instance_id()
	if not await _pointer_activate(local.find_child("LocalReturnToTable", true, false)): return false
	for frame in 3: await process_frame
	if not is_instance_valid(inspector) or inspector.get_parent() != main:
		_fail("closing a local draft failed to restore the shared inspector to the table")
		return false
	if not await _pointer_activate(_button_named("Resume current choice")): return false
	local = main.find_child("LocalDecisionWorkspace", true, false)
	if inspector.get_instance_id() != original_id or inspector.get_parent() != local.get_parent():
		_fail("resuming a local draft replaced or lost its shared inspector")
		return false
	return true

func _face(anchor: int) -> Control:
	var card := _card_for_anchor(anchor)
	return card.get_node_or_null("CardSurface/CardFace") as Control if card != null else null

func _same_face(anchor: int, previous_id: int, count: int) -> bool:
	var face := _face(anchor)
	if face == null or face.get_instance_id() != previous_id or face.get_child_count() != count:
		_fail("unchanged authorized face is reconstructed or duplicates its Ready children")
		return false
	return true

func _payment_wait_geometry_is_safe(probe: Node) -> bool:
	var original := _hand_surface().get_instance_id()
	var revision := str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
	if not await _sending_keeps_geometry(probe, original): return false
	if not await _waiting_resize_applies(original): return false
	return await _not_sent_resumes_payment(probe, revision)

func _sending_keeps_geometry(probe: Node, original: int) -> bool:
	probe.ShowSending(main)
	for frame in 3: await process_frame
	if _hand_surface().get_instance_id() != original or _payment_modal() != null:
		_fail("sending unnecessarily rebuilds the physical table or retains its unpaid editor")
		return false
	if _has_enabled_table_mutation():
		_fail("sending leaves an enabled table commitment")
		return false
	if "Resolving decision" not in _visible_text(main):
		_fail("sending preserves geometry but loses its operational explanation")
		return false
	return true

func _has_enabled_table_mutation() -> bool:
	for button in main.find_children("*", "Button", true, false):
		if button.get_meta("table_mutation", false) and button.is_visible_in_tree() and not button.disabled:
			return true
	return false

func _waiting_resize_applies(original: int) -> bool:
	var viewport_size: Vector2i = render_viewport.size
	render_viewport.size = viewport_size - Vector2i(32, 0)
	for frame in 3: await process_frame
	if _hand_surface().get_instance_id() == original:
		_fail("sending suppresses a real viewport resize")
		return false
	render_viewport.size = viewport_size
	for frame in 3: await process_frame
	return true

func _not_sent_resumes_payment(probe: Node, revision: String) -> bool:
	probe.ShowNotSent(main)
	for frame in 3: await process_frame
	if str((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != revision or _payment_modal() == null \
			or not await _payment_table_layout_is_safe(_payment_modal()):
		_fail("not-sent recovery fails to restore the same unpaid workspace and geometry")
		return false
	return true
