extends SceneTree

var main: Control
var stage := 0

func _initialize() -> void:
	if OS.get_environment("MARVEL_DECK_LIBRARY_DIRECTORY").is_empty():
		OS.set_environment("MARVEL_DECK_LIBRARY_DIRECTORY", OS.get_cache_dir().path_join("marvel-decks-probe-" + str(Time.get_ticks_usec())))
	root.size = Vector2i(1920, 1080)
	root.gui_embed_subwindows = true
	_run.call_deferred()

func _run() -> void:
	main = load("res://Main.tscn").instantiate()
	root.add_child(main)
	root.size = Vector2i(1920, 1080)
	if not await _wait(func(): return _button(root, "Deck library · browse, build, import") != null): return
	await _activate(_button(root, "Deck library · browse, build, import"))
	if not await _wait(func(): return _button(root, "Use deck") != null and not _button(root, "Use deck").disabled): return
	var library := main.get_node("DeckLibrary") as Window
	await process_frame
	await process_frame
	await _capture("initial")
	if not _check_workbench(library): return
	if not await _check_library_discovery(library): return
	if not await _check_bulk_edits(library): return
	await _check_save_and_use(library)

func _check_save_and_use(library: Window) -> void:
	await _menu_action(library, "DeckActions", "Clear deck")
	if not await _wait(func(): return _button(library, "Use deck").disabled): return
	if not await _wait(func(): return _text_contains(library, "40 through 50")): return
	if not await _wait(func(): return _button(library, "1 issue…") != null): return
	await _capture("invalid")
	if not _check_workbench(library): return
	await _menu_action(library, "DeckActions", "Save draft")
	if not await _wait(func(): return _text_contains(library, "Draft saved locally")): return
	if not await _check_secondary_tasks(library): return
	await _menu_action(library, "DeckActions", "New starter")
	if not await _wait(func(): return not _button(library, "Use deck").disabled): return
	await _check_filter_and_commit(library)

func _check_filter_and_commit(library: Window) -> void:
	await _activate(library.find_child("DeckFilters", true, false))
	await _capture("filters")
	if not _check_workbench(library): return
	var filters := _button(library, "Add filter").get_window()
	if not _inside_window(_button(library, "Add filter")): return
	await _escape(filters)
	await _activate(_button(library, "Use deck"))
	if not await _wait(func(): return _text_contains(library, "Deck captured for seat 1")): return
	await _start_captured_game(library)

func _check_secondary_tasks(library: Window) -> bool:
	await _menu_action(library, "SavedDecks", "Open saved")
	await _menu_action(library, "SavedDecks", "Delete saved")
	if _button(library, "Delete saved deck") == null:
		_fail("saved deck deletion did not hand off to confirmation")
		return false
	await _activate(_button(library, "Cancel"))
	await _menu_action(library, "DeckActions", "Import…")
	var url := library.find_child("DeckImportUrl", true, false) as LineEdit
	url.text = "file:///unavailable.json"
	await _activate(_button(library, "Import URL"))
	if not await _wait(func(): return _text_contains(url.get_window(), "Import failed:")): return false
	await _capture("import-error")
	if not _inside_window(_button(library, "Cancel import")): return false
	await _escape(url.get_window())
	return true

func _escape(window: Window) -> void:
	var event := InputEventKey.new()
	event.keycode = KEY_ESCAPE
	event.pressed = true
	window.push_input(event)
	await process_frame
	event.pressed = false
	window.push_input(event)
	await process_frame

func _inside_window(control: Control) -> bool:
	var window := control.get_window()
	if not Rect2(Vector2.ZERO, Vector2(window.size)).encloses(control.get_global_rect()):
		_fail("dialog control clipped outside its window")
		return false
	if not Rect2(Vector2.ZERO, Vector2(root.size)).encloses(Rect2(Vector2(window.position), Vector2(window.size))):
		_fail("dialog clipped outside the application")
		return false
	return true

