# Printed: "Futurist -- Action: Look at the top 3 cards of your deck. Add 1 to
# your hand and discard the others. (Limit once per round.)"
#
# The alter-ego side of Iron Man. Three things are worth pinning and only one of
# them is the obvious one: which card reached hand, that the *other two* were
# discarded rather than left on the deck, and that the printed limit is enforced
# by the option set rather than by failing when used twice.

Feature: Tony Stark

  Background:
    Given the scenario is "rhino"
    And the hero is "iron_man"

  @card:01029b
  Scenario: the chosen card goes to hand and the other two are discarded
    Given I am in alter-ego form
    And my deck is "Repulsor Blast", "Mark V Armor", "Pepper Potts", "Backflip"

    When I choose "Futurist"
    When I choose "Futurist" targeting "Mark V Armor"

    Then I have 1 cards in hand
    # "and discard the others" is the half a scenario forgets. Without these
    # two, an engine that added the choice to hand and left the rest on top of
    # the deck would pass.
    And I have 2 cards in my discard pile
    And I have 1 cards in my deck

  @card:01029b
  Scenario: exactly the top three cards are offered, and the fourth is not
    # "Look at the top 3 cards of your deck" is a claim about the option's legal
    # targets, not about the option set -- the engine offers one option named
    # Futurist and the three cards are its targets. Before MARVEL-94 there was
    # no step that could say this and the scenario had to be dropped.
    Given I am in alter-ego form
    And my deck is "Repulsor Blast", "Mark V Armor", "Pepper Potts", "Backflip"

    When I choose "Futurist"
    Then the legal targets for "Futurist" are
      | Repulsor Blast |
      | Mark V Armor   |
      | Pepper Potts   |
    And I cannot choose "Futurist" targeting "Backflip"

    # The decision still has to be answered: a transcript that walks away from
    # a question the engine is asking is FAIL-spec-wrong, by design.
    When I choose "Futurist" targeting "Repulsor Blast"
    Then "Backflip" is in the "PlayerDeck"

  @card:01029b
  Scenario: the card added to hand is the one chosen, not the top of the deck
    # The deck is ordered, so an engine that took the top card regardless of the
    # answer would pass the counts in the scenario above and fail here.
    Given I am in alter-ego form
    And my deck is "Repulsor Blast", "Mark V Armor", "Pepper Potts", "Backflip"

    When I choose "Futurist"
    When I choose "Futurist" targeting "Pepper Potts"

    Then "Pepper Potts" is in the "HandsArea"
    And "Repulsor Blast" is not in play
    And "Backflip" is in the "PlayerDeck"

  @card:01029b
  Scenario: the printed once-per-round limit removes the option, it does not error
    # "(Limit once per round.)" -- the second use is prevented by Futurist not
    # being on the menu, which is why the whole option set is asserted rather
    # than just its absence. `Play` is here because the card just added to hand
    # is playable; leaving it out would make this scenario fail for a reason
    # that has nothing to do with the limit.
    Given I am in alter-ego form
    And my deck is "Repulsor Blast", "Mark V Armor", "Pepper Potts", "Backflip"

    When I choose "Futurist"
    When I choose "Futurist" targeting "Mark V Armor"

    Then I am prompted to choose one
      | Change Form |
      | Play        |
