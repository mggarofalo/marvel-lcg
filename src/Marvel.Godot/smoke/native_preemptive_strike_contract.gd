extends RefCounted

# Prepared cross-pack component scene; actual authored 05014, no public Ms Marvel admission.
static func run(smoke) -> bool:
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 0 and \
        _readable_context(smoke._visible_text(smoke.main)),
        "individual boost interrupt identifies the attacking villain and answering hero"): return false
    await _capture(smoke, "preemptive-strike-interrupt")
    if not await smoke.ui.click(smoke.ui.button("Preemptive Strike")): return false
    if not await _pay(smoke): return false
    if not await smoke._commit_once("Preemptive Strike"): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 1 and smoke.witness.DecisionKind() == "PlayerAction",
        "one explicit event commitment completes the activation and returns to the player phase"): return false
    if not await smoke._wait_for_result_motion(): return false
    await _capture(smoke, "preemptive-strike-result")
    print("PREEMPTIVE_STRIKE_PREPARED_CROSS_PACK paid=1 revision=1")
    return true

static func _pay(smoke) -> bool:
    if not await smoke.ui.expect(func(): return smoke._payment_modal() != null,
        "actual boost interrupt opens resource payment"): return false
    var copy: String = smoke._visible_text(smoke._payment_modal())
    if "Preemptive Strike" not in copy or "Pay 1" not in copy or not _readable_context(copy):
        smoke._fail("interrupt payment lacks its actual source and one-resource cost: " + copy)
        return false
    var submit: Button = smoke._payment_button("Submit")
    if submit == null or not submit.disabled:
        smoke._fail("unpaid Preemptive Strike must not commit")
        return false
    var payment: Button = smoke._visible_button_beginning(smoke._payment_modal(), "Discard Swinging Web Kick")
    if payment == null or not await smoke.ui.click(smoke.ui.Locator.control(payment)): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 0,
        "staging the actual hand resource preserves the interrupt revision"): return false
    if "1 resource selected" not in smoke._visible_text(smoke._payment_modal()):
        smoke._fail("one physical hand payment must explain its selected contribution")
        return false
    await _capture(smoke, "preemptive-strike-payment")
    return true

static func _capture(smoke, filename: String) -> void:
    if DisplayServer.get_name() == "headless": return
    await smoke.process_frame
    await RenderingServer.frame_post_draw
    var directory := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
    DirAccess.make_dir_recursive_absolute(directory)
    smoke.render_viewport.get_texture().get_image().save_png(directory.path_join(filename + ".png"))

static func _readable_context(copy: String) -> bool:
    return "Rhino" in copy and "Spider-Man" in copy and "Boost card revealed" in copy and \
        "WhenBoostCardTurnedFaceup" not in copy and "2 uncancelled boost icons" in copy and \
        "Boost abilities and later effects can change the result" in copy
