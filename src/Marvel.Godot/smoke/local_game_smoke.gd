extends SceneTree

const TIMEOUT_MILLISECONDS := 15000
const MAX_DECISIONS := 20

var main: Control
var failed := false
var motion_enabled := true
var render_viewport: Viewport


func _initialize() -> void:
	_prepare_art_pack()
	motion_enabled = OS.get_environment("MARVEL_SMOKE_MOTION") != "disabled"
	var viewport := OS.get_environment("MARVEL_SMOKE_VIEWPORT").split("x")
	if viewport.size() == 2:
		var requested := Vector2i(int(viewport[0]), int(viewport[1]))
		var fixed_viewport := SubViewport.new()
		fixed_viewport.size = requested
		fixed_viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
		root.add_child(fixed_viewport)
		render_viewport = fixed_viewport
	else:
		render_viewport = root
	_run.call_deferred()


func _run() -> void:
	var packed := load("res://Main.tscn") as PackedScene
	if packed == null:
		_fail("Main.tscn could not be loaded")
		return

	main = packed.instantiate() as Control
	render_viewport.add_child(main)
	if not await _wait_for(func() -> bool: return _button_named("Start game") != null):
		_fail("setup never became ready")
		return
	if not await _visual_system_is_resolved():
		return
	if not await _entry_modes_are_explicit():
		return
	if not await _capture_checkpoint("setup"):
		return

	_select_named_option(_node("Setup/Selections/Fields/Grid/Hero"), "Spider-Man")
	_select_named_option(_node("Setup/Selections/Fields/Grid/Scenario"), "Rhino")
	_select_named_option(_node("Setup/Selections/Fields/Grid/Mode"), "Standard")
	var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
	seed.text = "1"
	seed.text_changed.emit(seed.text)
	var configured_motion := _node("Play/Prompt/Margin/Stack/EventHeader/Motion") as CheckButton
	configured_motion.button_pressed = motion_enabled
	configured_motion.toggled.emit(motion_enabled)
	await process_frame

	var start := _button_named("Start game")
	if start == null or start.disabled:
		_fail("the visible Start game control is unavailable")
		return
	start.pressed.emit()
	if not await _wait_for(func() -> bool: return _play().visible and _decision() != null):
		_fail("the opened table never became visible")
		return
	if not _procedural_cards_are_safe():
		return
	if not await _board_layout_is_resolved():
		return
	if not await _keyboard_selection_is_operable():
		return
	if not await _event_presentation_is_nonblocking():
		return
	if not await _synchronization_preserves_history(false):
		return
	if not await _capture_checkpoint("open-table-prompt-dense-concealed"):
		return

	var saw_mulligan := false
	var saw_pass := false
	var saw_end_phase := false
	var saw_nonblocking_motion := false
	var tested_active_motion_toggle := false
	var captured_villain_phase := false
	var decisions := 0
	while not _is_complete():
		if decisions >= MAX_DECISIONS:
			_fail("the visible-control journey exceeded %d decisions" % MAX_DECISIONS)
			return
		if not _visible_buttons_meet_pointer_floor():
			return

		var decision_text := _visible_text(_decision())
		saw_mulligan = saw_mulligan or "Mulligan" in decision_text
		saw_end_phase = saw_end_phase or "End Phase" in decision_text
		var ending_player_phase := "End Phase" in decision_text
		if ending_player_phase and not await _capture_checkpoint("player-phase"):
			return
		var pass_button := _visible_button(_decision(), "Pass / decline")
		if pass_button != null and not pass_button.disabled:
			saw_pass = true
			pass_button.pressed.emit()
		else:
			var submit := _visible_button(_decision(), "Submit decision")
			if submit == null or submit.disabled:
				var choice := _first_enabled_choice()
				if choice == null:
					_fail("no visible control can advance the current decision")
					return
				choice.pressed.emit()
				await process_frame
				submit = _visible_button(_decision(), "Submit decision")
			if submit == null or submit.disabled:
				_fail("the selected visible decision cannot be submitted")
				return
			submit.pressed.emit()

		decisions += 1
		await process_frame
		if not await _wait_for(func() -> bool:
			return _is_complete() or not _status().text.begins_with("DECISION SENT")):
			_fail("the engine did not reconcile decision %d" % decisions)
			return
		var event_skip := _node("Play/Prompt/Margin/Stack/EventHeader/Skip") as Button
		if motion_enabled and not event_skip.disabled \
				and (_is_complete() or _first_enabled_choice() != null):
			saw_nonblocking_motion = true
			if not tested_active_motion_toggle:
				var motion_history := (_node("Play/Prompt/Margin/Stack/EventLog") as RichTextLabel).text
				var motion := _node("Play/Prompt/Margin/Stack/EventHeader/Motion") as CheckButton
				motion.button_pressed = false
				motion.toggled.emit(false)
				await process_frame
				if not event_skip.disabled or motion_history != \
						(_node("Play/Prompt/Margin/Stack/EventLog") as RichTextLabel).text:
					_fail("disabling active motion did not settle without changing history")
					return
				motion.button_pressed = true
				motion.toggled.emit(true)
				tested_active_motion_toggle = true
		if not motion_enabled and not _disabled_motion_is_settled(event_skip):
			return
		var history_text := (_node("Play/Prompt/Margin/Stack/EventLog") as RichTextLabel) \
			.get_parsed_text().to_lower()
		if not captured_villain_phase and not _is_complete() and "villain phase" in history_text:
			if not await _capture_checkpoint("villain-phase"):
				return
			captured_villain_phase = true

	if not saw_mulligan or not saw_pass or not saw_end_phase:
		_fail("the journey missed a required visible decision path")
		return
	if not captured_villain_phase:
		_fail("the journey never reached a non-terminal villain-phase checkpoint")
		return
	if motion_enabled and not saw_nonblocking_motion:
		_fail("the journey never exposed an operable prompt while event motion was active")
		return
	if motion_enabled and not tested_active_motion_toggle:
		_fail("the journey never disabled event motion during active playback")
		return
	if not motion_enabled and saw_nonblocking_motion:
		_fail("the motion-disabled journey exposed active event playback")
		return
	if "VILLAIN WINS" not in _status().text:
		_fail("the terminal UI did not report the seeded villain win")
		return
	if not await _synchronization_preserves_history(true):
		return
	var terminal_decision := _visible_text(_decision()).to_upper()
	var terminal_prompt := _visible_text(
		_node("Play/Prompt/Margin/Stack/PromptHeader")).to_upper()
	if "DEFEAT" not in terminal_decision or "VILLAIN WON" not in terminal_decision \
			or "VILLAIN WON" not in terminal_prompt:
		_fail("the null-prompt terminal decision copy does not identify the villain outcome")
		return
	if _node("Status").theme_type_variation != &"DangerStatusPanel":
		_fail("the villain win did not receive the semantic danger treatment")
		return
	var event_log := _node("Play/Prompt/Margin/Stack/EventLog") as RichTextLabel
	var event_text := event_log.get_parsed_text().strip_edges()
	if event_text.is_empty() or event_text == "No events yet.":
		_fail("the visible event log is empty")
		return
	if "villain won the game" not in event_text.to_lower():
		_fail("the terminal outcome did not remain in recent history")
		return
	if not await _wait_for(func() -> bool:
		return _control_text_is_visible(_node("Title") as Control) \
			and _control_text_is_visible(_node("Description") as Control)):
		var terminal_title := _node("Title") as Control
		var terminal_description := _node("Description") as Control
		var page := main.get_node("Margin") as ScrollContainer
		_fail("the terminal page did not reveal its outcome and explanation" \
			+ "\nPage rect: %s scroll: %d" % [page.get_global_rect(), page.scroll_vertical] \
			+ "\nTitle rect: %s visible: %s" % [
				terminal_title.get_global_rect(),
				_visible_control_rect(terminal_title),
			] \
			+ "\nDescription rect: %s visible: %s" % [
				terminal_description.get_global_rect(),
				_visible_control_rect(terminal_description),
			])
		return
	if not await _capture_checkpoint("terminal"):
		return

	print("LOCAL_GAME_SMOKE_OK decisions=%d motion=%s" % [
		decisions,
		"enabled" if motion_enabled else "disabled",
	])
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)


