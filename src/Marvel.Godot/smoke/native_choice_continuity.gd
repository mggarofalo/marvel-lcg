extends RefCounted

static func exercise_scale(smoke, chooser: Control) -> Dictionary:
    var captured := capture(smoke, chooser)
    if not await preload("res://smoke/native_scale_checkpoint.gd").requested(smoke, "ambiguity"): return {}
    if "ambiguity" in OS.get_environment("MARVEL_NATIVE_SCALE_CHECKPOINTS"):
        if not await reject_stale(smoke, captured): return {}
    return captured

static func capture(smoke, chooser: Control) -> Dictionary:
    var buttons: Array[Button] = smoke._visible_buttons(chooser)
    var witness = preload("res://smoke/NativeGameWitness.cs").new()
    witness.Bind(smoke.main)
    return {"callback": buttons[0].pressed.get_connections()[0]["callable"],
        "draft": witness.Draft(), "revision": witness.Revision()}

static func reject_stale(smoke, captured: Dictionary) -> bool:
    var witness = preload("res://smoke/NativeGameWitness.cs").new()
    witness.Bind(smoke.main)
    captured.callback.call()
    await smoke.process_frame
    if witness.Draft() != captured.draft or witness.Revision() != captured.revision:
        smoke._fail("a replaced or dismissed choice callback changed the current draft")
        return false
    return true

static func stays_dismissed(smoke, captured: Dictionary) -> bool:
    if not await reject_stale(smoke, captured): return false
    var slider: HSlider = smoke._node("Toolbar/InterfaceScale")
    var before := slider.value
    slider.value = 80 if before != 80 else 100
    await smoke.process_frame
    await smoke.process_frame
    slider.value = before
    await smoke.process_frame
    await smoke.process_frame
    var popup := smoke.main.find_child("CardActionChoices", true, false) as Control
    if popup != null and popup.is_visible_in_tree():
        smoke._fail("scale resurrected a dismissed source choice")
        return false
    if not await reject_stale(smoke, captured): return false
    if smoke._task_commit() != null:
        smoke._fail("dismissed source choice retained commitment")
        return false
    return true
