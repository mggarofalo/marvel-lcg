extends "res://smoke/local_game_smoke_mulligan_checks.gd"
func _synchronization_preserves_history(expect_terminal: bool) -> bool:
	var outcome_before := _status().text
	var synchronize := main.find_child("Synchronize", true, false) as Button
	if synchronize == null or not synchronize.visible or synchronize.disabled:
		_fail("the table has no operable always-visible synchronization control")
		return false
	if synchronize.custom_minimum_size.y < 32:
		_fail("the compact synchronization control is too small to operate")
		return false
	var event_log := _node("Play/Prompt/Margin/Stack/Workbench/History/HistoryOccurrences") as Control
	var history_before := str(event_log.get_meta("chronology", ""))
	if not await _pointer_activate(synchronize):
		return false
	if not await _wait_for(func() -> bool:
		return not _status().text.begins_with("SYNCHRONIZING")):
		_fail("the explicit table synchronization did not settle")
		return false
	if history_before != str(event_log.get_meta("chronology", "")):
		_fail("synchronization replayed or cleared the visible event history")
		return false
	if expect_terminal:
		if not _is_complete() or _status().text != outcome_before:
			_fail("synchronizing the terminal table lost its authoritative outcome")
			return false
	elif main.find_child("VillainTable", true, false) != null \
			and _attached(_attached_name(IDENTITY, "Action")) == null \
			and _task_commit() == null:
		_fail("synchronizing an ordinary prompt left the table objects inoperable")
		return false
	elif main.find_child("VillainTable", true, false) == null \
			and _first_enabled_choice() == null \
			and (_decision().find_child("Decline", true, false) as Button) == null:
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
	if start.get_theme_font_size("font_size") < maxi(12, _scaled_metric(14)):
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
	slider.value = 100.0 if original_scale != 100.0 else 120.0
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
	var expected_focus := maxi(2, _scaled_metric(2))
	if focus.border_width_left < expected_focus:
		_fail("keyboard focus has no structural focus ring")
		return false
	for treatment in [normal, hover, disabled, start.get_theme_stylebox("pressed")]:
		for side in ["left", "top", "right", "bottom"]:
			if treatment.get("content_margin_" + side) != normal.get("content_margin_" + side) \
				or treatment.get("border_width_" + side) != 1:
				_fail("commitment interaction changes its fixed content or edge geometry")
				return false
	if disabled.bg_color.a != 1.0 or disabled.bg_color.is_equal_approx(normal.bg_color):
		_fail("unavailable commitment has no opaque distinct treatment")
		return false
	if hover.bg_color.is_equal_approx(normal.bg_color) or not start.has_meta("shared_control_bound"):
		_fail("pointer hover has no surface treatment and shared edge feedback")
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
	if legal.border_width_left != 3 or selected.border_width_left != 3:
		_fail("legal and selected targets have no fixed local marker rail")
		return false
	if selected.bg_color.is_equal_approx(theme.get_stylebox("normal", &"PrimaryButton").bg_color):
		_fail("a selected target competes with the primary commitment")
		return false
	if unavailable.border_width_left == legal.border_width_left:
		_fail("unavailable and legal actions differ by color alone")
		return false
	return true


func _setup_overflow_is_safe() -> bool:
	var advanced := _node("Setup/Selections/Fields/AdvancedSetup") as Button
	advanced.button_pressed = true
	advanced.toggled.emit(true)
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
	var control := main.get_node_or_null("Margin/Shell/Content/" + path) as Control
	if control is OptionButton or control is MenuButton:
		var choices := control.get_parent().get_node_or_null(str(control.name) + "Choices")
		control = choices.get_child(0) as Control if choices != null and choices.get_child_count() > 0 else null
	if control == null:
		_fail("setup control '%s' has no visible choice control" % path)
		return false
	var expected := _scaled_metric(44)
	if control.custom_minimum_size.y < expected:
		_fail("setup control '%s' is smaller than the pointer-target floor" % path)
		return false
	control.grab_focus()
	var observations: Array[String] = []
	if await preload("res://smoke/setup_control_reveal.gd").settle(page, control, self, expected, observations):
		return true
	_fail("setup control '%s' cannot be brought into the viewport; target=%s page=%s scroll=%s range=%s" % [
		path, control.get_global_rect(), page.get_global_rect(), page.scroll_vertical,
		page.get_v_scroll_bar().max_value - page.get_v_scroll_bar().page]
		+ "\nLayout frames:\n" + "\n".join(observations))
	return false


func _live_scale_rebuilds_the_decision() -> bool:
	if main.find_child("VillainTable", true, false) != null:
		return await _mulligan_scale_rebuilds_the_dock()
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


func _mulligan_scale_rebuilds_the_dock() -> bool:
	var slider := _node("Toolbar/InterfaceScale") as HSlider
	var submit := _task_commit()
	var history := main.find_child("ToggleHistory", true, false) as Button
	if slider == null or submit == null or history == null:
		_fail("the tabletop scale check has no card-local execute control or history drawer")
		return false
	var original_scale := slider.value
	var original_submit := submit.get_instance_id()
	slider.value = 100.0 if original_scale != 100.0 else 120.0
	await process_frame
	await process_frame
	submit = _task_commit()
	history = main.find_child("ToggleHistory", true, false) as Button
	if submit == null or history == null or submit.get_instance_id() == original_submit:
		_fail("changing scale did not rebuild the card-local action surface")
		return false
	if not await _control_owns_point(history, _visible_control_rect(history).get_center()) \
			or not await _prepare_activation(submit):
		_fail("changing scale left a table action or history control clipped or occluded")
		return false
	slider.value = original_scale
	await process_frame
	await process_frame
	return true