func _synchronization_preserves_history(expect_terminal: bool) -> bool:
	var synchronize := _button_named("Synchronize table")
	if synchronize == null:
		synchronize = _button_named("Reconnect table")
	if synchronize == null or not synchronize.visible or synchronize.disabled:
		_fail("the table has no operable always-visible synchronization control")
		return false
	if synchronize.custom_minimum_size.y < 44:
		_fail("the synchronization control is smaller than the pointer-target floor")
		return false

	var event_log := _node("Play/Prompt/Margin/Stack/EventLog") as RichTextLabel
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
		if "VILLAIN WINS" not in _status().text:
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
	var scale := OS.get_environment("MARVEL_UI_SCALE")
	var expected_height := 66 if scale == "extra-large" else 55 if scale == "large" else 44
	var expected_body := 24 if scale == "extra-large" else 20 if scale == "large" else 16
	var expected_focus := 5 if scale == "extra-large" else 4 if scale == "large" else 3
	if start.theme_type_variation != &"PrimaryButton":
		_fail("the primary action does not use its semantic theme role")
		return false
	if start.custom_minimum_size.y < expected_height:
		_fail("the primary action is smaller than the pointer-target floor")
		return false
	if start.get_theme_font_size("font_size") < expected_body:
		_fail("the primary action did not adopt the selected type scale")
		return false

	var focus := start.get_theme_stylebox("focus") as StyleBoxFlat
	var normal := start.get_theme_stylebox("normal") as StyleBoxFlat
	var hover := start.get_theme_stylebox("hover") as StyleBoxFlat
	var disabled := start.get_theme_stylebox("disabled") as StyleBoxFlat
	if focus == null or normal == null or hover == null or disabled == null:
		_fail("the primary action is missing a required interaction style")
		return false
	if focus.border_width_left < expected_focus or focus.expand_margin_left < expected_focus:
		_fail("keyboard focus has no structural focus ring")
		return false
	if hover.border_width_bottom == normal.border_width_bottom:
		_fail("pointer hover differs from rest by color alone")
		return false
	if disabled.border_width_bottom == normal.border_width_bottom:
		_fail("unavailable actions differ from rest by color alone")
		return false

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

	var page_scroll := main.get_node("Margin") as ScrollContainer
	if page_scroll == null:
		_fail("the scaled page has no outer scroll container")
		return false
	var page_bounds := Rect2(Vector2.ZERO, _viewport_size())
	var setup_bounds: Rect2 = (_node("Setup") as Control).get_global_rect()
	if setup_bounds.position.x < page_bounds.position.x - 1.0 \
			or setup_bounds.end.x > page_bounds.end.x + 1.0:
		_fail("the setup layout is horizontally inaccessible: setup=%s page=%s" % [
			setup_bounds,
			page_bounds,
		])
		return false
	for path in [
		"Setup/Selections/Fields/ConnectionGrid/Endpoint",
		"Setup/Selections/Fields/ConnectionGrid/GameId",
		"Setup/Selections/Fields/Grid/Hero",
		"Setup/Selections/Fields/Grid/SecondHero",
		"Setup/Selections/Fields/Grid/Scenario",
		"Setup/Selections/Fields/Grid/Mode",
		"Setup/Selections/Fields/Grid/Modular",
		"Setup/Selections/Fields/Grid/Seed",
	]:
		var control := _node(path) as Control
		if control.custom_minimum_size.y < expected_height:
			_fail("setup control '%s' is smaller than the pointer-target floor" % path)
			return false
		control.grab_focus()
		await process_frame
		await process_frame
		var page_rect := page_scroll.get_global_rect().intersection(
			Rect2(Vector2.ZERO, _viewport_size()))
		var control_rect := control.get_global_rect()
		if control_rect.end.y > page_rect.end.y:
			page_scroll.scroll_vertical += ceili(control_rect.end.y - page_rect.end.y)
		elif control_rect.position.y < page_rect.position.y:
			page_scroll.scroll_vertical -= ceili(page_rect.position.y - control_rect.position.y)
		await process_frame
		var visible_rect := control.get_global_rect().intersection(
			page_rect)
		if visible_rect.size.x < expected_height or visible_rect.size.y < expected_height:
			_fail("setup control '%s' cannot be brought into the viewport: control=%s visible=%s scroll=%s/%s" % [
				path,
				control.get_global_rect(),
				visible_rect,
				page_scroll.scroll_vertical,
				page_scroll.get_v_scroll_bar().max_value,
			])
			return false

	return true


