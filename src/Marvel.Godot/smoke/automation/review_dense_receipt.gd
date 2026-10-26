extends SceneTree

# Synthetic display-only review probe, independent of engine legality.
func _initialize() -> void:
    call_deferred("_run")

func _run() -> void:
    root.size = Vector2i(1920, 1080)
    var probe = load("res://smoke/EncounterStackReceiptProbe.cs").new()
    root.add_child(probe)
    probe.PrepareDense()
    for frame in range(10): await process_frame
    print("DENSE_BEFORE " + probe.DenseState())
    if probe.DenseHeadersOverlap():
        push_error("Prepared encounter headers overlap before receipt")
        quit(1)
        return
    probe.GrowReceipt()
    for frame in range(10): await process_frame
    var state: String = probe.DenseState()
    var overlap: bool = probe.DenseHeadersOverlap()
    print("DENSE_AFTER " + state)
    if DisplayServer.get_name() != "headless":
        await RenderingServer.frame_post_draw
        root.get_texture().get_image().save_png(OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY").get_base_dir().path_join("dense-receipt-overlap.png"))
    probe.queue_free()
    await process_frame
    if overlap:
        push_error("Updated encounter header overlaps the next exposed header")
        quit(1)
        return
    print("UI_ACCEPTANCE_OK review_dense_receipt")
    quit(0)
