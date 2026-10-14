extends "res://smoke/source_tableau_smoke.gd"

# Synthetic density fixture: use the actual painted grip, icons, title and idle body.
func _run() -> void:
	root.size = Vector2i(1920, 1080)
	var main := (load("res://Main.tscn") as PackedScene).instantiate()
	root.add_child(main)
	for frame in range(8): await process_frame
	var fixture := main.find_child("TableauFixture", true, false) as Control
	var tableau := fixture.find_child("SourceTableau", true, false) as Control
	var grid := tableau.find_child("SourceTableauGrid", true, false) as GridContainer
	var viewport := tableau.find_child("SourceTableauViewport", true, false) as ScrollContainer
	var failures: Array[String] = []
	var attempts := 0
	for slot in grid.get_children():
		var card := slot.find_child("ProceduralCard*", true, false) as Control
		if card == null: continue
		viewport.ensure_control_visible(card)
		for frame in range(3): await process_frame
		attempts += await _check_card(card, grid, fixture, failures)
	if attempts < 48: failures.append("too few real title/icon/body drag attempts: " + str(attempts))
	for failure in failures: push_error("SOURCE_DRAG_HIT_FAILED " + failure)
	if failures.is_empty(): print("SOURCE_DRAG_HIT_OK attempts=" + str(attempts) + " all_destinations=true body_click_inspects=true")
	quit(0 if failures.is_empty() else 1)

func _check_card(card: Control, grid: GridContainer, fixture: Control, failures: Array[String]) -> int:
	var attempts := 0
	for region in ["SourceTitle", "SourceType", "SourceState", "SourceMeaning", "Card*Action"]:
		var surface := card.find_child(region, true, false) as Control
		if surface == null or not surface.is_visible_in_tree(): continue
		await _check_region(card, surface, grid, fixture, failures)
		attempts += 1
	return attempts

func _check_region(card: Control, region: Control, grid: GridContainer, fixture: Control, failures: Array[String]) -> void:
	var original := grid.get_children()
	var inspected := int(fixture.get_meta("inspected_id", -1))
	var selected := int(fixture.get_meta("selected_id", -1))
	var start := region.get_global_rect().get_center()
	var initial_position := card.global_position
	# Stay in the source slot; capture and outlines must exist before reaching another slot.
	var finish := start + Vector2(12, 0)
	await _motion(start)
	await _pointer(start, true)
	await _motion(finish)
	if not card.global_position.is_equal_approx(initial_position + finish - start):
		failures.append(str(region.name) + " did not preserve the pointer grab point")
	if not card.is_set_as_top_level(): failures.append(str(card.name) + "/" + str(region.name) + " did not begin arrangement")
	for slot in original:
		var outline := slot.find_child("SourceSwapOutline", false, false) as Control
		if outline == null or not outline.is_visible_in_tree():
			failures.append(str(region.name) + " failed to reveal all drop destinations")
			break
	await _source_key(KEY_ESCAPE)
	await _pointer(finish, false)
	await _wait_for_source_slot(card, failures)
	if grid.get_children() != original or card.is_set_as_top_level(): failures.append("cancel failed for " + str(region.name))
	if int(fixture.get_meta("selected_id", -1)) != selected or int(fixture.get_meta("inspected_id", -1)) != inspected:
		failures.append("drag dispatched action/inspection from " + str(region.name))
	await _click(region.get_global_rect().get_center())
	var identity := int(str(card.name).trim_prefix("ProceduralCard"))
	var result_key := "selected_id" if str(region.name).begins_with("Card") else "inspected_id"
	if int(fixture.get_meta(result_key, -1)) != identity:
		failures.append("click failed to inspect from " + str(region.name))
