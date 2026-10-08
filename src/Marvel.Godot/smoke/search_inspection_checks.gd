extends RefCounted

static func perform(driver: SceneTree) -> bool:
	var cards: Array[Node] = driver._search_frame().find_children("SearchResult*", "Control", true, false)
	if cards.size() < 2:
		driver._fail("search comparison needs two authorized candidates")
		return false
	if not _selection_contrast(driver, cards): return false
	for index in range(2):
		if not await _inspect(driver, cards[index], index == 0): return false
	return true

static func _inspect(driver: SceneTree, card: Control, pointer: bool) -> bool:
	var before: String = driver._logical_prompt_identity()
	var revision := (driver._node("Toolbar/SyncStatus") as Label).text
	if pointer:
		if not await driver._pointer_activate_card_body(card): return false
	elif not await driver._keyboard_activate(card): return false
	for frame in range(3): await driver.process_frame
	var popup := driver.root.find_child("PileInspector", true, false) as PopupPanel
	if popup == null or not popup.visible:
		driver._fail("candidate inspection did not open above the choice sheet")
		return false
	var lifetime: WeakRef = weakref(popup)
	if not _read_only(driver, popup): return false
	if not await _compare(driver, popup): return false
	await _dismiss(driver, popup, pointer)
	if not await driver._wait_for(func() -> bool: return lifetime.get_ref() == null):
		driver._fail("candidate inspection did not dismiss")
		return false
	return _unchanged(driver, card, before, revision)

static func _unchanged(driver: SceneTree, card: Control, before: String, revision: String) -> bool:
	if not card.has_focus() or driver._search_frame() == null:
		driver._fail("inspection did not restore the same candidate and choice page")
		return false
	if driver._logical_prompt_identity() != before \
			or (driver._node("Toolbar/SyncStatus") as Label).text != revision:
		driver._fail("inspecting a candidate changed selection, draft, or revision")
		return false
	return true

static func _read_only(driver: SceneTree, popup: PopupPanel) -> bool:
	var rules := popup.find_child("RulesText", true, false) as RichTextLabel
	if rules == null or rules.get_content_height() > rules.size.y + 1:
		driver._fail("candidate inspection clips its complete rules")
		return false
	if not Rect2(Vector2.ZERO, driver._viewport_size()).encloses(Rect2(Vector2(popup.position), Vector2(popup.size))):
		driver._fail("candidate inspection exceeds its viewport")
		return false
	if not popup.find_children("Card*Action", "Button", true, false).is_empty():
		driver._fail("candidate inspection acquired a game action")
		return false
	return true

static func _compare(driver: SceneTree, popup: PopupPanel) -> bool:
	var count := popup.find_child("PileInspectorPosition", true, false) as Label
	var before := count.text
	var next := popup.find_child("NextPileCard", true, false) as Button
	if next.disabled: return true
	next.grab_focus()
	await _key(driver, popup, KEY_ENTER)
	for frame in range(3): await driver.process_frame
	if count.text == before:
		driver._fail("candidate comparison did not advance to the next authorized card")
		return false
	return _read_only(driver, popup)

static func _dismiss(driver: SceneTree, popup: PopupPanel, pointer: bool) -> void:
	if not pointer:
		await _key(driver, popup, KEY_ESCAPE)
		return
	var close := popup.find_child("ClosePileInspector", true, false) as Button
	var point := Vector2(popup.position) + close.get_global_rect().get_center()
	var motion := InputEventMouseMotion.new()
	motion.position = point
	Input.parse_input_event(motion)
	await driver.process_frame
	for pressed in [true, false]:
		var click := InputEventMouseButton.new()
		click.position = point
		click.button_index = MOUSE_BUTTON_LEFT
		click.pressed = pressed
		Input.parse_input_event(click)
		await driver.process_frame

static func _key(driver: SceneTree, popup: PopupPanel, code: int) -> void:
	var lifetime: WeakRef = weakref(popup)
	for pressed in [true, false]:
		if lifetime.get_ref() == null: return
		var key := InputEventKey.new()
		key.keycode = code
		key.pressed = pressed
		popup.push_input(key, true)
		await driver.process_frame

static func replace_surface(driver: SceneTree, size: Vector2i) -> bool:
	var card := driver._search_frame().find_child("SearchResult*", true, false) as Control
	if not await driver._keyboard_activate(card): return false
	await driver.process_frame
	var popup := driver.root.find_child("PileInspector", true, false) as PopupPanel
	if popup == null:
		driver._fail("replacement probe has no candidate inspection")
		return false
	var popup_lifetime: WeakRef = weakref(popup)
	var card_lifetime: WeakRef = weakref(card)
	driver.render_viewport.size = size
	if not await driver._wait_for(func() -> bool:
		return popup_lifetime.get_ref() == null and card_lifetime.get_ref() == null):
		driver._fail("replaced choice surface left its stale inspection open")
		return false
	return true


static func _selection_contrast(driver: SceneTree, cards: Array[Node]) -> bool:
	var unselected := 0
	for card in cards:
		var selector := card.find_child("Affordance*", true, false) as Button
		if selector == null: selector = card.find_child("Target*", true, false) as Button
		if selector == null or not selector.is_visible_in_tree():
			driver._fail("candidate has no visible selection glyph")
			return false
		if selector.text != "◎": continue
		unselected += 1
		if not _normal_selector_contrast(driver, card, selector): return false
	if unselected == 0:
		driver._fail("candidate contrast probe has no unselected selection glyph")
		return false
	return true


static func _normal_selector_contrast(driver: SceneTree, card: Control, selector: Button) -> bool:
	var header := card.find_child("InkPlane", true, false) as Polygon2D
	if header == null:
		driver._fail("selection glyph has no rendered header surface")
		return false
	var surface := header.color
	var ink := selector.get_theme_color("font_color")
	ink = surface.lerp(ink, ink.a * selector.self_modulate.a)
	var light := maxf(surface.srgb_to_linear().get_luminance(), ink.srgb_to_linear().get_luminance())
	var dark := minf(surface.srgb_to_linear().get_luminance(), ink.srgb_to_linear().get_luminance())
	if (light + 0.05) / (dark + 0.05) < 4.5:
		driver._fail("unselected selection glyph has insufficient normal-state contrast against its card header")
		return false
	return true
