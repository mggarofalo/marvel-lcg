extends SceneTree

var main: Node
var probe: Node

func _initialize() -> void:
    call_deferred("_run")

func _run() -> void:
    root.size = Vector2i(1920, 1080)
    main = load("res://Main.tscn").instantiate()
    root.add_child(main)
    await process_frame
    await process_frame
    probe = preload("res://smoke/CampaignTrackerNativeProbe.cs").new()
    root.add_child(probe)
    probe.Show(main, JSON.stringify(_response()))
    for unused in range(1200):
        if probe.Prepared or not probe.Failure.is_empty(): break
        await process_frame
    if not probe.Prepared:
        _fail("fixture did not prepare: " + probe.Failure)
        return
    await process_frame
    var ordinary := main.find_child("CampaignTracker", true, false) as Window
    if ordinary == null or ordinary.visible:
        _fail("ordinary tracker must remain closed until requested")
        return
    var tracker := main.find_child("SyntheticCampaignTracker", true, false) as Window
    var links := tracker.find_children("*", "LinkButton", true, false)
    if links.size() != 4:
        _fail("expected owned, weapon, consumed, and imprisoned card references")
        return
    for link in links:
        link.grab_focus()
        await process_frame
        await process_frame
        if tracker.find_child("RulesText", true, false) == null:
            _fail("keyboard inspection has no printed card")
            return
    links[2].grab_focus()
    await process_frame
    await process_frame
    var destination := OS.get_environment("MARVEL_CAMPAIGN_CAPTURE")
    if not destination.is_empty():
        var result := root.get_texture().get_image().save_png(destination)
        if result != OK:
            _fail("capture failed")
            return
    print("SYNTHETIC_CAMPAIGN_TRACKER_OK references=4 keyboard-preview=true no-game-created=true")
    quit(0)

func _response() -> Dictionary:
    var owned := {"key": "condition:04159a", "face": "04159a", "role": 1}
    var removed := {"key": "tech:04155", "face": "04155", "role": 0}
    var player := {"hero": "spider_man", "deck": {"cards": ["01002"]},
        "base_hit_points": 10, "recorded_hit_points": 7, "requires_recovery": false,
        "cards": [owned], "available_obligations": [], "engaged_with_enemy": false,
        "condition_hit_point_bonus": 1, "participating": true}
    var state := {"product": "rise_of_red_skull", "seed": 42, "expert": true,
        "stage": 3, "phase": 0, "attempt": 3, "expert_scenario": false,
        "players": [player], "removed_card_keys": [removed.key], "banned_ally_faces": ["01002"],
        "experimental_weapons": ["04072"], "delay_counters": 3, "reward_seats": [], "pending_outcome": null}
    return {"version": 34, "request_id": "synthetic", "game_id": "synthetic",
        "prompt": null, "world": null, "events": [], "campaign": {"id": "synthetic",
        "revision": 1, "state": state, "reward": null, "active_game_id": null,
        "removed_cards": [removed], "preparation_problems": ["Synthetic fixture; no game can be started."]}}

func _fail(message: String) -> void:
    push_error("SYNTHETIC_CAMPAIGN_TRACKER_FAILED " + message)
    quit(1)
