extends SceneTree

func _initialize() -> void:
	_run.call_deferred()

func _run() -> void:
	root.size = Vector2i(1920, 1080)
	var main := (load("res://Main.tscn") as PackedScene).instantiate()
	root.add_child(main)
	for frame in range(8): await process_frame
	var fixture := main.find_child("TableauFixture", true, false) as Control
	var tableau := main.find_child("SourceTableau", true, false) as Control
	var failures: Array[String] = []
	if fixture == null or tableau == null:
		push_error("SOURCE_TABLEAU_FAILED missing fixture")
		quit(1)
		return
	var viewport := tableau.find_child("SourceTableauViewport", true, false) as ScrollContainer
	var narrow := bool(fixture.get_meta("narrow", false))
	_check_table_bounds(fixture, tableau, narrow, failures)
	var copies := await _check_cards(tableau, viewport, narrow, failures)
	_check_host_attachment(fixture, tableau, failures)
	await _check_measured_title(tableau, viewport, failures)
	await _check_copy_input(copies, viewport, fixture, failures)
	await _check_source_slot_swaps(tableau, viewport, fixture, failures)
	await _check_collections(fixture, failures)
	await _capture()
	for failure in failures: push_error("SOURCE_TABLEAU_FAILED " + failure)
	if failures.is_empty(): print("SOURCE_TABLEAU_OK sixteen_reachable=true duplicates=true pointer_inspection=true keyboard=true")
	quit(0 if failures.is_empty() else 1)

func _check_table_bounds(fixture: Control, tableau: Control, narrow: bool, failures: Array[String]) -> void:
	if narrow and tableau.get_global_rect().end.x > fixture.global_position.x + 1320:
		failures.append("source viewport clipped by payment table")
	for hand in fixture.find_children("ProceduralCard*", "Control", true, false):
		if hand.has_meta("spatial_hand_index") and _bounds(hand).grow(6).intersects(tableau.get_global_rect()):
			failures.append("rotated hand obscures source tableau")

func _check_host_attachment(fixture: Control, tableau: Control, failures: Array[String]) -> void:
	var webbed: Control
	for card in fixture.find_children("ProceduralCard*", "Control", true, false):
		if card.get_meta("source_title", "") == "Webbed Up": webbed = card
	if webbed == null:
		failures.append("Webbed Up is absent from its Rhino host")
		return
	if tableau.is_ancestor_of(webbed): failures.append("Webbed Up appears in owner source grid")
	var host := webbed.get_parent()
	while host != null and not str(host.name).begins_with("ProceduralCard"): host = host.get_parent()
	if host == null:
		failures.append("attachment lacks physical host-local ancestry")
	else:
		var title := host.find_child("Title", true, false) as Label
		if title == null or title.text != "Rhino": failures.append("Webbed Up is displayed beneath the wrong host")
	if webbed.get_parent().find_children("*", "ScrollContainer", true, false).size() > 0:
		failures.append("host attachment creates nested scrollbars")

func _check_cards(tableau: Control, viewport: ScrollContainer, narrow: bool, failures: Array[String]) -> Array[Control]:
	viewport.scroll_vertical = 0
	for frame in range(3): await process_frame
	var cards := tableau.find_children("ProceduralCard*", "Control", true, false)
	if cards.size() != 16: failures.append("lost physical sources")
	var copies: Array[Control] = []
	if not narrow and not OS.get_cmdline_user_args().has("--marvel-tableau-symbols"):
		for card in cards.slice(0, 12):
			if not viewport.get_global_rect().encloses((card as Control).get_global_rect()):
				failures.append("dense source viewport does not expose its first twelve physical cards: " + str(card.name) + " " + str((card as Control).get_global_rect()) + " viewport=" + str(viewport.get_global_rect()))
	for card in cards:
		viewport.ensure_control_visible(card)
		for frame in range(3): await process_frame
		if not viewport.get_global_rect().encloses(card.get_global_rect()):
			failures.append("source hidden by scrolling: " + str(card.name) + " " + str(card.get_global_rect()))
		_check_overlap(card, cards, failures)
		_check_card_readability(card, failures)
		var title := card.find_child("SourceTitle", true, false) as Label
		if title.text == "Powered Gauntlets": copies.append(card)
	return copies

func _check_overlap(card: Control, cards: Array[Node], failures: Array[String]) -> void:
	for other in cards:
		if card != other and card.get_global_rect().intersects(other.get_global_rect()):
			failures.append("source tiles overlap")

