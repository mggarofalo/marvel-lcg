extends RefCounted

static func run(smoke) -> bool:
    if not await smoke._drag_to_prompt_owner_lane(smoke._tabletop_card_named("For Justice!")): return false
    if not await smoke._wait_for(func() -> bool: return smoke._payment_modal() != null): return false
    if not await smoke._choose_target(smoke.witness.Anchor("The Break-In!")): return false
    var source: Button = smoke._visible_button_beginning(smoke._payment_modal(), "Discard The Power of Justice")
    if source == null or not await smoke._pointer_activate(source): return false
    var anchor: int = smoke.witness.Anchor("The Power of Justice")
    if not await _declare(smoke, anchor, 0, "Energy"): return false
    if not await _declare(smoke, anchor, 1, "Energy"): return false
    if not await _declare(smoke, anchor, 0, "Mental"): return false
    if not await preload("res://smoke/native_scale_checkpoint.gd").verify(smoke, "resource-declaration"): return false
    if smoke.witness.Revision() != 0:
        smoke._fail("editing wild declarations committed the event")
        return false
    return await smoke._commit_once("For Justice!")

static func _declare(smoke, anchor: int, icon: int, resource: String) -> bool:
    var allocation := smoke._payment_modal().find_child("Allocation%d_%d" % [anchor, icon], true, false) as OptionButton
    if allocation == null:
        smoke._fail("declaration-sensitive payment has no explicit icon editor")
        return false
    var selected := -1
    for item in range(allocation.item_count):
        if resource in allocation.get_popup().get_item_tooltip(item): selected = item
    if selected < 0 or not await preload("res://smoke/native_keyboard_traversal.gd").reach(smoke, allocation): return false
    await preload("res://smoke/native_keyboard_traversal.gd").key(smoke, KEY_ENTER)
    var menu := allocation.get_popup()
    if not await _choose_from_menu(smoke, menu, selected): return false
    if smoke._payment_modal() == null:
        smoke._fail("editing a resource declaration closed the uncommitted payment")
        return false
    allocation = smoke._payment_modal().find_child("Allocation%d_%d" % [anchor, icon], true, false)
    if allocation == null or resource not in allocation.get_popup().get_item_tooltip(allocation.selected):
        smoke._fail("keyboard declaration edit was not retained: expected=%s index=%d actual=%s" % [resource, selected,
            "missing" if allocation == null else allocation.get_popup().get_item_tooltip(allocation.selected)])
        return false
    return true

static func _choose_from_menu(smoke, menu: PopupMenu, selected: int) -> bool:
    if not menu.visible:
        smoke._fail("keyboard activation did not open the resource declaration menu")
        return false
    for item in range(menu.item_count + 1):
        if menu.get_focused_item() == selected: break
        await _menu_key(smoke, menu, KEY_DOWN)
    if menu.get_focused_item() != selected:
        smoke._fail("arrow traversal did not reach the chosen declaration")
        return false
    await _menu_key(smoke, menu, KEY_ENTER)
    return true

static func _menu_key(smoke, menu: PopupMenu, code: Key) -> void:
    for pressed in [true, false]:
        if not is_instance_valid(menu) or not menu.visible: return
        var event := InputEventKey.new()
        event.keycode = code
        event.pressed = pressed
        Input.parse_input_event(event)
        await smoke.process_frame
