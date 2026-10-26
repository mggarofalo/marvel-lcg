extends RefCounted

const Locator = preload("res://smoke/automation/ui_locator.gd")
const Evidence = preload("res://smoke/automation/ui_evidence.gd")
var root: Node
var viewport: Viewport
var timeout_ms := 3000
var failure := ""
var evidence_directory := ""
var steps: Array = []
var report_failure: Callable

func _init(scope: Node, surface: Viewport, on_failure: Callable = Callable()) -> void:
    root = scope
    viewport = surface
    report_failure = on_failure

func button(text: String, exact: bool = true) -> RefCounted:
    return Locator.button(root, text, exact)

func named(node_name: String) -> RefCounted:
    return Locator.named(root, node_name)

func text(wanted: String) -> RefCounted:
    return Locator.text(root, wanted)

func expect(condition: Callable, description: String, deadline_ms: int = -1) -> bool:
    if not failure.is_empty(): return false
    steps.append("expect: " + description)
    var end := Time.get_ticks_msec() + (timeout_ms if deadline_ms < 0 else deadline_ms)
    while true:
        if condition.call(): return true
        if Time.get_ticks_msec() >= end: return _fail("Timed out: " + description)
        await viewport.get_tree().process_frame
    return false

func expect_visible(locator: RefCounted) -> bool:
    return await expect(func() -> bool: return locator.all().size() == 1,
        "exactly one visible " + locator.description)

func expect_fully_visible_enabled(locator: RefCounted) -> bool:
    return await expect(func() -> bool:
        var matches: Array = locator.all()
        return matches.size() == 1 and _fully_visible_enabled(matches[0]),
        "fully visible enabled " + locator.description)

func _fully_visible_enabled(control: Control) -> bool:
    if not _enabled(control) or control.size.x <= 0 or control.size.y <= 0: return false
    var transform := control.get_global_transform_with_canvas()
    var corners := [Vector2.ZERO, Vector2(control.size.x, 0), control.size, Vector2(0, control.size.y)]
    for corner in corners:
        var point: Vector2 = transform * corner
        # Rect2.has_point excludes the right/bottom edge; enclosure includes it.
        if not viewport.get_visible_rect().encloses(Rect2(point, Vector2.ZERO)): return false
        var ancestor: Node = control.get_parent()
        while ancestor != null and not ancestor is Viewport:
            if ancestor is Control and ancestor.clip_contents:
                var local: Vector2 = ancestor.get_global_transform_with_canvas().affine_inverse() * point
                if not Rect2(Vector2.ZERO, ancestor.size).encloses(Rect2(local, Vector2.ZERO)): return false
            ancestor = ancestor.get_parent()
    return true

func click(locator: RefCounted, button_index: MouseButton = MOUSE_BUTTON_LEFT) -> bool:
    if not failure.is_empty(): return false
    steps.append("click: " + locator.description)
    var point := await _ready_point(locator)
    if point == Vector2.INF: return false
    # No retries after press: a mutation may already have committed.
    _mouse_button(point, button_index, true)
    _mouse_button(point, button_index, false)
    await viewport.get_tree().process_frame
    return true

func hover(locator: RefCounted) -> bool:
    if not failure.is_empty(): return false
    steps.append("hover: " + locator.description)
    return await _ready_point(locator) != Vector2.INF

func press_key(code: Key, shift: bool = false, ctrl: bool = false) -> bool:
    if not failure.is_empty(): return false
    steps.append("key: %s shift=%s ctrl=%s" % [code, shift, ctrl])
    for pressed in [true, false]:
        var event := InputEventKey.new()
        event.keycode = code
        event.pressed = pressed
        event.shift_pressed = shift
        event.ctrl_pressed = ctrl
        viewport.push_input(event, true)
    await viewport.get_tree().process_frame
    return true

func drag(source: RefCounted, target: RefCounted, frames: int = 8) -> bool:
    if not failure.is_empty(): return false
    if frames < 2: return _fail("Drag needs at least two intermediate frames")
    steps.append("drag: %s -> %s" % [source.description, target.description])
    var finish := await _ready_point(target)
    if finish == Vector2.INF: return false
    var destination: Control = target.all()[0]
    var destination_pose := destination.get_global_transform_with_canvas()
    var destination_size := destination.size
    var start := await _ready_point(source)
    if start == Vector2.INF: return false
    if not _same_destination(target, destination, destination_pose, destination_size):
        return _fail("Drag destination changed before press")
    _mouse_button(start, MOUSE_BUTTON_LEFT, true)
    for index in range(1, frames + 1):
        _move(start.lerp(finish, float(index) / frames), MOUSE_BUTTON_MASK_LEFT)
        await viewport.get_tree().process_frame
        if not _same_destination(target, destination, destination_pose, destination_size):
            # Release outside the surface; never drop on a replacement control.
            _mouse_button(Vector2(-100, -100), MOUSE_BUTTON_LEFT, false)
            return _fail("Drag destination changed during gesture")
    _mouse_button(finish, MOUSE_BUTTON_LEFT, false)
    await viewport.get_tree().process_frame
    return true

