extends SceneTree

# Synthetic display-only receipt test; no engine session or game decisions.
func _initialize() -> void:
    call_deferred("_run")

func _run() -> void:
    var probe = load("res://smoke/EncounterStackReceiptProbe.cs").new()
    root.add_child(probe)
    var failure: String = probe.VerifyReceipt()
    probe.queue_free()
    await process_frame
    if not failure.is_empty():
        push_error(failure)
        quit(1)
        return
    print("UI_ACCEPTANCE_OK encounter_stack_receipt")
    quit(0)