func _check_card_readability(card: Control, failures: Array[String]) -> void:
	var title := card.find_child("SourceTitle", true, false) as Label
	var body := card.find_child("SourceTitleBand", true, false) as Control
	_check_source_grip(card, title, failures)
	_check_source_title(title, failures)
	_check_source_action(card, failures)
	_check_source_symbols(card, failures)
	if title.get_global_rect().position.x < body.global_position.x + 4:
		failures.append("title enters the B1 ink band inset")
	_check_excerpt(card, failures)
	if not card.get_global_rect().encloses(body.get_global_rect()):
		failures.append("B1 source body exceeds its tile")
	_check_counter_groups(card, body, failures)

func _check_source_grip(card: Control, title: Label, failures: Array[String]) -> void:
	var grip := card.find_child("SourceReorderGrip", true, false) as Button
	if grip == null or not card.get_global_rect().encloses(grip.get_global_rect()):
		failures.append("source reorder grip exceeds its physical tile")
	if grip != null and (grip.size.x < title.size.x or grip.size.y < 28 or grip.mouse_default_cursor_shape != Control.CURSOR_DRAG):
		failures.append("title does not expose a generous arrangement hit target / grab cursor")

func _check_source_title(title: Label, failures: Array[String]) -> void:
	if title.get_line_count() > title.get_visible_line_count(): failures.append("source title clipped")
	if title.get_theme_font_size("font_size") != 14 or title.autowrap_mode != TextServer.AUTOWRAP_OFF:
		failures.append("source title does not retain compact native typography")
	if title.accessibility_name != title.text or title.tooltip_text != title.text:
		failures.append("source title loses full accessible identity")

func _check_source_action(card: Control, failures: Array[String]) -> void:
	var button := card.find_child("Card*Action", true, false) as Button
	if button != null and (button.size.y < 28 or not card.get_global_rect().encloses(button.get_global_rect())):
		failures.append("source action exceeds tile")
	if card.find_child("SourceState", true, false) == null: failures.append("missing readiness")

func _check_source_symbols(card: Control, failures: Array[String]) -> void:
	var type := card.find_child("SourceType", true, false) as Control
	if type == null or type.accessibility_name.is_empty() or type.find_child("SourceSymbol", true, false) == null:
		failures.append("missing named vector card type")
	var state := card.find_child("SourceState", true, false) as Control
	if state == null or state.find_child("SourceSymbol", true, false) == null:
		failures.append("missing readiness symbol")
	var title := card.find_child("SourceTitle", true, false) as Control
	if type != null and state != null and (type.get_parent() != title.get_parent() or state.get_parent() != title.get_parent()):
		failures.append("source title and type/readiness do not share one header line")

func _check_counter_groups(card: Control, body: Control, failures: Array[String]) -> void:
	for counter in card.find_children("SourceCounter*", "HBoxContainer", true, false):
		if not counter.has_meta("counter_value"): continue
		if not body.get_global_rect().encloses(counter.get_global_rect()): failures.append("counter obscures title or action")
		if not counter.accessibility_name.contains(str(counter.get_meta("counter_name"))): failures.append("counter lost supplied semantic name")
		var count := int(counter.get_meta("pip_count"))
		var value := counter.find_child("SourceCounterValue", false, false) as Label
		_check_counter_value(value, failures)
		_check_counter_pips(counter, count, failures)

func _check_counter_value(value: Label, failures: Array[String]) -> void:
	if not value.text.is_empty() and (value.get_line_count() != 1 or value.get_theme_font("font").get_string_size(value.text, HORIZONTAL_ALIGNMENT_LEFT, -1, 10).x > value.size.x):
		failures.append("counter kind or explicit value is not readable on one line")

func _check_counter_pips(counter: Control, count: int, failures: Array[String]) -> void:
	if count == 0 and counter.find_child("SourceCounterPips", false, false) != null: failures.append("zero counter displays a filled pip")
	if count > 0 and count <= 6 and counter.find_child("SourceCounterPips", false, false) == null: failures.append("ordinary counter is not countable")
	if count < 0 and not str(counter.find_child("SourceCounterValue", false, false).text).contains(str(counter.get_meta("counter_value"))):
		failures.append("large counter loses its explicit value")

