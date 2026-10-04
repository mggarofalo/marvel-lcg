extends SceneTree

func _initialize() -> void:
    _run.call_deferred()

func _run() -> void:
    var checks = load("res://smoke/hosted_settlement_smoke_checks.gd").new()
    root.add_child(checks)
    var accepted: bool = await checks.verify()
    if accepted:
        print("HOSTED_SETTLEMENT_OBSERVER_OK cases=4")
    else:
        push_error("hosted observer case failed: " + checks.failed_case)
    checks.queue_free()
    await process_frame
    quit(0 if accepted else 1)