func _entry_modes_are_explicit() -> bool:
	var endpoint := _node("Setup/Selections/Fields/ConnectionGrid/Endpoint") as LineEdit
	var game_id := _node("Setup/Selections/Fields/ConnectionGrid/GameId") as LineEdit
	var second_hero := _node("Setup/Selections/Fields/Grid/SecondHero") as OptionButton
	var reload_setup := _button_named("Reload setup options")
	var join_flow := _button_named("Join a game")
	if endpoint == null or endpoint.max_length != 512:
		_fail("the engine endpoint is not visibly bounded to 512 characters")
		return false
	if game_id == null or game_id.text.is_empty():
		_fail("the start flow has no opaque game label")
		return false
	if second_hero == null or second_hero.item_count < 2 \
			or not second_hero.get_item_text(0).begins_with("Solo table"):
		_fail("the start flow does not offer an optional second hero (items=%d selected=%d)" % [
			second_hero.item_count if second_hero != null else -1,
			second_hero.selected if second_hero != null else -1,
		])
		return false
	if reload_setup == null or reload_setup.disabled:
		_fail("the start flow has no operable setup reload action")
		return false

	var host_name := (_node("Setup/Briefing/Frame/Copy/Hero") as Label).text
	second_hero.select(1)
	second_hero.item_selected.emit(1)
	await process_frame
	var guest_name := second_hero.get_item_text(1)
	var briefing_heroes := (_node("Setup/Briefing/Frame/Copy/Hero") as Label).text
	if host_name not in briefing_heroes or guest_name not in briefing_heroes:
		_fail("selecting a second hero did not refresh the encounter briefing")
		return false
	second_hero.select(0)
	second_hero.item_selected.emit(0)
	await process_frame
	if (_node("Setup/Briefing/Frame/Copy/Hero") as Label).text != host_name:
		_fail("returning to solo did not refresh the encounter briefing")
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
	if modular.text.count(",") < 1 \
			or not modular.get_popup().is_item_checked(3) \
			or not modular.get_popup().is_item_checked(4):
		_fail("modular encounter sets cannot be selected together")
		return false
	modular.get_popup().id_pressed.emit(0)
	await process_frame
	if not modular.text.begins_with("Use recommended"):
		_fail("the modular menu cannot return to the authored recommendation")
		return false

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
	if (_node("Title") as Label).text != "Assemble the table." \
			or _node("Status").theme_type_variation != &"StatusPanel":
		_fail("a successful setup retry did not clear the unavailable presentation")
		return false
	if join_flow == null:
		_fail("the setup screen has no explicit Join a game action")
		return false

	join_flow.pressed.emit()
	await process_frame
	var invitation := _node(
		"Setup/Selections/Fields/JoinFields/Invitation") as LineEdit
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


