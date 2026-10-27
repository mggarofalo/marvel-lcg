extends "res://smoke/search_choice_smoke.gd"

# Explicitly prepared nonpublic two-seat board. Every game decision uses viewport input.
const Driver = preload("res://smoke/automation/ui_driver.gd")
const Locator = preload("res://smoke/automation/ui_locator.gd")
var ui: RefCounted
var witness = preload("res://smoke/NativeGameWitness.cs").new()
var path := OS.get_environment("MARVEL_INVOCATION_PATH")
const RINGS = "Seven Rings of Raggadorr"
# Physical fixture identities, asserted by InvocationNativeScene before exposure.
const ENERGY = 41
const STRANGE = 21
const SPIDER_MAN = 0

func _entry_modes_are_explicit() -> bool:
    return (_node("Setup/Selections/Fields/ConnectionGrid/Endpoint") as LineEdit).text == OS.get_environment("MARVEL_ENGINE_ENDPOINT")

func _run() -> void:
    if not await _entry(): return
    if not await _action(): return
    if not await _payment(): return
    if not await _allocation(): return
    if not await _commit_payment(): return
    if not await _recipients(): return
    if not await _completion(): return
    print("UI_ACCEPTANCE_OK invocation " + path)
    quit(0)

func _entry() -> bool:
    if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
    await _configure_seeded_game()
    ui = Driver.new(main, render_viewport, _fail)
    for step in range(90):
        if render_viewport.gui_get_focus_owner() == _button_named("Start game"): break
        if not await ui.press_key(KEY_TAB): return false
    if not await ui.click(ui.button("Start game")): return false
    witness.Bind(main)
    if not await ui.expect(func(): return witness.CanResolve() and witness.DecisionKind() == "PlayerAction", "prepared seat 1 action prompt"): return false
    if not await _capture("entry"): return false
    return true

func _action() -> bool:
    if not await ui.click(ui.named("CompleteChoiceSheet")): return false
    var wanted := "Spell Mastery" if path == "spell" else "Master of the Mystic Arts"
    var offers: Array = main.find_children("Affordance*", "Button", true, false).filter(func(n): return n.is_visible_in_tree() and wanted in (n.text + _visible_text(n)))
    if not await ui.expect(func(): return offers.size() == 1, "one exact " + wanted + " action"): return false
    if not await ui.click(Locator.control(offers[0])): return false
    if not await _capture("composition"): return false
    print("INVOCATION_COMPOSITION " + _visible_text(main))
    var top := witness.Anchor(RINGS)
    if not await ui.expect(func(): return top >= 0, "owned public top is readable"): return false
    var target = ui.named("Target%d" % top)
    if target.all().size() == 1 and not target.all()[0].button_pressed:
        if not await ui.click(target): return false
    return true

func _payment() -> bool:
    if not await _capture("payment"): return false
    var copy := _visible_text(main)
    if not await ui.expect(func(): return RINGS in copy and "Special" in copy and ("Exhaust Doctor Strange" in copy if path == "spell" else "return that exact physical Invocation faceup" in copy), "visible Special cost and originating commitment"): return false
    if not await _reach("Resource%d" % ENERGY): return false
    if not await ui.click(ui.named("Resource%d" % ENERGY)): return false
    return true

func _allocation() -> bool:
    if path == "master":
        for icon in range(2):
            var allocation = ui.named("Allocation%d_%d" % [ENERGY, icon])
            if not await _reach("Allocation%d_%d" % [ENERGY, icon]): return false
            var menu: PopupMenu = allocation.all()[0].get_popup()
            var cost_label := "Cost %d" % (icon + 1)
            var matches: Array[int] = []
            for item in range(menu.item_count):
                if menu.get_item_text(item) == cost_label and "Energy" in menu.get_item_tooltip(item):
                    matches.append(item)
            if not await ui.expect(func(): return matches.size() == 1, "one exact Energy allocation to " + cost_label): return false
            if not await ui.press_key(KEY_ENTER): return false
            if not await preload("res://smoke/native_declaration_contract.gd")._choose_from_menu(self, menu, matches[0]): return false
            if not await ui.expect(func(): return ui.named("Allocation%d_%d" % [ENERGY, icon]).all()[0].text == cost_label, "exact cost allocation retained after rerender"): return false
    return true

func _commit_payment() -> bool:
    if not await ui.expect(func(): return witness.Revision() == 0, "payment stages without commitment"): return false
    var submit: Button = _payment_button("Submit") if _payment_modal() != null else _task_commit()
    if submit == null: submit = main.find_child("Submit", true, false) as Button
    if not await ui.expect(func(): return submit != null and not submit.disabled, "explicit paid action commitment"): return false
    if not await ui.click(Locator.control(submit)): return false
    if not await ui.expect(func(): return witness.Revision() == 1, "initiating action commits once"): return false
    return true

