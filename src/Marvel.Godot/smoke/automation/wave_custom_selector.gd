extends "res://smoke/automation/wave_native_playable.gd"

# Live selector-to-authoritative-game journey. Only the seed is prepared input;
# no opening-hand or gameplay decision is submitted by this check.
func _run() -> void:
    var evidence := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
    for store in ["MARVEL_CAMPAIGN_ACCESS_DIRECTORY", "MARVEL_DECK_LIBRARY_DIRECTORY", "MARVEL_SAVE_DIRECTORY"]:
        OS.set_environment(store, evidence.path_join(store))
    main = (load("res://Main.tscn") as PackedScene).instantiate()
    render_viewport.add_child(main)
    root.size = render_viewport.size
    ui = Driver.new(main, render_viewport, _fail)
    if not await _select_custom_setup(): return
    if not await ui.expect(func(): return _revision() == -1, "selectors do not open a game"): return
    if not await ui.click(ui.button("Start game")): return
    if not await ui.expect_visible(ui.button("Keep hand")): return
    if not await _assert_opening_table(): return
    if not await _release_table_resources(): return
    print("UI_ACCEPTANCE_OK wave_custom_selector BABA revision=0")
    quit(0)

func _select_custom_setup() -> bool:
    if not await _wait_for_setup(): return false
    # Deterministic fixture preparation, distinct from selector gestures below.
    (_node("Setup/Selections/Fields/Grid/Seed") as LineEdit).text = "1"
    if not await _choose_setup("Hero", "Captain America"): return false
    if not await _choose_setup("Scenario", "Wrecking Crew"): return false
    if not await _choose_setup("Mode", "Custom"): return false
    for index in range(4):
        var field := "ScenarioVariant" + str(index)
        # Exercise both named choices even where the final value is the default.
        if not await _choose_setup(field, "B"): return false
        if index % 2 == 1 and not await _choose_setup(field, "A"): return false
    return await ui.expect(func(): return (_node("Setup/Briefing/Frame/Copy/Mode") as Label).text == "Custom · Wrecker B · Thunderball A · Piledriver B · Bulldozer A",
        "selected Custom mode explains the exact B/A/B/A recipe")

func _assert_custom_faces() -> bool:
    # Expected public faces/HP from datasets/cards/cards.json: 07003, 07017,
    # 07033 and 07046. Distinguishes the exact mixed recipe from all-A/all-B.
    for expected in [["Wrecker", "B", "18/18"], ["Thunderball", "A", "13/13"],
        ["Piledriver", "B", "14/14"], ["Bulldozer", "A", "12/12"]]:
        var card := _card(expected[0])
        if not await ui.expect(func(): return card != null, "one public " + expected[0]): return false
        var stage := card.find_child("StageValue", true, false) as Label
        if not await ui.expect(func(): return stage != null and stage.text == expected[1] and _health(expected[0]) == expected[2], "exact stage and HP for " + expected[0]): return false
    return true

func _assert_opening_table() -> bool:
    if not await _assert_groups(): return false
    if not await _assert_custom_faces(): return false
    if not await ui.expect(func(): return _revision() == 0, "Start leaves opening hand uncommitted"): return false
    if not await ui.expect_visible(ui.button("Keep hand")): return false
    return true