func _check_excerpt(card: Control, failures: Array[String]) -> void:
	var rules := card.find_child("SourceMeaning", true, false) as RichTextLabel
	if rules == null or not rules.is_visible_in_tree(): return
	var hint := rules.find_child("SourceRulesDisclosure", false, false) as Label
	var overflow := rules.get_content_height() > rules.size.y
	if hint.visible != overflow: failures.append("abbreviated rules lack explicit disclosure")
	if not overflow: return
	if hint.get_line_count() != 1 or not rules.get_global_rect().encloses(hint.get_global_rect()):
		failures.append("rules disclosure is clipped: " + str(card.name) + " rules=" + str(rules.get_global_rect()) + " hint=" + str(hint.get_global_rect()))
	for line in range(rules.get_line_count()):
		if rules.get_line_range(line).x >= rules.visible_characters and rules.visible_characters >= 0: break
		if rules.get_line_offset(line) + rules.get_line_height(line) > hint.position.y:
			failures.append("partial rule line overlaps disclosure")

func _check_copy_input(copies: Array[Control], viewport: ScrollContainer, fixture: Control, failures: Array[String]) -> void:
	if copies.size() != 2:
		failures.append("duplicate gauntlets coalesced")
		return
	for copy in copies:
		viewport.ensure_control_visible(copy)
		for frame in range(3): await process_frame
		var original_position := copy.global_position
		await _click((copy.find_child("SourceType", true, false) as Control).get_global_rect().get_center())
		if copy.global_position.distance_to(original_position) > 1:
			failures.append("hover inspection reordered a physical source")
		var identity := int(str(copy.name).trim_prefix("ProceduralCard"))
		if int(fixture.get_meta("inspected_id", -1)) != identity:
			failures.append("body inspection lost physical source expected " + str(identity) + " got " + str(fixture.get_meta("inspected_id", -1)))
		var action := copy.find_child("Card*Action", true, false) as Button
		action.grab_focus()
		await _activate_key()
		if int(fixture.get_meta("selected_id", -1)) != identity:
			failures.append("keyboard selected wrong physical source")

func _activate_key() -> void:
	for pressed in [true, false]:
		var key := InputEventKey.new()
		key.keycode = KEY_ENTER
		key.pressed = pressed
		Input.parse_input_event(key)
		await process_frame

func _check_source_slot_swaps(tableau: Control, viewport: ScrollContainer, fixture: Control, failures: Array[String]) -> void:
	viewport.scroll_vertical = 0
	for frame in range(3): await process_frame
	var grid := tableau.find_child("SourceTableauGrid", true, false) as GridContainer
	var original := grid.get_children()
	var source := original[0].find_child("ProceduralCard*", true, false) as Control
	var destination := original[1] as Control
	var selected_before: int = fixture.get_meta("selected_id", -1)
	var inspected_before: int = fixture.get_meta("inspected_id", -1)
	await _check_intermediate_swap(tableau, source, destination, failures)
	await _source_grip_drag(source, destination.get_global_rect().get_center(), true)
	if grid.get_children() != original:
		failures.append("cancelled source reorder changed physical slot identities")
	await _source_grip_drag(source, Vector2(8, 8), false)
	if grid.get_children() != original:
		failures.append("outside source drop changed physical slot identities")
	await _source_grip_drag(source, destination.get_global_rect().get_center(), false)
	if grid.get_child(1) != original[0] or grid.get_child(0) != original[1]:
		failures.append("source reorder did not swap the exact physical instances")
	if int(fixture.get_meta("selected_id", -1)) != selected_before or int(fixture.get_meta("inspected_id", -1)) != inspected_before:
		failures.append("source grip accidentally triggered an engine action or inspection")
	if source.is_set_as_top_level() or not source.position.is_equal_approx(Vector2.ZERO):
		failures.append("source reorder did not settle inside its destination slot")
	await _check_source_keyboard_swap(grid, source, failures)
	await _check_every_grip(tableau, viewport, fixture, failures)

func _check_measured_title(tableau: Control, viewport: ScrollContainer, failures: Array[String]) -> void:
	var card := tableau.find_child("ProceduralCard*", true, false) as Control
	var title := card.find_child("SourceTitle", true, false) as Label
	var original := title.text
	title.text = "Synthetic extended source title"
	for frame in range(6): await process_frame
	viewport.ensure_control_visible(card)
	for frame in range(3): await process_frame
	if title.get_line_count() != 1:
		failures.append("compact source title changed its header height")
	if not card.get_global_rect().encloses(title.get_global_rect()): failures.append("measured source title grows outside its tile")
	title.text = original
	for frame in range(6): await process_frame
	viewport.scroll_vertical = 0
	for frame in range(3): await process_frame

