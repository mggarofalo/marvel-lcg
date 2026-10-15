extends RefCounted


static func wait_for_start(main: Control) -> Button:
	var started := Time.get_ticks_msec()
	while Time.get_ticks_msec() - started < 15000:
		for candidate in main.find_children("*", "Button", true, false):
			if candidate.text == "Start game" and candidate.is_visible_in_tree() and not candidate.disabled:
				return candidate as Button
		await main.get_tree().process_frame
	return null
