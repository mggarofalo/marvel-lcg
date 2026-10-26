extends "res://smoke/local_game_smoke.gd"

# Synthetic delayed discovery proves the native smoke waits for usable setup,
# and reports incomplete choice rows as failures without aborting its coroutine.
var published := false
var expected_failure := false
var reported_failure := ""


func _initialize() -> void:
	_run.call_deferred()


func _run() -> void:
	main = Control.new()
	root.add_child(main)
	var layout := VBoxContainer.new()
	layout.add_theme_constant_override("separation", 0)
	main.add_child(layout)
	var choices := Control.new()
	choices.custom_minimum_size = Vector2(200, 20)
	layout.add_child(choices)
	var start := Button.new()
	start.text = "Start game"
	start.disabled = true
	layout.add_child(start)
	_publish_setup(start, choices)
	if not await _wait_for_setup() or not published:
		super._fail("setup inspection began before delayed discovery completed")
		return
	if start.position.y != 120 or start.size.x != 300:
		super._fail("setup inspection began before deferred choice layout settled")
		return
	var parent: Node = main
	for part in ["Margin", "Shell", "Content"]:
		var child := Control.new()
		child.name = part
		parent.add_child(child)
		parent = child
	var hero := OptionButton.new()
	hero.name = "Hero"
	parent.add_child(hero)
	if not await _missing_choice_is_reported("Hero"):
		return
	var hero_choices := HFlowContainer.new()
	hero_choices.name = "HeroChoices"
	parent.add_child(hero_choices)
	if not await _missing_choice_is_reported("Hero"):
		return
	if not await _missing_choice_is_reported("Absent"):
		return
	if not await _late_setup_layout_is_visible():
		return
	print("SETUP_READINESS_SMOKE_OK")
	quit(0)


func _publish_setup(start: Button, choices: Control) -> void:
	for _frame in range(6):
		await process_frame
	published = true
	start.disabled = false
	_expand_choices.call_deferred(choices)


func _expand_choices(choices: Control) -> void:
	choices.custom_minimum_size = Vector2(250, 80)
	await process_frame
	choices.custom_minimum_size = Vector2(300, 120)


func _missing_choice_is_reported(path: String) -> bool:
	expected_failure = true
	reported_failure = ""
	var accepted := await _setup_control_is_visible(null, path)
	expected_failure = false
	if accepted or not reported_failure.contains("has no visible choice control"):
		super._fail("incomplete setup did not report its missing choice: " + path)
		return false
	return true


func _late_setup_layout_is_visible() -> bool:
	main.queue_free()
	await process_frame
	main = Control.new()
	root.add_child(main)
	render_viewport = root
	root.size = Vector2i(1920, 1080)
	var page := ScrollContainer.new()
	page.name = "Margin"
	page.position = Vector2(20, 20)
	page.size = Vector2(600, 300)
	page.follow_focus = true
	main.add_child(page)
	var shell := PanelContainer.new()
	shell.name = "Shell"
	page.add_child(shell)
	var content := VBoxContainer.new()
	content.name = "Content"
	shell.add_child(content)
	var preceding := Control.new()
	preceding.custom_minimum_size = Vector2(400, 700)
	content.add_child(preceding)
	var target := Button.new()
	target.name = "Seed"
	target.text = "Synthetic setup target"
	target.custom_minimum_size = Vector2(400, 66)
	content.add_child(target)
	var following := Control.new()
	following.custom_minimum_size = Vector2(400, 400)
	content.add_child(following)
	await process_frame
	await process_frame
	_expand_during_reveal(preceding)
	if not await _setup_control_is_visible(page, "Seed"):
		return false
	var ui = preload("res://smoke/automation/ui_driver.gd").new(main, root, _fail)
	var locator = ui.named("Seed")
	if not await ui.expect_fully_visible_enabled(locator): return false
	var clicks := [0]
	target.pressed.connect(func() -> void: clicks[0] += 1)
	if not await ui.click(locator): return false
	if clicks[0] != 1:
		super._fail("settled setup target did not receive exactly one native click")
		return false
	# A clickable exposed edge cannot satisfy the complete-target requirement.
	page.size.x = 200
	if not await _clipped_setup_is_reported(page): return false
	page.size = Vector2(600, 50)
	if not await _clipped_setup_is_reported(page): return false
	return true


func _expand_during_reveal(preceding: Control) -> void:
	await process_frame
	await process_frame
	preceding.custom_minimum_size.y += 200


func _clipped_setup_is_reported(page: ScrollContainer) -> bool:
	expected_failure = true
	reported_failure = ""
	var accepted := await _setup_control_is_visible(page, "Seed")
	expected_failure = false
	if accepted or not reported_failure.contains("cannot be brought into the viewport; target="):
		super._fail("inaccessible setup target was accepted or lacked geometry diagnostics")
		return false
	return true


func _fail(message: String) -> void:
	if expected_failure:
		reported_failure = message
	else:
		super._fail(message)
