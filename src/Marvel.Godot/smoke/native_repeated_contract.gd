extends RefCounted

static func run(smoke) -> bool:
    var revision: int = smoke.witness.Revision()
    var hero: int = smoke.witness.Anchor("Spider-Man")
    var ally: int = smoke.witness.Anchor("Black Cat")
    if not await _open_allocation(smoke, hero): return false
    if not await _click(smoke, hero, "Add"): return false
    if not await _click(smoke, hero, "Add"): return false
    if not await _click(smoke, hero, "Remove"): return false
    if not await _click(smoke, hero, "Add"): return false
    var commit: Button = _submit(smoke)
    if commit == null or not commit.disabled:
        smoke._fail("two of three indirect damage assignments can be committed")
        return false
    if not await _click(smoke, ally, "Add"): return false
    if not _capacity_is_closed(smoke, hero, ally): return false
    if not await preload("res://smoke/native_scale_checkpoint.gd").verify(smoke, "repeated-target-allocation"): return false
    return await _commit_allocation(smoke, revision)

static func _open_allocation(smoke, hero: int) -> bool:
    var sheet := smoke.main.find_child("CompleteChoiceSheet", true, false) as Button
    if sheet != null and sheet.is_visible_in_tree():
        if not await smoke._pointer_activate(sheet): return false
    if smoke.main.find_child("Target%dAdd" % hero, true, false) != null: return true
    for choice in smoke.main.find_children("Affordance*", "Button", true, false):
        if choice.is_visible_in_tree() and not choice.disabled:
            return await smoke._pointer_activate(choice)
    smoke._fail("indirect damage has no reachable allocation editor")
    return false

static func _capacity_is_closed(smoke, hero: int, ally: int) -> bool:
    for anchor in [hero, ally]:
        var add := smoke.main.find_child("Target%dAdd" % anchor, true, false) as Button
        if add == null or not add.disabled:
            smoke._fail("repeated-target controls permit a fourth assignment")
            return false
    return true

static func _commit_allocation(smoke, revision: int) -> bool:
    if smoke.witness.Revision() != revision:
        smoke._fail("allocation editing committed before explicit confirmation")
        return false
    var commit := _submit(smoke)
    if commit == null or commit.disabled: return false
    if not await smoke._pointer_activate(commit): return false
    return await smoke._wait_for(func() -> bool: return smoke.witness.Revision() == revision + 1)

static func _click(smoke, anchor: int, operation: String) -> bool:
    var button := smoke.main.find_child("Target%d%s" % [anchor, operation], true, false) as Button
    if button == null or button.disabled:
        smoke._fail("missing legal repeated-target operation: " + operation)
        return false
    return await smoke._pointer_activate(button)

static func _submit(smoke) -> Button:
    var complete: Control = smoke.main.find_child("CompleteChoicesFrame", true, false)
    if complete != null and complete.is_visible_in_tree(): return complete.find_child("Submit", true, false)
    return smoke._task_commit()
