extends "res://smoke/campaign_entry_smoke.gd"

const Driver = preload("res://smoke/automation/ui_driver.gd")

# Original Expert predecessor, with durable storage supplied by the runner.
func _run() -> void:
    var journey := OS.get_environment("MARVEL_EXPERT_JOURNEY")
    if journey not in ["inspect", "decline", "restart"]:
        _fail("unsupported Expert evidence journey")
        return
    if not await _load_expert_tracker(): return
    if not await _resume_campaign(): return
    var ui = Driver.new(main, render_viewport, _fail)
    if not await _verify_journey(ui, journey): return
    if not await _release_table_resources(): return
    print("EXPERT_CAMPAIGN_NATIVE_OK journey=", journey)
    quit(0)

func _load_expert_tracker() -> bool:
    if not await _open_campaign_tracker(): return false
    var list := main.find_child("CampaignList", true, false)
    if not await _wait_for(func(): return list.get_child_count() == 1):
        _fail("expected the isolated Expert predecessor")
        return false
    if not await _campaign_press(list.get_child(0) as Button): return false
    if not await _wait_for(func(): return _button_named("Return to current game") != null):
        _fail("Expert predecessor was not restored")
        return false
    var tracker := main.find_child("CampaignTracker", true, false)
    var description := _visible_text(tracker)
    for expected in ["Expert campaign", "Standard scenario", "▶ Absorbing Man", "recorded HP 7/10", "Experimental weapons", "Campaign cards"]:
        if not description.contains(expected):
            _fail("Expert manager omitted " + expected)
            return false
    return true

func _verify_journey(ui: RefCounted, journey: String) -> bool:
    var revision := 1 if journey == "restart" else 0
    if not await ui.expect(func(): return _revision() == revision, "exact restored revision"): return false
    if journey == "restart":
        return await ui.expect_visible(ui.button("Keep hand"))
    return await _verify_recovery_choice(ui, journey)

func _verify_recovery_choice(ui: RefCounted, journey: String) -> bool:
    if not await ui.expect(func(): return _visible_text(main).contains("Keep your recorded hit points, or add a random obligation to recover"), "recovery consequence explanation"): return false
    if not await ui.expect_visible(ui.button("Keep recorded hit points")): return false
    if not await ui.expect_visible(ui.button("Recover to full health")): return false
    if journey == "inspect": return true
    if not await ui.click(ui.button("Keep recorded hit points")): return false
    if not await ui.expect(func(): return _revision() == 1, "exactly one accepted recovery decision"): return false
    return await ui.expect_visible(ui.button("Keep hand"))

func _campaign_press(button: Button) -> bool:
    var surface := button.get_viewport()
    var ui = Driver.new(surface, surface, _fail)
    for index in range(80):
        if surface.gui_get_focus_owner() == button:
            return await ui.press_key(KEY_ENTER)
        if not await ui.press_key(KEY_TAB): return false
    _fail("campaign keyboard traversal did not reach " + button.text)
    return false

func _revision() -> int:
    return int((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))


