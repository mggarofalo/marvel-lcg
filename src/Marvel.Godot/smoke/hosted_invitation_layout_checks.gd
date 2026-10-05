extends RefCounted

const CaptionBounds = preload("res://smoke/local_game_smoke_caption_bounds.gd")

# A real unused seat invitation must not enlarge the desktop sidebar envelope.
static func check(driver: Node) -> bool:
	var main: Control = driver.host
	var viewport: SubViewport = driver.host_viewport
	if viewport.size.x < 1920:
		return true
	var initial_size := viewport.size
	var scale := main.get_node("StatusBar/InterfaceScale") as HSlider
	var initial_scale := scale.value
	viewport.size = Vector2i(1920, 1080)
	for percent in [100, 150]:
		scale.value = percent
		for expanded in [false, true]:
			if not await _history(driver, expanded):
				return false
			for frame in 12:
				await driver.get_tree().process_frame
			for problem in problems(main):
				driver._fail("unused invitation at%d: %s" % [percent, problem])
				return false
	if not await _history(driver, false):
		return false
	scale.value = initial_scale
	viewport.size = initial_size
	for frame in 12:
		await driver.get_tree().process_frame
	return true


static func _history(driver: Node, expanded: bool) -> bool:
	var log := driver.host.find_child("EventLog", true, false) as Control
	var toggle := driver.host.find_child("ToggleHistory", true, false) as Button
	if log == null or toggle == null:
		driver._fail("unused invitation table has no history controls")
		return false
	if log.visible != expanded:
		return await driver._pointer_activate(toggle)
	return true


static func problems(main: Control) -> Array[String]:
	var failures: Array[String] = []
	var prompt := main.get_node("Margin/Shell/Content/Play/Prompt") as Control
	var viewport := Rect2(Vector2.ZERO, main.get_viewport_rect().size)
	var rect := prompt.get_global_rect()
	if rect.position.x < -0.5 or rect.end.x > viewport.end.x + 0.5:
		failures.append("sidebar extends beyond the window: %s / %s" % [rect, viewport])
	var offer := prompt.find_child("InvitationOffer", true, false) as Control
	if offer != null and offer.is_visible_in_tree():
		var invitation := offer.get_global_rect()
		var toolbar := main.get_node("StatusBar") as Control
		if invitation.position.y < toolbar.get_global_rect().end.y - 0.5 \
				or invitation.end.y > viewport.end.y + 0.5:
			failures.append("invitation extends outside the table envelope: %s" % invitation)
	failures.append_array(CaptionBounds.problems(main))
	return failures
