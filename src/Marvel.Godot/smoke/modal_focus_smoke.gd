extends SceneTree

var probe: Node
var dismissed: Window
var invalid_parent_focus := false

func _initialize() -> void:
    call_deferred("_run")

func _run() -> void:
    root.size = Vector2i(1920, 1080)
    root.gui_embed_subwindows = true
    probe = preload("res://smoke/CampaignTrackerNativeProbe.cs").new()
    root.add_child(probe)
    root.focus_entered.connect(_parent_focused)
    for modal in [Window.new(), ConfirmationDialog.new(), FileDialog.new()]:
        if not await _verify_modal(modal): return
    probe.queue_free()
    await process_frame
    print("MODAL_FOCUS_OK hide-reopen=true nested=true nonexclusive=true")
    quit(0)

func _verify_modal(modal: Window) -> bool:
    modal.visible = false
    modal.transient = true
    modal.exclusive = true
    probe.BindModal(modal)
    root.add_child(modal)
    modal.popup_centered(Vector2i(480, 300))
    await process_frame
    var child := Window.new()
    child.visible = false
    child.transient = true
    child.exclusive = true
    probe.BindModal(child)
    modal.add_child(child)
    child.popup_centered(Vector2i(200, 100))
    await process_frame
    if not modal.exclusive: return _fail("visible parent lost modal exclusivity")
    child.hide()
    await process_frame
    if not modal.has_focus(): return _fail("nested dismissal did not restore parent focus")
    child.queue_free()
    if not await _verify_reopen(modal): return false
    return await _verify_nonmodal_and_disposal(modal)

func _verify_reopen(modal: Window) -> bool:
    for unused in range(2):
        dismissed = modal
        modal.hide()
        await process_frame
        if invalid_parent_focus: return _fail("parent focus retained a hidden exclusive child")
        if modal.visible or not modal.exclusive: return _fail("hidden modal lost reopen policy")
        dismissed = null
        modal.popup_centered()
        await process_frame
        if not modal.has_focus(): return _fail("reopened modal did not own focus")
    return true

func _verify_nonmodal_and_disposal(modal: Window) -> bool:
    modal.exclusive = false
    modal.hide()
    await process_frame
    if modal.exclusive: return _fail("nonexclusive window became modal")
    modal.exclusive = true
    modal.popup_centered()
    await process_frame
    if modal is ConfirmationDialog:
        modal.get_cancel_button().emit_signal("pressed")
        await process_frame
        if modal.visible: return _fail("dialog cancellation did not dismiss")
        modal.popup_centered()
        await process_frame
    modal.queue_free()
    await process_frame
    return true

func _parent_focused() -> void:
    if dismissed != null and not dismissed.visible and dismissed.exclusive:
        invalid_parent_focus = true

func _fail(message: String) -> bool:
    push_error(message)
    quit(1)
    return false