func _check_library_discovery(library: Window) -> bool:
	var draft := library.find_child("DraftCards", true, false) as ScrollContainer
	if draft == null or draft.get_node("Rows").get_child_count() == 0:
		_fail("starter deck was not visible")
		return false
	var identity := library.find_child("DeckIdentity", true, false) as OptionButton
	if identity == null or not _text_contains(library, "Identity"):
		_fail("identity selection was not labelled")
		return false
	if not await _prepare_spider_man_fixture(library, identity): return false
	var browser := library.find_child("CardBrowser", true, false) as ScrollContainer
	if browser.find_child("Card_01005", true, false) != null:
		_fail("signature cards appeared in the card pool")
		return false
	var required := draft.find_child("Card_01005", true, false)
	if required == null or required.find_child("Quantity0", true, false).visible:
		_fail("required signature row exposed removal")
		return false
	if not await _check_range_selection(library, browser): return false
	return await _check_research_paging(library, browser)

func _prepare_spider_man_fixture(library: Window, identity: OptionButton) -> bool:
	# Prepare two identities for the discovery assertions; these direct selections are fixture setup.
	var spider_man := -1
	var alternate := -1
	for index in range(1, identity.item_count):
		if identity.get_item_text(index) == "Spider-Man": spider_man = index
		elif index != identity.selected: alternate = index
	if spider_man < 0 or alternate < 0:
		_fail("identity fixture needs Spider-Man and another hero")
		return false
	identity.select(alternate)
	identity.item_selected.emit(alternate)
	if not await _wait(func(): return library.find_child("DeckCardPreview", true, false).get_child_count() > 1): return false
	identity.select(spider_man)
	identity.item_selected.emit(spider_man)
	if not await _wait(func(): return not _button(library, "Use deck").disabled): return false
	return true

func _check_research_paging(library: Window, browser: ScrollContainer) -> bool:
	var scope := library.find_child("CardScope", true, false) as OptionButton
	var started := Time.get_ticks_msec()
	scope.select(1)
	scope.item_selected.emit(1)
	await process_frame
	var rows := browser.get_node("Rows")
	if rows.get_child_count() != 50 or _button(library, "›").disabled:
		_fail("research scope was not bounded to a navigable page")
		return false
	var first := rows.get_child(0).name
	await _activate(_button(library, "›"))
	if rows.get_child(0).name == first:
		_fail("research page did not advance")
		return false
	print("DECK_RESEARCH_PAGING_MS ", Time.get_ticks_msec() - started)
	scope.select(0)
	scope.item_selected.emit(0)
	return true

func _check_range_selection(library: Window, browser: ScrollContainer) -> bool:
	var rows := browser.get_node("Rows")
	await _click_row(library, rows.get_child(0), false, false)
	await _click_row(library, rows.get_child(2), true, false)
	await _click_row(library, rows.get_child(4), true, true)
	if not _text_contains(library, "4 pool cards selected"):
		_fail("Ctrl+Shift range did not retain the separate selected row")
		return false
	for index in range(5):
		var selected: bool = rows.get_child(index).get_theme_stylebox("panel").bg_color.r > 0.1
		if selected != (index in [0, 2, 3, 4]):
			_fail("range selection selected wrong row %d" % index)
			return false
	return _check_workbench(library)

func _click_row(library: Window, row: Control, ctrl: bool, shift: bool) -> void:
	row.grab_focus()
	await process_frame
	await process_frame
	var event := InputEventMouseButton.new()
	event.position = row.global_position + Vector2(library.position) + Vector2(12, 12)
	event.global_position = event.position
	event.button_index = MOUSE_BUTTON_LEFT
	event.ctrl_pressed = ctrl
	event.shift_pressed = shift
	event.pressed = true
	root.push_input(event, true)
	await process_frame
	event.pressed = false
	root.push_input(event, true)
	await process_frame

