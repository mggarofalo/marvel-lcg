extends "res://smoke/repeated_commit_smoke.gd"

func _run() -> void:
    # Real Main registration must protect both live-window and retired-scene exit.
    if not await _stage_core_attack(): return
    if not await _repeat_preserves_next_commitment(): return
    main.queue_free()
    main = null
    await process_frame
    await process_frame
    var probe = load("res://smoke/ResourceFinalizerProbe.cs").new()
    root.add_child(probe)
    probe.QuitWithPendingReferences()
