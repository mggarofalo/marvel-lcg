extends RefCounted

# Synthetic global paid interrupt: the real host owns both imminent physical recipients.
static func run(smoke) -> bool:
    if not await _select_second_recipient(smoke): return false
    if not await _stage_payment(smoke): return false
    return await _commit_and_verify_remaining(smoke)

static func _select_second_recipient(smoke) -> bool:
    if not await smoke.ui.expect(func(): return _contexts(smoke, "copy 1 of 2").size() == 1 and \
        _contexts(smoke, "copy 2 of 2").size() == 1,
        "each same-title imminent recipient has a readable physical copy caption"): return false
    var context = _contexts(smoke, "copy 2 of 2")[0]
    if "Hydra Bomber" not in context.text or "Spider-Man" not in context.text or "engaged enemies area" not in context.text.to_lower():
        smoke._fail("recipient copy lacks title, owner and readable location: " + context.text)
        return false
    var actions = smoke.main.find_children("WindowAction*", "Button", true, false).filter(func(button): return button.is_visible_in_tree())
    if actions.size() != 2 or actions[0].text == actions[1].text or "would be defeated" not in actions[1].text:
        smoke._fail("shared window lacks two same-source same-title before-defeat actions")
        return false
    # Resolve the second row by its explicit context/handle association, never first text match.
    var second_action = context
    await _capture(smoke, "defeat-context-options")
    if not await smoke.ui.click(smoke.ui.Locator.control(second_action)): return false
    return true

static func _stage_payment(smoke) -> bool:
    if not await smoke.ui.expect(func(): return smoke.ui.named("Submit").all().size() == 1, "second exact offer enters ordinary payment composition"): return false
    var submit = smoke.ui.named("Submit").all()[0]
    if submit == null or not submit.disabled or smoke.witness.Revision() != 0:
        smoke._fail("selecting a causal recipient bypassed its unpaid cost")
        return false
    var payment = smoke.ui.named("Resource%d" % smoke.witness.Anchor("Energy")).all()[0]
    await smoke._scroll_control_into_view(payment)
    if payment == null or not await smoke.ui.click(smoke.ui.Locator.control(payment)): return false
    if smoke.witness.Revision() != 0:
        smoke._fail("staging payment committed the interrupt")
        return false
    if not smoke._control_is_fully_visible(smoke.ui.named("Submit").all()[0]):
        smoke._fail("payment commitment is clipped or requires scrolling")
        return false
    await _capture(smoke, "defeat-context-payment")
    if "copy 2 of 2" not in smoke._visible_text(smoke.main):
        smoke._fail("payment composition lost the exact selected physical recipient")
        return false
    return true

static func _commit_and_verify_remaining(smoke) -> bool:
    await smoke._scroll_control_into_view(smoke.ui.named("Submit").all()[0])
    if not await smoke.ui.click(smoke.ui.named("Submit")): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 1 and \
        smoke.main.find_children("WindowAction*", "Button", true, false).filter(func(button): return button.is_visible_in_tree()).size() == 1,
        "one explicit paid commitment leaves one recipient opportunity"): return false
    if _contexts(smoke, "copy 1 of 2").size() != 1 or _contexts(smoke, "copy 2 of 2").size() != 0:
        smoke._fail("choosing the second physical recipient did not retain only the first opportunity")
        return false
    await _capture(smoke, "defeat-context-remaining-first")
    return true

static func _contexts(smoke, marker: String) -> Array:
    return smoke.main.find_children("WindowAction*", "Button", true, false).filter(func(button): return button.is_visible_in_tree() and marker in button.text)

static func _capture(smoke, filename: String) -> void:
    if DisplayServer.get_name() == "headless": return
    await smoke.process_frame
    await RenderingServer.frame_post_draw
    var directory := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
    DirAccess.make_dir_recursive_absolute(directory)
    smoke.render_viewport.get_texture().get_image().save_png(directory.path_join(filename + ".png"))
