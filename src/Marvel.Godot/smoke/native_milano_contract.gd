extends RefCounted

# Rhino with the withheld Ship Command set, dealt by the test host only (ShipCommandNativeScene).
# milano-2: the second seat pays for Spider-Woman with the first player's Milano (rr:ability.8).
# milano-1: the solo first player pays for Aunt May with their own Milano; no other seat is named.
static func run(smoke) -> bool:
    var two: bool = smoke.contract == "milano-2"
    var milano: int = smoke.witness.Anchor("Milano")
    if milano < 0:
        smoke._fail("the Milano is not on the published table")
        return false
    if not _rail(smoke, two): return false
    if not await _capture(smoke, "table"): return false
    var card: String = "Spider-Woman" if two else "Aunt May"
    if not await _pay(smoke, card, milano, two): return false
    if not await _commit(smoke, card, milano): return false
    if two and not await _pass_and_inspect_first_player(smoke): return false
    if not await smoke.ui.expect(func(): return _exhausted(smoke._tabletop_card_named("Milano")),
        "the Milano shows exhausted on the first player's table"): return false
    return await _capture(smoke, "result")

static func _rail(smoke, two: bool) -> bool:
    if two: return _rail_marks_first_player(smoke)
    if smoke.main.find_child("SeatSwitch0", true, false) == null: return true
    smoke._fail("a solo table shows a seat rail")
    return false

# Opens the card's payment, checks the seat chip and composes a ready payment that spends the Milano.
static func _pay(smoke, card: String, milano: int, two: bool) -> bool:
    if not await smoke._drag_to_prompt_owner_lane(smoke._tabletop_card_named(card)): return false
    if not await smoke.ui.expect(func(): return smoke._payment_modal() != null, card + " opens its payment"): return false
    if not await _source_names_its_seat(smoke, milano, 0 if two else -1): return false
    if not await _capture(smoke, "payment"): return false
    if not await _select(smoke, "Resource%d" % milano): return false
    if two:
        var genius: Button = smoke._visible_button_beginning(smoke._payment_modal(), "Discard Genius")
        if genius == null or not await smoke.ui.click(smoke.ui.Locator.control(genius)): return false
    if not await smoke.ui.expect(func():
        var progress := smoke._payment_modal().find_child("PaymentProgress", true, false) as Control
        return progress != null and "Payment ready" in progress.tooltip_text and smoke.witness.Revision() == 0,
        "the Milano's wild resource counts toward the payment without committing it"): return false
    return await _capture(smoke, "payment-ready")

static func _commit(smoke, card: String, milano: int) -> bool:
    var commit: Button = smoke._payment_button("Submit")
    if commit == null or card not in commit.text: return false
    if not await smoke.ui.click(smoke.ui.Locator.control(commit)): return false
    return await smoke.ui.expect(func(): return smoke.witness.Revision() == 1 and not smoke.witness.Ready(milano),
        "one explicit payment plays the card and exhausts the Milano")

# Spider-Woman's optional Response follows her entry; passing it is the seat's second answer. The
# first player's tableau is then inspected without changing who answers.
static func _pass_and_inspect_first_player(smoke) -> bool:
    var pass_button: Button = smoke._visible_button_beginning(smoke.main, "Pass this opportunity")
    if pass_button == null or not await smoke.ui.click(smoke.ui.Locator.control(pass_button)): return false
    if not await smoke.ui.expect(func(): return smoke.witness.Revision() == 2 and smoke.witness.DecisionKind() == "PlayerAction",
        "the passed response returns the paying seat to its turn"): return false
    return await smoke.ui.click(smoke.ui.named("SeatSwitch0"))

# A table card's exhausted mark: the full card's badge, or a source tile's state symbol.
static func _exhausted(card: Control) -> bool:
    if card == null: return false
    if card.find_child("ExhaustedBadge", true, false) != null: return true
    var state: Control = card.find_child("SourceState", true, false)
    return state != null and state.accessibility_name == "Exhausted"

# The first player's tile carries the token; the paying seat's does not.
static func _rail_marks_first_player(smoke) -> bool:
    var first: Control = smoke.main.find_child("SeatSwitch0", true, false)
    var payer: Control = smoke.main.find_child("SeatSwitch1", true, false)
    if first == null or payer == null:
        smoke._fail("the two-player rail is missing a seat")
        return false
    var token: Control = first.find_child("RoleFirstPlayer", true, false)
    if token == null or not token.is_visible_in_tree() or token.tooltip_text != "This player holds the first-player token.":
        smoke._fail("the first player's tile does not show the first-player token")
        return false
    if payer.find_child("RoleFirstPlayer", true, false) != null:
        smoke._fail("the paying seat's tile shows the first-player token")
        return false
    return true

# Another seat's source carries that seat's chip beside it; the payer's own sources carry none.
static func _source_names_its_seat(smoke, milano: int, seat: int) -> bool:
    return await smoke.ui.expect(func(): return _chip_matches(smoke, milano, seat),
        "the Milano's payment row names the seat that controls it")

static func _chip_matches(smoke, milano: int, seat: int) -> bool:
    var modal: Control = smoke._payment_modal()
    var chips: Array = modal.find_children("SourceSeat*", "", true, false)
    var source: Control = modal.find_child("Resource%d" % milano, true, false)
    if source == null: return false
    if seat < 0: return chips.is_empty()
    if chips.size() != 1: return false
    var chip: Control = chips[0]
    return chip.name == "SourceSeat%d" % milano and int(chip.get_meta("source_seat", -1)) == seat \
        and chip.is_visible_in_tree() and source.get_parent() == chip.get_parent() \
        and "first player" in chip.tooltip_text and smoke.ui._fully_visible_enabled(source)

static func _select(smoke, name: String) -> bool:
    var source: Button = smoke._payment_modal().find_child(name, true, false)
    if source == null: return false
    if not await smoke.ui.click(smoke.ui.Locator.control(source)): return false
    return await smoke.ui.expect(func():
        var bound: Button = smoke._payment_modal().find_child(name, true, false)
        return bound != null and bound.button_pressed and smoke.witness.Revision() == 0,
        "the Milano is selected as a reversible payment source")

static func _capture(smoke, leaf: String) -> bool:
    return await preload("res://smoke/native_heimdall_contract.gd")._capture(smoke, smoke.contract + "-" + leaf)
