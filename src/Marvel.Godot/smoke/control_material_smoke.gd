extends SceneTree

var failures: Array[String] = []
var main: Control
var expect_materials := OS.get_environment("MARVEL_MATERIAL_EXPECT") == "new"

func _initialize() -> void:
    _run.call_deferred()

func _run() -> void:
    root.size = Vector2i(1920, 1080)
    main = (load("res://Main.tscn") as PackedScene).instantiate()
    root.add_child(main)
    for frame in range(8): await process_frame
    var controls := _create_specimen()
    for frame in range(4): await process_frame
    for control in controls: _check_styles(control)
    await _capture("rest")
    for index in range(3): await _exercise(controls[index], str(index))
    for control in controls: control.disabled = true
    await process_frame
    await _capture("unavailable")
    await _finish()

func _create_specimen() -> Array[Button]:
    var specimen := PanelContainer.new()
    specimen.name = "SyntheticControlSpecimen"
    specimen.position = Vector2(1080, 100)
    specimen.size = Vector2(680, 780)
    specimen.theme_type_variation = "SurfacePanel"
    main.add_child(specimen)
    var rows := VBoxContainer.new()
    rows.add_theme_constant_override("separation", 12)
    specimen.add_child(rows)
    var heading := Label.new()
    heading.text = "Synthetic control material specimen"
    rows.add_child(heading)
    var controls: Array[Button] = []
    for entry in [["PrimaryButton", "Attack Rhino"], ["ChoiceButton", "End turn"],
        ["TertiaryButton", "<<  History"], ["LegalTargetButton", "◇ Rhino"],
        ["SelectedTargetButton", "✓ Rhino"], ["ChoiceButton", "Choose a source\nReview its known cost"]]:
        var button := Button.new()
        button.name = "Material" + entry[0] + str(controls.size())
        button.text = entry[1]
        button.theme_type_variation = entry[0]
        button.custom_minimum_size = Vector2(560, 66 if OS.get_environment("MARVEL_UI_SCALE") == "150" else 44)
        rows.add_child(button)
        controls.append(button)
    return controls

func _finish() -> void:
    for failure in failures: push_error("CONTROL_MATERIAL_FAILED " + failure)
    if failures.is_empty(): print("CONTROL_MATERIAL_OK fixed_geometry=true hierarchy=true states=rest,hover,focus,press,unavailable")
    main.queue_free()
    for frame in range(3): await process_frame
    quit(0 if failures.is_empty() else 1)

func _check_styles(control: Button) -> void:
    var normal := control.get_theme_stylebox("normal") as StyleBoxFlat
    _check_fixed_geometry(control, normal)
    if expect_materials: _check_hierarchy(control, normal)

func _check_fixed_geometry(control: Button, normal: StyleBoxFlat) -> void:
    for state in ["hover", "pressed", "hover_pressed", "disabled"]:
        var treatment := control.get_theme_stylebox(state) as StyleBoxFlat
        for side in ["left", "top", "right", "bottom"]:
            if treatment.get("content_margin_" + side) != normal.get("content_margin_" + side):
                failures.append("caption margin moves for %s/%s/%s" % [control.name, state, side])
        if expect_materials and treatment.border_width_bottom != 1:
            failures.append("fake bottom bevel retained for %s/%s" % [control.name, state])

func _check_hierarchy(control: Button, normal: StyleBoxFlat) -> void:
    var radius := 9 if OS.get_environment("MARVEL_UI_SCALE") == "150" else 6
    if normal.corner_radius_top_left != radius: failures.append("wrong corner radius for " + str(control.name))
    if control.theme_type_variation == "PrimaryButton" and not normal.bg_color.is_equal_approx(Color("f2c14e")):
        failures.append("commitment does not carry the amber hierarchy")
    if control.theme_type_variation == "SelectedTargetButton" and normal.bg_color.is_equal_approx(Color("f2c14e")):
        failures.append("selected target competes with commitment")
    if control.theme_type_variation == "TertiaryButton" and normal.bg_color.a != 0:
        failures.append("utility has an opaque resting box")
    if control.get_theme_font("font").get_instance_id() == control.get_theme_font("font", "Label").get_instance_id():
        failures.append("control uses prose weight")

func _exercise(control: Button, id: String) -> void:
    var initial := control.get_global_rect()
    var minimum := control.get_combined_minimum_size()
    _move(initial.get_center())
    await create_timer(0.12).timeout
    await _capture("hover-" + id)
    control.grab_focus()
    await process_frame
    await _capture("focus-" + id)
    _button(initial.get_center(), true)
    await create_timer(0.07).timeout
    await _capture("press-" + id)
    _move(Vector2(10, 10))
    _button(Vector2(10, 10), false)
    control.release_focus()
    await create_timer(0.15).timeout
    if control.get_global_rect() != initial or control.get_combined_minimum_size() != minimum:
        failures.append("interaction moves control geometry: " + str(control.name))

func _capture(state: String) -> void:
    var directory := OS.get_environment("MARVEL_CONTROL_CAPTURE")
    if directory.is_empty(): return
    for frame in range(2): await process_frame
    await RenderingServer.frame_post_draw
    root.get_texture().get_image().save_png(directory.path_join("materials-%s-%s.png" % [OS.get_environment("MARVEL_UI_SCALE"), state]))

func _move(position: Vector2) -> void:
    var event := InputEventMouseMotion.new()
    event.position = position
    event.global_position = position
    Input.parse_input_event(event)

func _button(position: Vector2, pressed: bool) -> void:
    var event := InputEventMouseButton.new()
    event.position = position
    event.global_position = position
    event.button_index = MOUSE_BUTTON_LEFT
    event.pressed = pressed
    Input.parse_input_event(event)
