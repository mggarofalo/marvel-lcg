extends "res://smoke/campaign_entry_smoke.gd"

func _run() -> void:
    if not await _prepare_manager(): return
    if not await _rename_campaign(): return
    if not await _archive_restore(): return
    if not await _copy_campaign(): return
    if not await _resume_copy(): return
    if not await _delete_copy(): return
    print("CAMPAIGN_MANAGER_OK rename=true archive_restore=true portable_live_copy=true delete_cancel=true original_preserved=true")
    quit(0)

func _prepare_manager() -> bool:
    var isolated := OS.get_environment("MARVEL_SAVE_DIRECTORY")
    if isolated.is_empty():
        _fail("campaign manager journey requires an isolated save directory")
        return false
    OS.set_environment("MARVEL_CAMPAIGN_ACCESS_DIRECTORY", isolated.path_join("access"))
    if not await _open_campaign_tracker(): return false
    if not await _wait_for(func(): return _button_named("Create campaign") != null):
        _fail("creation missing")
        return false
    if not await _create_campaign(): return false
    if not await _wait_for(func(): return _button_named("End turn") != null):
        _fail("campaign table missing")
        return false
    if not await _pointer_activate(_button_named("Campaigns")): return false
    if not await _wait_for(func(): return _button_named("Return to current game") != null):
        _fail("resume missing")
        return false
    return true

func _rename_campaign() -> bool:
    if not await _campaign_activate("Rename…"): return false
    var tracker := main.find_child("CampaignTracker", true, false) as Window
    var rename := tracker.find_child("CampaignRenameDialog", true, false) as ConfirmationDialog
    var name_field := rename.find_child("CampaignName", true, false) as LineEdit
    name_field.text = "Friday heroes"
    if not await _campaign_press(rename.get_ok_button()): return false
    if not await _wait_for(func(): return _campaign_rows().size() == 1 and _campaign_rows()[0].text.begins_with("Friday heroes")):
        _fail("rename did not update campaign list")
        return false
    return true

func _archive_restore() -> bool:
    if not await _campaign_activate("Archive"): return false
    if not await _wait_for(func(): return _button_named("Restore") != null and not _button_named("Restore").disabled):
        _fail("archive not confirmed")
        return false
    if _button_named("Return to current game") != null:
        _fail("archived campaign still offers play")
        return false
    if not await _campaign_activate("Restore"): return false
    if not await _wait_for(func(): return _button_named("Return to current game") != null):
        _fail("restore missing")
        return false
    return true

func _copy_campaign() -> bool:
    var tracker := main.find_child("CampaignTracker", true, false) as Window
    var backup := OS.get_environment("MARVEL_SAVE_DIRECTORY").path_join("backup.marvelcampaign")
    if not await _campaign_activate("Export…"): return false
    if not await _choose_campaign_file(tracker, backup, "Save"): return false
    if not FileAccess.file_exists(backup):
        _fail("export did not write a backup")
        return false
    if not await _campaign_activate("Import campaign…"): return false
    if not await _choose_campaign_file(tracker, backup, "Open"): return false
    if not await _wait_for(func(): return _campaign_rows().size() == 2):
        _fail("import did not create an independent campaign")
        return false
    await _capture_checkpoint("campaign-manager-copies")
    return true

func _resume_copy() -> bool:
    var tracker := main.find_child("CampaignTracker", true, false) as Window
    if not await _campaign_activate("Return to current game"): return false
    if not await _wait_for(func(): return not tracker.visible and _button_named("End turn") != null):
        _fail("imported live game did not resume")
        return false
    if not await _pointer_activate(_button_named("Campaigns")): return false
    if not await _wait_for(func(): return tracker.visible and _button_named("Delete permanently…") != null):
        _fail("manager did not reopen")
        return false
    return true

func _delete_copy() -> bool:
    var tracker := main.find_child("CampaignTracker", true, false) as Window
    if not await _campaign_activate("Delete permanently…"): return false
    var deletion := tracker.find_child("CampaignDeleteDialog", true, false) as ConfirmationDialog
    if not await _campaign_press(deletion.get_cancel_button()): return false
    if _campaign_rows().size() != 2:
        _fail("cancelled deletion changed library")
        return false
    if not await _campaign_activate("Delete permanently…"): return false
    deletion = tracker.find_child("CampaignDeleteDialog", true, false) as ConfirmationDialog
    if not await _campaign_press(deletion.get_ok_button()): return false
    if not await _wait_for(func(): return _campaign_rows().size() == 1):
        _fail("permanent delete did not remove only the selected copy")
        return false
    if not _campaign_rows()[0].text.begins_with("Friday heroes"):
        _fail("original campaign was changed")
        return false
    return true

func _campaign_rows() -> Array[Node]:
    var list := main.find_child("CampaignList", true, false)
    return list.find_children("*", "Button", false, false) if list != null else []

func _choose_campaign_file(tracker: Window, path: String, action: String) -> bool:
    if not await _wait_for(func(): return tracker.find_children("*", "FileDialog", true, false).size() > 0):
        _fail("file dialog did not open")
        return false
    var dialog := tracker.find_children("*", "FileDialog", true, false)[0] as FileDialog
    dialog.current_path = path
    await process_frame
    await process_frame
    if action == "Open" and not await _select_backup(dialog, path.get_file()): return false
    await _capture_checkpoint("campaign-file-" + action.to_lower())
    if dialog.get_ok_button().text != action:
        _fail("unexpected file commitment: " + dialog.get_ok_button().text)
        return false
    if not await _campaign_press(dialog.get_ok_button()): return false
    for unused in range(180):
        if tracker.find_children("*", "FileDialog", true, false).is_empty(): break
        await process_frame
    if not tracker.find_children("*", "FileDialog", true, false).is_empty():
        _fail("file " + action + " did not complete")
        return false
    print("CAMPAIGN_FILE_OK ", action)
    return true

func _file_item(node: Node, filename: String) -> Dictionary:
    if node is ItemList:
        for index in range(node.item_count):
            if node.get_item_text(index) == filename: return {"list": node, "index": index}
    for child in node.get_children(true):
        var result := _file_item(child, filename)
        if not result.is_empty(): return result
    return {}

func _select_backup(dialog: FileDialog, filename: String) -> bool:
    var item := _file_item(dialog, filename)
    if item.is_empty():
        _fail("exported file is absent from the import browser")
        return false
    var items: ItemList = item.list
    items.grab_focus()
    for action_name in ["ui_left", "ui_right"]:
        for pressed in [true, false]:
            var key := InputEventAction.new()
            key.action = action_name
            key.pressed = pressed
            items.get_viewport().push_input(key, true)
            await process_frame
    if items.get_selected_items() != PackedInt32Array([item.index]):
        _fail("keyboard file selection did not select the exported backup")
        return false
    return true
