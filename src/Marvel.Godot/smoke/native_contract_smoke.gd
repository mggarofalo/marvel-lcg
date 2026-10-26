extends "res://smoke/search_choice_smoke.gd"

var contract := OS.get_environment("MARVEL_NATIVE_CONTRACT")
var witness = preload("res://smoke/NativeGameWitness.cs").new()
var stale_commit: Callable
var ui: RefCounted

func _entry_modes_are_explicit() -> bool:
    var endpoint := _node("Setup/Selections/Fields/ConnectionGrid/Endpoint") as LineEdit
    if endpoint.text != OS.get_environment("MARVEL_ENGINE_ENDPOINT"):
        _fail("prepared Core journey is not connected to its authoritative host")
        return false
    return true

func _run() -> void:
    if not await _open_setup(load("res://Main.tscn") as PackedScene): return
    print("NATIVE_CONTRACT_STAGE setup " + contract)
    await _configure_seeded_game()
    if contract.begins_with("wakanda"):
        _select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Black Panther")
    elif contract == "variable-payment":
        _select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Captain Marvel")
    elif contract in ["dense-table", "card-look"]:
        _select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Iron Man")
    if not await _pointer_activate(_button_named("Start game")): return
    witness.Bind(main)
    ui = preload("res://smoke/automation/ui_driver.gd").new(main, render_viewport, _fail)
    if not await _wait_for(_scene_ready):
        _fail("prepared scene did not become actionable: " + _visible_text(main))
        return
    print("NATIVE_CONTRACT_STAGE ready " + contract)
    var passed := false
    match contract:
        "lockjaw-discard": passed = await preload("res://smoke/native_lockjaw_contract.gd").run(self)
        "melee-sequential": passed = await preload("res://smoke/native_melee_contract.gd").run(self)
        "preemptive-strike": passed = await preload("res://smoke/native_preemptive_strike_contract.gd").run(self)
        "bruno-0", "bruno-1", "bruno-3": passed = await preload("res://smoke/native_bruno_contract.gd").run(self)
        "victory": passed = await _victory_contract()
        "lost-reply", "not-sent": passed = await _recovery_contract()
        "end-phase": passed = await _end_phase_contract()
        "wakanda-daggers-first", "wakanda-claws-first": passed = await preload("res://smoke/native_wakanda_contract.gd").run(self)
        "keyboard-payment": passed = await preload("res://smoke/native_keyboard_traversal.gd").payment(self)
        "keyboard-target": passed = await preload("res://smoke/native_keyboard_traversal.gd").targeted_attack(self)
        "variable-payment": passed = await preload("res://smoke/native_variable_contract.gd").run(self)
        "repeated-targets": passed = await preload("res://smoke/native_repeated_contract.gd").run(self)
        "dense-table": passed = await preload("res://smoke/native_scale_checkpoint.gd").verify(self, "dense-table")
        "card-look": passed = await preload("res://smoke/native_look_contract.gd").run(self)
        "resource-declaration": passed = await preload("res://smoke/native_declaration_contract.gd").run(self)
        _: _fail("unknown native contract: " + contract)
    if not passed:
        _fail("native contract did not satisfy its interaction assertions: " + contract)
        return
    if not await _release_table_resources(): return
    print("NATIVE_CONTRACT_OK " + contract)
    quit(0)

func _scene_ready() -> bool:
    if not witness.CanResolve(): return false
    if contract == "dense-table": return true
    if contract == "repeated-targets": return witness.HasRepeatedTargets()
    if contract == "preemptive-strike": return witness.DecisionKind() == "Interrupt"
    return witness.DecisionKind() == "PlayerAction"

func _stage_scene_attack() -> bool:
    if not await _select_attached_action(witness.Anchor("Spider-Man"), "Attack"): return false
    var commit := _task_commit()
    if commit == null or commit.text != "Attack Rhino":
        if not await _choose_target(witness.AreaAnchor("Rhino", "VillainArea")): return false
        commit = _task_commit()
    if commit == null or commit.disabled or commit.text != "Attack Rhino":
        _fail("prepared legal attack lacks its named explicit commitment")
        return false
    return witness.Revision() == 0

func _victory_contract() -> bool:
    if not await _stage_scene_attack(): return false
    if not await _commit_once("Attack Rhino"): return false
    if not await _wait_for(func() -> bool: return _is_complete()): return false
    if witness.Outcome() != "PlayersWin" or witness.Revision() != 1 or witness.CanResolve():
        _fail("final villain defeat did not publish exactly one terminal victory")
        return false
    if "PLAYERS WIN" not in _status().text.to_upper() and "VICTORY" not in _visible_text(main).to_upper():
        _fail("victory lacks a visible outcome explanation")
        return false
    if not _terminal_has_no_active_controls(): return false
    if not await preload("res://smoke/native_scale_checkpoint.gd").verify(self, "victory"): return false
    if not await _terminal_history_is_accessible(false): return false
    if not await _synchronization_preserves_history(true): return false
    return witness.Outcome() == "PlayersWin" and witness.Revision() == 1

