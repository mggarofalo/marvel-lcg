extends "res://smoke/local_game_smoke_card_checks.gd"

func _mulligan_result_and_payment_are_operable() -> bool:
	if not await _select_mulligan_cards():
		return false
	if not await _submit_mulligan():
		return false
	if not await _mulligan_result_is_operable():
		return false
	if not await _start_web_shooter_draft():
		return false
	return await _payment_is_keyboard_operable()


func _select_mulligan_cards() -> bool:
	var mulligan := _visible_button_beginning(_decision(), "Choose cards to discard and redraw")
	if mulligan == null or mulligan.disabled:
		_fail("the seeded opening hand has no operable mulligan action")
		return false
	mulligan.pressed.emit()
	await process_frame
	for title in ["Avengers Mansion", "Aunt May", "Swinging Web Kick"]:
		var target := _mulligan_target(title)
		if target == null:
			_fail("the seeded mulligan cannot select %s" % title)
			return false
		target.pressed.emit()
		await process_frame
	return true


func _mulligan_target(title: String) -> Button:
	for candidate in _visible_buttons(_decision()):
		if candidate.text.begins_with("◇ DISCARD AND REDRAW") and title in candidate.text:
			return candidate
	return null


func _submit_mulligan() -> bool:
	var submit := _submit_button()
	if submit == null or submit.disabled or "Discard 3 and redraw" not in submit.text:
		_fail("the three-card mulligan cannot be submitted")
		return false
	submit.pressed.emit()
	if not await _wait_for(func() -> bool:
		return "turn" in (_node("Play/Prompt/Margin/Stack/PromptHeader/Heading") as Label).text.to_lower()):
		_fail("the seeded mulligan did not reach the player turn")
		return false
	return true


func _mulligan_result_is_operable() -> bool:
	var result := _node("Play/Prompt/Margin/Stack/Workbench/Action/LastResult") as Control
	var summary := _node(
		"Play/Prompt/Margin/Stack/Workbench/Action/LastResult/Margin/Copy/Summary") as Label
	if not result.visible:
		_fail("the mulligan result is not visible")
		return false
	if "Spider-Man discarded Avengers Mansion, Aunt May, and Swinging Web Kick." not in summary.text:
		_fail("the mulligan result omits the discarded cards: %s" % summary.text)
		return false
	if "Spider-Man drew Daredevil, Black Cat, and Jessica Jones." not in summary.text:
		_fail("the mulligan result omits the drawn cards: %s" % summary.text)
		return false
	if not await _capture_checkpoint("mulligan-result"):
		return false
	return await _result_toggle_is_operable(summary)


func _result_toggle_is_operable(summary: Label) -> bool:
	var toggle := _node(
		"Play/Prompt/Margin/Stack/Workbench/Action/LastResult/Margin/Copy/Header/Toggle") as Button
	toggle.pressed.emit()
	await process_frame
	if summary.visible or toggle.text != "Expand":
		_fail("the transient result cannot be collapsed")
		return false
	toggle.pressed.emit()
	await process_frame
	if not summary.visible or toggle.text != "Collapse":
		_fail("the transient result cannot be expanded")
		return false
	return true


func _start_web_shooter_draft() -> bool:
	var web_shooter := _visible_button_beginning(_decision(), "Play Web-Shooter")
	if web_shooter == null or web_shooter.disabled:
		_fail("Web-Shooter is not playable after the mulligan")
		return false
	web_shooter.pressed.emit()
	await process_frame
	await process_frame
	var result := _node("Play/Prompt/Margin/Stack/Workbench/Action/LastResult") as Control
	if result.visible:
		_fail("opening a new draft did not clear the transient result")
		return false
	return true


func _payment_is_keyboard_operable() -> bool:
	var generators := _decision().find_children("Resource*", "Button", true, false)
	if generators.is_empty():
		_fail("Web-Shooter exposes no post-mulligan payment generators")
		return false
	var generator := generators[0] as Button
	generator.grab_focus()
	await process_frame
	await process_frame
	if render_viewport.gui_get_focus_owner() != generator:
		_fail("Web-Shooter's post-mulligan resource control cannot receive focus")
		return false
	if _visible_control_rect(generator).size.y < _scaled_metric(24):
		_fail("Web-Shooter's post-mulligan resource control is clipped")
		return false
	var press := InputEventAction.new()
	press.action = &"ui_accept"
	press.pressed = true
	render_viewport.push_input(press)
	await process_frame
	press.pressed = false
	render_viewport.push_input(press)
	await process_frame
	await process_frame
	var progress := _node("Play/Prompt/Margin/Stack/PromptHeader/Progress") as Label
	if "PAYMENT 1/1 ICONS" not in progress.text or "READY" not in progress.text:
		_fail("Peter Parker's post-mulligan resource cannot complete Web-Shooter's payment")
		return false
	return await _capture_checkpoint("post-mulligan-payment")


