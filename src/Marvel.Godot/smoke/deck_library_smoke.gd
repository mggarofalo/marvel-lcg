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
	if not await _wait(func(): return _button(root, "Deck library · browse, build, import") != null): return
	await _activate(_button(root, "Deck library · browse, build, import"))
	if not await _wait(func(): return _button(root, "Use for seat 1") != null and not _button(root, "Use for seat 1").disabled): return
	var library := main.get_node("DeckLibrary") as Window
	if not await _check_library_discovery(library): return
	if not await _check_bulk_edits(library): return
	await _activate(_button(library, "Clear deck"))
	if not await _wait(func(): return _button(library, "Use for seat 1").disabled): return
	if not await _wait(func(): return _text_contains(library, "40 through 50")): return
	await _activate(_button(library, "Save draft"))
	if not await _wait(func(): return _text_contains(library, "Draft saved locally")): return
	await _activate(_button(library, "New starter"))
	if not await _wait(func(): return not _button(library, "Use for seat 1").disabled): return
	await _activate(_button(library, "Use for seat 1"))
	if not await _wait(func(): return _text_contains(library, "Deck captured for seat 1")): return
	await _start_captured_game(library)

func _check_library_discovery(library: Window) -> bool:
	var draft := library.find_child("DraftCards", true, false) as ItemList
	if draft == null or draft.item_count == 0:
		_fail("starter deck was not visible")
		return false
	var identity := library.find_child("DeckIdentity", true, false) as OptionButton
	if identity == null or not _text_contains(library, "1. Choose identity"):
		_fail("identity selection was not labelled")
		return false
	var original := identity.selected
	identity.select(2 if original != 2 else 1)
	identity.item_selected.emit(identity.selected)
	if not await _wait(func(): return library.find_child("DeckCardPreview", true, false).get_child_count() > 1): return false
	identity.select(original)
	identity.item_selected.emit(original)
	if not await _wait(func(): return not _button(library, "Use for seat 1").disabled): return false
	var browser := library.find_child("CardBrowser", true, false) as ItemList
	for index in range(browser.item_count):
		if "[" in browser.get_item_text(index):
			_fail("card index codes were exposed in browser labels")
			return false
	return true

func _check_bulk_edits(library: Window) -> bool:
	await _activate(_button(library, "Clear deck"))
	var search := library.find_child("CardSearch", true, false) as LineEdit
	search.text = 'Haymaker|"First Aid"'
	search.text_changed.emit(search.text)
	await _activate(_button(library, "Select all results"))
	var three := library.find_child("DeckQuantity3", true, false) as Button
	if not await _wait(func(): return not three.disabled): return false
	await _activate(three)
	if not await _wait(func(): return _text_contains(library, "21 cards in draft")): return false
	await _activate(_button(library, "Undo"))
	if not await _wait(func(): return _text_contains(library, "15 cards in draft")): return false
	await _activate(_button(library, "Redo"))
	if not await _wait(func(): return _text_contains(library, "21 cards in draft")): return false
	search.text = "Nick Fury"
	search.text_changed.emit(search.text)
	var one := library.find_child("DeckQuantity1", true, false) as Button
	if not await _wait(func(): return not one.disabled): return false
	if not three.disabled:
		_fail("unique card offered three copies")
		return false
	await _activate(one)
	if not await _wait(func(): return _text_contains(library, "22 cards in draft")): return false
	await _activate(_button(library, "Undo"))
	await _activate(_button(library, "Undo"))
	if not await _wait(func(): return _text_contains(library, "15 cards in draft")): return false
	return await _check_drag(library, search)

func _check_drag(library: Window, search: LineEdit) -> bool:
	search.text = 'Haymaker|"First Aid"'
	search.text_changed.emit(search.text)
	await _activate(_button(library, "Select all results"))
	if not await _wait(func(): return not _button(library, "Add selected +1").disabled): return false
	var browser := library.find_child("CardBrowser", true, false) as ItemList
	var deck := library.find_child("DraftCards", true, false) as ItemList
	if not await _drag_to(library, browser): return false
	if not _text_contains(library, "15 cards in draft"):
		_fail("invalid drop changed the deck")
		return false
	if not await _drag_to(library, deck): return false
	return await _wait(func(): return _text_contains(library, "17 cards in draft"))

func _drag_to(library: Window, destination: Control) -> bool:
	var browser := library.find_child("CardBrowser", true, false) as ItemList
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

func _button(node: Node, text: String) -> Button:
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
	if is_instance_valid(button): button.get_viewport().push_input(event)
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
