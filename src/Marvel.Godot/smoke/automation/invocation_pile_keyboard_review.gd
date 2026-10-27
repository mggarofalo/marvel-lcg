extends "res://smoke/automation/invocation_public_acceptance.gd"

# Normal embedded setup and actual keyboard traversal; no forced focus or gameplay mutation.
func _run() -> void:
    if not await _entry_public(): return
    var opener := main.find_child("InspectOtherRegions", true, false) as MenuButton
    var draft: String = witness.Draft()
    if not await _reach_overflow(opener): return
    if not await _keyboard_inspect(opener): return
    if not await _dismiss_to_opener(opener, draft): return
    print("PILE_KEYBOARD_REVIEW restored_opener=true revision=1 draft_unchanged=true")
    if not await _release_table_resources(): return
    print("UI_ACCEPTANCE_OK invocation_pile_keyboard_review")
    quit(0)

func _reach_overflow(opener: MenuButton) -> bool:
    if not await ui.expect(func(): return render_viewport.gui_get_focus_owner() == opener,
        "Escape restores the Other piles opener after initial inspection"): return false
    if not await ui.expect_fully_visible_enabled(Locator.control(opener)): return false
    if not await ui.press_key(KEY_TAB): return false
    var visited: Array[String] = []
    var reached := false
    for step in range(80):
        if render_viewport.gui_get_focus_owner() == opener:
            print("PILE_KEYBOARD_REVIEW reached_opener=true tabs=%d focus_mode=%d" % [step + 1, opener.focus_mode])
            reached = true
            break
        var current := render_viewport.gui_get_focus_owner()
        if current != null and str(current.get_path()) not in visited:
            visited.append(str(current.get_path()))
        if not await ui.press_key(KEY_TAB): return false
    return await ui.expect(func(): return reached,
        "Other piles is reachable through normal Tab navigation after Escape dismissal", 0)

func _keyboard_inspect(opener: MenuButton) -> bool:
    if not await ui.press_key(KEY_ENTER): return false
    var menu := opener.get_popup()
    if not await ui.expect(func(): return menu.visible, "Enter opens the focused Other piles menu"): return false
    var matches: Array[int] = []
    for item in range(menu.item_count):
        if "invocation deck" in menu.get_item_text(item).to_lower(): matches.append(item)
    if not await ui.expect(func(): return matches.size() == 1, "one owned typed Invocation deck menu item"): return false
    if not await preload("res://smoke/native_declaration_contract.gd")._choose_from_menu(self, menu, matches[0]): return false
    var popup := root.find_child("PileInspector", true, false)
    if not await ui.expect(func(): return popup != null and popup.visible, "arrow keys and Enter open Invocation inspection"): return false
    if not await ui.expect(func(): return RINGS in _visible_text(popup) and "1 / 1" in _visible_text(popup),
        "keyboard inspection shows the single authorized public top"): return false
    return await _capture_keyboard_inspector(popup)

func _capture_keyboard_inspector(popup) -> bool:
    if not await _capture("keyboard-invocation-table"): return false
    if DisplayServer.get_name() != "headless":
        await RenderingServer.frame_post_draw
        if popup.get_texture().get_image().save_png(OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY").path_join("keyboard-invocation-inspector.png")) != OK: return false
    return true

func _dismiss_to_opener(opener: MenuButton, draft: String) -> bool:
    for pressed in [true, false]:
        var event := InputEventKey.new()
        event.keycode = KEY_ESCAPE
        event.pressed = pressed
        Input.parse_input_event(event)
        await process_frame
    if not await ui.expect(func(): return root.find_child("PileInspector", true, false) == null and render_viewport.gui_get_focus_owner() == opener,
        "Escape dismisses inspection and restores the keyboard opener"): return false
    if not await ui.expect(func(): return witness.Revision() == 1 and witness.Draft() == draft and invocation.PileMatches(RINGS, "", 4),
        "keyboard pile inspection preserves draft, public pile and the one mulligan commitment"): return false
    return await _capture("keyboard-opener-restored")