func _entry_modes_are_explicit() -> bool:
	var advanced := _node("Setup/Selections/Fields/AdvancedSetup") as Button
	advanced.button_pressed = true
	advanced.toggled.emit(true)
	var endpoint := _node("Setup/Selections/Fields/ConnectionGrid/Endpoint") as LineEdit
	var game_id := _node("Setup/Selections/Fields/ConnectionGrid/GameId") as LineEdit
	var second_hero := _node("Setup/Selections/Fields/Grid/SecondHero") as OptionButton
	var reload_setup := _node("Setup/Selections/Fields/ReloadSetup") as Button
	if not _start_fields_are_safe(endpoint, game_id, second_hero, reload_setup):
		return false
	if not await _second_hero_is_safe(second_hero):
		return false
	if not await _modular_and_seed_are_safe():
		return false
	if not await _endpoint_retry_is_safe(endpoint, reload_setup):
		return false
	if not await _join_flow_is_safe(): return false
	advanced.button_pressed = false
	advanced.toggled.emit(false)
	return true


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
	if second_hero.get_item_text(0) != "No additional player" or second_hero.selected != 0:
		_fail("the optional second seat does not default to no additional player")
		return false
	if reload_setup == null or reload_setup.disabled:
		_fail("the start flow has no operable setup reload action")
		return false
	return true


func _second_hero_is_safe(second_hero: OptionButton) -> bool:
	var briefing := _node("Setup/Briefing/Frame/Copy/Hero") as Label
	var host_name := briefing.text
	if not _optional_seats_are_safe(): return false
	second_hero.select(1)
	second_hero.item_selected.emit(1)
	await process_frame
	if not _optional_seats_are_safe(): return false
	var guest_name := second_hero.get_item_text(1)
	if host_name not in briefing.text or guest_name not in briefing.text:
		_fail("selecting a second hero did not refresh the encounter briefing")
		return false
	second_hero.select(0)
	second_hero.item_selected.emit(0)
	await process_frame
	if not _optional_seats_are_safe(): return false
	if briefing.text != host_name:
		_fail("returning to solo did not refresh the encounter briefing")
		return false
	return true


func _optional_seats_are_safe() -> bool:
	var grid := "Setup/Selections/Fields/Grid/"
	var host := _node(grid + "Hero") as OptionButton
	var used: Array[String] = [host.get_item_text(host.selected)]
	var previous_selected := true
	for name in ["SecondHero", "HeroSeat2", "HeroSeat3"]:
		var choice := _node(grid + name) as OptionButton
		if choice == null or choice.item_count < 2 \
				or choice.get_item_text(0) != "No additional player":
			_fail("an optional seat has no explicit no-additional-player choice")
			return false
		if name != "SecondHero":
			var row := _node(grid + name + "Choices") as Control
			if row.visible != previous_selected:
				_fail("optional seat choices do not follow the occupied preceding seat")
				return false
		if choice.disabled == previous_selected:
			_fail("optional seats do not require a contiguous table")
			return false
		if not previous_selected and choice.selected != 0:
			_fail("an inactive optional seat retained a hero")
			return false
		var offered: Array[String] = []
		for index in range(1, choice.item_count):
			var hero_key := str(choice.get_item_metadata(index))
			if hero_key.is_empty() or hero_key in offered \
					or choice.get_item_text(index) in used:
				_fail("an optional seat offers an empty, duplicate or already seated hero")
				return false
			offered.append(hero_key)
		previous_selected = choice.selected > 0
		if previous_selected:
			used.append(choice.get_item_text(choice.selected))
	return true


func _modular_and_seed_are_safe() -> bool:
	if not await _setup_validation_survives_unrelated_edits():
		return false
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


func _setup_validation_survives_unrelated_edits() -> bool:
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	var label := _node("Setup/Selections/Fields/ConnectionGrid/GameId") as LineEdit
	var original := label.text
	seed.text = "invalid"
	seed.text_changed.emit(seed.text)
	label.text = "review-validation"
	label.text_changed.emit(label.text)
	await process_frame
	var status := _node("Status") as Control
	if not status.visible or "Enter a seed" not in _status().text:
		_fail("editing the game label concealed invalid-seed guidance")
		return false
	seed.text = ""
	seed.text_changed.emit(seed.text)
	label.text = original
	label.text_changed.emit(label.text)
	await process_frame
	return true


func _endpoint_retry_is_safe(endpoint: LineEdit, reload_setup: Button) -> bool:
	endpoint.text = "not-an-endpoint"
	endpoint.text_changed.emit(endpoint.text)
	await process_frame
	if reload_setup.disabled or not (_button_named("Start game") as Button).disabled:
		_fail("changing the endpoint did not require an explicit setup reload")
		return false
	var label := _node("Setup/Selections/Fields/ConnectionGrid/GameId") as LineEdit
	label.text_changed.emit(label.text)
	await process_frame
	if not (_node("Status") as Control).visible or _status().text.is_empty():
		_fail("editing the game label concealed the endpoint reload guidance")
		return false
	endpoint.text = ""
	endpoint.text_changed.emit(endpoint.text)
	if not await _pointer_activate(reload_setup):
		return false
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
	var join_flow := _node("Setup/Selections/Fields/EntryModes/JoinFlow") as Button
	if join_flow == null:
		_fail("the setup screen has no explicit Join a game action")
		return false
	if not await _pointer_activate(join_flow):
		return false
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
	var start_flow := _node("Setup/Selections/Fields/EntryModes/StartFlow") as Button
	if start_flow == null:
		_fail("the setup screen cannot return to the explicit start flow")
		return false
	if not await _pointer_activate(start_flow):
		return false
	if not _node("Setup/Selections/Fields/Grid").visible:
		_fail("returning to start did not restore assignment controls")
		return false
	return true