func _check_intermediate_swap(tableau: Control, source: Control, destination: Control, failures: Array[String]) -> void:
	var grip := source.find_child("SourceReorderGrip", true, false) as Button
	var start := grip.get_global_rect().position + Vector2(16, 16)
	var finish := destination.get_global_rect().get_center()
	await _pointer(start, true)
	await _motion(finish)
	var feedback := tableau.find_child("SourceSwapFeedback", true, false) as Label
	if not source.is_set_as_top_level(): failures.append("source did not lift out of its slot")
	if source.get_global_rect().has_point(finish): failures.append("lifted source obscures destination pointer")
	if feedback == null or not feedback.is_visible_in_tree() or feedback.text != "↔":
		failures.append("source exchange lacks explicit destination feedback")
	elif not feedback.accessibility_name.contains("row ") or not feedback.accessibility_name.contains("column "):
		failures.append("source exchange does not explain both positions")
	if source.get_parent().find_child("SourceSwapOutline", false, false).visible != true:
		failures.append("source exchange has no vacant destination outline")
	var capture := OS.get_environment("MARVEL_TABLEAU_CAPTURE")
	if not capture.is_empty():
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png(capture.trim_suffix(".png") + "-swap.png")
	# Window focus notification exercises the application's native capture-loss cleanup.
	tableau.find_child("SourceGridGesture", true, false).notification(Node.NOTIFICATION_WM_WINDOW_FOCUS_OUT)
	await process_frame
	await _pointer(finish, false)
	if source.is_set_as_top_level() or feedback.is_visible_in_tree(): failures.append("focus loss retained a source arrangement preview")

func _check_every_grip(tableau: Control, viewport: ScrollContainer, fixture: Control, failures: Array[String]) -> void:
	var grid := tableau.find_child("SourceTableauGrid", true, false) as GridContainer
	var original := grid.get_children()
	for slot in original:
		var card := slot.find_child("ProceduralCard*", true, false) as Control
		viewport.ensure_control_visible(card)
		for frame in range(3): await process_frame
		await _source_grip_drag(card, card.get_global_rect().get_center() + Vector2(4, 4), true)
		if grid.get_children() != original: failures.append("cancelling a displayed source grip changed order")
		if not str(card.get_meta("source_copy", "")).is_empty():
			var marker := card.find_child("SourceCopy", true, false) as Label
			var grip := card.find_child("SourceReorderGrip", true, false) as Button
			if marker == null or not grip.accessibility_name.contains(marker.text): failures.append("duplicate source lacks visible / accessible copy marker")
	viewport.scroll_vertical = 0
	for frame in range(3): await process_frame

func _pointer(point: Vector2, pressed: bool) -> void:
	var event := InputEventMouseButton.new()
	event.position = point
	event.global_position = point
	event.button_index = MOUSE_BUTTON_LEFT
	event.pressed = pressed
	Input.parse_input_event(event)
	for frame in range(3): await process_frame

func _motion(point: Vector2) -> void:
	var event := InputEventMouseMotion.new()
	event.position = point
	event.global_position = point
	Input.parse_input_event(event)
	for frame in range(3): await process_frame

func _check_source_keyboard_swap(grid: GridContainer, source: Control, failures: Array[String]) -> void:
	var original := grid.get_children()
	var grip := source.find_child("SourceReorderGrip", true, false) as Button
	grip.grab_focus()
	await _source_key(KEY_SPACE)
	var feedback := grid.get_parent().get_parent().find_child("SourceSwapFeedback", true, false) as Label
	if not feedback.is_visible_in_tree() or not feedback.accessibility_name.contains(str(source.get_meta("source_identity"))):
		failures.append("keyboard pickup does not announce physical identity")
	await _source_key(KEY_LEFT)
	if grid.get_child(0) != original[1]: failures.append("keyboard source swap has no slot preview")
	await _source_key(KEY_RIGHT)
	if grid.get_children() != original: failures.append("returning to the pickup slot retained a stale swap")
	await _source_key(KEY_LEFT)
	await _source_key(KEY_ESCAPE)
	if grid.get_children() != original: failures.append("keyboard source cancellation changed the local order")
	await _source_key(KEY_SPACE)
	await _source_key(KEY_LEFT)
	await _source_key(KEY_ENTER)
	if grid.get_child(0) != original[1]: failures.append("keyboard source placement lost the chosen physical copy")
	if not grip.has_focus(): failures.append("keyboard placement lost the source grip focus")

func _source_key(code: Key) -> void:
	for pressed in [true, false]:
		var key := InputEventKey.new()
		key.keycode = code
		key.pressed = pressed
		Input.parse_input_event(key)
		for frame in range(3): await process_frame

