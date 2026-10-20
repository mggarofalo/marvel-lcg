extends "res://smoke/local_game_smoke.gd"

# Real campaign entry against an isolated durable server. Run create, restart
# that server with the same save directory, then run resume with the same access directory.
func _run() -> void:
    if not await _open_campaign_tracker(): return
    var resume := OS.get_environment("MARVEL_CAMPAIGN_JOURNEY") == "resume"
    if not resume and not await _wait_for(func(): return _button_named("Create campaign") != null):
        _fail("campaign creation was not explained")
        return
    if resume:
        if not await _resume_campaign(): return
    elif not await _create_campaign(): return
    if not await _wait_for(func(): return _button_named("End turn") != null):
        _fail("actual campaign did not reach or restore the player turn")
        return
    if not await _wait_for(func(): return not (main.find_child("CampaignTracker", true, false) as Window).visible):
        _fail("campaign tracker covered the resumed table")
        return
    await process_frame
    await process_frame
    var capture := OS.get_environment("MARVEL_CAMPAIGN_JOURNEY_CAPTURE")
    if not capture.is_empty(): render_viewport.get_texture().get_image().save_png(capture)
    print("RORS_CAMPAIGN_ENTRY_OK resume=", resume, " real-server=true")
    quit(0)

func _resume_campaign() -> bool:
    if not await _wait_for(func(): return _button_named("Return to current game") != null):
        _fail("saved campaign did not expose its current attempt")
        return false
    if not await _campaign_activate("Return to current game"): return false
    return true

func _create_campaign() -> bool:
    if not await _campaign_activate("Create campaign"): return false
    if not await _wait_for(func(): return _button_named("Begin Crossbones") != null):
        _fail("created campaign did not explain the first scenario")
        return false
    if not await _campaign_activate("Begin Crossbones"): return false
    if not await _wait_for(func(): return _button_named("Keep hand") != null):
        _fail("actual campaign did not open its mulligan")
        return false
    if not await _pointer_activate(_button_named("Keep hand")): return false
    return true

func _campaign_activate(label: String) -> bool:
    if not await _wait_for(func(): return _button_named(label) != null and not _button_named(label).disabled):
        _fail("campaign commitment did not become available: " + label)
        return false
    var button := _button_named(label)
    if button == null or button.disabled:
        _fail("campaign commitment unavailable: " + label)
        return false
    return await _campaign_press(button)

func _campaign_press(button: Button) -> bool:
    button.grab_focus()
    await process_frame
    if button.get_viewport().gui_get_focus_owner() != button:
        _fail("campaign commitment could not receive keyboard focus: " + button.text)
        return false
    for pressed in [true, false]:
        var key := InputEventAction.new()
        key.action = "ui_accept"
        key.pressed = pressed
        button.get_viewport().push_input(key, true)
        await process_frame
    return true

func _open_campaign_tracker() -> bool:
    render_viewport.gui_embed_subwindows = true
    main = load("res://Main.tscn").instantiate() as Control
    render_viewport.add_child(main)
    root.size = render_viewport.size
    if not await _wait_for_setup():
        _fail("setup discovery did not complete")
        return false
    if not await _wait_for(func(): return _button_named("Campaigns") != null):
        _fail("complete campaign was not admitted")
        return false
    if not await _pointer_activate(_button_named("Campaigns")): return false
    return true
