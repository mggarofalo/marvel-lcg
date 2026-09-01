# Printed: "Foresight -- Setup: Search your deck for a [[Black Panther]] upgrade
# and add it to your hand. Shuffle your deck."
#
# Printed statistics: 11 hit points, REC 4, hand size 6.
#
# ---------------------------------------------------------------------------
# This header used to say Foresight was not reachable from a puzzle scene. It is
# reachable now, and the reason it was not is worth keeping rather than deleting,
# because it is the shape of the whole `at setup` problem.
#
# `World` resolves character setup abilities at step 16 of game setup
# (`player.GetIdentity().Setup(False)`, game/world/world.py), and the harness
# applies every `Given` step *after* `GameSetup()` returns -- see `RunCaseInternal`
# in tools/spec/harness.py. A puzzle scene was built with `"player_deck": []`, so
# at the moment Foresight fired there was nothing in the deck to find and no
# `Given` could put anything there in time.
#
# the original investigation closed that with a step that is not a `Given` at all:
#
#     Given my deck at setup is "Vibranium Suit", "Combat Training", "Vibranium"
#
# is part of the **scene** the engine sets up from, alongside `the scenario is`,
# so the deck exists before step 16 rather than after it. It was one of 49 cards
# measured with this gap. Two consequences, both of them consequences of it
# being a real deck rather than a stack a `Given` placed: the setup shuffle
# destroys the written order, and the cards are the engine's rather than the
# scenario's so `#N` cannot name them.
#
# There used to be a third -- "a searching hero needs at least two cards" -- and
# it was not a property of the step at all. `SelectorEnd.DoShuffle` asserted its
# source deck was non-empty, so a one-card deck raised. That is the original investigation, it
# is fixed, and the last Foresight scenario below is the board it used to fail
# on. An authoring rule that is really a bug wearing a rule's clothes is worth
# deleting loudly: the next author would have read it as arbitrary.
#
# The two Foresight scenarios are the two arms of the script's `if face:`. The
# printed sentence does not spell the second one out, which is exactly why it is
# worth a scenario: "add it to your hand" says nothing about what happens when
# the deck holds no such card, and a port that took the top card instead, or
# raised, would pass the first scenario and fail the second.
#
# A puzzle scene deals no opening hand, so the hand after setup holds exactly
# what Foresight put in it and nothing else. That is what makes `I have 1 cards
# in hand` a claim about this ability rather than about a draw step.

Feature: T'Challa

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"
    And I am in alter-ego form

  @card:01040b
  Scenario: Foresight searches the deck for a Black Panther upgrade and adds it to hand
    # Combat Training is the control on the trait half of the filter: it is an
    # Upgrade, it is in the same deck, and it has no [[Black Panther]] trait, so
    # an engine that searched for "an upgrade" would have two candidates and
    # could answer either. Vibranium is the control on the type half -- a
    # resource, not an upgrade. Both stay in the deck, and the hand holds one
    # card because the scene deals no opening hand.
    Given my deck at setup is "Vibranium Suit", "Combat Training", "Vibranium"

    Then "Vibranium Suit" is in the "HandsArea"
    And I have 1 cards in hand
    And I have 2 cards in my deck
    And "Combat Training" is in the "PlayerDeck"
    And I am not prompted again

  @card:01040b
  Scenario: with no Black Panther upgrade in the deck Foresight adds nothing
    # The empty arm. The same deck minus the one card that matches: nothing is
    # added, nothing is discarded, and the deck is the size it started at. An
    # engine that fell back on the top card would put Combat Training or a
    # Vibranium in hand here and pass the scenario above regardless.
    Given my deck at setup is "Combat Training", "Vibranium", "Vibranium"

    Then I have 0 cards in hand
    And I have 3 cards in my deck
    And "Combat Training" is in the "PlayerDeck"
    And I am not prompted again

  @card:01040b
  Scenario: Foresight may take the only card in the deck
    # "Shuffle your deck" with nothing left to shuffle. the original investigation:
    # `SelectorEnd.DoShuffle` asserted its source deck was non-empty directly
    # above a branch written to handle it being empty, so this board raised --
    # and `Log.OnCrash` swallows on a release build, so what a run actually
    # produced was Vibranium Suit stranded in the processing area, no message,
    # and a game that carried on around it.
    #
    # This is the scenario the authoring rule "give a searching hero at least
    # two cards" existed to avoid. It was never a harness rule.
    Given my deck at setup is "Vibranium Suit"

    Then "Vibranium Suit" is in the "HandsArea"
    And I have 1 cards in hand
    And I have 0 cards in my deck
    And I am not prompted again

  @card:01040b
  Scenario: Recover heals his printed REC of 4
    # 6 damage in, 2 damage out. The alter-ego exhausts to do it, which is what
    # separates "the recover action ran" from "something healed him".
    Given "me" has 6 damage

    When I choose "Recover"
    Then I have 2 damage
    And "me" has 9 health
    And I am exhausted
    And I am not prompted again

  @card:01040b
  Scenario: the end phase draws him back up to his printed hand size of 6
    # 6, against Black Panther's 5 in 01040a-black-panther.feature. The two
    # scenarios differ only in which side is face up, so an engine that read one
    # hand size for the whole identity fails exactly one of them.
    Given my deck is "Vibranium", "Vibranium", "Vibranium", "Vibranium", "Vibranium", "Vibranium", "Vibranium", "Vibranium"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I pass
    Then I have 6 cards in hand
    And I have 2 cards in my deck
    And it is round 2