func _procedural_cards_are_safe() -> bool:
	var cards := main.find_children("ProceduralCard", "PanelContainer", true, false)
	if cards.is_empty():
		_fail("the opened table has no procedural card controls")
		return false

	var scale := OS.get_environment("MARVEL_UI_SCALE")
	var expected_width := 360 if scale == "extra-large" else 300 if scale == "large" else 240
	var saw_face := false
	var saw_back := false
	var saw_rules := false
	var saw_current := false
	var saw_local_art := false
	var saw_invalid_art_fallback := false
	for card in cards:
		if card.custom_minimum_size.x < expected_width:
			_fail("a board card does not honor the selected card geometry")
			return false
		var face := card.find_child("CardFace", true, false)
		var back := card.find_child("CardBack", true, false)
		if face != null:
			saw_face = true
			var title := face.find_child("Title", true, false) as Label
			if title == null or title.max_lines_visible != -1:
				_fail("a board card title is truncated")
				return false
			if title.size.y < title.get_theme_font_size("font_size"):
				_fail("a board card title collapsed out of its card: %s title=%s face=%s card=%s parent=%s" % [
					title.text,
					title.size,
					face.size,
					card.size,
					card.get_parent().size,
				])
				return false
			var rules := face.find_child("RulesText", true, false) as Label
			saw_rules = saw_rules or rules != null
			if rules != null and rules.max_lines_visible != -1:
				_fail("a board card rules box is truncated")
				return false
			if rules != null and rules.size.y < rules.get_theme_font_size("font_size"):
				_fail("a board card rules box collapsed out of its card: %s size=%s" % [
					title.text,
					rules.size,
				])
				return false
			saw_current = saw_current or face.find_child("LiveValues", true, false) != null
			if title.text == "Peter Parker":
				saw_local_art = face.find_child("Illustration", true, false) is TextureRect
			if title.text == "Rhino" and face.find_child("Illustration", true, false) == null:
				saw_invalid_art_fallback = true
		elif back != null:
			saw_back = true
			if back.find_child("Title", true, false) != null or back.find_child("RulesText", true, false) != null:
				_fail("a concealed card back contains face-identifying controls")
				return false
			if back.find_child("Illustration", true, false) != null:
				_fail("a concealed card consulted the face-art path")
				return false
			var back_text := _visible_text(back)
			if "secret" in back_text.to_lower() or "face-" in back_text.to_lower():
				_fail("a concealed card back leaked an identity")
				return false

	if not saw_face or not saw_back or not saw_rules or not saw_current:
		_fail("the table did not exercise face, back, rules, and current-value card regions")
		return false
	if not saw_local_art:
		_fail("an authorized local illustration did not load by stable face id")
		return false
	if not saw_invalid_art_fallback:
		_fail("invalid local art did not retain Rhino's procedural face")
		return false
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


