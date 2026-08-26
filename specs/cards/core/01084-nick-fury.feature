# Printed: "Forced Response: After Nick Fury enters play, choose one: remove 2
# threat from a scheme, draw 3 cards, or deal 4 damage to an enemy. At the end
# of the round, if Nick Fury is still in play, discard him."
#
# The card the format was designed around: a mid-resolution choice that a
# batched format cannot express, because the number and content of the prompts
# is behavior rather than something derivable from the printed text.
#
# Two abilities, and the first three scenarios are all about the first one. The
# fourth is the discard, which is the only one that needs a whole round walked.

Feature: Nick Fury

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  @card:01084
  Scenario: damage is dealt to the chosen enemy, not the first one
    Given I am in hero form
    And my hand is "Nick Fury", "Backflip", "Backflip", "Webbed Up", "Enhanced Spider-Sense"
    And "Shocker" is in play

    When I play "Nick Fury"
    Then I am prompted to choose one
      | Draw 3 cards              |
      | Deal 4 damage to an enemy |

    When I choose "Deal 4 damage to an enemy" targeting "Shocker"
    Then "Shocker" has 4 damage
    And "Rhino" has 0 damage
    And I am not prompted again

  @card:01084
  Scenario: the option to remove threat is not offered when no scheme has any
    # The card is printed as a three-way choice. The engine offers two, because
    # "remove 2 threat" has no legal target with the main scheme at zero. That
    # the option set is state-dependent is exactly why the prompt is asserted.
    Given I am in hero form
    And my hand is "Nick Fury", "Backflip", "Backflip", "Webbed Up", "Enhanced Spider-Sense"
    And "Shocker" is in play
    And the main scheme has 0 threat

    When I play "Nick Fury"
    Then I am prompted to choose one
      | Draw 3 cards              |
      | Deal 4 damage to an enemy |

  @card:01084
  Scenario: drawing puts three cards in hand
    Given I am in hero form
    And my hand is "Nick Fury", "Backflip", "Backflip", "Webbed Up", "Enhanced Spider-Sense"
    And my deck is "Backflip", "Backflip", "Backflip", "Enhanced Spider-Sense"

    When I play "Nick Fury"
    Then I am prompted to choose one
      | Draw 3 cards              |
      | Deal 4 damage to an enemy |

    When I choose "Draw 3 cards"
    Then I have 3 cards in hand
    And I have 1 card in my deck
    And I am not prompted again

  @card:01084
  Scenario: he is discarded at the end of the round, not at the end of the turn
    # The card's second ability, and the only one that needs a round walked.
    # "At the end of the round" is the claim, so the transcript checks he is
    # still in play *after* the turn has ended and the villain phase has begun
    # -- a scenario that only looked at round 2 would pass on an engine that
    # discarded him the moment his controller's turn finished.
    #
    # Both decks are stocked because a round draws from both: an unstocked
    # player deck eliminates the hero at the end of their turn and an unstocked
    # encounter deck stops the villain phase, and neither would be this card's
    # doing. Pepper Potts is the filler because it is a Support that sits inert
    # in hand -- the first draft used Backflip and the villain's attack opened a
    # play window for it that has nothing to do with Nick Fury.
    #
    # The damage branch is taken rather than the draw branch so that the hand is
    # empty at the end of the turn: with cards in hand the engine asks to
    # discard before drawing up, which is a beat about the end phase and not
    # about this card.
    Given I am in hero form
    And my hand is "Nick Fury", "Backflip", "Backflip", "Webbed Up", "Enhanced Spider-Sense"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I play "Nick Fury"
    Then I am prompted to choose one
      | Draw 3 cards              |
      | Deal 4 damage to an enemy |

    When I choose "Deal 4 damage to an enemy" targeting "Rhino"
    Then "Rhino" has 4 damage
    And "Nick Fury" is in play

    When I pass
    Then it is the villain phase
    And "Nick Fury" is in play
    And I am prompted to choose one
      | Spider-Sense |

    # Two Defense rows, because Nick Fury is an ally and an ally in play may
    # defend. That the option set grew is itself evidence he is still around at
    # the moment the villain activates.
    When I pass
    Then I am prompted to choose one
      | Defense |
      | Defense |

    When I pass
    Then it is round 2
    And "Nick Fury" is not in play
    And "Nick Fury" is in the "DiscardPile"