func _same_destination(locator: RefCounted, target: Control, pose: Transform2D, size: Vector2) -> bool:
    if not is_instance_valid(target): return false
    var current: Array = locator.all()
    return current.size() == 1 and current[0] == target and _enabled(target) \
        and target.get_global_transform_with_canvas().is_equal_approx(pose) and target.size.is_equal_approx(size)

func _ready_point(locator: RefCounted) -> Vector2:
    var end := Time.get_ticks_msec() + timeout_ms
    var reason := "no visible match"
    while Time.get_ticks_msec() <= end:
        var matches: Array = locator.all()
        if matches.size() > 1:
            _fail("Ambiguous %s: %d visible matches; scope the locator" % [locator.description, matches.size()])
            return Vector2.INF
        if matches.size() == 1:
            var control: Control = matches[0]
            reason = "disabled, clipped, moving or occluded"
            if _enabled(control):
                for fraction in [Vector2(0.5, 0.5), Vector2(0.2, 0.2), Vector2(0.8, 0.2), Vector2(0.2, 0.8), Vector2(0.8, 0.8)]:
                    var point: Vector2 = control.get_global_transform_with_canvas() * (control.size * fraction)
                    _move(point)
                    await viewport.get_tree().process_frame
                    if not is_instance_valid(control): break
                    # Pin identity across the awaited frame. A replacement is never
                    # clicked using geometry or intent from its predecessor.
                    var current: Array = locator.all()
                    if current.size() != 1 or current[0] != control: break
                    var fresh: Vector2 = control.get_global_transform_with_canvas() * (control.size * fraction)
                    if point.distance_to(fresh) > 0.5: continue
                    _move(point)
                    if _owns_point(control, point): return point
        await viewport.get_tree().process_frame
    _fail("Cannot act on %s: %s" % [locator.description, reason])
    return Vector2.INF

func _enabled(control: Control) -> bool:
    return control.is_visible_in_tree() and not control.is_queued_for_deletion() \
        and control.mouse_filter != Control.MOUSE_FILTER_IGNORE \
        and not (control is BaseButton and control.disabled)

func _owns_point(control: Control, point: Vector2) -> bool:
    if not _enabled(control) or not viewport.get_visible_rect().has_point(point): return false
    var ancestor: Node = control
    while ancestor != null and not ancestor is Viewport:
        if ancestor is Control and (ancestor == control or ancestor.clip_contents):
            var local: Vector2 = ancestor.get_global_transform_with_canvas().affine_inverse() * point
            if not Rect2(Vector2.ZERO, ancestor.size).has_point(local): return false
        ancestor = ancestor.get_parent()
    var hovered := viewport.gui_get_hovered_control()
    # Composite controls own their descendant hit surfaces. The caller asserts
    # the observed outcome; hit ownership alone does not establish activation.
    if hovered == control or (hovered != null and control.is_ancestor_of(hovered)): return true
    # Godot can report a PASS container while routing to its PASS descendant.
    if hovered != null and hovered.is_ancestor_of(control):
        var current: Control = control
        while current != hovered:
            if current.mouse_filter == Control.MOUSE_FILTER_STOP: return false
            current = current.get_parent() as Control
            if current == null: return false
        return hovered.mouse_filter == Control.MOUSE_FILTER_PASS
    return false

func _move(point: Vector2, mask: int = 0) -> void:
    var event := InputEventMouseMotion.new()
    event.position = point
    event.global_position = point
    event.relative = point - viewport.get_mouse_position()
    event.button_mask = mask
    viewport.push_input(event, true)

func _mouse_button(point: Vector2, index: MouseButton, pressed: bool) -> void:
    var event := InputEventMouseButton.new()
    event.position = point
    event.global_position = point
    event.button_index = index
    event.pressed = pressed
    event.button_mask = (1 << (index - 1)) if pressed else 0
    viewport.push_input(event, true)

func _fail(message: String) -> bool:
    if failure.is_empty():
        failure = message
        evidence_directory = Evidence.capture(root, viewport, message, steps)
        if report_failure.is_valid(): report_failure.call(message + " Evidence: " + evidence_directory)
        else: push_error(message + " Evidence: " + evidence_directory)
    return false
