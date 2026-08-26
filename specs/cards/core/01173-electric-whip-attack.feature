# Printed: "When Revealed: Choose to either deal 1 damage to your hero for each
# upgrade you control or choose and discard an upgrade you control."
# "[star] Boost: If the villain is making an undefended attack, choose and
# discard an upgrade you control."
#
# Two abilities that need different setups, and both of them are choices.
#
# The When Revealed is observable from a bare reveal: `"Electric Whip Attack" is
# revealed` in the Given runs the reveal pipeline and the first decision the
# policy answers is the card's own two-way choice. What makes the option set
# worth pinning is that the *first* option's label carries the number -- the
# engine builds it as "deal N damage to your hero" from the upgrade count, so
# the same card offers a different question on a different board. Three of the
# scenarios below differ only in how many upgrades are in play, and each one
# asserts the label it produced.
#
# The Boost is not observable that way at all. A boost ability fires when the
# card becomes a boost card, which only happens during an enemy activation, so
# the last two scenarios play a real villain phase: the encounter deck is
# stacked top-first and the first card written is the boost card. Both stock a
# player deck as well, because a turn that ends with no deck eliminates the hero
# for drawing up -- see specs/rules/phase-structure.feature.
#
# Iron Man is the hero throughout because his identity carries no interrupt and
# no response, so nothing but this card asks a question. His hero-form hand size
# is 1, which is why the villain-phase scenarios draw exactly one card.

Feature: Electric Whip Attack

  Background:
    Given the scenario is "rhino"
    And the hero is "iron_man"

  @card:01173
  Scenario: the damage branch counts one for each upgrade and discards nothing
    # Both upgrades surviving is the load-bearing half. Two damage on the hero
    # is equally consistent with an engine that resolved the discard branch and
    # dealt the damage anyway.
    Given I am in hero form
    And "Arc Reactor" is in play
    And "Powered Gauntlets" is in play
    And "Electric Whip Attack" is revealed

    Then I am prompted to choose one
      | deal 2 damage to your hero                |
      | Choose and discard an upgrade you control |

    When I choose "deal 2 damage to your hero"
    Then I have 2 damage
    And "Arc Reactor" is in play
    And "Powered Gauntlets" is in play
    And "Electric Whip Attack" is in the "EncounterDiscardPile"
    And I am not prompted again

  @card:01173
  Scenario: the discard branch takes the chosen upgrade and deals no damage
    # The engine offers one option with two legal targets rather than one option
    # per card, so the option table says nothing about which upgrades are
    # eligible and the targets have to be asserted separately.
    #
    # Pepper Potts is the negative control: a Support the player controls, which
    # the printed text does not name. A scenario with only upgrades on the board
    # would pass whether or not the card-type filter existed.
    Given I am in hero form
    And "Arc Reactor" is in play
    And "Powered Gauntlets" is in play
    And "Pepper Potts" is in play
    And "Electric Whip Attack" is revealed

    Then I am prompted to choose one
      | deal 2 damage to your hero                |
      | Choose and discard an upgrade you control |
    And the legal targets for "Choose and discard an upgrade you control" are
      | Arc Reactor       |
      | Powered Gauntlets |
    And I cannot choose "Choose and discard an upgrade you control" targeting "Pepper Potts"

    When I choose "Choose and discard an upgrade you control" targeting "Arc Reactor"
    Then "Arc Reactor" is in the "DiscardPile"
    And "Powered Gauntlets" is in play
    And "Pepper Potts" is in play
    And I have 0 damage
    And I am not prompted again

  @card:01173
  Scenario: one upgrade makes it one damage, not a flat amount
    # The counting claim. Paired with the two-upgrade scenario above this is
    # what separates "1 damage for each upgrade" from any fixed number, and the
    # option label is where the engine says which it computed.
    Given I am in hero form
    And "Arc Reactor" is in play
    And "Electric Whip Attack" is revealed

    Then I am prompted to choose one
      | deal 1 damage to your hero                |
      | Choose and discard an upgrade you control |

    When I choose "deal 1 damage to your hero"
    Then I have 1 damage
    And "Arc Reactor" is in play
    And I am not prompted again

  @card:01173
  Scenario: with no upgrade to discard the card asks nothing and deals nothing
    # Both printed branches collapse at zero upgrades: the discard has no legal
    # target and the damage is zero. One option with no target left is the one
    # case the engine resolves without asking, so this scenario has no `When` at
    # all -- and `I am not prompted again` is what says the card ran and
    # finished rather than stalling.
    Given I am in hero form
    And "Electric Whip Attack" is revealed

    Then I have 0 damage
    And "Electric Whip Attack" is in the "EncounterDiscardPile"
    And I am not prompted again

  @card:01173
  Scenario: as a boost card on an undefended attack it discards an upgrade
    # The star boost. Electric Whip Attack is written first in the encounter
    # deck, so it is the card dealt face down to boost Rhino's activation; the
    # hero declines to defend, and the ability fires as part of that attack.
    Given I am in hero form
    And "Arc Reactor" is in play
    And "Powered Gauntlets" is in play
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Electric Whip Attack", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I am prompted to choose one
      | Discard |

    When I choose "Discard" targeting "Arc Reactor"
    Then "Arc Reactor" is in the "DiscardPile"
    And "Powered Gauntlets" is in play
    And it is round 2

  @card:01173
  Scenario: defending the same attack leaves both upgrades alone
    # The control for the scenario above, and the reason the boost half needs
    # two scenarios rather than one. "If the villain is making an undefended
    # attack" is a condition, and a condition is only pinned by a board where it
    # is false. The same encounter deck, the same upgrades, the same villain --
    # the only difference is that the hero defends.
    #
    # That the boost card reached the encounter discard pile is asserted
    # deliberately: without it, "both upgrades survived" would also be true of a
    # run where Electric Whip Attack never became a boost card at all.
    Given I am in hero form
    And "Arc Reactor" is in play
    And "Powered Gauntlets" is in play
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Electric Whip Attack", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I choose "Defense"
    Then "Arc Reactor" is in play
    And "Powered Gauntlets" is in play
    And "Electric Whip Attack" is in the "EncounterDiscardPile"
    And it is round 2
