extends "res://smoke/local_game_smoke.gd"

# Real seeded Core setup; preparation must retire without creating a game or input owner.
func _run() -> void:
	if not await _open_setup(load("res://Main.tscn") as PackedScene): return
	await process_frame
	await process_frame
	if main.find_child("SourceControlsPreparing", true, false) != null \
			or int((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1)) != -1:
		_fail("source preparation retained a scene subtree or published a game before Start")
		return
	await _configure_seeded_game()
	if not await _pointer_activate(_button_named("Start game")): return
	if not await _wait_for(func() -> bool: return _task_commit() != null): return
	if not await _select_mulligan_cards() or not await _submit_mulligan(): return
	if not await _direct_web_shooter_is_played(): return
	print("SOURCE_PREPARATION_OK retired=true explicit_start=true legal_source_play=true source_inspection=true")
	if not await _release_table_resources(): return
	quit(0)
