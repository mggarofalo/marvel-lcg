extends "res://smoke/automation/wave_native_playable.gd"

# Public Ant-Man starter and Rhino, seed 1. Setup and every decision use viewport input.
var witness = preload("res://smoke/NativeGameWitness.cs").new()
var identity := -1
var rhino := -1
var rhino_health := 0

func _run() -> void:
    var evidence := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
    for store in ["MARVEL_CAMPAIGN_ACCESS_DIRECTORY", "MARVEL_DECK_LIBRARY_DIRECTORY"]:
        OS.set_environment(store, evidence.path_join(store))
    if DisplayServer.get_name() != "headless":
        OS.set_environment("MARVEL_SMOKE_CAPTURE_DIR", evidence)
    if not await _open_setup(load("res://Main.tscn") as PackedScene): return
    ui = Driver.new(main, render_viewport, _fail)
    if not await _public_setup(): return
    if not await _opening_hand(): return
    if not await _choose_giant(): return
    if not await _giant_response(): return
    if not await _release_table_resources(): return
    print("UI_ACCEPTANCE_OK ant_man_public seed=1 form=giant response=rhino")
    quit(0)

func _public_setup() -> bool:
    var tiles: Array = main.find_children("*", "Button", true, false).filter(func(n):
        return n.is_visible_in_tree() and n.text == "Ant-Man" and "Second" not in str(n.get_path()))
    if not await ui.expect(func(): return tiles.size() == 1, "one first-seat public Ant-Man tile"): return false
    if not await ui.click(Locator.control(tiles[0])): return false
    if not await _choose_setup("Scenario", "Rhino"): return false
    if not await _choose_setup("Mode", "Standard"): return false
    if not await _seed_setup(): return false
    if not await _capture_checkpoint("ant-man-public-setup"): return false
    if not await ui.click(ui.button("Start game")): return false
    witness.Bind(main)
    return true

func _seed_setup() -> bool:
    if not await ui.click(ui.button("Connection and seed")): return false
    var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
    if not await ui.click(Locator.control(seed)): return false
    if not await ui.press_key(KEY_A, false, true): return false
    for pressed in [true, false]:
        var event := InputEventKey.new()
        event.keycode = KEY_1
        event.unicode = 49
        event.pressed = pressed
        render_viewport.push_input(event, true)
    await process_frame
    var hero := _node("Setup/Selections/Fields/Grid/Hero") as OptionButton
    if not await ui.expect(func(): return seed.text == "1" and hero.get_item_text(hero.selected) == "Ant-Man",
        "public selection retains Ant-Man and typed seed"): return false
    return await _close_seed_options()

func _close_seed_options() -> bool:
    var button = ui.button("Connection and seed")
    for step in range(40):
        if button.all().size() == 1 and render_viewport.gui_get_focus_owner() == button.all()[0]:
            return await ui.press_key(KEY_ENTER)
        if not await ui.press_key(KEY_TAB): return false
    return await ui.expect(func(): return false, "keyboard reaches connection and seed", 0)

func _opening_hand() -> bool:
    if not await ui.expect_visible(ui.button("Keep hand")): return false
    if not await ui.expect(func(): return witness.Revision() == 0 and witness.HandCount() == 6,
        "Scott Lang starts with six cards and no accepted decision"): return false
    identity = witness.Anchor("Scott Lang")
    rhino = witness.AreaAnchor("Rhino", "VillainArea")
    if not await ui.expect(func(): return identity >= 0 and rhino >= 0 and witness.Face(identity) == "12001b",
        "public setup displays Scott Lang on one physical identity"): return false
    if not await _capture_checkpoint("ant-man-opening-hand"): return false
    if not await ui.click(ui.button("Keep hand")): return false
    return await ui.expect(func(): return witness.Revision() == 1 and witness.DecisionKind() == "PlayerAction" and \
        witness.CanResolve() and not bool(main.get_meta("response_staged", false)),
        "keeping the hand commits once and the player-action table settles")

