extends "res://smoke/automation/wasp_public_divided_attack.gd"

# Public Wasp starter and The Wrecking Crew Standard, seed 1. The scenario begins
# with four villain side schemes, so Giant Wasp's first basic thwart can be divided.
const DECLARED := "Pile It On!"
const OTHER := "Clear the Road"
var declared := -1
var other := -1
var declared_threat := 0
var other_threat := 0

func _run() -> void:
    scenario = "Wrecking Crew"
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
    if not await _declare_thwart(): return
    if not await _divide_threat(): return
    if not await _release_table_resources(): return
    print("UI_ACCEPTANCE_OK wasp_public_divided_thwart seed=1 form=giant shares=pile_it_on,clear_the_road")
    quit(0)

func _declare_thwart() -> bool:
    declared = witness.AreaAnchor(DECLARED, "SideSchemesArea")
    other = witness.AreaAnchor(OTHER, "SideSchemesArea")
    declared_threat = _threat_of(declared)
    other_threat = _threat_of(other)
    if not await ui.expect(func(): return declared_threat > 0 and other_threat > 0,
        "both side schemes show a threat baseline"): return false
    if not await _declare_power("Thwart", "Then divide the threat removal among schemes you can thwart",
        DECLARED, "wasp-thwart-targets"): return false
    return await ui.expect(func(): return declared in witness.CandidateIds() and other in witness.CandidateIds(),
        "the divided thwart allocates among the side schemes")

func _divide_threat() -> bool:
    if not await _open_allocation(declared): return false
    var copy := _visible_text(main)
    if not await ui.expect(func(): return "Divide 2 threat removal among schemes" in copy and \
        "Each scheme given threat removal is thwarted" in copy and \
        "Thwarted if given any threat removal" in copy, "allocation explains the total and thwart consequence"): return false
    var revision := witness.Revision()
    if not await _allocate_and_commit(declared, other, "wasp-thwart-allocation"): return false
    if not await ui.expect(func(): return witness.Revision() == revision + 1 and \
        _threat_of(declared) == declared_threat - 1 and _threat_of(other) == other_threat - 1 and \
        not witness.Ready(identity), "one commitment removes one threat from each chosen scheme"): return false
    return await _capture_checkpoint("wasp-thwart-result")

func _threat_of(anchor: int) -> int:
    var card := _table_card(anchor)
    var value := card.find_child("LiveProgressValue", true, false) as Label if card != null else null
    return int(value.text.get_slice("/", 0)) if value != null else -1