func _source_grip_drag(card: Control, finish: Vector2, cancel: bool) -> void:
	var grip := card.find_child("SourceReorderGrip", true, false) as Button
	var start := grip.get_global_rect().get_center()
	var press := InputEventMouseButton.new()
	press.position = start
	press.global_position = start
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	Input.parse_input_event(press)
	await process_frame
	var motion := InputEventMouseMotion.new()
	motion.position = finish
	motion.global_position = finish
	Input.parse_input_event(motion)
	for frame in range(3): await process_frame
	if cancel:
		var escape := InputEventKey.new()
		escape.keycode = KEY_ESCAPE
		escape.pressed = true
		Input.parse_input_event(escape)
		await process_frame
	var release := InputEventMouseButton.new()
	release.position = finish
	release.global_position = finish
	release.button_index = MOUSE_BUTTON_LEFT
	Input.parse_input_event(release)
	for frame in range(3): await process_frame

func _capture() -> void:
	var capture := OS.get_environment("MARVEL_TABLEAU_CAPTURE")
	if not capture.is_empty():
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png(capture)

func _click(point: Vector2) -> void:
	var motion := InputEventMouseMotion.new()
	motion.position = point
	Input.parse_input_event(motion)
	await process_frame
	for pressed in [true, false]:
		var event := InputEventMouseButton.new()
		event.position = point
		event.button_index = MOUSE_BUTTON_LEFT
		event.pressed = pressed
		Input.parse_input_event(event)
		await process_frame

func _bounds(control: Control) -> Rect2:
	var pose := control.get_global_transform()
	var bounds := Rect2(pose * Vector2.ZERO, Vector2.ZERO)
	for point in [Vector2(control.size.x, 0), control.size, Vector2(0, control.size.y)]:
		bounds = bounds.expand(pose * point)
	return bounds


func _check_collections(fixture: Control, failures: Array[String]) -> void:
	for zone in ["AlliesArea", "EngagedEnemiesArea", "SideSchemesArea"]:
		var collection := fixture.find_child("Collection" + zone, true, false) as Control
		if collection == null: continue
		if not collection.find_children("*", "MenuButton", true, false).is_empty():
			failures.append("collection uses dropdown: " + zone)
		var scroll := collection.find_child("Continuous" + zone, true, false) as ScrollContainer
		var cards := collection.find_children("ProceduralCard*", "Control", true, false)
		if zone == "SideSchemesArea": await _check_scheme_headers(cards, failures)
		if scroll == null or cards.is_empty():
			failures.append("missing continuous collection: " + zone)
			continue
		if zone == "AlliesArea":
			for card in cards.slice(0, 3):
				if not scroll.get_global_rect().encloses(_bounds(card)):
					failures.append("first three allies are not simultaneously visible")
		await _check_collection_reachability(cards, scroll, failures)

func _check_collection_reachability(cards: Array[Node], scroll: ScrollContainer, failures: Array[String]) -> void:
	for card in cards:
		if not (card as Control).scale.is_equal_approx(Vector2.ONE):
			failures.append("physical collection card uses post-render scaling: " + str(card.name))
		card.grab_focus()
		scroll.ensure_control_visible(card)
		for frame in range(3): await process_frame
		if not scroll.get_global_rect().intersects(_bounds(card)):
			failures.append("unreachable collection card: " + str(card.name))
		if not card.has_focus(): failures.append("stack keyboard focus lost " + str(card.name))
	scroll.scroll_horizontal = 0
	scroll.scroll_vertical = 0

func _check_scheme_headers(cards: Array[Node], failures: Array[String]) -> void:
	# Synthetic typography stress; the production labels must grow without hiding a preceding scheme.
	if not cards.is_empty():
		var long_title := cards[0].find_child("StackTitle", true, false) as Label
		long_title.text = "Synthetic exceptionally long scheme title with complete visible words"
		for frame in range(5): await process_frame
	for index in range(cards.size()):
		var header := cards[index].find_child("SchemeStackHeader", true, false) as Control
		if header == null:
			failures.append("scheme lacks inspectable header")
			continue
		for label in header.find_children("*", "Label", true, false):
			if label.get_line_count() > label.get_visible_line_count() or not header.get_global_rect().encloses(label.get_global_rect()):
				failures.append("scheme title or status clipped")
		if index + 1 < cards.size() and header.get_global_rect().end.y > cards[index + 1].global_position.y:
			failures.append("following scheme obscures preceding title or status")
