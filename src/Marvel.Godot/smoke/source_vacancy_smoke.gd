extends "res://smoke/source_tableau_smoke.gd"

# Synthetic seven-source layout fixture; arrangement changes only local presentation.
func _run() -> void:
	var saved := _prepare_fixture()
	var fixture_path: String = saved[0]
	var original_text: String = saved[1]
	root.size = Vector2i(1920, 1080)
	var main := (load("res://Main.tscn") as PackedScene).instantiate()
	root.add_child(main)
	for frame in range(8): await process_frame
	var output := FileAccess.open(fixture_path, FileAccess.WRITE)
	output.store_string(original_text)
	output.close()
	var fixture := main.find_child("TableauFixture", true, false) as Control
	fixture.find_child("FixtureContext", true, false).text = "SYNTHETIC SEVEN-SOURCE VACANCY FIXTURE"
	var tableau := fixture.find_child("SourceTableau", true, false) as Control
	var grid := tableau.find_child("SourceTableauGrid", true, false) as GridContainer
	var viewport := tableau.find_child("SourceTableauViewport", true, false) as ScrollContainer
	var failures: Array[String] = []
	var slots := grid.get_children()
	var vacancy: Control
	for slot in slots:
		if str(slot.name).begins_with("EmptySourceSlot"):
			vacancy = slot
			break
	if vacancy == null:
		failures.append("production renderer offers no vacant destination")
	else:
		await _check_vacancy(grid, viewport, vacancy, fixture, failures)
	await _capture()
	for failure in failures: push_error("SOURCE_VACANCY_FAILED " + failure)
	if failures.is_empty(): print("SOURCE_VACANCY_OK pointer=true keyboard=true cancel=true input_passthrough=true")
	quit(0 if failures.is_empty() else 1)

func _check_vacancy(grid: GridContainer, viewport: ScrollContainer, vacancy: Control, fixture: Control, failures: Array[String]) -> void:
	if vacancy.mouse_filter != Control.MOUSE_FILTER_IGNORE:
		failures.append("vacant source destination intercepts gameplay pointer input")
	viewport.ensure_control_visible(vacancy)
	for frame in range(3): await process_frame
	var source := grid.get_child(max(0, vacancy.get_index() - 1)) as Control
	var card := source.find_child("ProceduralCard*", true, false) as Control
	var original := grid.get_children()
	var index := vacancy.get_index()
	var point := vacancy.get_global_rect().get_center()
	var selected := int(fixture.get_meta("selected_id", -1))
	await _source_grip_drag(card, point, true, failures)
	if grid.get_children() != original: failures.append("vacant destination cancellation changed source order")
	await _source_grip_drag(card, point, false, failures)
	if grid.get_child(index) != source or grid.get_child(index - 1) != vacancy:
		failures.append("pointer placement failed to preserve the vacant source slot")
	var grip := card.find_child("SourceReorderGrip", true, false) as Button
	grip.grab_focus()
	await _source_key(KEY_SPACE)
	await _source_key(KEY_LEFT)
	if grid.get_child(index - 1) != source: failures.append("keyboard lacks vacant destination preview")
	await _source_key(KEY_ESCAPE)
	if grid.get_child(index) != source: failures.append("keyboard vacancy cancellation failed to restore order")
	await _source_key(KEY_SPACE)
	await _source_key(KEY_LEFT)
	await _source_key(KEY_ENTER)
	if grid.get_child(index - 1) != source or grid.get_child(index) != vacancy:
		failures.append("keyboard failed to place into a vacant destination")
	if int(fixture.get_meta("selected_id", -1)) != selected:
		failures.append("presentation arrangement dispatched a gameplay action")

func _integer_json(value: Variant) -> Variant:
	if value is float: return int(value)
	if value is Array:
		return value.map(_integer_json)
	if value is Dictionary:
		for key in value: value[key] = _integer_json(value[key])
	return value

func _prepare_fixture() -> Array:
	var path := ""
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--marvel-tableau="): path = arg.trim_prefix("--marvel-tableau=")
	var text := FileAccess.get_file_as_string(path)
	var areas: Array = JSON.parse_string(text)
	for area in areas:
		if int(area.Seat) != 0: continue
		if area.Zone == "UpgradesArea": area.Cards = area.Cards.slice(0, 7)
		if area.Zone == "SupportsArea": area.Cards = []
	var output := FileAccess.open(path, FileAccess.WRITE)
	output.store_string(JSON.stringify(_integer_json(areas)))
	output.close()
	return [path, text]
