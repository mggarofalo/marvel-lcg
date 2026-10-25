extends "res://smoke/local_game_smoke.gd"

# Live session; seed selection alone is prepared fixture input. All game decisions
# and setup catalog selections use the shared driver's viewport input.
const Driver = preload("res://smoke/automation/ui_driver.gd")
const Locator = preload("res://smoke/automation/ui_locator.gd")
var ui: RefCounted

func _run() -> void:
    var evidence := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
    for store in ["MARVEL_CAMPAIGN_ACCESS_DIRECTORY", "MARVEL_DECK_LIBRARY_DIRECTORY"]:
        OS.set_environment(store, evidence.path_join(store))
    if DisplayServer.get_name() != "headless":
        OS.set_environment("MARVEL_SMOKE_CAPTURE_DIR", evidence)
    main = (load("res://Main.tscn") as PackedScene).instantiate()
    render_viewport.add_child(main)
    root.size = render_viewport.size
    ui = Driver.new(main, render_viewport, _fail)
    if not await _open_public_game(): return
    if not await _assert_groups(): return
    if not await _capture_checkpoint("wave-four-live-groups"): return
    if not await _change_form_and_attack(): return
    if not await _capture_checkpoint("wave-inactive-villain-attacked"): return
    if not await _release_table_resources(): return
    print("UI_ACCEPTANCE_OK wave_native_playable CaptainAmerica WreckingCrew Standard")
    quit(0)

func _open_public_game() -> bool:
    if not await _select_public_setup(): return false
    if not await ui.click(ui.button("Start game")): return false
    if not await ui.expect_visible(ui.button("Keep hand")): return false
    if not await ui.expect(func(): return _revision() == 0, "uncommitted opening hand"): return false
    if not await ui.click(ui.button("Keep hand")): return false
    if not await ui.expect(func(): return _revision() == 1, "keep hand commits once"): return false
    # Seed 1 deals the Shield into the opening hand; Setup requires no search.
    if not await ui.expect(func(): return _card("Captain America's Shield") != null,
        "opening hand already contains Captain America's Shield"): return false
    return await ui.expect_visible(ui.button("End turn"))

func _select_public_setup() -> bool:
    if not await _wait_for_setup(): return false
    if not await _choose_setup("Hero", "Captain America"): return false
    if not await _choose_setup("Scenario", "Wrecking Crew"): return false
    if not await _choose_setup("Mode", "Standard"): return false
    # Reuse the documented seeded bootstrap for deterministic fixture preparation.
    var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
    seed.text = "1"
    seed.text_changed.emit(seed.text)
    return await _capture_checkpoint("wave-setup")

func _choose_setup(field: String, wanted: String) -> bool:
    var option := _node("Setup/Selections/Fields/Grid/" + field) as OptionButton
    var index := -1
    for i in range(option.item_count):
        if option.get_item_text(i) == wanted: index = i
    if not await ui.expect(func(): return index >= 0, "public discovery offers " + wanted): return false
    if option.selected != index:
        var scope := _node("Setup/Selections/Fields/Grid/" + field + "Choices")
        if not await ui.click(Locator.button(scope, wanted)): return false
    return await ui.expect(func(): return option.selected == index, field + " selects " + wanted)

func _revision() -> int:
    return int((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))

func _group_labels() -> Array[Node]:
    return main.find_children("EncounterGroup*", "Label", true, false).filter(func(n): return n.is_visible_in_tree())

func _assert_groups() -> bool:
    var titles: Array[String] = []
    var active := 0
    for label in _group_labels():
        titles.append(label.text.get_slice("\n", 0))
        if "ACTIVE VILLAIN" in label.text: active += 1
        if not await ui.expect(func(): return Rect2(Vector2.ZERO, Vector2(render_viewport.size)).encloses(label.get_global_rect()), "villain group caption visible"): return false
    titles.sort()
    return await ui.expect(func(): return titles == ["Bulldozer", "Piledriver", "Thunderball", "Wrecker"] and active == 1,
        "four named live villain groups and exactly one active villain")

func _card(title: String) -> Control:
    var matches: Array[Control] = []
    for card in main.find_children("ProceduralCard*", "Control", true, false):
        if card.is_visible_in_tree() and (card.accessibility_name == title or card.accessibility_name.begins_with(title + " · ")):
            matches.append(card)
    return matches[0] if matches.size() == 1 else null

func _health(title: String) -> String:
    var card := _card(title)
    var value := card.find_child("LiveProgressValue", true, false) if card != null else null
    return value.text if value != null else ""

func _menu_choice(text: String) -> RefCounted:
    return Locator.button(main.find_child("CardContextMenu", true, false), text, false)

func _change_form_and_attack() -> bool:
    if not await ui.click(Locator.control(_card("Steve Rogers")), MOUSE_BUTTON_RIGHT): return false
    if not await ui.click(_menu_choice("Change form")): return false
    if not await ui.expect(func(): return _revision() == 2, "change form commits once"): return false
    var inactive := "Wrecker"
    var active_before := ""
    for label in _group_labels():
        if "ACTIVE VILLAIN" in label.text: active_before = label.text
    if active_before.begins_with("Wrecker\n"): inactive = "Bulldozer"
    if not await ui.expect(func(): return not active_before.begins_with(inactive + "\n"),
        "chosen named villain is inactive"): return false
    return await _attack_inactive(inactive, active_before)

func _attack_inactive(inactive: String, active_before: String) -> bool:
    var before := _health(inactive)
    if not await ui.expect(func(): return not before.is_empty(), "inactive villain visible health baseline"): return false
    if not await ui.click(Locator.control(_card("Captain America")), MOUSE_BUTTON_RIGHT): return false
    if not await ui.click(_menu_choice("Attack")): return false
    if not await ui.click(_menu_choice(inactive)): return false
    if not await ui.expect(func(): return _revision() == 3, "attack commits once"): return false
    var after := _health(inactive)
    if not await ui.expect(func(): return int(after.get_slice("/", 0)) == int(before.get_slice("/", 0)) - 2,
        "inactive villain loses exactly Captain America's 2 basic attack damage"): return false
    if not await _assert_groups(): return false
    return await ui.expect(func(): return _group_labels().any(func(n): return n.text == active_before),
        "basic attack leaves the active villain counter unchanged")
