extends RefCounted

static func requested(smoke, label: String) -> bool:
    if label not in OS.get_environment("MARVEL_NATIVE_SCALE_CHECKPOINTS").split(","): return true
    var marker := "scale_checkpoint_" + label.replace("-", "_")
    if smoke.main.has_meta(marker): return true
    if not await verify(smoke, label): return false
    smoke.main.set_meta(marker, true)
    return true

static func verify(smoke, label: String) -> bool:
    var witness = preload("res://smoke/NativeGameWitness.cs").new()
    witness.Bind(smoke.main)
    var before: String = witness.Draft()
    var revision: int = witness.Revision()
    var surface := _surface(smoke)
    var slider: HSlider = smoke._node("Toolbar/InterfaceScale")
    var motion: CheckButton = smoke._node("Toolbar/Motion")
    var original_scale := slider.value
    var original_motion := motion.button_pressed
    var original_size: Vector2i = smoke.render_viewport.size
    for viewport in [Vector2i(1920, 1080), Vector2i(2560, 1440)]:
        smoke.render_viewport.size = viewport
        for enabled in [true, false]:
            motion.button_pressed = enabled
            if not await _scales(smoke, witness, slider, before, revision, label, surface): return false
    smoke.render_viewport.size = original_size
    slider.value = original_scale
    motion.button_pressed = original_motion
    await smoke.process_frame
    await smoke.process_frame
    if witness.Draft() != before or witness.Revision() != revision or _surface(smoke) != surface:
        smoke._fail("restoring scale profile changed draft or authority: " + label)
        return false
    print("NATIVE_SCALE_CHECKPOINT_OK state=%s scales=11 viewports=2 motion=both restored=true" % label)
    return true

static func _scales(smoke, witness, slider: HSlider, before: String, revision: int, label: String, surface: String) -> bool:
    for scale in range(50, 151, 10):
        slider.value = scale
        await smoke.process_frame
        await smoke.process_frame
        if witness.Draft() != before or witness.Revision() != revision or _surface(smoke) != surface:
            smoke._fail("live scale changed draft or authority: %s scale=%d" % [label, scale])
            return false
        if not _situation_ownership(smoke, label, scale): return false
        var control: Control = _commitment(smoke)
        if control == null or not await smoke._prepare_activation(control):
            smoke._fail("live scale lost the current commitment/review control: " + label)
            return false
        if not smoke._control_is_fully_visible(control) or not _readable(control):
            smoke._fail("live scale clips commitment geometry or its complete label: " + label)
            return false
        if not await _focus_commitment(smoke, control): return false
        if label == "dense-table":
            if not await preload("res://smoke/native_dense_table.gd").verify(smoke): return false
        if witness.Draft() != before or witness.Revision() != revision:
            smoke._fail("scale focus traversal changed the draft or committed: " + label)
            return false
    return true

static func _focus_commitment(smoke, control: Control) -> bool:
    if not control is BaseButton or control.disabled: return true
    return await preload("res://smoke/native_keyboard_traversal.gd").reach(smoke, control)

static func _situation_ownership(smoke, label: String, scale: int) -> bool:
    if label != "nested-window": return true
    var copy := smoke.main.find_child("CausalSituationCopy", true, false) as Control
    if copy != null and not copy.is_visible_in_tree(): return true
    smoke._fail("ability sheet repeats its situation after live scale: scale=%d owners=%d" % [scale, smoke.main.get_meta("ability_situation_owners", 0)])
    return false

static func _surface(smoke) -> String:
    var visible: Array[String] = []
    for name in ["CardActionChoices", "AbilityWindow", "CompleteChoicesFrame", "Setup"]:
        var node := smoke.main.find_child(name, true, false) as Control
        if node != null and node.is_visible_in_tree(): visible.append(name)
    if smoke._payment_modal() != null: visible.append("payment")
    for option in smoke.main.find_children("*", "OptionButton", true, false):
        if option.is_visible_in_tree(): visible.append("%s=%d" % [option.name, option.selected])
    return ",".join(visible)

static func _readable(control: Control) -> bool:
    if not control is Button: return true
    var button := control as Button
    var font := button.get_theme_font("font")
    var size := button.get_theme_font_size("font_size")
    var style := button.get_theme_stylebox("normal")
    var available := button.size - style.get_minimum_size()
    var text_size := font.get_string_size(button.text, HORIZONTAL_ALIGNMENT_LEFT, -1, size)
    if button.autowrap_mode != TextServer.AUTOWRAP_OFF:
        text_size = font.get_multiline_string_size(button.text, HORIZONTAL_ALIGNMENT_LEFT, available.x, size)
    return not button.text.is_empty() and text_size.x <= available.x + 1.0 and text_size.y <= available.y + 1.0

static func _commitment(smoke) -> Control:
    for name in ["CardActionChoices", "AbilityWindow"]:
        var surface := smoke.main.find_child(name, true, false) as Control
        if surface != null and surface.is_visible_in_tree():
            var buttons: Array[Button] = smoke._visible_buttons(surface)
            if not buttons.is_empty(): return buttons[0]
    if smoke._payment_modal() != null: return smoke._payment_button("Submit")
    var complete := smoke.main.find_child("CompleteChoicesFrame", true, false) as Control
    if complete != null and complete.is_visible_in_tree(): return complete.find_child("Submit", true, false)
    var commit: Control = smoke._task_commit()
    if commit != null: return commit
    var start: Button = smoke._button_named("Start game")
    if start != null and start.is_visible_in_tree(): return start
    return smoke.main.find_child("ToggleHistory", true, false)
