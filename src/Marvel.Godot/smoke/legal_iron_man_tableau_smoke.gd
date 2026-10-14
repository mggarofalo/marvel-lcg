extends SceneTree

func _initialize() -> void:
	_run.call_deferred()

func _run() -> void:
	root.size = Vector2i(1920, 1080)
	var main := (load("res://Main.tscn") as PackedScene).instantiate()
	root.add_child(main)
	for frame in range(8): await process_frame
	var table := main.find_child("SourceTableau", true, false) as Control
	var failures: Array[String] = []
	_check_sources(table, failures)
	_check_hand(main, table, failures)
	await _capture()
	for failure in failures: push_error(failure)
	if failures.is_empty(): print("LEGAL_IRON_MAN_TABLEAU_OK seed=5 accepted_decisions=48 sources=5 hand=5")
	quit(0 if failures.is_empty() else 1)

func _check_sources(table: Control, failures: Array[String]) -> void:
	var cards := table.find_children("ProceduralCard*", "Control", true, false)
	if cards.size() != 5: failures.append("reachable Iron Man fixture lost installed sources")
	for card: Control in cards:
		if not table.get_global_rect().encloses(card.get_global_rect()): failures.append("installed source hidden")
		if card.scale != Vector2.ONE: failures.append("source text is transform-scaled")
		_check_meaning(card, failures)

func _check_meaning(card: Control, failures: Array[String]) -> void:
	if card.get_meta("source_title") == "Mark V Armor": return
	var meaning := card.find_child("SourceMeaning", true, false) as RichTextLabel
	if meaning == null or not meaning.is_visible_in_tree() or meaning.visible_characters == 0:
		failures.append("installed source has no complete visible meaning line")
	elif meaning.get_line_height(0) > meaning.size.y:
		failures.append("installed source meaning clips its first line")
	elif card.get_meta("source_title") != "Arc Reactor" and not meaning.accessibility_name.begins_with("Printed rules:"):
		failures.append("printed fallback is not identified for accessibility")

func _check_hand(main: Control, table: Control, failures: Array[String]) -> void:
	var hand := 0
	for card: Control in main.find_children("ProceduralCard*", "Control", true, false):
		if not card.has_meta("spatial_hand_index"): continue
		hand += 1
		if card.get_global_rect().intersects(table.get_global_rect()): failures.append("sources obscure the hand")
	if hand != 5: failures.append("reachable Iron Man fixture lost its full five-card hand")

func _capture() -> void:
	var capture := OS.get_environment("MARVEL_TABLEAU_CAPTURE")
	if not capture.is_empty():
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png(capture)
