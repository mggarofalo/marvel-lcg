extends "res://smoke/automation/wave_native_playable.gd"

# Public seeded setup, followed solely by viewport-delivered game decisions.
# Printed 02001a/02006a replace damage with infamy removal; removing the last
# counter flips the same villain and environment to 02001b/02006b.
func _run() -> void:
    var evidence := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
    for store in ["MARVEL_CAMPAIGN_ACCESS_DIRECTORY", "MARVEL_DECK_LIBRARY_DIRECTORY", "MARVEL_SAVE_DIRECTORY"]:
        OS.set_environment(store, evidence.path_join(store))
    if DisplayServer.get_name() != "headless":
        OS.set_environment("MARVEL_SMOKE_CAPTURE_DIR", evidence)
    main = (load("res://Main.tscn") as PackedScene).instantiate()
    render_viewport.add_child(main)
    root.size = render_viewport.size
    ui = Driver.new(main, render_viewport, _fail)
    if not await _select_goblin_setup(): return
    if not await _open_goblin_table(): return
    if not await _assert_environment_counter("Criminal Enterprise", "2 Infamy"): return
    if not await _capture_clear_checkpoint("goblin-norman-before-attack"): return
    if not await _attack_norman(): return
    if not await _capture_clear_checkpoint("goblin-flipped-after-attack"): return
    if not await _release_table_resources(): return
    print("UI_ACCEPTANCE_OK goblin_risky_playable SpiderMan RiskyBusiness Standard")
    quit(0)

func _open_goblin_table() -> bool:
    if not await ui.click(ui.button("Start game")): return false
    if not await ui.expect_visible(ui.button("Keep hand")): return false
    if not await ui.click(ui.button("Keep hand")): return false
    if not await ui.expect(func(): return _revision() == 1, "keep hand commits once"): return false
    return await ui.expect(func(): return _card("Norman Osborn") != null and _card("Criminal Enterprise") != null,
        "Norman and Criminal Enterprise are visible before the attack")

func _select_goblin_setup() -> bool:
    if not await _wait_for_setup(): return false
    if not await _choose_setup("Hero", "Spider-Man"): return false
    if not await _choose_setup("Scenario", "Risky Business"): return false
    if not await _choose_setup("Mode", "Standard"): return false
    # Seed is fixture preparation; selectors and gameplay use viewport input.
    var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
    seed.text = "1"
    seed.text_changed.emit(seed.text)
    return true

func _attack_norman() -> bool:
    if not await ui.click(Locator.control(_card("Peter Parker")), MOUSE_BUTTON_RIGHT): return false
    if not await ui.click(_menu_choice("Change form")): return false
    if not await ui.expect(func(): return _revision() == 2, "form change commits once"): return false
    var health := _health("Norman Osborn")
    if not await ui.expect(func(): return not health.is_empty(), "Norman has visible health"): return false
    if not await ui.click(Locator.control(_card("Spider-Man")), MOUSE_BUTTON_RIGHT): return false
    if not await ui.click(_menu_choice("Attack")): return false
    if not await ui.click(_menu_choice("Norman Osborn")): return false
    if not await ui.expect(func(): return _revision() == 3, "basic attack commits once", 15000): return false
    return await _assert_flipped(health)

func _assert_flipped(health: String) -> bool:
    if not await ui.expect(func(): return _card("Green Goblin") != null and _card("State of Madness") != null,
        "removing the final two infamy flips villain and environment"): return false
    if not await ui.expect(func(): return _card("Norman Osborn") == null and _card("Criminal Enterprise") == null,
        "the replaced faces are absent from the live board"): return false
    if not await _assert_environment_counter("State of Madness", "0 Infamy · 2 Madness"): return false
    return await ui.expect(func(): return _health("Green Goblin") == health,
        "the replaced two damage does not reduce villain health")

func _assert_environment_counter(title: String, expected: String) -> bool:
    var card := _card(title)
    if not await ui.expect(func(): return card != null, title + " is on the live board"): return false
    var counter := card.find_child("StackStatuses", true, false) as Label
    return await ui.expect(func(): return counter != null and counter.is_visible_in_tree() and \
        counter.text == expected and Rect2(Vector2.ZERO, Vector2(render_viewport.size)).encloses(counter.get_global_rect()),
        title + " displays its authoritative " + expected + " counter")

func _assert_timeline_clear_of_encounter_piles() -> bool:
    var timeline := main.find_child("EventTimeline", true, false) as Control
    if not await ui.expect(func(): return timeline != null and timeline.is_visible_in_tree(),
        "live event timeline is visible"): return false
    var piles := main.find_children("Pile*", "PanelContainer", true, false).filter(func(n):
        return n.is_visible_in_tree() and (n.tooltip_text.begins_with("ENCOUNTER") or n.name == "PileEmptyEncounterDiscard"))
    if not await ui.expect(func(): return piles.size() == 2,
        "scenario encounter deck and discard remain visible"): return false
    for pile in piles:
        if not await ui.expect(func(): return not timeline.get_global_rect().intersects(pile.get_global_rect()),
            "timeline clears scenario encounter pile"): return false
    return true
