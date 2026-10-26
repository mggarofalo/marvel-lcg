extends RefCounted

# Prepared cross-pack component scene; actual full-book Lockjaw, no public Ms Marvel admission.
static func run(smoke) -> bool:
    var anchor: int = smoke.witness.AreaAnchor("Lockjaw", "DiscardPile")
    if anchor < 0:
        smoke._fail("actual Lockjaw is not in the displayed discard pile")
        return false
    if not await _select(smoke): return false
    if not await _pay(smoke): return false
    if not await smoke._commit_once("Lockjaw"): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 1 and \
        smoke.witness.AreaAnchor("Lockjaw", "AlliesArea") == anchor and \
        smoke.witness.AreaAnchor("Lockjaw", "DiscardPile") == -1,
        "one explicit commitment plays the exact discarded Lockjaw as an ally"): return false
    if not await smoke._wait_for_result_motion(): return false
    await _capture(smoke, "lockjaw-result")
    print("LOCKJAW_PREPARED_CROSS_PACK discard-to-ally paid=4 revision=1")
    return true

static func _select(smoke) -> bool:
    if not await smoke.ui.click(smoke.ui.named("CompleteChoiceSheet")): return false
    if not await smoke.ui.expect(func(): return _offers(smoke).size() == 1,
        "one explicit ordinary Lockjaw play offer in complete choices"): return false
    if not await smoke.ui.expect(func(): return "Discard pile" in smoke._visible_text(smoke.main),
        "discard-origin play is grouped under its actual source pile"): return false
    await _capture(smoke, "lockjaw-discard-choice")
    var offers: Array = _offers(smoke)
    if offers.size() != 1:
        smoke._fail("discarded Lockjaw must have one explicit ordinary play offer")
        return false
    if not await smoke.ui.click(smoke.ui.Locator.control(offers[0])): return false
    if not await smoke.ui.expect(func(): return smoke._payment_modal() != null,
        "discard-origin Lockjaw opens ordinary payment"): return false
    return true

static func _pay(smoke) -> bool:
    var commit: Button = smoke._payment_button("Submit")
    if commit == null or not commit.disabled:
        smoke._fail("unpaid Lockjaw must require four resources")
        return false
    for title in ["Energy", "Genius"]:
        var source: Button = smoke._visible_button_beginning(smoke._payment_modal(), "Discard " + title)
        if source == null or not await smoke.ui.click(smoke.ui.Locator.control(source)): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 0,
        "payment staging leaves the discarded ally and authoritative revision unchanged"): return false
    if not _explanation(smoke): return false
    await _capture(smoke, "lockjaw-payment")
    return true

static func _explanation(smoke) -> bool:
    var copy: String = smoke._visible_text(smoke._payment_modal())
    if "Play Lockjaw" not in copy or "Pay 4" not in copy or "4 resources selected" not in copy \
        or "Pay and play Lockjaw" not in copy:
        smoke._fail("ordinary payment lacks Lockjaw source and four-resource cost: " + copy)
        return false
    return true

static func _offers(smoke) -> Array:
    return smoke.main.find_children("Affordance*", "Button", true, false).filter(
        func(node): return node.is_visible_in_tree() and "Lockjaw" in smoke._visible_text(node))

static func _capture(smoke, filename: String) -> void:
    if DisplayServer.get_name() == "headless": return
    await smoke.process_frame
    await RenderingServer.frame_post_draw
    var directory := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
    DirAccess.make_dir_recursive_absolute(directory)
    smoke.render_viewport.get_texture().get_image().save_png(directory.path_join(filename + ".png"))
