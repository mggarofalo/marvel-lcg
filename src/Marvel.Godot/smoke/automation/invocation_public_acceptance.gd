extends "res://smoke/search_choice_smoke.gd"

# Normal production setup, printed starter and seed 2; no prepared gameplay state.
const Driver = preload("res://smoke/automation/ui_driver.gd")
const Locator = preload("res://smoke/automation/ui_locator.gd")
var ui: RefCounted
var witness = preload("res://smoke/NativeGameWitness.cs").new()
var invocation = witness
var rings := -1
const RINGS = "Seven Rings of Raggadorr"

func _run() -> void:
    if not await _entry_public(): return
    if not await _natural_talent(): return
    if not await _result_public(): return
    if not await _release_table_resources(): return
    print("UI_ACCEPTANCE_OK invocation_public seed=2 resolves=2")
    quit(0)

func _entry_public() -> bool:
    if not await _open_setup(load("res://Main.tscn") as PackedScene): return false
    ui = Driver.new(main, render_viewport, _fail)
    if not await _setup(): return false
    witness.Bind(main)
    invocation.Bind(main)
    if not await ui.expect_visible(ui.button("Keep hand")): return false
    if not await ui.click(ui.button("Keep hand")): return false
    if not await ui.expect(func(): return witness.Revision() == 1, "normal mulligan commits once: commitment accepted", Driver.COMMIT_DEADLINE_MS): return false
    if not await ui.expect(func(): return witness.Revision() == 1 and witness.DecisionKind() == "PlayerAction", "normal mulligan commits once"): return false
    if not await _settled(): return false
    if not await ui.expect(func(): return invocation.PileMatches(RINGS, "", 4), "typed seat-0 Invocation deck has one public top and four anonymous cards"): return false
    rings = witness.Anchor(RINGS)
    return await _inspect_top(RINGS, "public-invocation-top")

func _natural_talent() -> bool:
    if not await _choose_action("Natural Talent"): return false
    print("PUBLIC_NATURAL_COMPOSITION " + _visible_text(main))
    if not await ui.expect(func(): return "Use Natural Talent" in _visible_text(main) and "Stephen Strange" in _visible_text(main), "the named identity action has an explicit next commitment"): return false
    if not await _capture("public-natural-commitment"): return false
    if not await _commit(1): return false
    if not await ui.expect(func(): return invocation.PileMatches("Crimson Bands of Cyttorak", RINGS, 3), "Natural Talent discards exact top and reveals Crimson Bands while three cards remain concealed"): return false
    if not await ui.expect(func(): return witness.AreaAnchor(RINGS, "AdditionalDiscardPile") == rings, "the exact formerly public physical top is in Invocation discard"): return false
    if not await _capture("public-natural-result"): return false
    if not await ui.expect(func(): return invocation.HandCount() == 6 and invocation.IdentityReady(), "Natural Talent spends no hand cards and does not exhaust the identity"): return false
    return true

func _result_public() -> bool:
    if not await _inspect_top("Crimson Bands of Cyttorak", "public-next-top"): return false
    if not await ui.click(ui.named("ToggleHistory")): return false
    if not await ui.expect(func(): return "Natural Talent" in _visible_text(main) and "Crimson Bands of Cyttorak" in _visible_text(main), "history retains public Natural Talent and newly revealed top"): return false
    if not await _capture("public-history"): return false
    return true

func _setup() -> bool:
    if (_node("Toolbar/Motion") as CheckButton).button_pressed:
        if not await ui.click(ui.named("Motion")): return false
    var choices: Array = main.find_children("*", "Button", true, false).filter(func(n): return n.is_visible_in_tree() and n.text == "Doctor Strange" and "Second" not in str(n.get_path()))
    if not await ui.expect(func(): return choices.size() == 1, "one first-seat public Doctor Strange tile"): return false
    if not await ui.click(Locator.control(choices[0])): return false
    if not await ui.click(ui.button("Connection and seed")): return false
    if not await _enter_seed(): return false
    if not await ui.click(ui.button("Connection and seed")): return false
    if not await _capture("public-setup"): return false
    return await ui.click(ui.button("Start game"))

