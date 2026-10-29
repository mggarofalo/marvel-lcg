extends "res://smoke/automation/ant_man_public_acceptance.gd"

# Public starter, seed 2. All gameplay decisions use viewport input.
var payment_witness = preload("res://smoke/NativePaymentWitness.cs").new()
var hanks: Array
var army: Array

func _run() -> void:
    if not await _setup_journey(): return
    if not await _open_hank_payment(): return
    if not await _stage_hank_payment(): return
    if not await _commit_hank_payment(): return
    if not await _accept_hank_interrupt(): return
    if not await _release_table_resources(): return
    print("UI_ACCEPTANCE_OK ant_man_hank_optional_overpayment seed=2 counters=1 hp=1")
    quit(0)

func _setup_journey() -> bool:
    var evidence := OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY")
    for store in ["MARVEL_CAMPAIGN_ACCESS_DIRECTORY", "MARVEL_DECK_LIBRARY_DIRECTORY"]:
        OS.set_environment(store, evidence.path_join(store))
    if DisplayServer.get_name() != "headless": OS.set_environment("MARVEL_SMOKE_CAPTURE_DIR", evidence)
    if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
    ui = Driver.new(main, render_viewport, _fail)
    if not await _public_setup(): return false
    payment_witness.Bind(main)
    if not await _opening_hand(): return false
    return true

func _open_hank_payment() -> bool:
    hanks = Array(payment_witness.Cards("12011", "HandsArea", 0))
    if not await ui.expect(func(): return hanks.size() == 1, "one physical Hank in public opening hand"): return false
    var hank: Control = main.find_child("ProceduralCard%d" % hanks[0], true, false)
    if not await ui.expect(func(): return hank != null and hank.is_visible_in_tree(), "physical Hank visible"): return false
    if not await ui.click(Locator.control(hank), MOUSE_BUTTON_RIGHT): return false
    var menu = main.find_child("CardContextMenu", true, false)
    if not await ui.expect(func(): return menu != null, "Hank menu visible"): return false
    print("HANK_MENU " + _visible_text(menu))
    if not await _capture_checkpoint("hank-menu"): return false
    if not await ui.click(_menu_choice("Play Ant-Man")): return false
    var explanation := _visible_text(main).to_lower()
    if not await ui.expect(func(): return "optional entry interrupt" in explanation and "per resource overpaid" in explanation and \
        "maximum 4" in explanation and "+1 hit point" in explanation, \
        "before payment the optional counter, cap, and health benefit are explained"): return false
    print("HANK_COMPOSITION " + _visible_text(main))
    if not await _capture_checkpoint("hank-composition"): return false
    return true

func _stage_hank_payment() -> bool:
    army = Array(payment_witness.Cards("12007", "HandsArea", 0))
    if not await ui.expect(func(): return army.size() == 1 and witness.Revision() == 1, "one physical Army and no commitment"): return false
    if not await ui.click(ui.named("Resource%d" % army[0])): return false
    if not await ui.expect(func(): return witness.Revision() == 1 and \
        Array(payment_witness.DraftResources()) == [army[0]] and "1 resource" in _visible_text(main), \
        "the exact Army is staged as one resource without commitment"): return false
    var selected_copy := _visible_text(main).to_lower()
    if not await ui.expect(func(): return "excess" in selected_copy and "lost" not in selected_copy, \
        "selected excess is described without claiming it is lost"): return false
    if not await _capture_checkpoint("hank-selected"): return false
    return true

func _commit_hank_payment() -> bool:
    if not await ui.click(ui.button("Pay and play Ant-Man · Hank Pym")): return false
    if not await ui.expect(func(): return witness.Revision() == 2, "one play commitment"): return false
    if not await ui.expect(func(): return witness.CanResolve() and not bool(main.get_meta("response_staged", false)), "entry interrupt settles"): return false
    print("HANK_AFTER " + _visible_text(main))
    if not await ui.expect(func(): return witness.DecisionKind() == "Interrupt" and \
        payment_witness.InArea(army[0], "DiscardPile", 0) and \
        payment_witness.InArea(hanks[0], "RevealingArea", 0) and \
        Array(witness.OfferedAnchors()) == [hanks[0]], \
        "the exact paid Army is discarded and the exact Hank offers his entry interrupt"): return false
    if not await _capture_checkpoint("hank-after"): return false
    return true

func _accept_hank_interrupt() -> bool:
    var window := main.find_child("LocalDecisionWorkspace", true, false) as Control
    if not await ui.expect(func(): return window != null and window.is_visible_in_tree(), "entry choice stays visible"): return false
    var copy := _visible_text(window).to_lower()
    if not await ui.expect(func(): return "player 1" in copy and "pass this opportunity" in copy and \
        "counter" in copy and ("hit point" in copy or "health" in copy) and "12011" not in copy, \
        "entry choice explains the optional counter and health consequence without an internal id"): return false
    var group := window.find_child("ChoiceGroup0", true, false)
    if not await ui.expect(func(): return group != null, "current decision group is visible"): return false
    var choices: Array = group.find_children("Affordance*", "Button", true, false).filter(func(n): return n.is_visible_in_tree())
    if not await ui.expect(func(): return choices.size() == 1, "one exact Hank entry interrupt"): return false
    if not await ui.click(Locator.control(choices[0])): return false
    if not await ui.expect(func(): return witness.Revision() == 3 and witness.CanResolve() and not bool(main.get_meta("response_staged", false)), "entry interrupt commits once and settles"): return false
    if not await ui.expect(func(): return witness.DecisionKind() == "PlayerAction" and \
        payment_witness.InArea(hanks[0], "AlliesArea", 0) and \
        payment_witness.InArea(army[0], "DiscardPile", 0) and \
        payment_witness.Counter(hanks[0], "pym") == 1 and \
        payment_witness.Value(hanks[0], "HP") == 1 and payment_witness.Damage(hanks[0]) == 0, \
        "the same Hank enters with exactly one pym counter and one undamaged HP; the paid Army stays discarded"): return false
    if not await _capture_checkpoint("hank-interrupt"): return false
    return true

func _seed_setup() -> bool:
    if not await ui.click_revealed(ui.button("Connection and seed")): return false
    var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
    if not await ui.click_revealed(Locator.control(seed)): return false
    if not await ui.press_key(KEY_A, false, true): return false
    for pressed in [true, false]:
        var event := InputEventKey.new()
        event.keycode = KEY_2
        event.unicode = 50
        event.pressed = pressed
        render_viewport.push_input(event, true)
    await process_frame
    if not await ui.expect(func(): return seed.text == "2", "typed public seed 2"): return false
    return await _close_seed_options()