func _synchronization_preserves_history(expect_terminal: bool) -> bool:
	var synchronize := main.find_child("Synchronize", true, false) as Button
	if synchronize == null or not synchronize.visible or synchronize.disabled:
		_fail("the table has no operable always-visible synchronization control")
		return false
	if synchronize.custom_minimum_size.y < 32:
		_fail("the compact synchronization control is too small to operate")
		return false
	var event_log := _node("Play/Prompt/Margin/Stack/Workbench/History/EventLog") as RichTextLabel
	var history_before := event_log.text
	synchronize.pressed.emit()
	if not await _wait_for(func() -> bool:
		return not _status().text.begins_with("SYNCHRONIZING")):
		_fail("the explicit table synchronization did not settle")
		return false
	if history_before != event_log.text:
		_fail("synchronization replayed or cleared the visible event history")
		return false
	if expect_terminal:
		if "VILLAIN WINS" not in _status().text and "PLAYERS LOSE" not in _status().text:
			_fail("synchronizing the terminal table lost its authoritative outcome")
			return false
	elif _first_enabled_choice() == null \
			and _visible_button(_decision(), "Pass / decline") == null:
		_fail("synchronizing an ordinary prompt left the decision inoperable")
		return false
	return true


func _visual_system_is_resolved() -> bool:
	if main.theme == null:
		_fail("the root scene has no reusable client theme")
		return false
	var start := _button_named("Start game")
	if not _primary_button_theme_is_safe(start):
		return false
	if not await _scale_toolbar_is_safe(start):
		return false
	if not _interaction_styles_are_safe(start):
		return false
	if not _semantic_styles_are_safe():
		return false
	return await _setup_overflow_is_safe()


func _primary_button_theme_is_safe(start: Button) -> bool:
	if start.theme_type_variation != &"PrimaryButton":
		_fail("the primary action does not use its semantic theme role")
		return false
	if start.custom_minimum_size.y < _scaled_metric(44):
		_fail("the primary action is smaller than the pointer-target floor")
		return false
	if start.get_theme_font_size("font_size") < _scaled_metric(15):
		_fail("the primary action did not adopt the selected type scale")
		return false
	return true


func _scale_toolbar_is_safe(start: Button) -> bool:
	var slider := _node("Toolbar/InterfaceScale") as HSlider
	var value := _node("Toolbar/ScaleValue") as Label
	if slider == null or value == null:
		_fail("the density toolbar has no interface scale control")
		return false
	if slider.min_value != 50.0 or slider.max_value != 150.0 or slider.step != 10.0:
		_fail("the interface scale slider does not expose 50–150% in ten-percent steps")
		return false
	await process_frame
	await process_frame
	await main.get_tree().create_timer(0.05).timeout
	var original_scale := slider.value
	var original_font := start.get_theme_font_size("font_size")
	var original_rect := slider.get_global_rect()
	slider.value = 50.0 if original_scale != 50.0 else 60.0
	await process_frame
	await process_frame
	if start.get_theme_font_size("font_size") == original_font:
		_fail("the interface scale slider did not update the live theme")
		return false
	if not slider.get_global_rect().is_equal_approx(original_rect):
		_fail("the interface scale slider changed geometry or absolute position")
		return false
	slider.value = original_scale
	await process_frame
	if "Scale" not in value.text:
		_fail("the interface scale slider has no readable value")
		return false
	return true


func _interaction_styles_are_safe(start: Button) -> bool:
	var focus := start.get_theme_stylebox("focus") as StyleBoxFlat
	var normal := start.get_theme_stylebox("normal") as StyleBoxFlat
	var hover := start.get_theme_stylebox("hover") as StyleBoxFlat
	var disabled := start.get_theme_stylebox("disabled") as StyleBoxFlat
	if focus == null or normal == null or hover == null or disabled == null:
		_fail("the primary action is missing a required interaction style")
		return false
	var expected_focus := _scaled_metric(3)
	if focus.border_width_left < expected_focus or focus.expand_margin_left < expected_focus:
		_fail("keyboard focus has no structural focus ring")
		return false
	if hover.border_width_bottom == normal.border_width_bottom:
		_fail("pointer hover differs from rest by color alone")
		return false
	if disabled.border_width_bottom == normal.border_width_bottom:
		_fail("unavailable actions differ from rest by color alone")
		return false
	return true


