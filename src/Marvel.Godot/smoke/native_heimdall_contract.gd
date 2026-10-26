extends RefCounted

# Public Thor setup; ally entry and encounter stack are labelled prepared fixture input.
static func run(smoke) -> bool:
    if not await _open_look(smoke): return false
    var group: Array[int] = await _discard(smoke)
    if group.size() != 3: return false
    return await _return_in_order(smoke, group)

static func _open_look(smoke) -> bool:
    if not await smoke.ui.expect(func(): return _choices(smoke, "WindowAction").size() == 1,
        "Heimdall entry offers one explicit optional response"): return false
    if "Heimdall" not in smoke._visible_text(smoke.main): return false
    if not await _capture(smoke, "heimdall-entry"): return false
    if not await smoke.ui.click(smoke.ui.Locator.control(_choices(smoke, "WindowAction")[0])): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 1,
        "one response commitment opens private look initiation"): return false
    if not await smoke.ui.click(smoke.ui.button("Look at encounter cards")): return false
    return await _click_commit(smoke, "Look", 1)

static func _discard(smoke) -> Array[int]:
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 2 and smoke._search_frame() != null,
        "private look exposes exactly three encounter candidates"): return []
    var cards: Array = _choices(smoke, "Affordance")
    if cards.size() != 3: return []
    var group: Array[int] = []
    for choice in cards: group.append(int(str(choice.name).trim_prefix("Affordance")))
    if not await _capture(smoke, "heimdall-private-three"): return []
    if not await smoke.ui.click(smoke.ui.named("Affordance%d" % group[1])): return []
    if not await _click_commit(smoke, "Discard this card", 2): return []
    return group

static func _return_in_order(smoke, group: Array[int]) -> bool:
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 3,
        "one discarded physical encounter leaves an uncommitted ordered return"): return false
    if not await smoke.ui.click(smoke.ui.named("Affordance%d" % smoke.witness.Anchor("Heimdall"))): return false
    for id in [group[2], group[0]]:
        if not await _reach_target(smoke, "Target%d" % id): return false
        if not await smoke.ui.click(smoke.ui.named("Target%d" % id)): return false
    if Array(smoke.witness.SelectedTargets()) != [group[2], group[0]]: return false
    if "top-to-bottom" not in smoke._visible_text(smoke.main): return false
    if not await _capture(smoke, "heimdall-selected-order"): return false
    if not await _click_commit(smoke, "Return in selected order", 3): return false
    return await smoke.ui.expect(func(): return smoke.witness.Revision() == 4 and smoke.witness.DecisionKind() == "PlayerAction",
        "one explicit ordered-return commitment conceals the deck and returns to play")

static func _reach_target(smoke, node_name: String) -> bool:
    var locator = smoke.ui.named(node_name)
    if not await smoke.ui.expect_visible(locator): return false
    # Real Tab navigation exercises the scrolling body's focus-following access.
    for step in range(40):
        if smoke.render_viewport.gui_get_focus_owner() == locator.all()[0]:
            return await smoke.ui.expect_fully_visible_enabled(locator)
        if not await smoke.ui.press_key(KEY_TAB): return false
    return await smoke.ui.expect(func(): return false, "Tab reaches ordered target " + node_name, 0)

static func _choices(smoke, prefix: String) -> Array:
    return smoke.main.find_children(prefix + "*", "Button", true, false).filter(func(button): return button.is_visible_in_tree())

static func _click_commit(smoke, label: String, revision: int) -> bool:
    if not await smoke.ui.expect_fully_visible_enabled(smoke.ui.button(label)): return false
    var frame: Control = smoke._search_frame()
    if frame != null:
        if not await smoke.ui.expect(func():
            return frame.get_global_rect().encloses(smoke.ui.button(label).all()[0].get_global_rect()),
            "complete commitment rectangle inside decision modal"): return false
    if smoke.witness.Revision() != revision: return false
    return await smoke.ui.click(smoke.ui.button(label))

static func _capture(smoke, leaf: String) -> bool:
    if DisplayServer.get_name() == "headless": return true
    await smoke.process_frame
    await RenderingServer.frame_post_draw
    var directory := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
    DirAccess.make_dir_recursive_absolute(directory)
    return smoke.render_viewport.get_texture().get_image().save_png(directory.path_join(leaf + ".png")) == OK
