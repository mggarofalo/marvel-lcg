extends RefCounted


static func settle(page: ScrollContainer, control: Control, smoke: SceneTree,
		minimum_extent: float, observations: Array[String]) -> bool:
	var previous := Rect2()
	var previous_page := Rect2()
	var stable_frames := 0
	# Setup choice rows and focus restoration sort asynchronously. Recompute the
	# reveal after each layout frame, then require two unchanged full hit areas.
	# An inaccessible control still fails within this short frame budget.
	for _frame in range(12):
		await control.get_tree().process_frame
		var bounds := control.get_global_rect()
		var page_bounds := page.get_global_rect()
		observations.append("%d target=%s page=%s scroll=%s" % [_frame, bounds, page_bounds, page.scroll_vertical])
		if control.is_visible_in_tree() and smoke._control_is_fully_visible(control) \
				and bounds.size.x >= minimum_extent and bounds.size.y >= minimum_extent \
				and bounds == previous and page_bounds == previous_page:
			stable_frames += 1
			if stable_frames >= 2: return true
		else:
			stable_frames = 0
		previous = bounds
		previous_page = page_bounds
		page.ensure_control_visible(control)
	return false
