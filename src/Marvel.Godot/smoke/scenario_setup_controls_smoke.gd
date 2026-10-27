extends SceneTree

func _initialize() -> void:
    call_deferred("_run")

func _run() -> void:
    var main = load("res://Main.tscn").instantiate()
    root.add_child(main)
    var start = main.find_child("Start", true, false)
    for attempt in range(1200):
        await process_frame
        if not start.disabled:
            break
    if start.disabled:
        push_error("Setup discovery never became ready")
        quit(1)
        return
    var probe = load("res://smoke/ScenarioSetupControlsProbe.cs").new()
    root.add_child(probe)
    var failure: String = probe.Verify(main)
    if failure != "":
        push_error(failure)
        quit(1)
        return
    print("PASS: 19 authored scenario recipes, all custom versions, four distinct seats, modular isolation")
    main.queue_free()
    probe.queue_free()
    await process_frame
    quit(0)
