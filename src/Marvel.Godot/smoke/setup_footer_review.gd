extends "res://smoke/setup_footer_smoke.gd"

func _run() -> void:
        var evidence := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
        if not evidence.is_empty() and DisplayServer.get_name() != "headless":
                DirAccess.make_dir_recursive_absolute(evidence)
                OS.set_environment("MARVEL_SMOKE_CAPTURE_DIR", evidence)
        if not await _open_setup(load("res://Main.tscn") as PackedScene): return
        var footer := main.find_child("SetupFooter", true, false) as Control
        if not await _entry_footer_is_safe(footer): return
        if not await _choose_modular_and_start(): return
        if not await _play_footer_is_safe(footer): return
        if not await _release_table_resources(): return
        print("REVIEW_SETUP_LIFECYCLE_OK")
        quit(0)

func _entry_footer_is_safe(footer: Control) -> bool:
        if not footer.is_visible_in_tree() or not _control_is_fully_visible(_button_named("Start game")):
                _fail("default setup commitment is not fully visible")
                return false
        if not await _capture_checkpoint("review-default"): return false
        if not await _pointer_activate(_node("Setup/Selections/Fields/EntryModes/JoinFlow")): return false
        if footer.visible:
                _fail("setup footer remains visible in join mode")
                return false
        if not await _capture_checkpoint("review-join"): return false
        if not await _pointer_activate(_node("Setup/Selections/Fields/EntryModes/StartFlow")): return false
        if not footer.visible:
                _fail("setup footer did not return in create mode")
                return false
        return true

func _play_footer_is_safe(footer: Control) -> bool:
        var slider := _node("Toolbar/InterfaceScale") as HSlider
        var before := slider.value
        if not await _prepare_activation(slider): return false
        var bounds := _visible_control_rect(slider)
        _inject_pointer_click(Vector2(bounds.position.x + bounds.size.x * 0.8, bounds.get_center().y))
        await process_frame
        if slider.value == before or footer.visible:
                _fail("scale input failed or revived setup footer on table")
                return false
        var motion := _node("Toolbar/Motion") as CheckButton
        var motion_before := motion.button_pressed
        if not await _pointer_activate(motion): return false
        if motion.button_pressed == motion_before or footer.visible:
                _fail("motion setting failed or revived setup footer")
                return false
        render_viewport.size = Vector2i(1600, 900)
        await process_frame
        await process_frame
        if footer.visible:
                _fail("resize revived setup footer on table")
                return false
        render_viewport.size = Vector2i(1920, 1080)
        await process_frame
        if footer.visible:
                _fail("restoring viewport revived setup footer")
                return false
        return await _capture_checkpoint("review-play")
