extends "res://smoke/local_game_smoke.gd"

# Synthetic review probe: these controls test reveal geometry, not gameplay.
const REVEAL := preload("res://smoke/setup_control_reveal.gd")
var moving_target: Control
var move_right := false


func _initialize() -> void:
	_run_review.call_deferred()


func _run_review() -> void:
	root.size = Vector2i(1920, 1080)
	render_viewport = root
	main = Control.new()
	root.add_child(main)
	var page := ScrollContainer.new()
	page.position = Vector2(200, 100)
	page.size = Vector2(600, 300)
	main.add_child(page)
	var content := Control.new()
	content.custom_minimum_size = Vector2(1500, 1200)
	page.add_child(content)
	var clip := Control.new()
	clip.position = Vector2(900, 700)
	clip.size = Vector2(220, 120)
	clip.clip_contents = true
	content.add_child(clip)
	var target := Button.new()
	target.name = "ReviewTarget"
	target.text = "Synthetic review target"
	target.size = Vector2(180, 66)
	clip.add_child(target)
	var observations: Array[String] = []
	if not await REVEAL.settle(page, target, self, 44.0, observations):
		_fail("offset two-axis reveal failed: " + "\n".join(observations))
		return
	if page.scroll_horizontal <= 0 or page.scroll_vertical <= 0:
		_fail("two-axis fixture did not exercise both scroll corrections")
		return
	var ui = preload("res://smoke/automation/ui_driver.gd").new(main, root, _fail)
	var clicks := [0]
	target.pressed.connect(func() -> void: clicks[0] += 1)
	var locator = ui.named("ReviewTarget")
	if not await ui.expect_fully_visible_enabled(locator): return
	if not await ui.click(locator): return
	if clicks[0] != 1:
		_fail("two-axis revealed target did not receive exactly one viewport click")
		return
	if not await _reject_unusable_geometry(page, clip, target): return
	print("SETUP_VIEWPORT_REVIEW_OK")
	quit(0)


func _reject_unusable_geometry(page: ScrollContainer, clip: Control, target: Control) -> bool:
	var observations: Array[String] = []
	clip.size.x = 80
	if await REVEAL.settle(page, target, self, 44.0, observations):
		_fail("nested clipping ancestor was ignored")
		return false
	if observations.size() != 12:
		_fail("unreachable target did not exhaust the bounded diagnostic samples")
		return false
	clip.size.x = 220
	moving_target = target
	process_frame.connect(_move_review_target)
	observations.clear()
	var moving_accepted := await REVEAL.settle(page, target, self, 44.0, observations)
	process_frame.disconnect(_move_review_target)
	if moving_accepted or observations.size() != 12:
		_fail("continuously moving geometry was accepted or escaped the frame budget")
		return false
	observations.clear()
	if not await REVEAL.settle(page, target, self, 44.0, observations):
		_fail("target did not become usable after motion stopped")
		return false
	return true


func _move_review_target() -> void:
	move_right = not move_right
	moving_target.position.x = 2.0 if move_right else 1.0
