extends "res://smoke/local_game_smoke.gd"

# A real seeded Core opening. Shared setup supplies the engine-backed client;
# the driver owns input, while assertions observe visible UI and accepted revision.
const Driver = preload("res://smoke/automation/ui_driver.gd")
var ui: RefCounted

func _run() -> void:
    if not await _open_setup(load("res://Main.tscn") as PackedScene): return
    await _configure_seeded_game()
    ui = Driver.new(main, render_viewport, _fail)
    if not await ui.click(ui.button("Start game")): return
    if not await ui.expect_visible(ui.button("Keep hand")): return
    if not await ui.expect(func() -> bool: return _revision() == 0,
            "opening mulligan has no committed decisions"): return
    if not await ui.click(ui.button("Keep hand")): return
    if not await ui.expect(func() -> bool: return _revision() == 1,
            "keeping the hand commits exactly one decision"): return
    if not await ui.expect_visible(ui.button("End turn")): return
    if not await ui.expect(func() -> bool: return ui.button("Keep hand").all().is_empty(),
            "mulligan commitment is no longer offered"): return
    if not await _release_table_resources(): return
    print("UI_ACCEPTANCE_OK opening_hand revision=1")
    quit(0)

func _revision() -> int:
    return int((_node("Toolbar/SyncStatus") as Label).get_meta("response_revision", -1))
