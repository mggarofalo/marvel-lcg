extends "res://smoke/campaign_entry_smoke.gd"

# Development-runtime probe; release templates require external interaction.
func _run() -> void:
    if not await _open_campaign_tracker(): return
    if not await _restore_predecessor(): return
    if not await _capture("manager"): return
    if not await _resume_campaign(): return
    var journey := OS.get_environment("MARVEL_UPGRADE_JOURNEY")
    if not await _verify_journey(journey): return
    if not await _capture("table"): return
    if not await _release_table_resources(): return
    print("CAMPAIGN_UPGRADE_NATIVE_OK journey=", journey, " revision=", 5 if journey == "inspect" else 6)
    quit(0)

func _restore_predecessor() -> bool:
    var list := main.find_child("CampaignList", true, false)
    if not await _wait_for(func(): return list != null and list.get_child_count() == 1):
        _fail("predecessor campaign connection was not recovered")
        return false
    var row := list.get_child(0) as Button
    if row == null or not await _campaign_press(row): return false
    if not await _wait_for(func(): return _button_named("Return to current game") != null):
        _fail("predecessor campaign could not restore its active attempt")
        return false
    var text := _visible_text(main.find_child("CampaignTracker", true, false))
    for expected in ["Standard campaign", "▶ Taskmaster", "Spider-Man",
            "Experimental weapons", "Consumed upgrades", "Campaign cards"]:
        if not text.contains(expected):
            _fail("restored campaign manager omitted " + expected)
            return false
    return true

func _verify_journey(journey: String) -> bool:
    var resume_only := journey == "restart"
    var expected_revision := 6 if resume_only else 5
    if not await _wait_for(func(): return _upgrade_revision() == expected_revision):
        _fail("restored table did not retain the exact predecessor revision")
        return false
    if journey == "continue":
        if not await _campaign_activate("End turn"): return false
        if not await _wait_for(func(): return _upgrade_revision() == 6):
            _fail("real End turn input did not commit exactly one next decision")
            return false
    var expected_prompt := "Choose end-of-phase discards" if journey != "inspect" else "End turn"
    if not await _wait_for(func(): return _visible_text(main).contains(expected_prompt)):
        _fail("continued campaign did not expose the independently recorded next prompt")
        return false
    return true

func _upgrade_revision() -> int:
    return int((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))

func _capture(checkpoint: String) -> bool:
    await process_frame
    await process_frame
    var directory := OS.get_environment("MARVEL_UPGRADE_CAPTURE_DIRECTORY")
    if not directory.is_empty():
        var path := directory.path_join(OS.get_environment("MARVEL_UPGRADE_JOURNEY") + "-" + checkpoint + ".png")
        if render_viewport.get_texture().get_image().save_png(path) != OK:
            _fail("could not retain native upgrade screenshot")
            return false
    return true
