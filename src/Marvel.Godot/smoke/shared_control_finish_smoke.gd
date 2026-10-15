extends SceneTree

var failures: Array[String] = []
var main: Control

func _initialize() -> void:
    _run.call_deferred()

func _run() -> void:
    root.size = Vector2i(1920, 1080)
    main = (load("res://Main.tscn") as PackedScene).instantiate()
    root.add_child(main)
    for frame in range(8): await process_frame
    var primary: Button
    for node in main.find_children("*", "Button", true, false):
        if node.is_visible_in_tree() and node.theme_type_variation == "PrimaryButton" and not node.disabled:
            primary = node
    if primary == null:
        push_error("CONTROL_FINISH_FAILED missing primary")
        quit(1)
        return
    await _exercise(primary)
    for failure in failures: push_error("CONTROL_FINISH_FAILED " + failure)
    if failures.is_empty(): print("CONTROL_FINISH_OK geometry=true cursor=true focus=true reduced_motion=true")
    quit(0 if failures.is_empty() else 1)

func _exercise(primary: Button) -> void:
    var initial := primary.get_global_rect()
    var initial_minimum := primary.get_combined_minimum_size()
    _check_baselines(primary)
    await _capture("rest")
    _move(primary.get_global_rect().get_center())
    await create_timer(0.15).timeout
    await _capture("hover")
    primary.grab_focus()
    await process_frame
    await _capture("focus")
    _button(primary.get_global_rect().get_center(), true)
    await create_timer(0.08).timeout
    await _capture("press")
    _move(Vector2(10, 10))
    _button(Vector2(10, 10), false)
    primary.release_focus()
    await create_timer(0.15).timeout
    if primary.get_global_rect() != initial or primary.get_combined_minimum_size() != initial_minimum:
        failures.append("interaction changed commitment hit geometry")
    if primary.mouse_default_cursor_shape != Control.CURSOR_POINTING_HAND:
        failures.append("enabled commitment lacks pointing hand")
    primary.disabled = true
    await process_frame
    await _capture("disabled")
    if primary.mouse_default_cursor_shape != Control.CURSOR_ARROW:
        failures.append("disabled commitment retains interactive cursor")
    primary.theme_type_variation = "WaitingButton"
    primary.text = "Sending…"
    await process_frame
    await _capture("waiting")
    primary.disabled = false
    primary.theme_type_variation = "PrimaryButton"
    primary.text = "Start game"
    var motion := main.find_child("Motion", true, false) as BaseButton
    motion.button_pressed = false
    _move(primary.get_global_rect().get_center())
    await process_frame
    await _capture("reduced-motion-hover")
    if not primary.has_meta("shared_control_bound"):
        failures.append("shared feedback was not installed on authored commitment")

func _check_baselines(primary: Button) -> void:
    var normal := primary.get_theme_stylebox("normal")
    for state in ["hover", "pressed", "hover_pressed", "disabled"]:
        var treatment := primary.get_theme_stylebox(state)
        if treatment.content_margin_top != normal.content_margin_top or treatment.content_margin_bottom != normal.content_margin_bottom:
            failures.append("interaction moved the caption baseline: " + state)


func _capture(state: String) -> void:
    var directory := OS.get_environment("MARVEL_CONTROL_CAPTURE")
    if directory.is_empty(): return
    for frame in range(2): await process_frame
    await RenderingServer.frame_post_draw
    root.get_texture().get_image().save_png(directory.path_join("controls-%s-%s.png" % [OS.get_environment("MARVEL_UI_SCALE"), state]))

func _move(position: Vector2) -> void:
    var input := InputEventMouseMotion.new()
    input.position = position
    input.global_position = position
    Input.parse_input_event(input)

func _button(position: Vector2, pressed: bool) -> void:
    var input := InputEventMouseButton.new()
    input.position = position
    input.global_position = position
    input.button_index = MOUSE_BUTTON_LEFT
    input.pressed = pressed
    Input.parse_input_event(input)
