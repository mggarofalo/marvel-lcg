extends "res://smoke/local_game_smoke.gd"

const Counter = preload("res://smoke/theme_notification_counter.gd")
const Lifetime = preload("res://smoke/local_game_smoke_style_lifetime.gd")

func _run() -> void:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	var scale := main.get_node("StatusBar/InterfaceScale") as HSlider
	if not await _scale_palette_is_stable(scale): return
	scale.value = _scale_percentage()
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _wait_for(func() -> bool: return _task_commit() != null): return
	var cards := main.find_children("ProceduralCard*", "Control", true, false)
	if not _opening_cards_share_theme(cards): return
	if not _card_overrides_are_local(cards): return
	var count := cards.size()
	if not await _table_resources_follow_scene_lifetimes(): return
	print("THEME_PALETTE_SMOKE_OK scales=11 opening_cards=%d simultaneous_tables=2" % count)
	quit(0)


func _scale_palette_is_stable(scale: HSlider) -> bool:
	var observer := Counter.new()
	observer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	main.get_node("Margin/Shell").add_child(observer)
	var identities: Dictionary = {}
	for value in range(50, 151, 10):
		scale.value = value
		await process_frame
		identities[value] = main.theme.get_instance_id()
	for value in range(150, 49, -10):
		scale.value = value
		await process_frame
		if main.theme.get_instance_id() != identities[value]:
			_fail("returning to a reading scale replaces its theme resource")
			return false
	var before: int = observer.theme_changes
	scale.value_changed.emit(scale.value)
	await process_frame
	if observer.theme_changes != before:
		_fail("reapplying the current scale repeats ancestor theme notifications")
		return false
	return true


func _opening_cards_share_theme(cards: Array[Node]) -> bool:
	var card_theme_id := 0
	for card in cards:
		if card_theme_id == 0: card_theme_id = card.theme.get_instance_id()
		elif card.theme.get_instance_id() != card_theme_id:
			_fail("physical cards at the same reading scale use different themes")
			return false
	if cards.size() < 6:
		_fail("theme comparison did not reach the actual opening hand")
		return false
	return true


func _card_overrides_are_local(cards: Array[Node]) -> bool:
	var first := cards[0] as Control
	var second := cards[1] as Control
	var neighbor_size := second.get_theme_font_size("font_size", "Label")
	first.add_theme_font_size_override("font_size", neighbor_size + 3)
	if second.get_theme_font_size("font_size", "Label") != neighbor_size:
		_fail("a card-local type override changes another card sharing its scale")
		return false
	return true


func _table_resources_follow_scene_lifetimes() -> bool:
	var styles := Lifetime.observe(main)
	var themes := Lifetime.observe_themes(main)
	if styles.is_empty() or themes.size() != 2:
		_fail("the native lifetime check cannot observe the actual table resources")
		return false
	var retained_theme: WeakRef = weakref(main.theme)
	var shared_id := main.theme.get_instance_id()
	var neighbor := (load("res://Main.tscn") as PackedScene).instantiate() as Control
	render_viewport.add_child(neighbor)
	neighbor.hide()
	await process_frame
	if neighbor.theme.get_instance_id() != shared_id:
		_fail("simultaneous table scenes cannot reuse their common reading scale")
		return false
	main.queue_free()
	await process_frame
	await process_frame
	if retained_theme.get_ref() == null:
		_fail("closing one table retires a theme still used by another table")
		return false
	neighbor.queue_free()
	await process_frame
	await process_frame
	if retained_theme.get_ref() != null:
		_fail("the last table's shared theme remains owned after both scenes close")
		return false
	var failures := Lifetime.problems(styles)
	failures.append_array(Lifetime.problems(themes, "shared scale theme"))
	if not failures.is_empty():
		_fail("; ".join(failures))
		return false
	return true