func _check_bulk_edits(library: Window) -> bool:
	await _menu_action(library, "DeckActions", "Clear deck")
	var search := library.find_child("CardSearch", true, false) as LineEdit
	search.text = 'Haymaker|"First Aid" e:core'
	search.text_changed.emit(search.text)
	await _menu_action(library, "PoolActions", "Select page")
	if not await _wait(func(): return not library.find_child("DeckAdd", true, false).disabled): return false
	await _activate(library.find_child("DeckAdd", true, false))
	if not await _wait(func(): return _text_contains(library, "17 cards")): return false
	await _activate(_button(library, "Undo"))
	if not await _wait(func(): return _text_contains(library, "15 cards")): return false
	await _activate(_button(library, "Redo"))
	if not await _wait(func(): return _text_contains(library, "17 cards")): return false
	if not await _check_row_quantities(library): return false
	await _menu_action(library, "DeckActions", "Clear deck")
	if not await _check_unique_row(library, search): return false
	return await _check_drag(library, search)

func _check_row_quantities(library: Window) -> bool:
	var pool := library.find_child("CardBrowser", true, false)
	var row := pool.find_child("Card_01086", true, false) as Control
	var three := row.find_child("Quantity3", true, false) as Button
	if not await _wait(func(): return three.visible): return false
	var row_id := row.get_instance_id()
	var button_id := three.get_instance_id()
	await _activate(three)
	if not await _wait(func(): return _text_contains(library, "19 cards")): return false
	for frame in range(10):
		await process_frame
		if not is_instance_valid(row) or not is_instance_valid(three):
			_fail("quantity edit destroyed the active row")
			return false
		if row.get_instance_id() != row_id or three.get_instance_id() != button_id or not three.visible:
			_fail("quantity controls flickered or changed identity")
			return false
	await _activate(row.find_child("Quantity0", true, false))
	if not await _wait(func(): return _text_contains(library, "16 cards")): return false
	return true

func _check_unique_row(library: Window, search: LineEdit) -> bool:
	search.text = '"Nick Fury"'
	search.text_changed.emit(search.text)
	var row := library.find_child("CardBrowser", true, false).find_child("Card_01084", true, false)
	if not await _wait(func(): return row.find_child("Quantity1", true, false).visible): return false
	if row.find_child("Quantity2", true, false).visible or row.find_child("Quantity3", true, false).visible:
		_fail("unique card exposed excess quantities")
		return false
	await _activate(row.find_child("Quantity1", true, false))
	if not await _wait(func(): return _text_contains(library, "16 cards")): return false
	await _activate(row.find_child("Quantity0", true, false))
	return await _wait(func(): return _text_contains(library, "15 cards"))

func _check_drag(library: Window, search: LineEdit) -> bool:
	search.text = 'Haymaker|"First Aid" e:core'
	search.text_changed.emit(search.text)
	await _menu_action(library, "PoolActions", "Select page")
	var browser := library.find_child("CardBrowser", true, false) as ScrollContainer
	var deck := library.find_child("DraftCards", true, false) as ScrollContainer
	if not await _drag_to(library, browser): return false
	if not _text_contains(library, "15 cards"):
		_fail("invalid drop changed the deck")
		return false
	if not await _drag_to(library, deck): return false
	return await _wait(func(): return _text_contains(library, "17 cards"))

func _drag_to(library: Window, destination: Control) -> bool:
	var browser := library.find_child("CardBrowser", true, false).get_node("Rows").get_child(0) as Control
	browser.grab_focus()
	await process_frame
	var origin := browser.global_position + Vector2(library.position) + Vector2(60, 12)
	var target := destination.global_position + Vector2(library.position) + Vector2(60, 40)
	_mouse(origin, true)
	await process_frame
	var motion := InputEventMouseMotion.new()
	motion.position = origin + Vector2(30, 0)
	motion.global_position = motion.position
	motion.relative = Vector2(30, 0)
	motion.button_mask = MOUSE_BUTTON_MASK_LEFT
	root.push_input(motion, true)
	await process_frame
	target = destination.global_position + Vector2(library.position) + Vector2(60, 40)
	motion.position = target
	motion.global_position = motion.position
	motion.relative = target - origin
	root.push_input(motion, true)
	await process_frame
	if not library.gui_is_dragging():
		_fail("native result drag did not start")
		return false
	_mouse(target, false)
	await process_frame
	await process_frame
	return true

func _mouse(position: Vector2, pressed: bool) -> void:
	var event := InputEventMouseButton.new()
	event.position = position
	event.global_position = position
	event.button_index = MOUSE_BUTTON_LEFT
	event.pressed = pressed
	root.push_input(event, true)

