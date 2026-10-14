extends "res://smoke/attack_activity_lifecycle_smoke.gd"

var motion_intervals: Array[Dictionary] = []
var motion_started := 0
var motion_caption := ""
var motion_caption_started := 0
var motion_frames := 0


func _process(_delta: float) -> bool:
	if not is_instance_valid(main): return false
	var staged := bool(main.get_meta("response_staged", false))
	var now := Time.get_ticks_msec()
	if staged:
		if motion_started == 0: motion_started = now
		var caption := main.find_child("TimelineCaption", true, false) as Label
		if caption != null and caption.text != motion_caption:
			_record_caption(now)
			motion_caption = caption.text
			motion_caption_started = now
		for card in main.find_children("ProceduralCard*", "Control", true, false):
			if not card.scale.is_equal_approx(Vector2.ONE):
				_fail("accepted activity scaled printed card text: " + str(card.name))
				return false
		motion_frames += 1
	elif motion_started > 0:
		_record_caption(now)
		motion_intervals.append({"staged_ms": now - motion_started})
		motion_started = 0
		motion_caption = ""
	return false


func _record_caption(now: int) -> void:
	if not motion_caption.is_empty():
		motion_intervals.append({"caption": motion_caption, "visible_ms": now - motion_caption_started})


func _run() -> void:
	if not await _legal_enemy_attack_is_causal(): return
	if not saw_attack_intro:
		_fail("no source/target introduction before attack consequences")
		return
	if not await _basic_attack_stages(): return
	if motion_enabled and motion_frames == 0:
		_fail("motion probe did not observe intermediate accepted activity frames")
		return
	var folder := OS.get_environment("MARVEL_SMOKE_CAPTURE_DIR")
	if not folder.is_empty():
		var output := FileAccess.open(folder.path_join("motion-intervals.json"), FileAccess.WRITE)
		output.store_string(JSON.stringify(motion_intervals, "\t"))
		output.close()
	print("MOTION_FINISH_SMOKE_OK stable_card_text=true attack_lifecycle=true frames=", motion_frames)
	quit(0)