func _board_layout_is_resolved() -> bool:
	var scenario_lane := _node("Play/Board/Margin/Areas").find_child(
		"ScenarioLane", true, false) as Control
	var player_lane := _node("Play/Board/Margin/Areas").find_child(
		"PlayerLane0", true, false) as Control
	if scenario_lane == null or player_lane == null:
		_fail("the opened table does not expose scenario and player lanes")
		return false

	var areas := main.find_children("Area*", "PanelContainer", true, false)
	var area_ids: Dictionary = {}
	for area in areas:
		if area.name in area_ids:
			_fail("a board area was rendered more than once: %s" % area.name)
			return false
		area_ids[area.name] = true
	if areas.is_empty():
		_fail("the opened table has no rendered areas")
		return false

	var area_flow := scenario_lane.find_child("AreaFlow", true, false) as HFlowContainer
	var card_scroll := main.find_child("CARDSScroll", true, false) as ScrollContainer
	var decision_scroll := _node("Play/Prompt/Margin/Stack/DecisionScroll") as ScrollContainer
	if area_flow == null or card_scroll == null or decision_scroll == null:
		_fail("the table is missing its wrapped areas or bounded overflow rails")
		return false
	if card_scroll.horizontal_scroll_mode == ScrollContainer.SCROLL_MODE_DISABLED \
			or decision_scroll.horizontal_scroll_mode == ScrollContainer.SCROLL_MODE_DISABLED:
		_fail("a dense card or prompt rail cannot scroll horizontally")
		return false
	if scenario_lane.find_child("AreaScroll", true, false) != null:
		_fail("the finite area layout still requires its own scrollbar")
		return false

	await process_frame
	await process_frame
	var board := _node("Play/Board") as Control
	var prompt := _node("Play/Prompt") as Control
	var page := main.get_node("Margin") as ScrollContainer
	if OS.get_environment("MARVEL_UI_SCALE") == "standard" \
			and (page.scroll_vertical != 0 \
			or not _control_text_is_visible(_node("Eyebrow") as Control) \
			or not _control_text_is_visible(_node("Title") as Control) \
			or not _control_text_is_visible(_node("Description") as Control)):
		_fail("the play layout moved its fixed header outside the viewport: page=%s scroll=%d" % [
			page.get_global_rect(),
			page.scroll_vertical,
		])
		return false
	if board.size.x < 480.0 or prompt.size.x < 330.0 or prompt.size.x > board.size.x:
		_fail("the responsive table did not preserve usable board and prompt widths: %s/%s" % [
			board.size.x,
			prompt.size.x,
		])
		return false
	var viewport := OS.get_environment("MARVEL_SMOKE_VIEWPORT")
	var scale := OS.get_environment("MARVEL_UI_SCALE")
	if viewport == "1600x900" and scale == "standard" \
			and (prompt.size.x < 435.0 or prompt.size.x > 445.0):
		_fail("the wide desktop prompt did not grow to its 440px cap: %s" % prompt.size.x)
		return false
	if board.get_global_rect().intersects(prompt.get_global_rect()):
		_fail("the prompt rail overlaps the board")
		return false
	return true


