# Resource icons: what a player card prints in its top-left corner, and what
# that buys. MARVEL-120.
#
# Every player card carries between zero and two resource icons in one of four
# colours -- physical, mental, energy, wild -- and a card in hand may be
# discarded to pay for another card with the icons it prints. Most costs are
# generic and any icon pays them, so the colour is usually invisible; a cost
# that names a colour is where it stops being decoration.
#
# `Then "<card>" has <n> "<icon>" resource icons` is the step that reads them.
# It reads the icons **printed**, not the costs payable: a wild icon pays a
# physical cost and this still answers 0 physical. What an icon buys is already
# observable the ordinary way, by playing something and seeing what the engine
# took, and the last scenario in this file does exactly that.
#
# ---------------------------------------------------------------------------
# Why this file is under specs/rules/ and tagged
#
# The claim is a rule -- every player card prints icons and the engine reads
# them at payment time -- and the cards below are the sharpest instances of it
# rather than the subject. `specs/rules/crisis-bypass.feature` is the same
# shape: rulebook behaviour, measured on the specific cards that can show it,
# with a `@card:` tag on each scenario so the claim is credited to the card it
# is about.
#
# ---------------------------------------------------------------------------
# Four ids, one card, four icons
#
# 01043a, 01043b, 01043c and 01043d are four printings of Wakanda Forever!.
# They agree byte for byte on printed text, they all run
# `cards/pack/core/black_panther/01043a.py`, and b, c and d carry an
# `{"kind": "ability", "card_id": "01043a"}` link to a. The one thing that
# differs is the icon -- energy, mental, physical, wild -- which is why
# `Coverage.Equivalents()` declines to credit a's scenarios to the other three:
# its `Identity()` compares the engine's attribute block and that block carries
# `RES`. That refusal is correct, because two printings are not interchangeable
# against a cost that names a colour. Until this step existed the tool counted
# four cards of work while the vocabulary could express one claim.
#
# ---------------------------------------------------------------------------
# How unaffordable options are asserted
#
# `I am not offered "<option>" on "<card>"` observes the menu rather than
# declaring what a hand can pay. That distinction matters because resources in
# play, discounts, targets and other players can change affordability. Vision's
# negative case lives in `01068-vision.feature`; this file keeps its focus on
# what each card prints and the positive proof that an energy icon pays energy.

Feature: Resource icons

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"
    And I am in hero form

  # --------------------------------------------------------------------------
  # The four printings of Wakanda Forever!
  #
  # One scenario per printing rather than one naming all four, so a printing
  # whose icon changed loses its own coverage and not the other three's. Each
  # names the card by id, because all four answer to the printed name and a
  # bare "Wakanda Forever!" would be ambiguous with any other in hand.
  #
  # Each pins the icon it has *and* the three it does not. Without the zeroes an
  # engine that gave every card one of each colour would satisfy every scenario
  # here.

  @card:01043a
  Scenario: the first printing carries one energy icon and nothing else
    Given my hand is "01043a"

    Then "01043a" has 1 "energy" resource icon
    And "01043a" has 0 "mental" resource icons
    And "01043a" has 0 "physical" resource icons
    And "01043a" has 0 "wild" resource icons

  @card:01043b
  Scenario: the second printing carries one mental icon and nothing else
    Given my hand is "01043b"

    Then "01043b" has 1 "mental" resource icon
    And "01043b" has 0 "energy" resource icons
    And "01043b" has 0 "physical" resource icons
    And "01043b" has 0 "wild" resource icons

  @card:01043c
  Scenario: the third printing carries one physical icon and nothing else
    Given my hand is "01043c"

    Then "01043c" has 1 "physical" resource icon
    And "01043c" has 0 "energy" resource icons
    And "01043c" has 0 "mental" resource icons
    And "01043c" has 0 "wild" resource icons

  @card:01043d
  Scenario: the fourth printing carries one wild icon and nothing else
    Given my hand is "01043d"

    Then "01043d" has 1 "wild" resource icon
    And "01043d" has 0 "energy" resource icons
    And "01043d" has 0 "mental" resource icons
    And "01043d" has 0 "physical" resource icons

  # --------------------------------------------------------------------------
  # Two icons on one card

  @card:01044
  Scenario: Vibranium carries two wild icons
    # A resource card with no ability text, so its icons are the whole of it.
    # The count needed two scenarios to bound from either side before this step
    # existed -- a cost of 2 that one Vibranium pays alone says "at least 2",
    # and a cost of 3 that consumes both says "no more than 2" -- and neither
    # of them says *wild*, because every cost in the core set is generic.
    Given my hand is "Vibranium"

    Then "Vibranium" has 2 "wild" resource icons
    And "Vibranium" has 0 "physical" resource icons
    And "Vibranium" has 0 "mental" resource icons
    And "Vibranium" has 0 "energy" resource icons

  # --------------------------------------------------------------------------
  # The icons are behaviour, not metadata
  #
  # Vision (01068) prints "Action: Spend an [energy] resource -> choose THW or
  # ATK", which is the only cost in the core set that names a single colour. The
  # energy printing of Wakanda Forever! pays it and is discarded doing so, which
  # is the same icon the first scenario in this file reads, observed from the
  # engine's side.
  #
  # The hand holds exactly one card, so there is nothing else the payment could
  # have come from and nothing for the runner to choose between.

  @card:01043a
  @card:01068
  Scenario: an energy icon pays a cost that names energy
    Given "Vision" is in play
    And my hand is "01043a"

    Then "01043a" has 1 "energy" resource icon

    When I choose "Action" on "Vision"
    Then I am prompted to choose one
      | THW |
      | ATK |

    When I choose "THW"
    Then "01043a" is in the "DiscardPile"
    And I have 0 cards in hand
    And I am not prompted again
