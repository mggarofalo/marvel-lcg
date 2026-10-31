extends "res://smoke/automation/wave_native_playable.gd"

# Public Wasp starter and Ultron Standard, seed 1. Ultron's setup engages a facedown
# Drone, so Giant Wasp's first basic attack can be divided between two enemies.
# Setup selections, seed entry, menu gestures and decisions use viewport input.
var witness = preload("res://smoke/NativeGameWitness.cs").new()
var identity := -1
var ultron := -1
var drone := -1
var ultron_health := 0
var scenario := "Ultron"

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
    if not await _declare_attack(): return
    if not await _divide(): return
    if not await _release_table_resources(): return
    print("UI_ACCEPTANCE_OK wasp_public_divided_attack seed=1 form=giant shares=ultron,drone")
    quit(0)

func _public_setup() -> bool:
    var tiles: Array = main.find_children("*", "Button", true, false).filter(func(n):
        return n.is_visible_in_tree() and n.text == "Wasp" and "Second" not in str(n.get_path()))
    if not await ui.expect(func(): return tiles.size() == 1, "one first-seat public Wasp tile"): return false
    if not await ui.click(Locator.control(tiles[0])): return false
    if not await _choose_setup("Scenario", scenario): return false
    if not await _choose_setup("Mode", "Standard"): return false
    if not await _seed_setup(): return false
    if not await _capture_checkpoint("wasp-public-setup"): return false
    if not await ui.click(ui.button("Start game")): return false
    witness.Bind(main)
    return true

func _seed_setup() -> bool:
    if not await ui.click_revealed(ui.button("Connection and seed")): return false
    var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
    if not await ui.click_revealed(Locator.control(seed)): return false
    if not await ui.press_key(KEY_A, false, true): return false
    for pressed in [true, false]:
        var event := InputEventKey.new()
        event.keycode = KEY_1
        event.unicode = 49
        event.pressed = pressed
        render_viewport.push_input(event, true)
    await process_frame
    var hero := _node("Setup/Selections/Fields/Grid/Hero") as OptionButton
    if not await ui.expect(func(): return seed.text == "1" and hero.get_item_text(hero.selected) == "Wasp",
        "public selection retains Wasp and typed seed"): return false
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
    if not await ui.expect(func(): return witness.Revision() == 0, "no accepted decision before the hand is kept"): return false
    identity = witness.Anchor("Nadia Van Dyne")
    if not await ui.expect(func(): return identity >= 0 and witness.Face(identity) == "13001b",
        "public setup displays Nadia Van Dyne"): return false
    if not await ui.click(ui.button("Keep hand")): return false
    return await ui.expect(func(): return witness.Revision() == 1 and witness.DecisionKind() == "PlayerAction" and \
        witness.CanResolve() and not bool(main.get_meta("response_staged", false)),
        "keeping the hand commits once and the player-action table settles")

func _choose_giant() -> bool:
    if not await ui.expect(func(): return _table_card(identity) != null, "Nadia Van Dyne card body is visible"): return false
    if not await _open_identity_menu(): return false
    var tiny = _menu_choice("Change to Wasp (AVENGER, TINY)")
    var giant = _menu_choice("Change to Wasp (AVENGER, GIANT)")
    if not await ui.expect_fully_visible_enabled(tiny): return false
    if not await ui.expect_fully_visible_enabled(giant): return false
    if not await _capture_checkpoint("wasp-destinations"): return false
    if not await ui.click(giant): return false
    if not await ui.expect(func(): return witness.Revision() == 2 and witness.Face(identity) == "13001c" and \
        witness.DecisionKind() == "PlayerAction" and witness.CanResolve(),
        "one form commitment changes the same identity to Giant Wasp"): return false
    return await _dismiss_receipt()

func _declare_attack() -> bool:
    ultron = witness.AreaAnchor("Ultron", "VillainArea")
    ultron_health = _health_of(ultron)
    if not await ui.expect(func(): return ultron_health > 0, "Ultron has a visible health baseline"): return false
    if not await _declare_power("Attack", "Then divide the damage among enemies you can attack", "Ultron",
        "wasp-attack-targets"): return false
    var candidates: Array = witness.CandidateIds()
    for candidate in candidates:
        if candidate != ultron: drone = candidate
    return await ui.expect(func(): return candidates.size() == 2 and ultron in candidates and drone >= 0,
        "Ultron and the engaged Drone are the two legal recipients")

# Declares a Giant basic power on one target through the identity menu and passes any
# optional interrupt windows until the division allocation is offered.
func _declare_power(power: String, note: String, target: String, checkpoint: String) -> bool:
    if not await _offer_power(power, note): return false
    if not await ui.click(_menu_choice(power)): return false
    if not await ui.expect_fully_visible_enabled(_menu_choice(target)): return false
    if not await ui.expect(func(): return "if all of it is assigned here" in \
        _visible_text(main.find_child("CardContextMenu", true, false)),
        "the declared target preview is conditional on the division"): return false
    if not await _capture_checkpoint(checkpoint): return false
    if not await ui.click(_menu_choice(target)): return false
    return await _await_allocation()

# Optional interrupt windows, such as Rapid Growth in hand, are passed explicitly.
func _await_allocation() -> bool:
    for frame in range(1800):
        if witness.HasRepeatedTargets() and witness.CanResolve(): break
        if witness.CanResolve() and ui.button("Pass this opportunity", false).all().size() == 1:
            if not await _pass_window(): return false
        await process_frame
    return await ui.expect(func(): return witness.HasRepeatedTargets() and witness.CanResolve(),
        "the divided power opens its allocation")