func _keyboard_selection_is_operable() -> bool:
	var header := _node("Play/Prompt/Margin/Stack/PromptHeader") as Control
	var decision_scroll := _node("Play/Prompt/Margin/Stack/DecisionScroll") as ScrollContainer
	if header == null or decision_scroll == null or decision_scroll.is_ancestor_of(header):
		_fail("the active seat and question are not pinned above the decision body")
		return false
	var header_text := _visible_text(header)
	if "PLAYER 1" not in header_text or "MULLIGAN" not in header_text.to_upper() \
			or ("DECISION REQUIRED" not in header_text and "PASS AVAILABLE" not in header_text):
		_fail("the pinned prompt summary omits seat, question, or cancellability")
		return false

	await process_frame
	await process_frame
	var focused := render_viewport.gui_get_focus_owner() as Button
	if focused == null or not _decision().is_ancestor_of(focused) or focused.disabled:
		_fail("a fresh prompt did not focus its first keyboard-operable action")
		return false
	var focus_name := focused.name
	var press := InputEventAction.new()
	press.action = &"ui_accept"
	press.pressed = true
	render_viewport.push_input(press)
	await process_frame
	var release := InputEventAction.new()
	release.action = &"ui_accept"
	release.pressed = false
	render_viewport.push_input(release)
	await process_frame
	await process_frame
	var restored := render_viewport.gui_get_focus_owner() as Button
	if restored == null or restored.name != focus_name or not _decision().is_ancestor_of(restored):
		_fail("keyboard focus was lost when the selected decision control rebuilt")
		return false
	if not await _wait_for(func() -> bool: return _focused_control_is_visible(restored)):
		var focused_scroll := _node("Play/Prompt/Margin/Stack/DecisionScroll") as ScrollContainer
		var page_scroll := main.get_node("Margin") as ScrollContainer
		_fail("keyboard focus moved outside the visible viewport: control=%s decision=%s page=%s root=%s scroll=%d/%d" % [
			restored.get_global_rect(),
			focused_scroll.get_global_rect(),
			page_scroll.get_global_rect(),
			_viewport_size(),
			focused_scroll.scroll_vertical,
			page_scroll.scroll_vertical,
		])
		return false
	if decision_scroll.scroll_horizontal != 0:
		_fail("keyboard focus horizontally clipped the selected decision label")
		return false
	for prompt_path in [
		"Play/Prompt/Margin/Stack/PromptHeader/Eyebrow",
		"Play/Prompt/Margin/Stack/PromptHeader/Heading",
		"Play/Prompt/Margin/Stack/PromptHeader/Context",
	]:
		if not _control_text_is_visible(_node(prompt_path) as Control):
			_fail("keyboard focus hid active prompt context: %s" % prompt_path)
			return false
	if "SELECTED" not in restored.text:
		_fail("ui_accept did not select the focused decision action")
		return false
	var progress := _node("Play/Prompt/Margin/Stack/PromptHeader/Progress") as Label
	if progress == null or ("READY" not in progress.text and "INCOMPLETE" not in progress.text) \
			or ("TARGETS" not in progress.text and "GROUP" not in progress.text \
			and "NO TARGETS" not in progress.text):
		_fail("the pinned prompt summary did not update target and readiness progress")
		return false
	if not await _focused_board_area_is_visible():
		return false
	return true


func _focused_control_is_visible(control: Control) -> bool:
	var visible_rect := _visible_control_rect(control)
	var scale := OS.get_environment("MARVEL_UI_SCALE")
	var expected := 66 if scale == "extra-large" else 55 if scale == "large" else 44
	return visible_rect.size.x >= expected and visible_rect.size.y >= expected


func _control_text_is_visible(control: Control) -> bool:
	var visible_rect := _visible_control_rect(control)
	return visible_rect.size.x >= minf(100.0, control.size.x) \
		and visible_rect.size.y >= control.size.y - 1.0


func _visible_control_rect(control: Control) -> Rect2:
	var visible_rect := control.get_global_rect().intersection(Rect2(Vector2.ZERO, _viewport_size()))
	var ancestor := control.get_parent()
	while ancestor != null:
		if ancestor is ScrollContainer:
			visible_rect = visible_rect.intersection(ancestor.get_global_rect())
		ancestor = ancestor.get_parent()
	return visible_rect


