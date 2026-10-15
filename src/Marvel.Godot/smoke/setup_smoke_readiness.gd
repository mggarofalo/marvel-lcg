extends RefCounted


static func wait_for_start(main: Control) -> Button:
	var started := Time.get_ticks_msec()
	var previous: Button
	var bounds := Rect2()
	var minimum := Vector2.ZERO
	var stable_frames := 0
	while Time.get_ticks_msec() - started < 15000:
		await main.get_tree().process_frame
		var candidate := _enabled_start(main)
		if candidate == null:
			previous = null
			stable_frames = 0
			continue
		var current_bounds := candidate.get_global_rect()
		var current_minimum := candidate.get_combined_minimum_size()
		if candidate == previous and current_bounds == bounds and current_minimum == minimum:
			stable_frames += 1
		else:
			stable_frames = 0
		# Discovery publishes choices before deferred container sorting. Observe
		# unchanged hit geometry across two subsequent layout frames before input.
		if stable_frames >= 2:
			return candidate
		previous = candidate
		bounds = current_bounds
		minimum = current_minimum
	return null


static func _enabled_start(main: Control) -> Button:
	for candidate in main.find_children("*", "Button", true, false):
		if candidate.text == "Start game" and candidate.is_visible_in_tree() and not candidate.disabled:
			return candidate as Button
	return null