func _semantic_styles_are_safe() -> bool:
	var theme := main.theme
	var legal := theme.get_stylebox("normal", &"LegalTargetButton") as StyleBoxFlat
	var selected := theme.get_stylebox("normal", &"SelectedTargetButton") as StyleBoxFlat
	var unavailable := theme.get_stylebox("normal", &"UnavailableButton") as StyleBoxFlat
	if legal == null or selected == null or unavailable == null:
		_fail("the theme does not define every semantic action state")
		return false
	if legal.border_width_left == selected.border_width_left:
		_fail("legal and selected targets differ by color alone")
		return false
	if unavailable.border_width_left == legal.border_width_left:
		_fail("unavailable and legal actions differ by color alone")
		return false
	return true


func _setup_overflow_is_safe() -> bool:
	var page := main.get_node("Margin") as ScrollContainer
	if page == null:
		_fail("the scaled page has no outer scroll container")
		return false
	var page_bounds := Rect2(Vector2.ZERO, _viewport_size())
	var setup_bounds: Rect2 = (_node("Setup") as Control).get_global_rect()
	if setup_bounds.position.x < page_bounds.position.x - 1.0 \
			or setup_bounds.end.x > page_bounds.end.x + 1.0:
		if page.horizontal_scroll_mode != ScrollContainer.SCROLL_MODE_AUTO:
			_fail("the scaled setup overflow is not horizontally accessible")
			return false
	for path in _setup_control_paths():
		if not await _setup_control_is_visible(page, path):
			return false
	return true


func _setup_control_paths() -> Array[String]:
	return [
		"Setup/Selections/Fields/ConnectionGrid/Endpoint",
		"Setup/Selections/Fields/ConnectionGrid/GameId",
		"Setup/Selections/Fields/Grid/Hero",
		"Setup/Selections/Fields/Grid/SecondHero",
		"Setup/Selections/Fields/Grid/Scenario",
		"Setup/Selections/Fields/Grid/Mode",
		"Setup/Selections/Fields/Grid/Modular",
		"Setup/Selections/Fields/Grid/Seed",
	]


func _setup_control_is_visible(page: ScrollContainer, path: String) -> bool:
	var control := _node(path) as Control
	var expected := _scaled_metric(44)
	if control.custom_minimum_size.y < expected:
		_fail("setup control '%s' is smaller than the pointer-target floor" % path)
		return false
	control.grab_focus()
	await process_frame
	await process_frame
	var page_rect := page.get_global_rect().intersection(Rect2(Vector2.ZERO, _viewport_size()))
	var control_rect := control.get_global_rect()
	if control_rect.end.y > page_rect.end.y:
		page.scroll_vertical += ceili(control_rect.end.y - page_rect.end.y)
	elif control_rect.position.y < page_rect.position.y:
		page.scroll_vertical -= ceili(page_rect.position.y - control_rect.position.y)
	await process_frame
	var visible := control.get_global_rect().intersection(page_rect)
	if visible.size.x >= expected and visible.size.y >= expected:
		return true
	_fail("setup control '%s' cannot be brought into the viewport" % path)
	return false


func _live_scale_rebuilds_the_decision() -> bool:
	var slider := _node("Toolbar/InterfaceScale") as HSlider
	var original := slider.value
	var choice := _first_enabled_choice()
	if choice == null:
		_fail("the live scale check has no decision action")
		return false
	var original_height := choice.custom_minimum_size.y
	slider.value = 60.0 if original == 50.0 else 50.0
	await process_frame
	await process_frame
	var resized_choice := _first_enabled_choice()
	if resized_choice == null or resized_choice.custom_minimum_size.y == original_height:
		_fail("changing scale did not rebuild the open decision controls")
		return false
	slider.value = original
	await process_frame
	await process_frame
	return true


func _entry_modes_are_explicit() -> bool:
	var endpoint := _node("Setup/Selections/Fields/ConnectionGrid/Endpoint") as LineEdit
	var game_id := _node("Setup/Selections/Fields/ConnectionGrid/GameId") as LineEdit
	var second_hero := _node("Setup/Selections/Fields/Grid/SecondHero") as OptionButton
	var reload_setup := _button_named("Reload setup options")
	if not _start_fields_are_safe(endpoint, game_id, second_hero, reload_setup):
		return false
	if not await _second_hero_is_safe(second_hero):
		return false
	if not await _modular_and_seed_are_safe():
		return false
	if not await _endpoint_retry_is_safe(endpoint, reload_setup):
		return false
	return await _join_flow_is_safe()


