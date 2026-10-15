extends "res://smoke/local_game_smoke.gd"


func _entry_modes_are_explicit() -> bool:
	if not await super._entry_modes_are_explicit(): return false
	if not _exposed_setup_is_readable(): return false
	if not await _exposed_hero_binding_is_safe(): return false
	if not await _exposed_encounter_binding_is_safe(): return false
	var start := _button_named("Start game")
	if not _control_is_fully_visible(start):
		_fail("choosing a game moved its commitment outside the default setup sheet")
		return false
	return true


func _exposed_setup_is_readable() -> bool:
	for name in ["Hero", "SecondHero", "Scenario", "Mode", "Modular"]:
		var adapter := _node("Setup/Selections/Fields/Grid/" + name) as BaseButton
		var row := _node("Setup/Selections/Fields/Grid/" + name + "Choices") as HFlowContainer
		if adapter.visible or adapter.focus_mode != Control.FOCUS_NONE or row == null:
			_fail("setup duplicates an exposed choice with a focusable dropdown")
			return false
		for button: Button in row.get_children():
			if not button.tooltip_text.is_empty() or button.get_theme_font_size("font_size") < 16:
				_fail("setup choice has redundant hover copy or unreadable essential text")
				return false
	return true


func _exposed_hero_binding_is_safe() -> bool:
	var hero := _node("Setup/Selections/Fields/Grid/Hero") as OptionButton
	if not await _pointer_activate(_choice("Hero", "Iron Man")): return false
	if hero.get_item_text(hero.selected) != "Iron Man":
		_fail("exposed hero choice did not update the authoritative setup draft")
		return false
	if _choice("SecondHero", "Iron Man") != null:
		_fail("the second-hero row retained the selected first hero")
		return false
	if not await _keyboard_activate(_choice("Hero", "Captain Marvel")): return false
	if hero.get_item_text(hero.selected) != "Captain Marvel" \
			or render_viewport.gui_get_focus_owner() != _choice("Hero", "Captain Marvel"):
		_fail("keyboard setup selection lost its choice or focused control during refresh")
		return false
	if not await _pointer_activate(_choice("Hero", "Spider-Man")): return false
	return true


func _exposed_encounter_binding_is_safe() -> bool:
	if not await _pointer_activate(_choice("Scenario", "Ultron")): return false
	var scenario := _node("Setup/Selections/Fields/Grid/Scenario") as OptionButton
	if scenario.get_item_text(scenario.selected) != "Ultron":
		_fail("exposed scenario choice did not update the existing selection binding")
		return false
	if not await _pointer_activate(_choice("Scenario", "Rhino")): return false
	if not await _pointer_activate(_choice("Modular", "Bomb Scare")): return false
	if not await _pointer_activate(_choice("Modular", "Masters of Evil")): return false
	var modular := _node("Setup/Selections/Fields/Grid/Modular") as MenuButton
	if "Bomb Scare" not in modular.text or "Masters of Evil" not in modular.text:
		_fail("exposed modular choices did not preserve multi-selection")
		return false
	if not await _pointer_activate(_choice("Modular", "Use recommended · Bomb Scare")): return false
	return true


func _choice(row_name: String, text: String) -> Button:
	var row := _node("Setup/Selections/Fields/Grid/" + row_name + "Choices")
	for button: Button in row.get_children():
		if button.text.trim_prefix("✓ ") == text: return button
	return null


func _terminal_table_is_safe(state: Dictionary) -> bool:
	if not await super._terminal_table_is_safe(state): return false
	var frame := main.get_node_or_null("TerminalOutcome") as Control
	var heading := main.find_child("OutcomeHeading", true, false) as Label
	var review := main.find_child("ReviewGame", true, false) as Button
	if frame == null or frame.size.y > 300 or heading.get_theme_font_size("font_size") < 24:
		_fail("terminal result is unbounded or its outcome heading is unreadable")
		return false
	if not _control_is_fully_visible(review) or not await _pointer_activate(review): return false
	await process_frame
	if frame.visible or not (_node("Play/Board") as Control).is_visible_in_tree():
		_fail("reviewing the result hid the final board or retained its result overlay")
		return false
	if not main.find_child("ToggleHistory", true, false).text == ">>":
		_fail("Review game did not open the retained authoritative history")
		return false
	if not await _synchronization_preserves_history(true): return false
	await process_frame
	if frame.visible:
		_fail("synchronizing the retained terminal table reopened a dismissed result")
		return false
	return await _capture_checkpoint("terminal-review")
