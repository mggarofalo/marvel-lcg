extends RefCounted

const INSPECTION = preload("res://smoke/search_inspection_checks.gd")
const CANDIDATES := [13, 31, 16, 17, 38, 11]

static func perform(driver: SceneTree) -> bool:
	var before: String = driver._node("Toolbar/SyncStatus").text
	var original_size: Vector2i = driver.render_viewport.size
	if not await _inspect_empty_selection(driver): return false
	if not await _narrow_gallery(driver): return false
	if not await _exclude_duplicate(driver): return false
	if not await _release_duplicate(driver): return false
	if not await _inspect_two_selected(driver): return false
	if not await _reopen_compact_views(driver): return false
	if not await _restore_desktop_and_reopen(driver, original_size): return false
	if driver._node("Toolbar/SyncStatus").text != before:
		driver._fail("gallery staging, paging, inspection or reopening submitted an answer")
		return false
	return await driver._capture_checkpoint("ancestral-gallery-selected")


static func _inspect_empty_selection(driver: SceneTree) -> bool:
	if not _fits(driver): return false
	if not _selected(driver, 0): return false
	var explanation: String = driver._visible_text(driver._search_frame())
	if "different titles" not in explanation or "Ancestral Knowledge" not in explanation:
		driver._fail("gallery lost the source or the different-title instruction")
		return false
	var cards := _cards(driver)
	if not INSPECTION._selection_contrast(driver, cards): return false
	if not await INSPECTION._inspect(driver, cards[0], true): return false
	if not _selected(driver, 0): return false
	print("DISCARD_CHECKPOINT inspected unselected")
	return true


static func _narrow_gallery(driver: SceneTree) -> bool:
	driver.render_viewport.size = Vector2i(1280, 900)
	for frame in range(6): await driver.process_frame
	if driver.main.find_child("NextTargetPage", true, false) == null:
		driver._fail("narrow target gallery did not offer pages")
		return false
	if not await _all_pages(driver): return false
	print("DISCARD_CHECKPOINT all pages reached")
	return true


static func _exclude_duplicate(driver: SceneTree) -> bool:
	if not await _page_to(driver, 16): return false
	if not await driver._keyboard_activate(_toggle(driver, 16)): return false
	await driver.process_frame
	if not _selected(driver, 1): return false
	if not await _page_to(driver, 17): return false
	if not _toggle(driver, 17).disabled:
		driver._fail("the second physical Vibranium remains selectable with the first selected")
		return false
	print("DISCARD_CHECKPOINT duplicate disabled")
	if not await INSPECTION._inspect(driver, _face(driver, 17), false): return false
	if not _selected(driver, 1): return false
	print("DISCARD_CHECKPOINT inspected excluded")
	return true


static func _release_duplicate(driver: SceneTree) -> bool:
	# Removing the selected physical copy releases its excluded peer; no submit occurs.
	if not await _page_to(driver, 16): return false
	if not await driver._pointer_activate(_toggle(driver, 16)): return false
	await driver.process_frame
	if not _selected(driver, 0): return false
	if not await _page_to(driver, 17): return false
	if _toggle(driver, 17).disabled:
		driver._fail("unselecting a title did not release the other physical copy")
		return false
	if not await _page_to(driver, 16): return false
	if not await driver._pointer_activate(_toggle(driver, 16)): return false
	print("DISCARD_CHECKPOINT toggle restored")
	return true


static func _inspect_two_selected(driver: SceneTree) -> bool:
	if not await _page_to(driver, 31): return false
	if not await driver._keyboard_activate(_toggle(driver, 31)): return false
	await driver.process_frame
	if not _selected(driver, 2): return false
	if not await INSPECTION._inspect(driver, _face(driver, 31), true): return false
	if not await _all_pages(driver): return false
	if not _selected(driver, 2): return false
	return true


static func _reopen_compact_views(driver: SceneTree) -> bool:
	# Reopen before desktop rendering can replace a stale complete-choice entry.
	for size in [Vector2i(1280, 900), Vector2i(1060, 1080)]:
		driver.render_viewport.size = size
		for frame in range(6): await driver.process_frame
		if not _fits(driver) or not _selected(driver, 2): return false
		if not await _dismiss_and_reopen(driver): return false
		if not _selected(driver, 2): return false
		print("DISCARD_CHECKPOINT compact dismiss and reopen %s" % size)
	return true


static func _restore_desktop_and_reopen(driver: SceneTree, original_size: Vector2i) -> bool:
	driver.render_viewport.size = original_size
	for frame in range(6): await driver.process_frame
	if not _fits(driver) or not _selected(driver, 2): return false
	if not await _dismiss_and_reopen(driver): return false
	if not _selected(driver, 2): return false
	return true


