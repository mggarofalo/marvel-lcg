extends SceneTree

func _initialize() -> void:
    _run.call_deferred()

func _run() -> void:
    root.size = Vector2i(1920, 1080)
    var probe = preload("res://smoke/EncounterGroupsNativeProbe.cs").new()
    root.add_child(probe)
    var args := OS.get_cmdline_user_args()
    var path := args[0] if args.size() > 0 else OS.get_environment("MARVEL_GROUP_FIXTURE")
    var extreme := "--extreme" in args
    if extreme: path += ".extreme"
    var json := FileAccess.get_file_as_string(path)
    var fixture: Dictionary = JSON.parse_string(json)
    probe.Show(json)
    for unused in range(8): await process_frame
    var surface = probe.get_node("EncounterGroupFixture")
    var cards = surface.find_children("ProceduralCard*", "Control", true, false)
    var checks := [_check_bounds(surface, cards), _check_piles(surface, fixture),
        await _check_hover(surface, cards, fixture), await _check_upcoming(surface, probe, fixture, extreme)]
    for failure in checks:
        if not failure.is_empty():
            push_error("WRECKING_LAYOUT_FAILED " + failure)
            quit(1)
            return
    await _capture()
    print("WRECKING_LAYOUT_CONTRACT_OK groups=4 piles=8 direct-cards=9 no-game=true")
    probe.queue_free()
    await process_frame
    quit(0)

func _check_bounds(surface: Control, cards: Array[Node]) -> String:
    if surface.find_children("EncounterGroup*", "Label", true, false).size() != 4:
        return "missing named villain groups"
    if cards.size() < 9: return "missing villain/scheme/main cards"
    for card in cards:
        if not Rect2(Vector2.ZERO, Vector2(root.size)).encloses(card.get_global_rect()):
            return "offscreen " + str(card.name)
    for i in range(cards.size()):
        for j in range(i + 1, cards.size()):
            if cards[i].get_global_rect().intersects(cards[j].get_global_rect()):
                return "overlapping cards " + str(cards[i].name) + " " + str(cards[j].name)
    return ""

func _check_piles(surface: Control, fixture: Dictionary) -> String:
    var piles: Array[Node] = []
    for group in fixture["EncounterGroups"]:
        for key in ["Deck", "Discard"]:
            var pile = surface.find_child("InspectPile" + str(int(group[key])), true, false)
            if pile != null: piles.append(pile)
    return "" if piles.size() == 8 else "missing or conflated encounter piles"

func _printed(fixture: Dictionary) -> Dictionary:
    var printed: Dictionary = {}
    for area in fixture["Areas"]:
        for card in area["Cards"]:
            if card["TargetId"] != null: printed[int(card["TargetId"])] = card["RulesText"]
    return printed

func _check_hover(surface: Control, cards: Array[Node], fixture: Dictionary) -> String:
    var printed := _printed(fixture)
    for card in cards:
        var motion := InputEventMouseMotion.new()
        motion.position = card.get_global_rect().get_center()
        motion.global_position = motion.position
        Input.parse_input_event(motion)
        await process_frame
        await process_frame
        var expected := int(str(card.name).trim_prefix("ProceduralCard"))
        if int(surface.get_meta("preview_id", -1)) != expected:
            return "unreachable card preview " + str(card.name)
        if str(surface.get_meta("preview_text", "")) != str(printed[expected]):
            return "truncated or wrong printed preview"
    return ""

func _future_id(fixture: Dictionary, group_id: int) -> int:
    for group in fixture["EncounterGroups"]:
        if int(group["Group"]) == group_id: return int(group["Stages"][-1])
    return -1

func _click(button: Control) -> void:
    var press := InputEventMouseButton.new()
    press.position = button.get_global_rect().get_center()
    press.button_index = MOUSE_BUTTON_LEFT
    press.pressed = true
    Input.parse_input_event(press)
    await process_frame
    press.pressed = false
    Input.parse_input_event(press)
    for unused in range(4): await process_frame

func _check_upcoming(surface: Control, probe: Node, fixture: Dictionary, extreme: bool) -> String:
    var upcoming = surface.find_children("EncounterUpcoming*", "Button", true, false)
    if upcoming.size() != (4 if extreme else 0): return "wrong per-group future-stage controls"
    for button in upcoming:
        var group_id := int(str(button.name).trim_prefix("EncounterUpcoming"))
        await _click(button)
        if probe.InspectorTarget() != _future_id(fixture, group_id): return "wrong future villain stage"
        var popup = root.find_child("PileInspector", true, false)
        var rules = popup.find_child("RulesText", true, false)
        if rules == null or rules.scroll_active or rules.get_content_height() > rules.size.y + 1:
            return "future-stage preview does not show full text"
        popup.hide()
        await process_frame
    return ""

func _capture() -> void:
    await create_timer(0.3).timeout
    var capture := OS.get_environment("MARVEL_GROUP_CAPTURE")
    if capture.is_empty(): return
    await RenderingServer.frame_post_draw
    root.get_texture().get_image().save_png(capture)