func _enter_seed() -> bool:
    var seed := _node("Setup/Selections/Fields/Grid/Seed") as LineEdit
    if not await ui.click(Locator.control(seed)): return false
    if not await ui.press_key(KEY_A, false, true): return false
    for pressed in [true, false]:
        var event := InputEventKey.new()
        event.keycode = KEY_2
        event.unicode = 50
        event.pressed = pressed
        render_viewport.push_input(event, true)
    await process_frame
    if not await ui.expect(func(): return seed.text == "2" and (_node("Setup/Selections/Fields/Grid/Hero") as OptionButton).get_item_text((_node("Setup/Selections/Fields/Grid/Hero") as OptionButton).selected) == "Doctor Strange", "public setup retains hero and typed seed"): return false
    return true

func _choose_action(wanted: String) -> bool:
    if not await ui.click(ui.named("CompleteChoiceSheet")): return false
    var offers: Array = main.find_children("Affordance*", "Button", true, false).filter(func(n): return n.is_visible_in_tree() and wanted.to_lower() in (n.text + _visible_text(n)).to_lower())
    if not await ui.expect(func(): return offers.size() == 1, "one exact offered " + wanted): return false
    return await ui.click(Locator.control(offers[0]))

func _commit(revision: int) -> bool:
    if not await ui.expect(func(): return witness.Revision() == revision, "staging leaves revision unchanged"): return false
    var submit := main.find_child("Submit", true, false) as Button
    if submit == null or not submit.is_visible_in_tree(): submit = _task_commit()
    if not await ui.expect(func(): return submit != null and not submit.disabled, "explicit enabled commitment"): return false
    if not await ui.click(Locator.control(submit)): return false
    if not await ui.expect(func(): return witness.Revision() == revision + 1, "exactly one accepted decision returns to play: commitment accepted", Driver.COMMIT_DEADLINE_MS): return false
    if not await ui.expect(func(): return witness.Revision() == revision + 1 and witness.DecisionKind() == "PlayerAction", "exactly one accepted decision returns to play"): return false
    return await _settled()

func _settled() -> bool:
    return await ui.expect(func(): return witness.CanResolve() and not bool(main.get_meta("response_staged", false)), "authoritative response and table presentation settle")

func _inspect_top(title: String, leaf: String) -> bool:
    if not await _open_pile_menu(): return false
    var popup := root.find_child("PileInspector", true, false)
    if not await ui.expect(func(): return popup != null and popup.visible, "Invocation pile inspector opens"): return false
    var all := _visible_text(popup)
    if not await ui.expect(func(): return title in all and "1 / 1" in all and "invocation" in all.to_lower(), "inspector exposes only the public top"): return false
    if not await _capture(leaf): return false
    if DisplayServer.get_name() != "headless":
        await RenderingServer.frame_post_draw
        if popup.get_texture().get_image().save_png(OS.get_environment("MARVEL_UI_EVIDENCE_DIRECTORY").path_join(leaf + "-inspector.png")) != OK: return false
    for pressed in [true, false]:
        var event := InputEventKey.new()
        event.keycode = KEY_ESCAPE
        event.pressed = pressed
        Input.parse_input_event(event)
        await process_frame
    return await ui.expect(func(): return root.find_child("PileInspector", true, false) == null, "Escape dismisses public top inspection without commitment")

func _open_pile_menu() -> bool:
    if not await ui.click(ui.named("InspectOtherRegions")): return false
    var drawer := main.find_child("InspectOtherRegions", true, false) as MenuButton
    var menu := drawer.get_popup()
    if not await ui.expect(func(): return menu.visible, "Other piles menu opens"): return false
    var matches: Array[int] = []
    for item in range(menu.item_count):
        if "invocation deck" in menu.get_item_text(item).to_lower(): matches.append(item)
    print("PUBLIC_PILE_MENU " + str(menu.item_count) + " matches=" + str(matches))
    if not await ui.expect(func(): return matches.size() == 1, "one owned typed Invocation deck menu"): return false
    if not await preload("res://smoke/native_declaration_contract.gd")._choose_from_menu(self, menu, matches[0]): return false
    return true

func _capture(leaf: String) -> bool:
    return await preload("res://smoke/native_heimdall_contract.gd")._capture(self, leaf)
