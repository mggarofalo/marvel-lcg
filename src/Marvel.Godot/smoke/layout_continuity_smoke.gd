extends "res://smoke/local_game_smoke.gd"

func _run() -> void:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _wait_for(func() -> bool: return main.find_child("CausalSituation", true, false) != null): return
	for frame in range(8): await process_frame
	var cause := main.find_child("CausalSituation", true, false) as Control
	var hand := main.find_child("HandDropArea", true, false) as Control
	if cause.get_global_rect().intersects(hand.get_global_rect()):
		_fail("causal context overlaps hand")
		return
	if not await _draft_summary_remains_reachable(): return
	if not await _seats_remain_reachable(): return
	if not await _capture_checkpoint("stable-layout-two-seat"): return
	if not await _release_table_resources(): return
	print("LAYOUT_CONTINUITY_SMOKE_OK")
	quit(0)

func _draft_summary_remains_reachable() -> bool:
	var viewport := main.find_child("DraftSummaryViewport", true, false) as ScrollContainer
	var summary := main.find_child("DraftProgress", true, false) as Label
	if viewport == null or summary == null:
		_fail("draft summary lacks a bounded viewport")
		return false
	var original := summary.text
	# Synthetic copy stress checks geometry without inventing an engine decision.
	summary.text = "Synthetic draft summary\nKnown cost\nChosen target\nKnown consequence\nFinal consequence remains reachable"
	for frame in range(5): await process_frame
	if viewport.get_global_rect().end.y > _viewport_size().y + 1:
		_fail("draft consequences extend below the screen: " + str(viewport.get_global_rect()) + " root " + str(_viewport_size()))
		return false
	viewport.scroll_vertical = int(viewport.get_v_scroll_bar().max_value)
	for frame in range(3): await process_frame
	if summary.get_global_rect().end.y > viewport.get_global_rect().end.y + 1:
		_fail("last draft consequence cannot be reached by scrolling")
		return false
	summary.text = original
	viewport.scroll_vertical = 0
	return true

func _seats_remain_reachable() -> bool:
	var seats := main.find_child("SeatSummaryViewport", true, false) as ScrollContainer
	if OS.get_environment("MARVEL_SMOKE_TWO_PLAYER") == "true":
		if seats == null:
			_fail("two seats lack bounded viewport")
			return false
		var buttons := seats.find_children("SeatSwitch*", "Button", true, false)
		if buttons.size() != 2:
			_fail("lost a public seat")
			return false
		for button in buttons:
			seats.ensure_control_visible(button)
			for frame in range(3): await process_frame
			if not seats.get_global_rect().intersects(button.get_global_rect()):
				_fail("seat unreachable through continuous scroll")
				return false
		for pile in main.find_children("Pile*", "PanelContainer", true, false):
			if seats.get_global_rect().intersects(pile.get_global_rect()):
				_fail("seat summaries overlap a pile")
				return false
		if not await _cooperative_seat_switch_is_safe(): return false
	return true