func _recipients() -> bool:
    if not await _recipient_context(): return false
    return await _stage_recipients()

func _recipient_context() -> bool:
    if not await _capture("recipients"): return false
    print("INVOCATION_NESTED " + _visible_text(main))
    var copy := _visible_text(main)
    if not await ui.expect(func(): return RINGS in copy and "Already paid:" in copy and RINGS + "'s Special discards this Invocation and reveals the next Invocation, even if its other effects do not apply." in copy and "Closing this choice does not refund these costs" in copy and ("Spell Mastery" in copy and "was exhausted for this cost" in copy if path == "spell" else "After " + RINGS + "'s Special completes, return that exact physical Invocation" in copy and "event cost is also paid" in copy), "nested recipient choice retains originating paid action and return consequence"): return false
    var instructions := "Choose up to 3 different characters. Give each a tough status card. Confirm with none selected to give no status cards; later instructions still resolve."
    var choice_surface = _search_frame() if _search_frame() != null else main.find_child("CausalSituationCopy", true, false)
    if not await ui.expect(func(): return choice_surface != null and _visible_text(choice_surface).count(instructions) == 1, "current recipient instructions appear once alongside paid parent context on the active surface"): return false
    return true

func _stage_recipients() -> bool:
    if _search_frame() == null:
        if not await ui.click(ui.named("CompleteChoiceSheet")): return false
    if not await ui.expect(func(): return Array(witness.CandidateIds()).has(STRANGE) and Array(witness.CandidateIds()).has(SPIDER_MAN), "both exact hero recipients are offered"): return false
    for id in [STRANGE, STRANGE, SPIDER_MAN]:
        if not await _reach("Target%d" % id): return false
        if not await ui.click(ui.named("Target%d" % id)): return false
    if not await ui.expect(func(): return witness.Revision() == 1 and Array(witness.SelectedTargets()) == [SPIDER_MAN], "stage Strange, unstage Strange and select only Spider-Man without commitment"): return false
    if not await _capture("selected-spider-man"): return false
    return true

func _completion() -> bool:
    var submit = main.find_child("Submit", true, false) as Button
    if not await ui.click(Locator.control(submit)): return false
    if not await ui.expect(func(): return witness.Revision() == 2 and witness.DecisionKind() == "PlayerAction", "one complete subset answer returns to play"): return false
    if not await _capture("result"): return false
    print("INVOCATION_RESULT " + _visible_text(main))
    if not await _history(): return false
    return await _release_table_resources()

func _history() -> bool:
    if not await ui.click(ui.named("ToggleHistory")): return false
    var details = ui.button("▸ Details")
    if not await ui.click(details): return false
    print("INVOCATION_HISTORY " + _visible_text(main))
    if not await ui.expect(func(): return "Reveal · Winds of Watoomb" in _visible_text(main), "history preserves the exact intermediate public Winds reveal"): return false
    if not await _capture("history"): return false
    return await _scroll_to_reveal()

func _scroll_to_reveal() -> bool:
    var components := main.find_child("OccurrenceComponents", true, false) as ScrollContainer
    var reveal_rows: Array = components.find_children("*", "Button", true, false).filter(func(row): return "Reveal · Winds of Watoomb" in _visible_text(row))
    if not await ui.expect(func(): return reveal_rows.size() == 1, "one physical intermediate reveal in history details"): return false
    var reveal: Control = reveal_rows[0]
    for step in range(30):
        if _control_is_fully_visible(reveal): break
        var point := _visible_control_rect(components).get_center()
        ui._move(point)
        ui._mouse_button(point, MOUSE_BUTTON_WHEEL_DOWN, true)
        ui._mouse_button(point, MOUSE_BUTTON_WHEEL_DOWN, false)
        await process_frame
    if not await ui.expect(func(): return _control_is_fully_visible(reveal), "public intermediate reveal is fully readable after native scrolling"): return false
    if not await _capture("history-reveal"): return false
    return true

func _capture(leaf: String) -> bool:
    return await preload("res://smoke/native_heimdall_contract.gd")._capture(self, leaf)

func _reach(node_name: String) -> bool:
    var locator = ui.named(node_name)
    if not await ui.expect_visible(locator): return false
    for step in range(90):
        if render_viewport.gui_get_focus_owner() == locator.all()[0]:
            return await ui.expect_fully_visible_enabled(locator)
        if not await ui.press_key(KEY_TAB): return false
    return await ui.expect(func(): return false, "Tab reaches " + node_name, 0)