static func _all_pages(driver: SceneTree) -> bool:
	if not await _first_page(driver): return false
	var reached: Array[int] = []
	for page in range(6):
		if not _fits(driver): return false
		for face in _cards(driver):
			var id := int(str(face.name).trim_prefix("VisibleTargetCard"))
			if reached.has(id):
				driver._fail("target pages repeat the same physical card")
				return false
			reached.append(id)
		var next := driver.main.find_child("NextTargetPage", true, false) as Button
		if next == null or next.disabled: break
		if not await driver._keyboard_activate(next): return false
		await driver.process_frame
	if reached != CANDIDATES:
		driver._fail("target pages do not expose all six authorized physical cards: %s" % reached)
		return false
	return true


static func _page_to(driver: SceneTree, id: int) -> bool:
	if not await _first_page(driver): return false
	for page in range(6):
		if _face(driver, id) != null: return true
		var next := driver.main.find_child("NextTargetPage", true, false) as Button
		if next == null or next.disabled: break
		if not await driver._keyboard_activate(next): return false
		await driver.process_frame
	driver._fail("target %d is unreachable through gallery pages" % id)
	return false


static func _first_page(driver: SceneTree) -> bool:
	for page in range(6):
		var previous := driver.main.find_child("PreviousTargetPage", true, false) as Button
		if previous == null or previous.disabled: return true
		if not await driver._keyboard_activate(previous): return false
		await driver.process_frame
	driver._fail("target gallery cannot return to its first page")
	return false


static func _dismiss_and_reopen(driver: SceneTree) -> bool:
	var close := driver._button_named("Return to table") as Button
	if close == null or not await driver._pointer_activate(close): return false
	if not await driver._wait_for(func() -> bool: return driver._search_frame() == null): return false
	var reopen := driver.main.find_child("CompleteChoiceSheet", true, false) as Button
	if not await _restored_entry_focus(driver, reopen): return false
	if not await driver._keyboard_activate(reopen): return false
	return await driver._wait_for(func() -> bool: return driver._search_frame() != null)


static func _restored_entry_focus(driver: SceneTree, entry: Button) -> bool:
	if entry == null or entry.disabled or not entry.is_visible_in_tree():
		driver._fail("dismissal lost the current complete-choice entry")
		return false
	for frame in range(2): await driver.process_frame
	if entry.get_viewport().gui_get_focus_owner() != entry:
		driver._fail("dismissal did not restore focus to the current complete-choice entry")
		return false
	return true


static func _fits(driver: SceneTree) -> bool:
	var frame: Control = driver._search_frame()
	if frame == null or not driver._control_is_fully_visible(frame):
		driver._fail("target gallery exceeds the fixed viewport")
		return false
	if not frame.find_children("*", "ScrollContainer", true, false).is_empty():
		driver._fail("target gallery introduced scrolling")
		return false
	var cards := _cards(driver)
	if cards.is_empty():
		driver._fail("target gallery page has no full card faces")
		return false
	for card in cards:
		if not driver._control_is_fully_visible(card):
			driver._fail("target gallery clips a full card face")
			return false
	return true


static func _selected(driver: SceneTree, count: int) -> bool:
	var label := driver.main.find_child("VisibleTargetSelection", true, false) as Label
	var prefix := "No cards selected." if count == 0 else "%d selected:" % count
	if label == null or not label.text.begins_with(prefix):
		driver._fail("target gallery lost its explicit selection count %d" % count)
		return false
	var commit := driver._button_named(driver.SHUFFLE) as Button
	if count > 0 and (commit == null or commit.disabled):
		driver._fail("staged target cards lost the named explicit commitment")
		return false
	if count == 0 and commit != null and not commit.disabled:
		driver._fail("target gallery can commit without explicit card selection")
		return false
	return true


static func _cards(driver: SceneTree) -> Array[Node]:
	var cards: Array[Node] = []
	for candidate in driver._search_frame().find_children("VisibleTargetCard*", "Control", true, false):
		if str(candidate.name).trim_prefix("VisibleTargetCard").is_valid_int(): cards.append(candidate)
	return cards


static func _face(driver: SceneTree, id: int) -> Control:
	return driver._search_frame().find_child("VisibleTargetCard%d" % id, true, false) as Control


static func _toggle(driver: SceneTree, id: int) -> Button:
	return driver._search_frame().find_child("Target%d" % id, true, false) as Button
