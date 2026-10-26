extends RefCounted

# Prepared cross-pack component scene; actual authored Melee, no public Ms Marvel admission.
static func run(smoke) -> bool:
    var guard: int = smoke.witness.Anchor("Hydra Mercenary")
    var villain: int = smoke.witness.AreaAnchor("Rhino", "VillainArea")
    if not await _pay(smoke): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 1 and Array(smoke.witness.OfferedAnchors()) == [guard],
        "Melee first choice offers only the Guard after paying exactly three resources"): return false
    if not await _choose_enemy(smoke, guard, 1): return false
    return await _second_choice(smoke, villain)

static func _pay(smoke) -> bool:
    if not await smoke._drag_to_prompt_owner_lane(smoke._tabletop_card_named("Melee")): return false
    if not await smoke._wait_for(func() -> bool: return smoke._payment_modal() != null): return false
    var commit: Button = smoke._payment_button("Submit")
    if commit == null or not commit.disabled:
        smoke._fail("unpaid Melee must require its three resources")
        return false
    for title in ["Energy", "Swinging Web Kick"]:
        var resource: Button = smoke._visible_button_beginning(smoke._payment_modal(), "Discard " + title)
        if resource == null or not await smoke.ui.click(smoke.ui.Locator.control(resource)): return false
    if smoke.witness.Revision() != 0: return false
    if not await smoke._commit_once("Melee"): return false
    return true

static func _second_choice(smoke, villain: int) -> bool:
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 2 and Array(smoke.witness.OfferedAnchors()) == [villain],
        "Guard defeat removes the first target and unlocks Rhino for a separate second choice"): return false
    if not await smoke.ui.expect(func(): return "Choose another enemy" in smoke._visible_text(smoke.main) and "Melee" in smoke._visible_text(smoke.main),
        "second choice retains Melee source and Choose another enemy explanation"): return false
    if not await smoke._wait_for_result_motion(): return false
    await _capture(smoke)
    if smoke.witness.Revision() != 2:
        smoke._fail("sole second enemy was submitted without explicit input")
        return false
    if not await _choose_enemy(smoke, villain, 2): return false
    return await smoke.ui.expect(func(): return smoke.witness.Revision() == 3 and smoke.witness.DecisionKind() == "PlayerAction",
        "second explicit commitment completes Melee and returns to the player turn")

static func _choose_enemy(smoke, anchor: int, revision: int) -> bool:
    var choices: Array = smoke.ui.named("Affordance%d" % anchor).all()
    if choices.size() == 1:
        if not await smoke.ui.click(smoke.ui.Locator.control(choices[0])): return false
        if not await smoke.ui.expect(func(): return smoke.witness.Revision() == revision,
            "staging a sole Melee choice does not commit"): return false
        var commit: Button = smoke._task_commit()
        if commit == null or commit.disabled: return false
        if not await smoke.ui.click(smoke.ui.Locator.control(commit)): return false
        return await smoke.ui.expect(func(): return smoke.witness.Revision() == revision + 1,
            "one explicit local Melee target commitment advances exactly one revision")
    var action = smoke._attached(smoke._attached_name(anchor, "Action"))
    if action == null:
        smoke._fail("Melee enemy lacks an explicit attached choice: %d" % anchor)
        return false
    if smoke.witness.Revision() != revision: return false
    if not await smoke._pointer_activate_attached(action): return false
    return await smoke.ui.expect(func(): return smoke.witness.Revision() == revision + 1,
        "one explicit Melee target commitment advances exactly one revision")

static func _capture(smoke) -> void:
    print("MELEE_PREPARED_CROSS_PACK second-choice explicit revision=2")
    if DisplayServer.get_name() == "headless": return
    await smoke.process_frame
    await RenderingServer.frame_post_draw
    var directory := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
    DirAccess.make_dir_recursive_absolute(directory)
    smoke.render_viewport.get_texture().get_image().save_png(directory.path_join("melee-second-choice.png"))
