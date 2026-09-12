extends "res://smoke/local_game_smoke_card_summary.gd"

func _card_inspector_is_safe(hand_card: Control) -> bool:
	if not await _action_card_preview_is_safe(hand_card):
		return false
	var inspector := await _open_card_inspector(hand_card)
	if inspector == null:
		return false
	if not _inspector_frame_is_safe(inspector):
		return false
	if not await _capture_checkpoint("card-inspector"):
		return false
	if not await _pinned_inspector_is_safe(inspector):
		return false
	if not await _inspector_backdrop_is_safe(inspector):
		return false
	return await _keyboard_inspector_is_safe(hand_card, inspector)


func _action_card_preview_is_safe(hand_card: Control) -> bool:
	var mulligan_action := _first_enabled_choice()
	if mulligan_action == null:
		_fail("the mulligan decision has no selectable action")
		return false
	mulligan_action.pressed.emit()
	await process_frame
	var action_card: Button = null
	for candidate in main.find_children("Target*", "Button", true, false):
		if "Avengers Mansion" in (candidate as Button).text:
			action_card = candidate as Button
			break
	if action_card == null:
		_fail("the mulligan action has no card-naming option to preview")
		return false
	action_card.mouse_entered.emit()
	await process_frame
	var inspector := main.get_node("CardInspector") as Control
	if inspector == null or not inspector.visible:
		_fail("hovering a card-naming action did not preview its hand card")
		return false
	var frame := inspector.get_node("Frame") as PanelContainer
	if frame.get_global_rect().end.y > hand_card.get_global_rect().position.y + 1.0:
		_fail("the action-card preview was not placed above the hand card")
		return false
	action_card.mouse_exited.emit()
	await main.get_tree().create_timer(0.35).timeout
	if inspector.visible:
		_fail("the temporary action-card preview remained after hover ended")
		return false
	return true


func _open_card_inspector(hand_card: Control) -> Control:
	var click := InputEventMouseButton.new()
	click.button_index = MOUSE_BUTTON_LEFT
	click.pressed = true
	hand_card.gui_input.emit(click)
	await process_frame
	var inspector := main.get_node("CardInspector") as Control
	if inspector == null or not inspector.visible:
		_fail("clicking a card did not open the card inspector")
		return null
	return inspector


func _inspector_frame_is_safe(inspector: Control) -> bool:
	var face := inspector.find_child("CardFace", true, false)
	if face == null or face.find_child("RulesText", true, false) == null:
		_fail("the card inspector did not render full authorized card data")
		return false
	var frame := inspector.get_node("Frame") as PanelContainer
	if frame.get_global_rect().intersection(
			Rect2(Vector2.ZERO, _viewport_size())).size != frame.size:
		_fail("the pointer-aware card inspector left the visible viewport")
		return false
	if face.find_child("IllustrationRegion", true, false) == null:
		_fail("the full card frame did not reserve an illustration region")
		return false
	if not _inspector_resources_are_safe(face):
		return false
	return _inspector_detail_is_safe(inspector)


func _inspector_resources_are_safe(face: Control) -> bool:
	var resources := face.find_child("ResourceIcons", true, false) as HBoxContainer
	if face.find_child("PrimaryValue", true, false) == null or resources == null:
		_fail("the player-card frame did not keep cost and resource positions")
		return false
	if resources.find_child("ResourceLabel", true, false) != null:
		_fail("the inspector printed resources retained a text label")
		return false
	if not resources.tooltip_text.is_empty():
		_fail("the inspector printed resources retained tooltip text")
		return false
	var icon := resources.find_child("InspectorResourceIconSlot0", true, false) as Label
	if icon == null or icon.text != "M" or not icon.tooltip_text.is_empty():
		_fail("the inspector printed resource is not an icon-only row")
		return false
	return _inspector_resource_icon_is_safe(icon)


