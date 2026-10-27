extends "res://smoke/automation/invocation_public_acceptance.gd"

# Review probe: normal embedded setup; no forced focus or gameplay mutation.
func _run() -> void:
    if not await _entry_public(): return
    var opener := main.find_child("InspectOtherRegions", true, false) as Control
    var focused := render_viewport.gui_get_focus_owner()
    print("PILE_KEYBOARD_REVIEW opener_focus_mode=%d after_dismissal=%s revision=%d" % [
        opener.focus_mode, "none" if focused == null else str(focused.get_path()), witness.Revision()])
    var visited: Array[String] = []
    for step in range(80):
        if render_viewport.gui_get_focus_owner() == opener:
            print("PILE_KEYBOARD_REVIEW reached_opener=true tabs=%d" % step)
            if not await _release_table_resources(): return
            print("UI_ACCEPTANCE_OK invocation_pile_keyboard_review")
            quit(0)
            return
        if not await ui.press_key(KEY_TAB): return
        var current := render_viewport.gui_get_focus_owner()
        if current != null and str(current.get_path()) not in visited:
            visited.append(str(current.get_path()))
    print("PILE_KEYBOARD_REVIEW reached_opener=false visited=" + str(visited))
    await ui.expect(func(): return false, "Other piles is reachable through normal Tab navigation after Escape dismissal", 0)
