extends "res://smoke/local_game_smoke.gd"

var arrival_samples: Array = []
var sampled_faces := {}
var arrival_images: Array[Image] = []

func _process(delta: float) -> bool:
	if not is_instance_valid(main): return false
	for card in main.find_children("ActivityArrival*", "Control", true, false):
		if card.is_visible_in_tree():
			arrival_samples.append({"milliseconds": Time.get_ticks_msec(), "delta": delta, "card": str(card.name), "alpha": card.modulate.a, "x": card.position.x, "y": card.position.y})
			if card.modulate.a > 0.1 and card.modulate.a < 0.95:
				sampled_faces[str(card.name)] = true
				if arrival_images.size() < 3 and DisplayServer.get_name() != "headless" \
						and not OS.get_environment("MARVEL_SMOKE_CAPTURE_DIR").is_empty():
					arrival_images.append(render_viewport.get_texture().get_image())
	return false

func _run() -> void:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _wait_for(func(): return _task_commit() != null): return
	if not await _select_arrival_replacements(): return
	if not await _commit_once("Redraw 2"): return
	if sampled_faces.size() != 2:
		_fail("two-card mulligan lacked simultaneous native arrival frames: " + str(sampled_faces))
		return
	_save_arrival_frames()
	if not await _capture_checkpoint("arrival-settled"): return
	print("CAUSAL_ARRIVAL_SMOKE_OK authorized_arrivals=true simultaneous=2 intermediate_frames=", arrival_samples.size())
	main.queue_free()
	await process_frame
	await process_frame
	quit(0)

func _select_arrival_replacements() -> bool:
	for title in ["Avengers Mansion", "Aunt May"]:
		var card := _mulligan_card(title)
		if card == null:
			_fail("seeded mulligan omitted " + title)
			return false
		if not await _keyboard_stage_card(card): return false
	return true

func _save_arrival_frames() -> void:
	var folder := OS.get_environment("MARVEL_SMOKE_CAPTURE_DIR")
	if not folder.is_empty():
		var log := FileAccess.open(folder.path_join("arrival-frames.json"), FileAccess.WRITE)
		log.store_string(JSON.stringify(arrival_samples))
		log.close()
		for index in range(arrival_images.size()): arrival_images[index].save_png(folder.path_join("arrival-native-%d.png" % index))
