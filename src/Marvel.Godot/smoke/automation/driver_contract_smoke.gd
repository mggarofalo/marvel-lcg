extends SceneTree

const Driver = preload("res://smoke/automation/ui_driver.gd")
const Locator = preload("res://smoke/automation/ui_locator.gd")
var surface: SubViewport
var scene: Control
var clicks := 0
var moves_held := 0
var releases := 0
var failures: Array[String] = []

func _initialize() -> void:
    _run.call_deferred()

func _run() -> void:
    surface = SubViewport.new()
    surface.size = Vector2i(800, 600)
    surface.render_target_update_mode = SubViewport.UPDATE_ALWAYS
    root.add_child(surface)
    surface.notify_mouse_entered()
    scene = Control.new()
    scene.size = surface.size
    surface.add_child(scene)
    var play := _button("Play", Vector2(30, 30))
    play.pressed.connect(func() -> void: clicks += 1)
    await process_frame
    await process_frame
    var ui := Driver.new(scene, surface)
    _check(await ui.click(ui.button("Play")), "native click: " + ui.failure)
    _check(clicks == 1, "click must commit exactly once")
    _check(await ui.press_key(KEY_ENTER), "native keyboard dispatch")
    _check(clicks == 2, "Enter must activate the focused button once")
    var hidden := _button("Play", Vector2(30, 100))
    hidden.hide()
    _check(await ui.click(ui.button("Play")), "hidden duplicate must not be selected")
    _check(clicks == 3, "hidden duplicate changed activation")
    hidden.show()
    _check(await ui.click(Locator.button(play, "Play")), "scoped duplicate must remain usable")
    _check(clicks == 4, "scoped activation must commit once")
    var ambiguous := Driver.new(scene, surface, _expected_failure)
    _check(not await ambiguous.click(ambiguous.button("Play")), "duplicate must be rejected")
    _check(ambiguous.failure.contains("Ambiguous"), "duplicate diagnostic")
    _check(not await ambiguous.press_key(KEY_ENTER) and clicks == 4, "failure must latch and block later input")
    hidden.queue_free()
    await process_frame
    play.disabled = true
    await _reject(play, "disabled")
    play.disabled = false
    var cover := _button("Cover", play.position)
    await _reject(play, "occluded")
    cover.queue_free()
    await process_frame
    var clip := Control.new()
    clip.position = Vector2(300, 20)
    clip.size = Vector2(20, 20)
    clip.clip_contents = true
    scene.add_child(clip)
    var clipped := _button("Clipped", Vector2(40, 40))
    clipped.reparent(clip, false)
    await _reject(clipped, "clipped")
    var delayed := _button("Later", Vector2(30, 160))
    delayed.disabled = true
    _enable_later(delayed)
    _check(await ui.click(ui.button("Later")), "wait for enabled control")
    var source := _button("Drag source", Vector2(30, 230))
    var target := _button("Drop target", Vector2(300, 230))
    source.gui_input.connect(func(event: InputEvent) -> void:
        if event is InputEventMouseMotion and event.button_mask == MOUSE_BUTTON_MASK_LEFT: moves_held += 1
        if event is InputEventMouseButton and not event.pressed: releases += 1)
    _check(await ui.drag(Locator.control(source), Locator.control(target), 6), "native drag")
    _check(moves_held >= 2 and releases == 1, "drag must deliver intermediate held frames and release")
    source.button_down.connect(func() -> void: target.position.x += 40, CONNECT_ONE_SHOT)
    var stale := Driver.new(scene, surface, _expected_failure)
    _check(not await stale.drag(Locator.control(source), Locator.control(target)), "moving destination must cancel the drag")
    _check(stale.failure.contains("destination changed") and releases == 2, "cancelled drag must release held input")
    var label := Label.new()
    label.text = "Visible explanation"
    label.name = "Explanation"
    scene.add_child(label)
    _check(await ui.expect_visible(ui.text("Visible explanation")), "visible text locator")
    _check(await ui.expect_visible(ui.named("Explanation")), "named control locator")
    _check(await ui.hover(ui.button("Drop target")), "hover on exposed target")
    var secret := LineEdit.new()
    secret.text = "private-canary-value"
    scene.add_child(secret)
    var timeout := Driver.new(scene, surface, _expected_failure)
    _check(not await timeout.expect(func() -> bool: return false, "unreachable condition", 20), "bounded timeout")
    _check(timeout.failure.contains("unreachable condition"), "timeout must identify condition")
    var evidence := FileAccess.get_file_as_string(timeout.evidence_directory.path_join("failure.json"))
    var record: Variant = JSON.parse_string(evidence)
    _check(not evidence.contains("private-canary-value"), "evidence must omit editable field values")
    _check(record is Dictionary and record.message.contains("unreachable condition") and not record.controls.is_empty(), "failure evidence must describe visible controls")
    if DisplayServer.get_name() == "headless":
        _check(record.screenshot.begins_with("unavailable"), "headless must not claim pixel evidence")
    else:
        _check(record.screenshot == "failure.png" and FileAccess.file_exists(timeout.evidence_directory.path_join("failure.png")), "rendered failure must save screenshot")
    scene.queue_free()
    await process_frame
    if failures.is_empty(): print("UI_ACCEPTANCE_OK driver_contracts")
    quit(0 if failures.is_empty() else 1)

func _button(text: String, position: Vector2) -> Button:
    var button := Button.new()
    button.text = text
    button.position = position
    button.size = Vector2(160, 50)
    scene.add_child(button)
    return button

func _reject(control: Control, reason: String) -> void:
    var driver := Driver.new(scene, surface, _expected_failure)
    driver.timeout_ms = 40
    var before := clicks
    _check(not await driver.click(Locator.control(control)), reason + " target accepted")
    _check(clicks == before, reason + " target caused a mutation")
    _check(not driver.evidence_directory.is_empty(), reason + " failure lacks evidence")

func _enable_later(button: Button) -> void:
    for frame in 4: await process_frame
    button.disabled = false

func _expected_failure(message: String) -> void:
    print("EXPECTED_UI_FAILURE ", message)

func _check(condition: bool, message: String) -> void:
    if not condition:
        failures.append(message)
        push_error(message)