func _inspector_resource_icon_is_safe(icon: Label) -> bool:
	if icon.custom_minimum_size.x != icon.custom_minimum_size.y:
		_fail("the inspector resource icon does not use a square slot")
		return false
	if icon.horizontal_alignment != HORIZONTAL_ALIGNMENT_CENTER:
		_fail("the inspector resource icon is not horizontally centered")
		return false
	if icon.vertical_alignment != VERTICAL_ALIGNMENT_CENTER:
		_fail("the inspector resource icon is not vertically centered")
		return false
	if not icon.has_theme_font_override("font"):
		_fail("the inspector resource icon does not use its pinned font")
		return false
	if icon.get_theme_font("font").resource_path != "res://assets/fonts/ChampionsIcons.runtime.tres":
		_fail("the inspector resource icon resolved an unexpected font")
		return false
	return true


func _inspector_detail_is_safe(inspector: Control) -> bool:
	var scroll := inspector.get_node("Frame/Stack/Scroll") as ScrollContainer
	if scroll.horizontal_scroll_mode != ScrollContainer.SCROLL_MODE_SHOW_NEVER:
		_fail("the card inspector exposed horizontal scrollbar chrome")
		return false
	if scroll.vertical_scroll_mode != ScrollContainer.SCROLL_MODE_SHOW_NEVER:
		_fail("the card inspector exposed vertical scrollbar chrome")
		return false
	var detail := scroll.get_node("Content").get_child(0) as Control
	# Font metrics can place the themed border on a fractional pixel across renderers.
	if not scroll.get_global_rect().grow(1.0).encloses(detail.get_global_rect()):
		_fail("the full card does not fit inside the scrollbar-free inspector")
		return false
	var rules := detail.find_child("RulesText", true, false) as RichTextLabel
	if rules != null and rules.get_content_height() > rules.size.y:
		_fail("the inspected card clips its rules text without a scrollbar")
		return false
	var kind := detail.find_child("Kind", true, false) as Label
	if kind == null or kind.text in ["Player card", "Identity", "Enemy", "Scheme", "Environment"]:
		_fail("the inspected player card did not prioritize its printed card type")
		return false
	return _inspector_copy_is_safe(inspector, detail)


func _inspector_copy_is_safe(inspector: Control, detail: Control) -> bool:
	var detail_text := _visible_text(detail)
	if "Web-Shooter" in detail_text and detail_text.count("Uses (3 web counters)") != 1:
		_fail("the inspected player card duplicated its Uses text")
		return false
	if (inspector.get_node("Frame/Stack/Header") as Control).visible:
		_fail("the card inspector exposed a redundant modal header")
		return false
	return true


func _pinned_inspector_is_safe(inspector: Control) -> bool:
	var hand_card := _node("Play/Board/HandShelf").find_child(
		"ProceduralCard", true, false) as Control
	hand_card.mouse_exited.emit()
	await main.get_tree().create_timer(0.35).timeout
	if not inspector.visible:
		_fail("the clicked card inspector did not remain pinned")
		return false
	inspector.mouse_entered.emit()
	await main.get_tree().create_timer(0.4).timeout
	if not inspector.visible:
		_fail("the card inspector closed while the pointer was over its scrollable content")
		return false
	return true


func _inspector_backdrop_is_safe(inspector: Control) -> bool:
	var decision_before := _visible_text(_decision())
	var background_action := _button_named("Keep hand")
	var click := InputEventMouseButton.new()
	click.button_index = MOUSE_BUTTON_LEFT
	click.pressed = true
	click.position = background_action.get_global_rect().get_center()
	render_viewport.push_input(click)
	await process_frame
	if inspector.visible:
		_fail("clicking outside the inspected card did not close the inspector")
		return false
	if _visible_text(_decision()) != decision_before:
		_fail("the inspector backdrop click activated its underlying control")
		return false
	return true


func _keyboard_inspector_is_safe(hand_card: Control, inspector: Control) -> bool:
	var restore_focus := _first_enabled_choice()
	if restore_focus == null:
		return true
	restore_focus.grab_focus()
	await process_frame
	hand_card.grab_focus()
	if not await _open_inspector_with_keyboard(hand_card, inspector):
		return false
	if not await _inspector_traps_tab(inspector):
		return false
	if not await _close_inspector_with_keyboard(hand_card, inspector):
		return false
	if not await _capture_named_card("Web-Shooter", "card-inspector-long-text"):
		return false
	if not await _capture_named_card("The Break-In!", "card-inspector-main-scheme"):
		return false
	if not await _capture_named_card("Peter Parker", "card-inspector-identity-current"):
		return false
	restore_focus.grab_focus()
	await process_frame
	return true