func _signal_host(name: String) -> void:
    var file := FileAccess.open(OS.get_environment("MARVEL_NATIVE_CONTROL").path_join(name), FileAccess.WRITE)
    file.store_string("ready")
    file.close()

func _recovery_contract() -> bool:
    if not await _stage_scene_attack(): return false
    if contract == "not-sent":
        if not await _wait_for(func() -> bool: return FileAccess.file_exists(OS.get_environment("MARVEL_NATIVE_CONTROL").path_join("disconnected"))): return false
    var commit := _task_commit()
    stale_commit = commit.pressed.get_connections()[0]["callable"]
    if not await _pointer_activate(commit): return false
    if contract == "not-sent":
        if not await _retry_not_sent(): return false
    elif not await _recover_lost_reply(): return false
    if not await _wait_for(func() -> bool: return witness.Revision() == 1 and witness.CanResolve()): return false
    return await _verify_recovered_draft()

func _verify_recovered_draft() -> bool:
    if _task_commit() != null and _task_commit().text == "Attack Rhino":
        _fail("synchronization retained the stale attack draft")
        return false
    stale_commit.call()
    await process_frame
    if witness.Revision() != 1: return false
    return true

func _retry_not_sent() -> bool:
    if not await _wait_for(func() -> bool: return witness.ProgressKind() == "DecisionNotSent"): return false
    if witness.Revision() != 0:
        _fail("connection refusal changed authority")
        return false
    _signal_host("retry")
    if not await _wait_for(func() -> bool: return FileAccess.file_exists(OS.get_environment("MARVEL_NATIVE_CONTROL").path_join("listening"))): return false
    return await _commit_once("Attack Rhino")

func _recover_lost_reply() -> bool:
    if not await _wait_for(func() -> bool: return FileAccess.file_exists(OS.get_environment("MARVEL_NATIVE_CONTROL").path_join("committed"))): return false
    if not await _wait_for(func() -> bool: return not witness.CanResolve()): return false
    if _task_commit() != null and not _task_commit().disabled:
        _fail("uncertain committed operation leaves a second commitment enabled: progress=%s editor_locked=%s" % [witness.ProgressKind(), witness.SubmissionLocked()])
        return false
    if not await _inspect_while_uncertain(): return false
    stale_commit.call()
    await process_frame
    if witness.CanResolve() or witness.Revision() != 0:
        _fail("stale commitment escaped the uncertain-operation lock")
        return false
    _signal_host("recover")
    return true

func _inspect_while_uncertain() -> bool:
    var draft: String = witness.Draft()
    var card := _tabletop_card_named("Black Cat")
    if card == null or not await _pointer_activate_card_body(card): return false
    var inspector := main.find_child("CardInspector", true, false) as Control
    if not inspector.visible:
        _fail("uncertain response disabled read-only card inspection")
        return false
    await preload("res://smoke/native_keyboard_traversal.gd").key(self, KEY_ESCAPE)
    if inspector.visible or witness.Draft() != draft or witness.CanResolve():
        _fail("inspection changed a locked draft or reopened mutation")
        return false
    return true

func _end_phase_contract() -> bool:
    var end_turn := _button_named("End turn")
    if end_turn == null or not await _pointer_activate(end_turn): return false
    if not await _wait_for(func() -> bool: return witness.DecisionKind() == "EndPhaseDiscards"): return false
    if not await _choose_target(witness.Anchor("Black Cat")): return false
    if not await _cancel_discard_and_restage(): return false
    if not await preload("res://smoke/native_scale_checkpoint.gd").verify(self, "end-phase-discard"): return false
    if witness.Revision() != 1:
        _fail("end-phase selection committed before confirmation")
        return false
    var commit := _task_commit()
    if commit == null or commit.disabled or "1" not in commit.text:
        _fail("end-phase discard lacks a counted explicit commitment")
        return false
    if not await _pointer_activate(commit): return false
    return await _wait_for(func() -> bool: return witness.Revision() == 2)

func _cancel_discard_and_restage() -> bool:
    var clear := _task_clear_draft()
    if clear == null or not await _pointer_activate(clear): return false
    if witness.Revision() != 1 or witness.DecisionKind() != "EndPhaseDiscards":
        _fail("clearing staged discard changed the authoritative prompt")
        return false
    return await _choose_target(witness.Anchor("Black Cat"))
