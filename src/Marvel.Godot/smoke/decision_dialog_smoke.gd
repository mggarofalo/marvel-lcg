extends "res://smoke/local_game_smoke.gd"

func _run() -> void:
    if not await _open_setup(load("res://Main.tscn") as PackedScene): return
    var arguments := OS.get_cmdline_user_args()
    if arguments.is_empty():
        _fail("real Raid prompt fixture is required")
        return
    var probe := preload("res://smoke/DecisionDialogNativeProbe.cs").new()
    main.add_child(probe)
    probe.Show(main, FileAccess.get_file_as_string(arguments[0]))
    if not await _wait_for(func(): return main.find_child("CompleteChoicesFrame", true, false) != null):
        _fail("ordering did not open a decision dialog")
        return
    for unused in range(8): await process_frame
    var frame := main.find_child("CompleteChoicesFrame", true, false) as Control
    if not _choices_visible(frame, probe): return
    if not await _stage_choice(frame, probe): return
    var selected: int = probe.Selected
    if not await _inspect_and_resume(frame, probe, selected): return
    frame = main.find_child("CompleteChoicesFrame", true, false) as Control
    if not await _commit_choice(frame, probe, selected): return
    print("DECISION_DIALOG_OK actual_raid_snapshot=true both_choices_visible=true inspect_preserves=true escape_resume=true explicit_commit=true")
    quit(0)

func _choices_visible(frame: Control, probe: Node) -> bool:
    var choices := _dialog_choices(frame)
    if choices.size() != 2 or probe.Submissions != 0 or probe.Selected != -1:
        _fail("opening the dialog must offer both choices without selecting or committing")
        return false
    var body := frame.find_child("DecisionBodyScroll", true, false) as ScrollContainer
    for choice in choices:
        if not body.get_global_rect().encloses(choice.get_global_rect()):
            _fail("both Raid choices must be visible without scrolling")
            return false
    return true

func _stage_choice(frame: Control, probe: Node) -> bool:
    var choices := _dialog_choices(frame)
    if not await _pointer_activate(choices[0]): return false
    var first: int = probe.Selected
    if probe.Submissions != 0 or first == -1:
        _fail("choice click did not stage without committing")
        return false
    choices = _dialog_choices(frame)
    if not await _pointer_activate(choices[1]): return false
    if probe.Selected == first or probe.Submissions != 0:
        _fail("choice could not be changed without committing")
        return false
    return true

func _inspect_and_resume(frame: Control, probe: Node, selected: int) -> bool:
    if not await _pointer_activate(frame.find_child("DecisionSource", true, false) as Button): return false
    await process_frame
    var inspector := main.find_child("CardInspector", true, false) as Control
    if inspector == null or not inspector.is_visible_in_tree():
        _fail("source inspection did not open above the dialog")
        return false
    await _dialog_escape()
    if inspector.is_visible_in_tree() or probe.Selected != selected or not is_instance_valid(frame):
        _fail("inspection dismissal lost the decision")
        return false
    await _dialog_escape()
    if probe.Selected != selected or probe.Submissions != 0:
        _fail("return to table answered or lost the choice")
        return false
    if not await _pointer_activate(main.find_child("CompleteChoiceSheet", true, false) as Button): return false
    await process_frame
    await process_frame
    return true

func _commit_choice(frame: Control, probe: Node, selected: int) -> bool:
    var commit := frame.find_child("Submit", true, false) as Button
    if commit == null or not commit.text.begins_with("Resolve "):
        _fail("explicit commitment did not name the selected effect")
        return false
    await _capture_checkpoint("raid-order-dialog")
    if not await _pointer_activate(commit): return false
    if probe.Submissions != 1 or probe.Submitted != selected:
        _fail("commit did not submit exactly the staged answer")
        return false
    return true

func _dialog_choices(frame: Node) -> Array[Button]:
    var result: Array[Button] = []
    for candidate in frame.find_children("Affordance*", "Button", true, false):
        result.append(candidate as Button)
    return result

func _dialog_escape() -> void:
    for pressed in [true, false]:
        var key := InputEventKey.new()
        key.keycode = KEY_ESCAPE
        key.pressed = pressed
        render_viewport.push_input(key, true)
        await process_frame
