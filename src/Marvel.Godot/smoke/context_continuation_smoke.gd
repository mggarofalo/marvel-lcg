extends "res://smoke/context_targets_smoke.gd"

func _run() -> void:
    if not await _open_setup(load("res://Main.tscn") as PackedScene): return
    await _configure_seeded_game()
    _select_named_option(_node("Setup/Selections/Fields/Grid/Scenario") as OptionButton, "Ultron")
    if not await _pointer_activate(_button_named("Start game")): return
    if not await _keep_recipient_opening(): return
    if not await _change_context_form(): return
    if not await _begin_local_event(): return
    if not await _resume_payment(): return
    if not await _commit_payment_and_target(): return
    if not await _release_table_resources(): return
    print("CONTEXT_CONTINUATION_SMOKE_OK")
    quit(0)

func _begin_local_event() -> bool:
    var cards := _visible_cards_named("Swinging Web Kick")
    if cards.is_empty():
        _fail("legal seed omitted Swinging Web Kick")
        return false
    if not await _open_context(cards[-1]): return false
    var play := _context_row_containing("Swinging Web Kick")
    if play == null or not await _pointer_activate(play): return false
    if not await _wait_for(func() -> bool: return _payment_modal() != null):
        _fail("event did not open local payment")
        return false
    var frame := _payment_modal() as Control
    if not render_viewport.get_visible_rect().encloses(frame.get_global_rect()):
        _fail("local payment extends outside the viewport")
        return false
    return await _capture_checkpoint("local-payment")

func _resume_payment() -> bool:
    var source := _payment_button("Resource*")
    if source == null: return false
    var selected_name := source.name
    var card_id := str(selected_name).trim_prefix("Resource")
    var table_source := main.find_child("Card" + card_id + "Generator", true, false) as Button
    if table_source == null:
        _fail("local payment omitted shared table resource affordance")
        return false
    if not await _keyboard_activate(table_source): return false
    var staged := _payment_button(selected_name)
    if staged == null or not staged.button_pressed:
        _fail("local payment did not stage the selected resource")
        return false
    if not await preload("res://smoke/table_bounds_continuity.gd").fractional_translation(self, staged): return false
    var before := _recipient_revision()
    var close := _payment_button("LocalReturnToTable")
    if close == null or not await _pointer_activate(close): return false
    if not await _wait_for(func() -> bool: return _payment_modal() == null): return false
    var resume := _button_named("Resume current choice")
    if resume == null or not await _keyboard_activate(resume):
        _fail("dismissed payment was not resumable")
        return false
    var restored := _payment_button(selected_name)
    if restored == null or not restored.button_pressed or _recipient_revision() != before:
        _fail("return/resume lost selected payment or committed it")
        return false
    return true

func _commit_payment_and_target() -> bool:
    var before := _recipient_revision()
    if not await _pay_local_resources(): return false
    if not await _wait_for(func() -> bool: return _recipient_revision() == before + 1): return false
    if not await _wait_for_result_motion(): return false
    var local := _payment_modal()
    if local == null or not "Previous choice committed" in _visible_text(local):
        _fail("paid event lost local causal continuation: " + _visible_text(main))
        return false
    if not await _capture_checkpoint("local-followup"): return false
    before = _recipient_revision()
    await _menu_key(KEY_ESCAPE)
    if not await _wait_for(func() -> bool: return _payment_modal() == null): return false
    if _recipient_revision() != before:
        _fail("Escape committed or refunded the mandatory target choice")
        return false
    var resume := _button_named("Resume current choice")
    if resume == null or not await _keyboard_activate(resume): return false
    return await _choose_event_target(before)

func _choose_event_target(before: int) -> bool:
    var target: Button
    for candidate in _payment_modal().find_children("Target*", "Button", true, false):
        if "Ultron" in candidate.text: target = candidate
    if target == null:
        for candidate in _payment_modal().find_children("Affordance*", "Button", true, false):
            if candidate.text.begins_with("Ultron"): target = candidate
    if target == null:
        _fail("local followup omitted offered villain target")
        return false
    if not await _keyboard_activate(target, 3): return false
    if not await _wait_for(func() -> bool: return _recipient_revision() == before + 1):
        _fail("explicit followup target did not commit once")
        return false
    return await _verify_local_result()

func _verify_local_result() -> bool:
    if not await _wait_for_result_motion(): return false
    var receipt := main.find_child("LocalDecisionReceipt", true, false)
    if receipt == null or not "Ultron" in _visible_text(receipt):
        _fail("accepted local action omitted its authoritative result")
        return false
    if not render_viewport.get_visible_rect().encloses((receipt as Control).get_global_rect()):
        _fail("local result extends outside the viewport")
        return false
    return await _capture_checkpoint("local-result")

func _pay_local_resources() -> bool:
    for step in 12:
        var submit := _payment_button("Submit")
        if submit != null and not submit.disabled: return await _pointer_activate(submit)
        var next: Button
        for candidate in _payment_modal().find_children("Resource*", "Button", true, false):
            if not candidate.disabled and not candidate.button_pressed:
                next = candidate
                break
        if next == null or not await _pointer_activate(next):
            _fail("local payment cannot select enough offered resources")
            return false
    return false
