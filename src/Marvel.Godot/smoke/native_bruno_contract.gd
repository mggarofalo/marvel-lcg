extends RefCounted

# Synthetic cross-pack fixture; actual Bruno full-book actions and engine offers.
static func run(smoke) -> bool:
    if not await smoke._select_attached_action(smoke.witness.Anchor("Bruno Carrelli"), "Action"): return false
    var start: Button = smoke._task_commit()
    if start == null or start.disabled:
        smoke._fail("actual Bruno retrieval has no explicit commitment")
        return false
    if not await smoke.ui.click(smoke.ui.Locator.control(start)): return false
    if not await smoke._wait_for(func() -> bool: return smoke.witness.Revision() == 1): return false
    var sheet := smoke.main.find_child("CompleteChoiceSheet", true, false) as Button
    if smoke._search_frame() == null and sheet != null and sheet.is_visible_in_tree():
        if not await smoke.ui.click(smoke.ui.Locator.control(sheet)): return false
    var ids: Array = Array(smoke.witness.CandidateIds())
    if ids.size() != 4:
        smoke._fail("actual Bruno must offer all four stored candidates")
        return false
    var count := int(smoke.contract.trim_prefix("bruno-"))
    if not await _stage(smoke, ids, count): return false
    return await _confirm(smoke, count)

static func _stage(smoke, ids: Array, count: int) -> bool:
    if not await _toggle(smoke, ids[0]): return false
    if not await _selected(smoke, [ids[0]]): return false
    if not await _toggle(smoke, ids[0]): return false
    if not await _selected(smoke, []): return false
    for index in count:
        if not await _toggle(smoke, ids[index]): return false
    if not await _selected(smoke, ids.slice(0, count)): return false
    if count == 3:
        var fourth: Button = _target(smoke, ids[3])
        if fourth == null or not fourth.disabled:
            smoke._fail("fourth Bruno candidate remains selectable at maximum")
            return false
    return true

static func _confirm(smoke, count: int) -> bool:
    var copy: String = smoke._visible_text(smoke.main)
    if "Bruno" not in copy or "3" not in copy:
        smoke._fail("Bruno choice lacks source or bounded selection explanation")
        return false
    var submit: Button = _submit(smoke)
    if submit == null or submit.disabled:
        smoke._fail("Bruno zero/partial/max draft has no enabled confirmation")
        return false
    print("BRUNO_SYNTHETIC_NATIVE staged=%d commit=%s revision=1" % [count, submit.text])
    await _capture_selection(smoke, count)
    if not await smoke.ui.click(smoke.ui.Locator.control(submit)): return false
    return await smoke._wait_for(func() -> bool: return smoke.witness.Revision() == 2)

static func _capture_selection(smoke, count: int) -> void:
    if DisplayServer.get_name() != "headless":
        await smoke.process_frame
        await RenderingServer.frame_post_draw
        var destination := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
        DirAccess.make_dir_recursive_absolute(destination)
        smoke.render_viewport.get_texture().get_image().save_png(destination.path_join("bruno-%d-selection.png" % count))

static func _target(smoke, id: int) -> Button:
    var matches: Array[Node] = smoke.main.find_children("Target%d" % id, "Button", true, false).filter(func(node): return node.is_visible_in_tree())
    return matches[0] as Button if matches.size() == 1 else null

static func _toggle(smoke, id: int) -> bool:
    var control: Button = _target(smoke, id)
    if control == null:
        smoke._fail("missing unique real Bruno candidate control: %d" % id)
        return false
    return await smoke.ui.click(smoke.ui.Locator.control(control))

static func _selected(smoke, expected: Array) -> bool:
    return await smoke.ui.expect(func(): return Array(smoke.witness.SelectedTargets()) == expected and smoke.witness.Revision() == 1,
        "Bruno reversible draft retains exact physical cards without commitment")

static func _submit(smoke) -> Button:
    var frame: Control = smoke.main.find_child("CompleteChoicesFrame", true, false)
    if frame != null and frame.is_visible_in_tree(): return frame.find_child("Submit", true, false) as Button
    return smoke._task_commit()
