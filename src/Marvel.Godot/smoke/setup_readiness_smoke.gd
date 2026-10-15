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
	var start := Button.new()
	start.text = "Start game"
	start.disabled = true
	main.add_child(start)
	_publish_setup(start)
	if not await _wait_for_setup() or not published:
		super._fail("setup inspection began before delayed discovery completed")
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
	var choices := HFlowContainer.new()
	choices.name = "HeroChoices"
	parent.add_child(choices)
	if not await _missing_choice_is_reported("Hero"):
		return
	if not await _missing_choice_is_reported("Absent"):
		return
	print("SETUP_READINESS_SMOKE_OK")
	quit(0)


func _publish_setup(start: Button) -> void:
	await process_frame
	await process_frame
	published = true
	start.disabled = false


func _missing_choice_is_reported(path: String) -> bool:
	expected_failure = true
	reported_failure = ""
	var accepted := await _setup_control_is_visible(null, path)
	expected_failure = false
	if accepted or not reported_failure.contains("has no visible choice control"):
		super._fail("incomplete setup did not report its missing choice: " + path)
		return false
	return true


func _fail(message: String) -> void:
	if expected_failure:
		reported_failure = message
	else:
		super._fail(message)