func _viewport_size() -> Vector2:
	return Vector2(render_viewport.size)


func _focused_board_area_is_visible() -> bool:
	await process_frame
	await process_frame
	var saw_focused_card := false
	for card in main.find_children("ProceduralCard", "PanelContainer", true, false):
		if card.theme_type_variation != &"FocusedCard":
			continue
		saw_focused_card = true
		var area := card.get_parent()
		while area != null and not (area is PanelContainer and area.name.begins_with("Area")):
			area = area.get_parent()
		var board := _node("Play/Board") as ScrollContainer
		if area == null or board == null:
			_fail("a focused board card is not contained by the board viewport")
			return false
		var area_rect: Rect2 = area.get_global_rect()
		var board_rect: Rect2 = board.get_global_rect()
		if area_rect.position.x < board_rect.position.x - 1.0 \
				or area_rect.end.x > board_rect.end.x + 1.0:
			_fail("keyboard highlighting clipped the focused board area's heading")
			return false
		var title := card.find_child("Title", true, false) as Label
		if title != null:
			var title_rect := title.get_global_rect()
			if title_rect.position.y < board_rect.position.y - 1.0 \
					or title_rect.end.y > board_rect.end.y + 1.0:
				_fail("keyboard highlighting did not reveal the focused card title: title=%s board=%s scroll=%d/%d" % [
					title_rect,
					board_rect,
					board.scroll_vertical,
					board.get_v_scroll_bar().max_value,
				])
				return false
	if not saw_focused_card:
		_fail("keyboard selection did not highlight its board anchor")
		return false
	return true


func _event_presentation_is_nonblocking() -> bool:
	var cue := _node("Play/Prompt/Margin/Stack/EventCue") as Control
	var motion := _node("Play/Prompt/Margin/Stack/EventHeader/Motion") as CheckButton
	var skip := _node("Play/Prompt/Margin/Stack/EventHeader/Skip") as Button
	var log := _node("Play/Prompt/Margin/Stack/EventLog") as RichTextLabel
	var scale := OS.get_environment("MARVEL_UI_SCALE")
	var expected_height := 66 if scale == "extra-large" else 55 if scale == "large" else 44
	if cue == null or not cue.visible or cue.custom_minimum_size.y < 68.0:
		_fail("event presentation has no fixed reserved cue region")
		return false
	if motion == null or skip == null \
			or motion.custom_minimum_size.y < expected_height \
			or skip.custom_minimum_size.y < expected_height:
		_fail("event presentation controls miss the pointer-target floor")
		return false
	if motion.button_pressed != motion_enabled:
		_fail("the configured motion preference was not applied before the game opened")
		return false
	var action := _first_enabled_choice()
	if action == null or action.disabled:
		_fail("event presentation blocked the current engine decision")
		return false

	var history := log.text
	if motion_enabled and not skip.disabled:
		skip.pressed.emit()
		await process_frame
	if not skip.disabled or log.text != history:
		_fail("skipping motion changed or cleared event history")
		return false
	var cue_text := _visible_text(cue)
	if "TABLE SYNCED" not in cue_text or "authoritative board" not in cue_text:
		_fail("skipping motion did not settle on the authoritative snapshot")
		return false

	if not motion_enabled and not _disabled_motion_is_settled(skip):
		return false
	return true


func _disabled_motion_is_settled(skip: Button) -> bool:
	if not skip.disabled:
		_fail("motion-disabled presentation left playback active")
		return false
	var cue := _node("Play/Prompt/Margin/Stack/EventCue") as Control
	if "TABLE SYNCED" not in _visible_text(cue):
		_fail("motion-disabled presentation did not settle on the authoritative snapshot")
		return false
	return true


