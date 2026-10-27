extends "res://smoke/shell_finish_smoke.gd"


func _run() -> void:
        var evidence := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
        if not evidence.is_empty() and DisplayServer.get_name() != "headless":
                DirAccess.make_dir_recursive_absolute(evidence)
                OS.set_environment("MARVEL_SMOKE_CAPTURE_DIR", evidence)
        var packed := load("res://Main.tscn") as PackedScene
        if not await _open_setup(packed): return
        if not await _choose_modular_and_start(): return
        if not await _release_table_resources(): return
        print("SETUP_FOOTER_SMOKE_OK")
        quit(0)


func _choose_modular_and_start() -> bool:
        if not await _pointer_activate(_choice("Modular", "Kree Fanatic")): return false
        var modular := _node("Setup/Selections/Fields/Grid/Modular") as MenuButton
        if "Kree Fanatic" not in modular.text:
                _fail("the last modular choice was not retained")
                return false
        var start := _button_named("Start game")
        if not _control_is_fully_visible(start) or start.get_theme_font_size("font_size") < 16:
                _fail("scrolling to a modular choice clipped the commitment or made its text unreadable")
                return false
        if not await _capture_checkpoint("setup-footer"): return false
        if not await _pointer_activate(start): return false
        if not await _wait_for(func() -> bool: return _play().visible and _decision() != null):
                _fail("the setup commitment did not open the table")
                return false
        if main.find_child("SetupFooter", true, false).visible:
                _fail("setup commitment remained visible on the table")
                return false
        return true
