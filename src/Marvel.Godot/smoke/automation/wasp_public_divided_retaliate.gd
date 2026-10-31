extends "res://smoke/automation/wasp_public_divided_attack.gd"

# Public Wasp starter and The Wrecking Crew Standard, seed 1. Giant Wasp declares a
# basic attack on Wrecker and divides it with Piledriver, whose Retaliate 1 hits her.
const DECLARED := "Wrecker"
const RETALIATOR := "Piledriver"
var declared := -1
var retaliator := -1
var declared_health := 0
var retaliator_health := 0
var wasp_health := 0

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
    if not await _declare(): return
    if not await _divide_with_retaliator(): return
    if not await _release_table_resources(): return
    print("UI_ACCEPTANCE_OK wasp_public_divided_retaliate seed=1 form=giant shares=wrecker,piledriver")
    quit(0)

func _declare() -> bool:
    declared = witness.AreaAnchor(DECLARED, "VillainArea")
    retaliator = witness.AreaAnchor(RETALIATOR, "VillainArea")
    declared_health = _health_of(declared)
    retaliator_health = _health_of(retaliator)
    wasp_health = _health_of(identity)
    if not await ui.expect(func(): return declared_health > 0 and retaliator_health > 0 and wasp_health > 0,
        "both villains and Wasp show a health baseline"): return false
    if not await _declare_power("Attack", "Then divide the damage among enemies you can attack", DECLARED,
        "wasp-retaliate-targets"): return false
    return await ui.expect(func(): return declared in witness.CandidateIds() and retaliator in witness.CandidateIds(),
        "the divided attack offers both villains")

func _divide_with_retaliator() -> bool:
    if not await _open_allocation(declared): return false
    var row := _visible_text(_allocation_row(retaliator))
    if not await ui.expect(func(): return "Attacked if given any damage" in row and "Retaliate 1 will hit Wasp" in row,
        "Piledriver's row explains that attacking it triggers Retaliate 1 against Wasp"): return false
    var revision := witness.Revision()
    if not await _allocate_and_commit(declared, retaliator, "wasp-retaliate-allocation"): return false
    if not await ui.expect(func(): return witness.Revision() == revision + 1 and \
        _health_of(declared) == declared_health - 1 and _health_of(retaliator) == retaliator_health - 1 and \
        _health_of(identity) == wasp_health - 1,
        "each villain takes one damage and Piledriver's Retaliate deals one to Wasp", 15000): return false
    return await _capture_checkpoint("wasp-retaliate-result")

func _allocation_row(anchor: int) -> Node:
    var add := main.find_child("Target%dAdd" % anchor, true, false)
    return add.get_parent() if add != null else main
