extends RefCounted

const Driver = preload("res://smoke/automation/ui_driver.gd")
# The host answers every prompt up to the next checkpoint before it replies to a Synchronize, and it
# replays the session for each answer; the run to the reunion takes it about half a minute.
const SYNC_DEADLINE_MS := 90000

# A real Kang recipe through KangRecipeFactory (tests only). The host answers the prompts between
# checkpoints; the client opens, synchronizes and answers the join prompt with viewport input.
static func run(smoke) -> bool:
    var kang = preload("res://smoke/KangNativeWitness.cs").new()
    kang.Bind(smoke.main)
    var players := int(smoke.contract.substr(5))
    if not await _split(smoke, kang, players): return false
    if smoke.contract == "kang-3": return true
    if smoke.contract == "kang-4": return await _retained(smoke, kang)
    if not await _acting(smoke, kang): return false
    if not await _departure(smoke, kang): return false
    if not await _join(smoke, kang): return false
    return await _reunion(smoke, kang)

static func _split(smoke, kang, players: int) -> bool:
    if not await smoke.ui.expect(func(): return kang.Stage() == 2 and _surface(smoke) != null,
        "the opened Kang table is separated"): return false
    var areas: Array = kang.LocalAreas()
    if areas.size() != players:
        smoke._fail("expected one game area per player, got %s" % [areas])
        return false
    var seat := int(_surface(smoke).get_meta("expanded_seat"))
    var own := int(kang.SeatArea(seat))
    for area in areas:
        if not _column_names_its_players(smoke, kang, area) or not _column_marks_reach(smoke, area, own): return false
    if not _reach_is_dimmed(smoke, kang, seat, own): return false
    if not _central_and_shared(smoke, kang): return false
    if "Kang (II)" not in str(_tracker(smoke).get_meta("tracker_reading", "")) or "Kang (II)" not in _tracker(smoke).tooltip_text:
        smoke._fail("the tracker does not show the remaining Kang (II)")
        return false
    if not await _capture(smoke, "split"): return false
    return await _rules_open_and_close(smoke)

static func _rules_open_and_close(smoke) -> bool:
    if not await smoke.ui.click(smoke.ui.named("SeparatedTableRules")): return false
    if not await smoke.ui.expect(func():
        var panel = _surface(smoke).find_child("SeparatedTableRulesPanel", false, false)
        return panel != null and panel.is_visible_in_tree() and "turn order" in _text(smoke, "SeparatedTableRulesText"),
        "the separated-table rules open beside the table"): return false
    if not await _capture(smoke, "rules"): return false
    if not await smoke.ui.click(smoke.ui.named("SeparatedTableRules")): return false
    return await smoke.ui.expect(func(): return not _surface(smoke).find_child("SeparatedTableRulesPanel", false, false).visible,
        "the separated-table rules close again")

# Each column is titled by its players' names on one line; its Kang (II) stands under it.
static func _column_names_its_players(smoke, kang, area: int) -> bool:
    var frame = _surface(smoke).find_child("GameArea%d" % area, false, false)
    if frame == null or not frame.is_visible_in_tree():
        smoke._fail("game area %d has no visible column" % area)
        return false
    var title := _title(smoke, area)
    for name in kang.Names(area):
        if name not in title:
            smoke._fail("game area %d title %s does not name %s" % [area, title, name])
            return false
    for villain in kang.Villains(area):
        if _card(smoke, villain) == null:
            smoke._fail("game area %d does not show its Kang (II)" % area)
            return false
    return true

static func _column_marks_reach(smoke, area: int, own: int) -> bool:
    var facts := _facts(smoke, area)
    if area == own and ("YourArea" not in facts or "OutOfReach" in facts):
        smoke._fail("the inspected seat's area is not marked as theirs: %s" % [facts])
        return false
    if area != own and ("OutOfReach" not in facts or _header(smoke, area).find_child("FactOutOfReach", false, false) == null
            or "cannot affect another game area" not in _header(smoke, area).tooltip_text):
        smoke._fail("another area lacks its lock and the reason on hover: %s" % [facts])
        return false
    return true

