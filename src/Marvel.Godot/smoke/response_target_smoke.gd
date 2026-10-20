extends "res://smoke/ability_window_smoke.gd"

func _run() -> void:
        if not await _open_window_game() or not await _play_mockingbird() \
                or not await _verify_named_response():
                _fail("response target journey did not complete")
                return
        main.queue_free()
        await process_frame
        await process_frame
        quit(0)

func _verify_named_response() -> bool:
        var source := _tabletop_card_named("Mockingbird")
        var revision := _recipient_revision()
        if not await _check_response_dismissal(source, revision): return false
        if not await _keyboard_response_target(source): return false
        var target := main.find_child("Menu1Item0", true, false) as Button
        if "Rhino" not in target.text:
                _fail("response target omitted Rhino: " + target.text)
                return false
        if not await _capture_checkpoint("response-context-targets"): return false
        if OS.get_environment("MARVEL_RESPONSE_TARGET_DRAG") == "true":
                if not await _drag_response_target(source, revision): return false
        else:
                if not await _pointer_activate(target): return false
        return await _verify_response_result(revision)

func _check_response_dismissal(source: Control, revision: int) -> bool:
        var bottom_actions := main.find_child("ContextualActionObjects", true, false) as Control
        if bottom_actions != null and bottom_actions.is_visible_in_tree():
                _fail("response repeats its commitments in the bottom action strip")
                return false
        if not await _capture_checkpoint("response-table-before-context"): return false
        if not await _open_response_context(source): return false
        _response_key(KEY_ESCAPE)
        if not await _wait_for(func(): return _response_menu() == null): return false
        if _recipient_revision() != revision:
                _fail("dismissing response menu committed or passed")
                return false
        return true

func _keyboard_response_target(source: Control) -> bool:
        var table_entry := _window().find_child("WindowUseTable", true, false) as Button
        if not await _pointer_activate(table_entry): return false
        if render_viewport.gui_get_focus_owner() != source:
                _fail("response keyboard table entry did not focus its source")
                return false
        _response_key(KEY_MENU)
        if not await _wait_for(func(): return _response_menu() != null): return false
        if not await _wait_for(func(): return render_viewport.gui_get_focus_owner() == main.find_child("Menu0Item0", true, false)): return false
        _response_key(KEY_RIGHT)
        if not await _wait_for(func(): return main.find_child("Menu1Item0", true, false) != null):
                _fail("response source omitted current target submenu")
                return false
        return true

func _drag_response_target(source: Control, revision: int) -> bool:
        _response_key(KEY_ESCAPE)
        _response_key(KEY_ESCAPE)
        if not await _wait_for(func(): return _response_menu() == null): return false
        if not await _drag(source, _card_for_anchor(RHINO).get_global_rect().get_center()): return false
        if not await _wait_for(func(): return main.find_child("LocalDecisionWorkspace", true, false) != null):
                _fail("response drag did not start localized draft")
                return false
        if _recipient_revision() != revision:
                _fail("response drag committed before review")
                return false
        if not await _capture_checkpoint("response-drag-draft"): return false
        var commit := _button_named("Stun Rhino")
        return commit != null and await _pointer_activate(commit)

func _verify_response_result(revision: int) -> bool:
        if not await _wait_for(func(): return _recipient_revision() == revision + 1): return false
        if not await _wait_for_result_motion(): return false
        var statuses := _card_for_anchor(RHINO).find_children("StatusCard*", "Control", true, false)
        if statuses.is_empty() or statuses[0].accessibility_name != "Stunned":
                _fail("response target did not stun Rhino")
                return false
        print("RESPONSE_TARGET_SMOKE_OK")
        return true

func _response_menu() -> Node:
        return main.find_child("CardContextMenu", true, false)

func _response_key(code: Key) -> void:
        var press := InputEventKey.new()
        press.keycode = code
        press.pressed = true
        render_viewport.push_input(press)
        var release := InputEventKey.new()
        release.keycode = code
        render_viewport.push_input(release)

func _open_response_context(source: Control) -> bool:
        await _scroll_control_into_view(source)
        var point := source.get_global_rect().get_center()
        if not await _control_owns_point(source, point):
                _fail("response sheet obscures the source card")
                return false
        var motion := InputEventMouseMotion.new()
        motion.position = point
        motion.global_position = point
        render_viewport.push_input(motion, true)
        await process_frame
        for pressed in [true, false]:
                var event := InputEventMouseButton.new()
                event.button_index = MOUSE_BUTTON_RIGHT
                event.position = point
                event.global_position = point
                event.pressed = pressed
                render_viewport.push_input(event, true)
        if not await _wait_for(func(): return _response_menu() != null):
                _fail("right-click response source did not open menu")
                return false
        return await _wait_for(func(): return render_viewport.gui_get_focus_owner() == main.find_child("Menu0Item0", true, false))
