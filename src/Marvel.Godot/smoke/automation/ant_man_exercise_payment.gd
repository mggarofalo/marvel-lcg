extends "res://smoke/automation/ant_man_public_acceptance.gd"

# Public Ant-Man starter, Rhino Standard, seed 1; no prepared gameplay state.
var payment_witness = preload("res://smoke/NativePaymentWitness.cs").new()

func _run() -> void:
    if not await _setup_journey(): return
    if not await _exercise_journey(): return
    if not await _release_table_resources(): return
    print("UI_ACCEPTANCE_OK ant_man_exercise_scoped_payment seed=1 ordinary=1 discounted=0")
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

func _exercise_journey() -> bool:
    var exercises: Array = Array(payment_witness.Cards("12024", "HandsArea", 0))
    var helmets: Array = Array(payment_witness.Cards("12008", "HandsArea", 0))
    var armies: Array = Array(payment_witness.Cards("12007", "HandsArea", 0))
    if not await ui.expect(func(): return exercises.size() == 1 and helmets.size() == 1 and armies.size() == 2, "exact seed 1 physical opening cards"): return false
    var source: int = exercises[0]
    var spent_army: int = armies[0]
    var child: int = armies[1]
    if not await ui.expect(func(): return payment_witness.OfferedPrice(child) == "1", "ordinary Army offer costs one before Exercise"): return false
    if not await _play_exercise(source, helmets[0], spent_army): return false
    if not await _become_tiny(): return false
    if not await _exercise_child(source, child, helmets[0], spent_army): return false
    return true

func _play_exercise(source: int, helmet: int, army: int) -> bool:
    var card: Control = main.find_child("ProceduralCard%d" % source, true, false)
    if not await ui.expect(func(): return card != null and card.is_visible_in_tree(), "Exercise is visible in hand"): return false
    if not await ui.click(Locator.control(card), MOUSE_BUTTON_RIGHT): return false
    if not await ui.click(_menu_choice("Play Team-Building Exercise")): return false
    if not await ui.expect(func(): return witness.Revision() == 1 and "Pay 2" in _visible_text(main), "ordinary Exercise payment requires two resources"): return false
    for id in [helmet, army]:
        if not await ui.click(ui.named("Resource%d" % id)): return false
    if not await ui.expect(func(): return witness.Revision() == 1 and Array(payment_witness.DraftResources()) == [helmet, army], "exact two physical cards staged without commitment"): return false
    if not await _capture_checkpoint("exercise-payment"): return false
    if not await ui.click(ui.named("Submit")): return false
    return await ui.expect(func(): return witness.Revision() == 2 and witness.DecisionKind() == "PlayerAction" and \
        witness.CanResolve() and not bool(main.get_meta("response_staged", false)) and \
        payment_witness.InArea(source, "SupportsArea", 0) and payment_witness.Ready(source) and \
        payment_witness.InArea(helmet, "DiscardPile", 0) and payment_witness.InArea(army, "DiscardPile", 0), \
        "one commitment plays the ready Exercise and discards both selected resources")

func _become_tiny() -> bool:
    if not await _open_form_menu(): return false
    if not await ui.click(_menu_choice("Change to Ant-Man (AVENGER, TINY)")): return false
    if not await ui.expect(func(): return witness.Revision() == 3 and witness.Face(identity) == "12001a" and \
        witness.DecisionKind() == "PlayerAction" and witness.CanResolve() and not bool(main.get_meta("response_staged", false)), \
        "one form commitment changes the same identity to Tiny and returns to player action"): return false
    return await _capture_checkpoint("exercise-tiny")

func _exercise_child(source: int, child: int, helmet: int, spent_army: int) -> bool:
    if not await _offer_child(source, child): return false
    return await _select_child(source, child, helmet, spent_army)

func _offer_child(source: int, child: int) -> bool:
    var card: Control = main.find_child("ProceduralCard%d" % source, true, false)
    if not await ui.expect(func(): return card != null and card.is_visible_in_tree(), "played Exercise source is visible"): return false
    if not await ui.click(Locator.control(card), MOUSE_BUTTON_RIGHT): return false
    var menu := main.find_child("CardContextMenu", true, false)
    if not await ui.expect(func(): return menu != null, "Exercise source menu opens"): return false
    var actions: Array = menu.find_children("Menu*Item*", "Button", true, false).filter(func(n):
        return n.is_visible_in_tree() and not n.disabled and "Inspect card" not in n.text)
    if not await ui.expect(func(): return actions.size() == 1, "one offered Exercise action"): return false
    if not await _capture_checkpoint("exercise-action"): return false
    if not await ui.click(Locator.control(actions[0])): return false
    if not await ui.expect(func(): return witness.Revision() == 4 and witness.CanResolve() and \
        not bool(main.get_meta("response_staged", false)) and not payment_witness.Ready(source) and \
        Array(witness.OfferedAnchors()).has(child) and payment_witness.OfferedPrice(child) == "0", \
        "Exercise exhausts once and offers the exact Army for zero after its scoped reduction"): return false
    return true

func _select_child(source: int, child: int, helmet: int, spent_army: int) -> bool:
    if not await _capture_checkpoint("exercise-child-offer"): return false
    var choice = ui.named("Affordance%d" % child)
    if choice.all().is_empty():
        if not await ui.click(ui.named("CompleteChoiceSheet")): return false
    if not await ui.expect_visible(choice): return false
    if not await ui.expect(func(): return "Play Army of Ants · 0" in _visible_text(main) and \
        Array(payment_witness.DraftResources()).is_empty(), "child offer displays zero cost without resource selection"): return false
    if not await _capture_checkpoint("exercise-child-payment"): return false
    if not await ui.click(choice): return false
    if not await ui.expect(func(): return witness.Revision() == 5 and witness.DecisionKind() == "PlayerAction" and \
        witness.CanResolve() and not bool(main.get_meta("response_staged", false)) and \
        payment_witness.InArea(child, "SupportsArea", 0) and payment_witness.InArea(source, "SupportsArea", 0) and \
        not payment_witness.Ready(source) and payment_witness.InArea(helmet, "DiscardPile", 0) and \
        payment_witness.InArea(spent_army, "DiscardPile", 0) and witness.HandCount() == 2, \
        "zero-cost child enters once, Exercise remains exhausted, and no other hand card is spent"): return false
    return await _capture_checkpoint("exercise-child-result")