func _open_inspector_with_keyboard(hand_card: Control, inspector: Control) -> bool:
	var enter := InputEventKey.new()
	enter.keycode = KEY_ENTER
	enter.pressed = true
	hand_card.gui_input.emit(enter)
	await process_frame
	if not inspector.visible:
		_fail("keyboard activation did not open the card inspector")
		return false
	var detail := inspector.get_node("Frame/Stack/Scroll/Content").get_child(0) as Control
	if render_viewport.gui_get_focus_owner() != detail:
		_fail("the opened card inspector did not move focus to its card")
		return false
	return true


func _inspector_traps_tab(inspector: Control) -> bool:
	var detail := inspector.get_node("Frame/Stack/Scroll/Content").get_child(0) as Control
	var tab := InputEventKey.new()
	tab.keycode = KEY_TAB
	tab.pressed = true
	render_viewport.push_input(tab)
	await process_frame
	if render_viewport.gui_get_focus_owner() != detail:
		_fail("Tab escaped the pinned card inspector")
		return false
	tab.echo = true
	render_viewport.push_input(tab)
	await process_frame
	if render_viewport.gui_get_focus_owner() != detail:
		_fail("a repeated Tab event escaped the pinned card inspector")
		return false
	return true


func _close_inspector_with_keyboard(hand_card: Control, inspector: Control) -> bool:
	var escape := InputEventAction.new()
	escape.action = "ui_cancel"
	escape.pressed = true
	render_viewport.push_input(escape)
	await process_frame
	if inspector.visible:
		_fail("Escape did not close the card inspector")
		return false
	if render_viewport.gui_get_focus_owner() != hand_card:
		_fail("closing the card inspector did not restore card focus")
		return false
	return true


func _capture_named_card(title: String, checkpoint: String) -> bool:
	var card: Control = null
	for candidate in main.find_children("*", "", true, false):
		var face := candidate.find_child("CardFace", false, false)
		if face == null:
			continue
		var title_label := face.find_child("Title", false, false) as Label
		if title_label != null and title_label.text == title:
			card = candidate as Control
			break
	if card == null:
		_fail("the table has no readable '%s' card for visual inspection" % title)
		return false
	var click := InputEventMouseButton.new()
	click.button_index = MOUSE_BUTTON_LEFT
	click.pressed = true
	card.gui_input.emit(click)
	await process_frame
	var inspector := main.get_node("CardInspector") as Control
	if not inspector.visible:
		_fail("'%s' did not open for visual inspection" % title)
		return false
	if not await _capture_checkpoint(checkpoint):
		return false
	var close := inspector.get_node("Frame/Stack/Header/Close") as Button
	close.pressed.emit()
	await process_frame
	return true


func _prepare_art_pack() -> void:
	var root_path := ProjectSettings.globalize_path("user://smoke-art-pack")
	DirAccess.make_dir_recursive_absolute(root_path)
	var illustration := Image.create(4, 4, false, Image.FORMAT_RGBA8)
	illustration.fill(Color(0.2, 0.55, 0.75, 1.0))
	illustration.save_png(root_path.path_join("peter-parker.png"))
	var invalid := FileAccess.open(root_path.path_join("rhino.png"), FileAccess.WRITE)
	invalid.store_string("not an image")
	invalid.close()
	var manifest := FileAccess.open(root_path.path_join("manifest.json"), FileAccess.WRITE)
	manifest.store_string(JSON.stringify({
		"version": 1,
		"entries": {
			"01001b": {
				"file": "peter-parker.png",
				"authorized": true,
				"rights": "Generated by the native smoke test for local verification."
			},
			"01094": {
				"file": "rhino.png",
				"authorized": true,
				"rights": "Invalid fixture generated by the native smoke test."
			}
		}
	}))
	manifest.close()
	OS.set_environment("MARVEL_ART_PACK", root_path)
