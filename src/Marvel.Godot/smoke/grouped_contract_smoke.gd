extends SceneTree

var specimen: Control

func _initialize() -> void:
    call_deferred("_run")

func _run() -> void:
    root.size = Vector2i(1920, 1080)
    specimen = preload("res://smoke/GroupedDecisionNativeProbe.cs").new()
    root.add_child(specimen)
    await process_frame
    await process_frame
    if not await _click("Affordance7"): return
    if not _disabled("Submit"):
        _fail("empty grouped choice is ready")
        return
    if not await _click("Group0"): return
    if not await _scale_selection(): return
    if not await _click("Submit"): return
    if specimen.Submissions() != 1 or specimen.SubmittedTargets() != "12,11":
        _fail("explicit submission did not retain the exact ordered group")
        return
    print("SYNTHETIC_GROUPED_CONTRACT_OK ordered=true replacement=true explicit-commit=true scales=11")
    quit(0)

func _scale_selection() -> bool:
    if specimen.Selection() != "12,11":
        _fail("first group's authored order was lost")
        return false
    if not await _click("Group1"): return false
    if specimen.Selection() != "11,13":
        _fail("changing groups retained an earlier member")
        return false
    if not await _click("Group0"): return false
    for scale in range(50, 151, 10):
        specimen.SetReadingScale(scale)
        await process_frame
        await process_frame
        if specimen.Selection() != "12,11" or specimen.Submissions() != 0:
            _fail("scaling changed the synthetic selection or committed it")
            return false
        if _disabled("Submit"):
            _fail("pooled min/max rejected a complete authoritative group")
            return false
    return true

func _disabled(name: String) -> bool:
    var button := specimen.find_child(name, true, false) as Button
    return button == null or button.disabled

func _click(name: String) -> bool:
    var button := specimen.find_child(name, true, false) as Button
    if button == null or button.disabled or not button.is_visible_in_tree():
        _fail("missing enabled synthetic control " + name)
        return false
    var rect := button.get_global_rect()
    if not Rect2(Vector2.ZERO, Vector2(root.size)).encloses(rect):
        _fail("synthetic control is clipped: " + name)
        return false
    for pressed in [true, false]:
        var event := InputEventMouseButton.new()
        event.button_index = MOUSE_BUTTON_LEFT
        event.pressed = pressed
        event.position = rect.get_center()
        root.push_input(event, true)
        await process_frame
    return true

func _fail(message: String) -> void:
    push_error(message)
    quit(1)