func _open_form_menu() -> bool:
    if not await ui.expect(func(): return _table_card(identity) != null, "Scott Lang card body is visible"): return false
    if not await ui.click(Locator.control(_table_card(identity)), MOUSE_BUTTON_RIGHT): return false
    var menu := main.find_child("CardContextMenu", true, false)
    if not await ui.expect(func(): return menu != null, "identity action menu opens"): return false
    var tiny = Locator.button(menu, "Change to Ant-Man (AVENGER, TINY)", false)
    var giant = Locator.button(menu, "Change to Ant-Man (AVENGER, GIANT)", false)
    if not await ui.expect_fully_visible_enabled(tiny): return false
    if not await ui.expect_fully_visible_enabled(giant): return false
    return await ui.expect(func(): return not tiny.all()[0].get_global_rect().intersects(giant.all()[0].get_global_rect()),
        "Tiny and Giant are distinct, nonoverlapping destination choices")

func _choose_giant() -> bool:
    print("ANT_MAN_ANCHORS identity=%d rhino=%d" % [identity, rhino])
    rhino_health = _rhino_health()
    if not await ui.expect(func(): return rhino_health > 0, "Rhino has a visible health baseline"): return false
    if not await _open_form_menu(): return false
    if not await _capture_checkpoint("ant-man-destinations"): return false
    if not await ui.press_key(KEY_ESCAPE): return false
    if not await ui.expect(func(): return main.find_child("CardContextMenu", true, false) == null and \
        witness.Revision() == 1 and witness.Face(identity) == "12001b", "dismissing the menu changes no game state"): return false
    if not await _open_form_menu(): return false
    if not await ui.click(_menu_choice("Change to Ant-Man (AVENGER, GIANT)")): return false
    return await ui.expect(func(): return witness.Revision() == 2 and witness.Face(identity) == "12001c" and \
        witness.Anchor("Ant-Man") == identity and witness.DecisionKind() == "Response",
        "one form commitment changes the same identity to Giant and opens its response")

func _giant_response() -> bool:
    if not await ui.expect(func(): return main.find_child("LocalDecisionWorkspace", true, false) != null and \
        witness.CanResolve() and not bool(main.get_meta("response_staged", false)), "optional entry response is visible"): return false
    var window := main.find_child("LocalDecisionWorkspace", true, false) as Control
    var copy := _visible_text(window)
    if not await ui.expect(func(): return "Ant-Man" in copy and "Player 1" in copy and "Pass this opportunity" in copy and \
        "Choose an enemy" in copy and "damage: 1" in copy, "optional response explains the answering seat and one damage"): return false
    if not await _capture_checkpoint("ant-man-giant-response"): return false
    var responses: Array = window.find_children("Affordance*", "Button", true, false).filter(func(n):
        return n.is_visible_in_tree() and "Ant-Man (Giant)" in n.text)
    if not await ui.expect(func(): return responses.size() == 1, "one offered Giant response"): return false
    if not await ui.click(Locator.control(responses[0])): return false
    if not await ui.expect(func(): return witness.Revision() == 3 and witness.CanResolve() and \
        not bool(main.get_meta("response_staged", false)), "one response commitment opens the target decision"): return false
    return await _choose_response_target()

func _choose_response_target() -> bool:
    var target = ui.named("Affordance%d" % rhino)
    if not await ui.expect_fully_visible_enabled(target): return false
    if not await _capture_checkpoint("ant-man-giant-target"): return false
    if not await ui.click(target): return false
    if not await ui.expect(func(): return witness.Revision() == 4 and witness.DecisionKind() == "PlayerAction" and \
        witness.Face(identity) == "12001c" and _rhino_health() == rhino_health - 1,
        "one target commitment deals exactly one damage and returns to play"): return false
    return await _capture_checkpoint("ant-man-giant-result")

func _table_card(anchor: int) -> Control:
    var table := main.find_child("AstraTableSurface", true, false)
    var card := table.get_node_or_null("ProceduralCard%d" % anchor) as Control if table != null else null
    return card if card != null and card.is_visible_in_tree() else null

func _rhino_health() -> int:
    var card := _table_card(rhino)
    var value := card.find_child("LiveProgressValue", true, false) as Label if card != null else null
    return int(value.text.get_slice("/", 0)) if value != null else -1
