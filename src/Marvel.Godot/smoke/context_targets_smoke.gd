extends "res://smoke/context_menu_smoke.gd"

func _run() -> void:
    if not await _open_setup(load("res://Main.tscn") as PackedScene): return
    await _configure_seeded_game()
    _select_named_option(_node("Setup/Selections/Fields/Grid/Scenario") as OptionButton, "Ultron")
    if not await _pointer_activate(_button_named("Start game")): return
    if not await _keep_recipient_opening(): return
    if not await _change_context_form(): return
    if not await _attack_other_player_minion(): return
    if not await _release_table_resources(): return
    print("CONTEXT_TARGETS_SMOKE_OK")
    quit(0)

func _change_context_form() -> bool:
    var identity := _visible_cards_named("Peter Parker")[-1] as Control
    if not await _open_context(identity): return false
    var form := _context_row_containing("Change form")
    if form == null:
        _fail("identity menu omitted change form")
        return false
    var before := _recipient_revision()
    if not await _pointer_activate(form): return false
    if not await _wait_for(func() -> bool: return _recipient_revision() == before + 1): return false
    if not await _wait_for_result_motion(): return false
    return true

func _attack_other_player_minion() -> bool:
    var hero := _visible_cards_named("Spider-Man")[-1] as Control
    if not await _open_context(hero): return false
    var attack := _context_row_containing("Attack")
    if attack == null:
        _fail("Ultron opening has no basic attack menu")
        return false
    await _hover_context(attack)
    if not await _wait_for(func() -> bool: return main.find_child("Menu1Item0", true, false) != null): return false
    await process_frame
    var groups := main.find_children("Menu1Item*", "Button", true, false)
    var player_one: Button
    var player_two: Button
    for row in groups:
        print("TARGET_GROUP ", row.text)
        if row.text.begins_with("Player 1 minions"): player_one = row
        if row.text.begins_with("Player 2 minions"): player_two = row
    if player_one == null or player_two == null:
        _fail("two-player Ultron minions were not grouped by engaged seat")
        return false
    return await _commit_minion(player_two)

func _commit_minion(player_two: Button) -> bool:
    await _hover_context(player_two)
    if not await _wait_for(func() -> bool: return main.find_child("Menu2Item0", true, false) != null): return false
    var target := main.find_child("Menu2Item0", true, false) as Button
    await _hover_context(target)
    var inspector := main.find_child("CardInspector", true, false) as Control
    if not await _wait_for(func() -> bool: return inspector.visible and inspector.get_meta("inspected_card_anchor", -1) == target.get_meta("offered_target_anchor")):
        _fail("hovered target did not replace source card preview")
        return false
    if not await _capture_checkpoint("context-engaged-minions"): return false
    var before := _recipient_revision()
    if not await _pointer_activate(target): return false
    if not await _wait_for(func() -> bool: return _recipient_revision() == before + 1):
        _fail("menu did not commit the selected other-player minion attack once")
        return false
    return true

func _hover_context(control: Control) -> void:
    await process_frame
    var point := control.get_global_rect().get_center()
    var motion := InputEventMouseMotion.new()
    motion.position = point
    motion.global_position = point
    render_viewport.push_input(motion, true)
    await process_frame
    await process_frame