func _start_fields_are_safe(
		endpoint: LineEdit,
		game_id: LineEdit,
		second_hero: OptionButton,
		reload_setup: Button) -> bool:
	if endpoint == null or endpoint.max_length != 512:
		_fail("the engine endpoint is not visibly bounded to 512 characters")
		return false
	if game_id == null or game_id.text.is_empty():
		_fail("the start flow has no opaque game label")
		return false
	if second_hero == null or second_hero.item_count < 2:
		_fail("the start flow does not offer an optional second hero")
		return false
	if not second_hero.get_item_text(0).begins_with("Solo table"):
		_fail("the first second-hero option is not the solo table")
		return false
	if reload_setup == null or reload_setup.disabled:
		_fail("the start flow has no operable setup reload action")
		return false
	return true


func _second_hero_is_safe(second_hero: OptionButton) -> bool:
	var briefing := _node("Setup/Briefing/Frame/Copy/Hero") as Label
	var host_name := briefing.text
	second_hero.select(1)
	second_hero.item_selected.emit(1)
	await process_frame
	var guest_name := second_hero.get_item_text(1)
	if host_name not in briefing.text or guest_name not in briefing.text:
		_fail("selecting a second hero did not refresh the encounter briefing")
		return false
	second_hero.select(0)
	second_hero.item_selected.emit(0)
	await process_frame
	if briefing.text != host_name:
		_fail("returning to solo did not refresh the encounter briefing")
		return false
	return true


func _modular_and_seed_are_safe() -> bool:
	var modular := _node("Setup/Selections/Fields/Grid/Modular") as MenuButton
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	if modular == null or modular.get_popup().item_count < 7:
		_fail("the start flow has no complete modular-set multi-select menu")
		return false
	if seed == null or not seed.text.is_empty() or (_button_named("Start game") as Button).disabled:
		_fail("a blank seed does not remain an available random-deal choice")
		return false
	modular.get_popup().id_pressed.emit(2)
	modular.get_popup().id_pressed.emit(3)
	await process_frame
	if modular.text.count(",") < 1:
		_fail("modular encounter sets cannot be selected together")
		return false
	if not modular.get_popup().is_item_checked(3) or not modular.get_popup().is_item_checked(4):
		_fail("selected modular encounter sets are not checked")
		return false
	modular.get_popup().id_pressed.emit(0)
	await process_frame
	if not modular.text.begins_with("Use recommended"):
		_fail("the modular menu cannot return to the authored recommendation")
		return false
	return true


func _endpoint_retry_is_safe(endpoint: LineEdit, reload_setup: Button) -> bool:
	endpoint.text = "not-an-endpoint"
	endpoint.text_changed.emit(endpoint.text)
	await process_frame
	if reload_setup.disabled or not (_button_named("Start game") as Button).disabled:
		_fail("changing the endpoint did not require an explicit setup reload")
		return false
	endpoint.text = ""
	endpoint.text_changed.emit(endpoint.text)
	reload_setup.pressed.emit()
	if not await _wait_for(func() -> bool:
		return not reload_setup.disabled and not (_button_named("Start game") as Button).disabled):
		_fail("correcting the endpoint and retrying did not restore setup options")
		return false
	if (_node("Title") as Label).text != "Assemble the table.":
		_fail("a successful setup retry did not restore the setup title")
		return false
	if _node("Status").theme_type_variation != &"StatusPanel":
		_fail("a successful setup retry did not clear the unavailable status")
		return false
	return true


func _join_flow_is_safe() -> bool:
	var join_flow := _button_named("Join a game")
	if join_flow == null:
		_fail("the setup screen has no explicit Join a game action")
		return false
	join_flow.pressed.emit()
	await process_frame
	var invitation := _node("Setup/Selections/Fields/JoinFields/Invitation") as LineEdit
	var join := _button_named("Join game")
	if invitation == null or not invitation.secret or invitation.max_length != 256:
		_fail("the join invitation is not a bounded masked secret")
		return false
	if join == null or not join.disabled:
		_fail("join is available without an explicit remote endpoint and invitation")
		return false
	if _node("Setup/Selections/Fields/Grid").visible:
		_fail("the join flow exposes unrelated start-game assignment controls")
		return false
	if not await _capture_checkpoint("join-setup"):
		return false
	var start_flow := _button_named("Start a game")
	if start_flow == null:
		_fail("the setup screen cannot return to the explicit start flow")
		return false
	start_flow.pressed.emit()
	await process_frame
	if not _node("Setup/Selections/Fields/Grid").visible:
		_fail("returning to start did not restore assignment controls")
		return false
	return true
