# How many targets an effect takes. MARVEL-120 / MARVEL-134.
#
# A selector has a floor and a ceiling, and the two are pinned by different
# assertions. `Then the target minimum for "<option>" is <n>` reads the floor;
# `Then the target maximum for "<option>" is <n>` reads the ceiling. Both are
# equalities over the live option range rather than inequalities inferred from
# whether one particular selection resolved.
#
# `Then the target maximum for "<option>" is <n>` is an equality rather than the
# "at most N" it is tempting to spell it as: a ceiling of 2 does not satisfy it.
# The minimum spelling follows the same rule: "at least N" would wrongly accept
# a floor that had become too high.
#
# The steps read `target_num_range[0]` and `[1]` at the decision: the
# **effective** range the browser receives. `Selector.GetTargetRange` clamps the
# printed maximum to the board, and if a dynamic floor crosses that ceiling it
# lowers the floor to the same value. `range="All"` computes both ends from the
# number of legal candidates. A raw floor that cannot be satisfied filters the
# option out before a decision, so a minimum assertion is unresolvable rather
# than pretending to expose a raw card-script value. Consequences in this file:
#
#   * the claim only bites on a board offering **more** candidates than the
#     ceiling. With three cards in the pile an engine that had lost the maximum
#     entirely still answers 3.
#   * for a selector with no printed maximum the number is the board's, not the
#     card's -- which is a claim worth making too, because "each X you control"
#     and "up to 3 X" differ in exactly that.
#   * for "each," the minimum moves with the maximum: a two-upgrade board is
#     2..2 and a three-upgrade board is 3..3.
#
# ---------------------------------------------------------------------------
# Under specs/rules/ rather than specs/cards/ because the claim is about how a
# selection is bounded; the two cards are the core set's only instances of the
# two shapes. `specs/cards/core/01042-ancestral-knowledge.feature` covers what
# Ancestral Knowledge *does* with the cards it chooses, and this file does not
# repeat it.

Feature: Target counts

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"

  # --------------------------------------------------------------------------
  # A printed ceiling: Ancestral Knowledge (01042)
  #
  # "Alter-Ego Action: Choose up to 3 different cards in your discard pile and
  #  shuffle them into your deck."
  #
  # `range=(1, 3)` in the card script. Four cards in the pile, so the board
  # offers one more candidate than the card allows and the 3 is the card's.

  @card:01042
  Scenario: up to 3 is a ceiling of 3 even with four cards to choose from
    # Both halves of the selection stated together, which is the pairing the
    # ceiling step exists to complete: `the legal targets` says which cards may
    # be chosen and this says how many of them.
    Given I am in alter-ego form
    And my hand is "01042", "Vibranium"
    And my discard pile is "Panther Claws", "Tactical Genius", "Energy Daggers", "Vibranium Suit"

    Then the legal targets for "Play" are
      | Panther Claws  |
      | Tactical Genius |
      | Energy Daggers |
      | Vibranium Suit |
    And the target minimum for "Play" is 1
    And the target maximum for "Play" is 3

  @card:01042
  Scenario: a fifth card in the pile does not raise the ceiling
    # The control for the scenario above. If the number 3 came from the board
    # rather than from the card it would move here, and it does not.
    Given I am in alter-ego form
    And my hand is "01042", "Vibranium"
    And my discard pile is "Panther Claws", "Tactical Genius", "Energy Daggers", "Vibranium Suit", "Combat Training"

    Then the target maximum for "Play" is 3

  # --------------------------------------------------------------------------
  # No printed ceiling: Wakanda Forever! (01043a)
  #
  # "Hero Action: Resolve the "Special" ability on each [[Black Panther]]
  #  upgrade you control in any order."
  #
  # `range="All"` in the card script. "Each" is not a number, so the ceiling is
  # however many upgrades are in play -- which is the contrast that makes the
  # two scenarios above a claim about Ancestral Knowledge and not about
  # selectors in general.
  #
  # It was `range=(1, "All")` before MARVEL-129, making "each" resolvable one at
  # a time. `range="All"` moves both ends together; the paired assertions below
  # are the direct control that the old 1..N range cannot satisfy.

  @card:01043a
  Scenario: an each-you-control effect takes as many targets as the board offers
    Given I am in hero form
    And my hand is "01043a", "Vibranium"
    And "Panther Claws" is in play
    And "Tactical Genius" is in play

    Then the target minimum for "Play" is 2
    And the target maximum for "Play" is 2

  @card:01043a
  Scenario: a third upgrade raises that ceiling to three
    # Combat Training is deliberately not used here: it is an upgrade the hero
    # controls and it is not a [[Black Panther]] upgrade, so it would not move
    # the number. Energy Daggers is the third that does.
    Given I am in hero form
    And my hand is "01043a", "Vibranium"
    And "Panther Claws" is in play
    And "Tactical Genius" is in play
    And "Energy Daggers" is in play
    And "Combat Training" is in play

    Then the target minimum for "Play" is 3
    And the target maximum for "Play" is 3
