extends "res://smoke/context_menu_smoke.gd"

func _run() -> void:
    if not await _open_setup(load("res://Main.tscn") as PackedScene): return
    await _configure_seeded_game()
    _select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Iron Man")
    _select_named_option(_node("Setup/Selections/Fields/Grid/SecondHero"), "Captain Marvel")
    if not await _pointer_activate(_button_named("Start game")): return
    if not await _keep_recipient_opening(): return
    if not await _start_futurist(): return
    if not await _choose_looked_at_card(): return
    if not await _release_table_resources(): return
    print("CONTEXT_SEARCH_SMOKE_OK")
    quit(0)

func _start_futurist() -> bool:
    var identity := _visible_cards_named("Tony Stark")[-1] as Control
    if not await _open_context(identity): return false
    var action := _context_row_containing("Futurist")
    if action == null:
        _fail("identity menu omitted printed Futurist action")
        return false
    var before := _recipient_revision()
    if not await _pointer_activate(action): return false
    if not await _wait_for(func() -> bool: return _recipient_revision() == before + 1): return false
    if not await _wait_for_result_motion(): return false
    return true

func _choose_looked_at_card() -> bool:
    var local := _payment_modal()
    if local == null or not "Previous choice committed" in _visible_text(local):
        _fail("Futurist did not retain localized look/search continuation")
        return false
    var choices := local.find_children("Affordance*", "Button", true, false)
    if choices.size() != 3:
        _fail("Futurist continuation did not expose all three offered cards")
        return false
    var choice := choices[0] as Button
    choice.grab_focus()
    await process_frame
    if not await _capture_checkpoint("local-search"): return false
    var before := _recipient_revision()
    if not await _keyboard_activate(choice, 3): return false
    var submit := _payment_modal().find_child("Submit", true, false) as Button
    if submit != null and not submit.disabled:
        if not await _keyboard_activate(submit, 3): return false
    if not await _wait_for(func() -> bool: return _recipient_revision() == before + 1):
        _fail("explicit looked-at card choice did not commit exactly once")
        return false
    return true