func _offer_power(power: String, note: String) -> bool:
    if not await _open_identity_menu(): return false
    if not await ui.expect_fully_visible_enabled(_menu_choice(power)): return false
    return await ui.expect(func(): return note in _visible_text(main.find_child("CardContextMenu", true, false)),
        "the offer says its amount is divided after the declaration")

func _open_identity_menu() -> bool:
    if not await ui.expect(func(): return _table_card(identity) != null, "identity card body is visible"): return false
    return await _right_click_identity()

func _dismiss_receipt() -> bool:
    # The accepted choice stays explained beside its card until the player dismisses it.
    var dismiss = ui.named("LocalReceiptDismiss")
    if not await ui.expect_fully_visible_enabled(dismiss): return false
    if not await ui.click(dismiss): return false
    return await ui.expect(func(): return main.find_child("LocalDecisionReceipt", true, false) == null,
        "the accepted-choice receipt closes")

func _right_click_identity() -> bool:
    if not await ui.click(Locator.control(_table_card(identity)), MOUSE_BUTTON_RIGHT): return false
    return await ui.expect(func(): return main.find_child("CardContextMenu", true, false) != null, "identity action menu opens")

func _pass_window() -> bool:
    var pass_button = ui.button("Pass this opportunity", false)
    var revision := witness.Revision()
    if not await ui.click(pass_button): return false
    return await ui.expect(func(): return witness.Revision() == revision + 1, "passing an optional window commits once")

func _divide() -> bool:
    if not await _open_allocation(ultron): return false
    var copy := _visible_text(main)
    if not await ui.expect(func(): return "Divide 2 damage among enemies" in copy and \
        "Each enemy dealt damage is attacked, so its Retaliate applies" in copy and \
        "Attacked if given any damage" in copy, "allocation explains the total, attack and retaliate consequences"): return false
    var revision := witness.Revision()
    if not await _allocate_and_commit(ultron, drone, "wasp-divided-allocation"): return false
    if not await ui.expect(func(): return witness.Revision() == revision + 1 and _health_of(ultron) == ultron_health - 1 and \
        witness.Face(identity) == "13001c" and not witness.Ready(identity) and \
        witness.Zone(drone) != "EngagedEnemiesArea",
        "one commitment deals one damage to Ultron and defeats the one-hit-point Drone"): return false
    if not await ui.expect(func(): return not _visible_text(main).contains("Engaged enemies"),
        "the defeated Drone leaves the engaged table"): return false
    return await _capture_checkpoint("wasp-divided-result")

# Assigns one point to each recipient, proving partial and full allocations stay
# uncommitted, then commits once.
func _allocate_and_commit(first: int, second: int, checkpoint: String) -> bool:
    var revision := witness.Revision()
    if not await _press(first, "Add"): return false
    var commit := _submit()
    if not await ui.expect(func(): return commit != null and commit.disabled and witness.Revision() == revision,
        "one of two points cannot be committed"): return false
    if not await _press(second, "Add"): return false
    if not await ui.expect(func(): return _disabled(first, "Add") and _disabled(second, "Add"),
        "a full allocation closes further assignment"): return false
    if not await _capture_checkpoint(checkpoint): return false
    commit = _submit()
    if not await ui.expect(func(): return commit != null and not commit.disabled and witness.Revision() == revision,
        "a complete allocation awaits explicit commitment"): return false
    return await ui.click(Locator.control(commit))

func _open_allocation(anchor: int) -> bool:
    if main.find_child("Target%dAdd" % anchor, true, false) != null: return true
    var sheet := main.find_child("CompleteChoiceSheet", true, false) as Button
    if sheet != null and sheet.is_visible_in_tree():
        if not await ui.click(Locator.control(sheet)): return false
    if main.find_child("Target%dAdd" % anchor, true, false) != null: return true
    for choice in main.find_children("Affordance*", "Button", true, false):
        if choice.is_visible_in_tree() and not choice.disabled:
            if not await ui.click(Locator.control(choice)): return false
            break
    return await ui.expect(func(): return main.find_child("Target%dAdd" % anchor, true, false) != null,
        "the repeated-target allocation editor is reachable")

func _press(anchor: int, operation: String) -> bool:
    var button = ui.named("Target%d%s" % [anchor, operation])
    if not await ui.expect_fully_visible_enabled(button): return false
    return await ui.click(button)

func _disabled(anchor: int, operation: String) -> bool:
    var button := main.find_child("Target%d%s" % [anchor, operation], true, false) as Button
    return button != null and button.disabled

func _submit() -> Button:
    var complete: Control = main.find_child("CompleteChoicesFrame", true, false)
    if complete != null and complete.is_visible_in_tree(): return complete.find_child("Submit", true, false)
    return _task_commit()

func _table_card(anchor: int) -> Control:
    var table := main.find_child("AstraTableSurface", true, false)
    var card := table.get_node_or_null("ProceduralCard%d" % anchor) as Control if table != null else null
    return card if card != null and card.is_visible_in_tree() else null

func _health_of(anchor: int) -> int:
    var card := _table_card(anchor)
    var value := card.find_child("LiveProgressValue", true, false) as Label if card != null else null
    return int(value.text.get_slice("/", 0)) if value != null else -1