func _capture_checkpoint(checkpoint: String) -> bool:
	var capture_dir := OS.get_environment("MARVEL_SMOKE_CAPTURE_DIR")
	if capture_dir.is_empty():
		return true
	await process_frame
	await process_frame
	if checkpoint == "open-table-prompt-dense-concealed" \
			and not await _focused_board_area_is_visible():
		return false
	var image := render_viewport.get_texture().get_image()
	if image == null or image.is_empty():
		_fail("visual checkpoint '%s' needs a non-headless rendering driver" % checkpoint)
		return false
	var requested_viewport := OS.get_environment("MARVEL_SMOKE_VIEWPORT").split("x")
	if requested_viewport.size() == 2 \
			and image.get_size() != Vector2i(
				int(requested_viewport[0]), int(requested_viewport[1])):
		_fail("visual checkpoint '%s' has size %s instead of %s" % [
			checkpoint,
			image.get_size(),
			OS.get_environment("MARVEL_SMOKE_VIEWPORT"),
		])
		return false

	var colors: Dictionary = {}
	var sample := image.duplicate()
	sample.resize(32, 18, Image.INTERPOLATE_NEAREST)
	for x_step in sample.get_width():
		for y_step in sample.get_height():
			var pixel: Color = sample.get_pixel(x_step, y_step)
			colors[pixel.to_html()] = true
	if colors.size() < 6:
		_fail("visual checkpoint '%s' is blank or materially unrendered" % checkpoint)
		return false

	var absolute_dir := ProjectSettings.globalize_path(capture_dir)
	var error := DirAccess.make_dir_recursive_absolute(absolute_dir)
	if error != OK:
		_fail("visual checkpoint directory could not be created: %s" % absolute_dir)
		return false
	var viewport := OS.get_environment("MARVEL_SMOKE_VIEWPORT")
	var scale := OS.get_environment("MARVEL_UI_SCALE")
	var motion := "motion" if motion_enabled else "reduced-motion"
	var path := absolute_dir.path_join("%s-%s-%s-%s.png" % [
		viewport,
		scale,
		motion,
		checkpoint,
	])
	if image.save_png(path) != OK:
		_fail("visual checkpoint could not be saved: %s" % path)
		return false
	return true


func _first_enabled_choice() -> Button:
	for button in _visible_buttons(_decision()):
		if not button.disabled and button.text not in ["Submit decision", "Pass / decline", "+", "−"]:
			return button
	return null


func _visible_buttons_meet_pointer_floor() -> bool:
	var scale := OS.get_environment("MARVEL_UI_SCALE")
	var expected := 66 if scale == "extra-large" else 55 if scale == "large" else 44
	for button in _visible_buttons(_decision()):
		if button.size.x < expected or button.size.y < expected:
			_fail("visible decision control '%s' misses the pointer-target floor" % button.text)
			return false
	return true


func _select_named_option(option: OptionButton, wanted: String) -> void:
	for index in option.item_count:
		if option.get_item_text(index).begins_with(wanted):
			option.select(index)
			option.item_selected.emit(index)
			return
	_fail("visible option '%s' is unavailable" % wanted)


func _button_named(wanted: String) -> Button:
	return _visible_button(main, wanted)


func _visible_button(node: Node, wanted: String) -> Button:
	for button in _visible_buttons(node):
		if button.text == wanted:
			return button
	return null


func _visible_buttons(node: Node) -> Array[Button]:
	var found: Array[Button] = []
	for child in node.get_children():
		if child is Button and child.is_visible_in_tree():
			found.append(child)
		found.append_array(_visible_buttons(child))
	return found


func _visible_text(node: Node) -> String:
	var text := ""
	for child in node.get_children():
		if child is Label and child.is_visible_in_tree():
			text += child.text + "\n"
		elif child is Button and child.is_visible_in_tree():
			text += child.text + "\n"
		text += _visible_text(child)
	return text


func _wait_for(condition: Callable) -> bool:
	var started := Time.get_ticks_msec()
	while Time.get_ticks_msec() - started < TIMEOUT_MILLISECONDS:
		if condition.call():
			return true
		await process_frame
	return false


func _is_complete() -> bool:
	return _status().text.begins_with("GAME COMPLETE")


func _node(relative: String) -> Node:
	return main.get_node("Margin/Shell/Content/" + relative)


func _play() -> Control:
	return _node("Play") as Control


func _decision() -> Control:
	return _node("Play/Prompt/Margin/Stack/DecisionScroll/Decision") as Control


func _status() -> Label:
	return _node("Status/Text") as Label


func _fail(message: String) -> void:
	if failed:
		return
	failed = true
	push_error(message + "\nVisible UI:\n" + (_visible_text(main) if main != null else "<none>"))
	quit(1)