static func _reach_is_dimmed(smoke, kang, seat: int, own: int) -> bool:
    var dimmed := 0
    for id in kang.OutOfReach(seat):
        var card = _card(smoke, id)
        if card == null: continue
        if not card.get_meta("out_of_reach", false) or card.modulate.a > 0.6:
            smoke._fail("out-of-reach card %d is not dimmed" % id)
            return false
        dimmed += 1
    for villain in kang.Villains(own):
        var card = _card(smoke, villain)
        if card.get_meta("out_of_reach", false) or card.modulate.a < 1.0:
            smoke._fail("the seat's own Kang is dimmed")
            return false
    if dimmed == 0:
        smoke._fail("no out-of-reach card is dimmed")
        return false
    return true

# Stage 2B and the shared encounter piles stand in the central column, between the game areas.
static func _central_and_shared(smoke, kang) -> bool:
    var central: Array = kang.CentralSchemes()
    if central.size() != 1 or _card(smoke, central[0]) == null or _card(smoke, central[0]).get_meta("out_of_reach", false):
        smoke._fail("central stage 2B is not shown within every seat's reach")
        return false
    var heading = _surface(smoke).find_child("GameAreaCentralHeading", false, false)
    if heading == null or "stage 4A" not in heading.tooltip_text or "share one encounter deck" not in heading.tooltip_text \
            or heading.find_child("FactShared", false, false) == null:
        smoke._fail("the central column does not mark and explain what is shared")
        return false
    var column: Rect2 = _rect(_surface(smoke).find_child("GameAreaCentral", false, false))
    for pile in kang.SharedPiles():
        var node = _surface(smoke).find_child("Pile%d" % pile, true, false)
        if node == null or not column.encloses(_rect(node)):
            smoke._fail("shared pile %d does not stand in the central column" % pile)
            return false
    var left := INF
    var right := -INF
    for area in kang.LocalAreas():
        var bounds := _rect(_surface(smoke).find_child("GameArea%d" % area, false, false))
        left = min(left, bounds.position.x)
        right = max(right, bounds.end.x)
    if column.position.x <= left or column.end.x >= right:
        smoke._fail("the central column is not between the game areas")
        return false
    return true

static func _acting(smoke, kang) -> bool:
    if not await _synchronize(smoke, func(): return kang.ActingArea() >= 0, "a villain-phase step resolving in one area"): return false
    var area := int(kang.ActingArea())
    if "Acting" not in _facts(smoke, area) or _header(smoke, area).find_child("ActingStep", false, false) == null \
            or "Villain phase step" not in _header(smoke, area).tooltip_text:
        smoke._fail("the acting area lacks its icon, step pips and step on hover")
        return false
    for other in kang.LocalAreas():
        if other != area and "Acting" in _facts(smoke, other):
            smoke._fail("more than one area is marked as acting")
            return false
    return await _capture(smoke, "acting")

static func _departure(smoke, kang) -> bool:
    if not await _synchronize(smoke, func(): return kang.DepartureSources().size() > 0, "a pending departure"): return false
    var source := int(kang.DepartureSources()[0])
    if "Departing" not in _facts(smoke, source) or "at the end of the phase" not in _header(smoke, source).tooltip_text:
        smoke._fail("the departing area lacks its arrow and the reason on hover")
        return false
    if not _destinations_lit(smoke, kang): return false
    return await _capture(smoke, "departure")

static func _destinations_lit(smoke, kang) -> bool:
    for area in kang.LocalAreas():
        if ("Destination" in _facts(smoke, area)) != (area in kang.DepartureDestinations()):
            smoke._fail("game area %d is lit as a destination when it is not one, or not lit when it is" % area)
            return false
    return true

static func _join(smoke, kang) -> bool:
    if not await _synchronize(smoke, func(): return kang.Trigger() == "ResolveKangPhaseJoin", "the join prompt"): return false
    if not _destinations_lit(smoke, kang): return false
    if not await _capture(smoke, "join"): return false
    if not await _answer_join(smoke, kang): return false
    var area := int(kang.LocalAreas()[0])
    if kang.SeatArea(0) != area or kang.SeatArea(1) != area or "YourArea" not in _facts(smoke, area):
        smoke._fail("the joined area does not hold both seats")
        return false
    return await _capture(smoke, "joined")