func _start_captured_game(library: Window) -> void:
	var screenshot := OS.get_environment("MARVEL_DECK_SCREENSHOT")
	if not screenshot.is_empty() and DisplayServer.get_name() != "headless":
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png(screenshot)
	await _activate(_button(library, "Back to setup"))
	if not await _wait(func(): return main.get_node_or_null("DeckLibrary") == null): return
	var seed := main.get_node("Margin/Shell/Content/Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "7"
	seed.text_changed.emit(seed.text)
	await _activate(_button(root, "Start game"))
	if not await _wait(func(): return _button(root, "Keep hand") != null): return
	print("DECK_LIBRARY_SMOKE_OK keyboard_edit=true invalid_draft_saved=true recipe_started=true")
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)

func _check_workbench(library: Window) -> bool:
	var pool := library.find_child("CardBrowser", true, false) as Control
	var deck := library.find_child("DraftCards", true, false) as Control
	print("DECK_GEOMETRY ", library.size, " pool=", pool.size, " deck=", deck.size)
	if pool.size.y < library.size.y * 0.4 or deck.size.y < library.size.y * 0.4:
		_fail("card work areas lost vertical room to editor chrome")
		return false
	if not _inside_window(_button(library, "Use deck")): return false
	var origin := -1.0
	for row in deck.get_node("Rows").get_children():
		var column := row.find_child("QuantityColumn", true, false) as Control
		var choices := row.find_child("QuantityChoices", true, false) as Control
		if origin < 0: origin = column.global_position.x
		if abs(column.global_position.x - origin) > 1 or abs(choices.global_position.x - origin) > 1:
			_fail("quantity columns or left-aligned choices do not share one origin")
			return false
		var zero := row.find_child("Quantity0", true, false) as Button
		if zero.visible and (zero.size.y > 35 or zero.size.x > 35):
			_fail("quantity controls exceed compact target size")
			return false
	return true

func _capture(label: String) -> void:
	var path := OS.get_environment("MARVEL_DECK_SCREENSHOT")
	if path.is_empty() or DisplayServer.get_name() == "headless": return
	await RenderingServer.frame_post_draw
	root.get_texture().get_image().save_png(path.get_basename() + "-" + label + ".png")

func _menu_action(library: Window, trigger: String, action: String) -> void:
	print("DECK_MENU ", trigger, " / ", action)
	await _activate(library.find_child(trigger, true, false))
	if action == "Save draft": await _capture("commands")
	await _activate(_button(library, action))

func _button(node: Node, text: String) -> Button:
	if node is Window and not node.visible: return null
	if node is Button and node.text == text and node.is_visible_in_tree(): return node
	for child in node.get_children(true):
		var found := _button(child, text)
		if found != null: return found
	return null

func _text_contains(node: Node, text: String) -> bool:
	if (node is Label or node is TextEdit) and text in node.text: return true
	for child in node.get_children(true):
		if _text_contains(child, text): return true
	return false

func _activate(button: Button) -> void:
	button.grab_focus()
	await process_frame
	var event := InputEventKey.new()
	event.keycode = KEY_ENTER
	event.pressed = true
	button.get_viewport().push_input(event)
	await process_frame
	event.pressed = false
	if is_instance_valid(button): root.get_last_exclusive_window().push_input(event)
	await process_frame

func _wait(condition: Callable) -> bool:
	stage += 1
	print("DECK_LIBRARY_CHECKPOINT ", stage)
	for attempt in range(600):
		if condition.call(): return true
		await create_timer(0.025).timeout
	_fail("deck library condition timed out at stage %d" % stage)
	return false

func _fail(message: String) -> void:
	_dump(root)
	push_error(message)
	quit(1)

func _dump(node: Node) -> void:
	if node is Button: print("BUTTON ", node.text, " visible=", node.is_visible_in_tree())
	if node is Label: print("LABEL ", node.text)
	if node is Window: print("WINDOW ", node.title, " visible=", node.visible)
	for child in node.get_children(true): _dump(child)