# Chooses the one named destination, then its named commitment.
static func _answer_join(smoke, kang) -> bool:
    var before := int(smoke.witness.Revision())
    # Each offer names the destination by its players and Kang (protocol 45 labels).
    var offers = smoke.main.find_children("Affordance*", "Button", true, false).filter(
        func(button): return button.is_visible_in_tree() and button.text.begins_with("Join ") and "game area" in button.text)
    if offers.size() != 1:
        smoke._fail("the two-player join prompt does not offer exactly one named destination")
        return false
    if not await smoke.ui.click(smoke.ui.Locator.control(offers[0])): return false
    if not await smoke.ui.expect(func(): return smoke._task_commit() != null and not smoke._task_commit().disabled,
        "choosing the destination enables its named commitment"): return false
    var commit = smoke._task_commit()
    if "join" not in commit.text.to_lower() or smoke.witness.Revision() != before:
        smoke._fail("the join commitment is not named for the destination, or choosing it committed: " + commit.text)
        return false
    if not await smoke.ui.click(smoke.ui.Locator.control(commit)): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == before + 1 and kang.LocalAreas().size() == 1,
        "the answered join leaves one game area holding both seats", Driver.COMMIT_DEADLINE_MS): return false
    return true

static func _reunion(smoke, kang) -> bool:
    if not await _synchronize(smoke, func(): return kang.Stage() == 3, "the reunion against Kang (III)"): return false
    if _surface(smoke).find_child("GameAreaCentral", false, false) != null:
        smoke._fail("the reunited table still shows separated columns")
        return false
    if "Defeat Kang (III) to win." not in _tracker(smoke).tooltip_text or "Kang (III)" not in str(_tracker(smoke).get_meta("tracker_reading", "")):
        smoke._fail("the reunited table lacks its progress toward winning")
        return false
    return await _capture(smoke, "reunion")

static func _retained(smoke, kang) -> bool:
    if not await _synchronize(smoke, func(): return kang.RetainedAreas().size() > 0, "a retained area"): return false
    var area := int(kang.RetainedAreas()[0])
    if "Retained" not in _facts(smoke, area) or "(retained)" not in _title(smoke, area):
        smoke._fail("the retained area is not marked")
        return false
    return await _capture(smoke, "retained")

static func _synchronize(smoke, reached: Callable, description: String) -> bool:
    if not await smoke.ui.click(smoke.ui.named("Synchronize")): return false
    return await smoke.ui.expect(func(): return reached.call() and _surface(smoke) != null, description,
        SYNC_DEADLINE_MS)

static func _surface(smoke) -> Control:
    return smoke.main.find_child("AstraTableSurface", true, false) as Control

static func _card(smoke, id: int) -> Control:
    return _surface(smoke).find_child("ProceduralCard%d" % id, false, false) as Control

static func _header(smoke, area: int) -> Control:
    return _surface(smoke).find_child("GameArea%dHeading" % area, false, false) as Control

# Every fact a column states: its header icons, its step pips and its frame's edge.
static func _facts(smoke, area: int) -> PackedStringArray:
    var frame = _surface(smoke).find_child("GameArea%d" % area, false, false)
    return PackedStringArray() if frame == null else PackedStringArray(frame.get_meta("table_facts", PackedStringArray()))

static func _title(smoke, area: int) -> String:
    var header := _header(smoke, area)
    return "" if header == null else str(header.get_node("Title").text)

static func _tracker(smoke) -> Control:
    return _surface(smoke).find_child("KangTracker", false, false) as Control

static func _text(smoke, name: String) -> String:
    var label = _surface(smoke).find_child(name, true, false)
    return "" if label == null else str(label.text)

static func _rect(node: Control) -> Rect2:
    return preload("res://smoke/native_kang_layout.gd")._rect(node)

static func _capture(smoke, leaf: String) -> bool:
    if not preload("res://smoke/native_kang_layout.gd").check(smoke, _surface(smoke), leaf): return false
    return await preload("res://smoke/native_heimdall_contract.gd")._capture(smoke, smoke.contract + "-" + leaf)
