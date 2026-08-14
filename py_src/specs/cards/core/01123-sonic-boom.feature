# Printed: "When Revealed: Either spend [energy] [mental] [physical] resources
# or exhaust each character you control."
# "[star] Boost: If this activation deals damage to you, exhaust your hero."
#
# A treachery whose When Revealed is a choice, which makes the option set the
# thing worth pinning. Both branches are asserted against the *other* branch's
# effect not happening, because "exhaust each character you control" and "spend
# three resources" are both invisible to an assertion that only looks at one.
#
# ---------------------------------------------------------------------------
# The star boost is a second, separate ability, and it needs a real villain
# phase: a boost card is never revealed, so nothing a `Given` can do puts one
# into play. The three boost scenarios below stock the encounter deck and walk
# round 1 to the villain's activation.
#
# Klaw stage I is what makes the numbers here work, and it is worth writing
# down because none of them are the printed attack value. He prints ATK 0 and
# "Forced Interrupt: When Klaw attacks, give him 1 additional boost card for
# this activation", so his activation takes *two* cards off the top of the
# encounter deck and the attack is 0 plus whatever boost icons those two carry.
# Sonic Boom is a [star] boost with no icons of its own. So:
#
#   "Sonic Boom", "Hydra Mercenary"  ->  0 + 0 + 1  =  1 damage
#   "Sonic Boom", "Sonic Boom"       ->  0 + 0 + 0  =  0 damage
#
# which is what gives the negative case below a boosted activation that
# genuinely deals nothing, rather than one that was never an attack at all.
#
# Spider-Sense is a beat in every one of them. Spider-Man's identity interrupt
# fires when the villain initiates an attack, and the harness never answers a
# decision the transcript omits, so declining it is written out. Pepper Potts is
# the deck filler because it is a Support that sits inert in hand -- the first
# draft used Backflip, whose own interrupt opens a play window in the middle of
# the villain's attack that has nothing to do with this card.

Feature: Sonic Boom

  Background:
    Given the scenario is "klaw"
    And the hero is "spider_man"

  @card:01123
  Scenario: the exhaust branch exhausts the ally as well as the hero
    # "each character you control" is the claim -- a scenario with only a hero
    # in play would pass on an engine that exhausted just the hero.
    Given I am in hero form
    And "Black Cat" is in play
    And my hand is "Backflip", "Backflip", "Backflip", "Backflip", "Backflip"
    And "Sonic Boom" is revealed

    Then I am prompted to choose one
      | Spend [[energy]][[mental]][[physical]] |
      | Exhaust each character you control     |

    When I choose "Exhaust each character you control"
    Then I am exhausted
    And "Black Cat" is exhausted

  @card:01123
  Scenario: paying the three resources leaves every character ready
    Given I am in hero form
    And "Black Cat" is in play
    And my hand is "Haymaker", "Enhanced Spider-Sense", "Backflip", "Backflip", "Backflip"
    And "Sonic Boom" is revealed

    Then I am prompted to choose one
      | Spend [[energy]][[mental]][[physical]] |
      | Exhaust each character you control     |

    When I choose "Spend [[energy]][[mental]][[physical]]"
    Then I am not exhausted
    And "Black Cat" is not exhausted
    # Without this the scenario passes on an engine that resolved nothing at
    # all: "not exhausted" is the state the board was already in. Three
    # resources spent is three cards out of a five-card hand.
    And I have 2 cards in hand

  @card:01123
  Scenario: as a boost card it exhausts the hero the activation damaged
    # The star boost. Declining to defend does not exhaust a hero by itself, so
    # "I am exhausted" here is this card's doing and nothing else's; 1 damage is
    # the boosted attack landing, which is the condition the ability is printed
    # with.
    Given I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Sonic Boom", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then I am prompted to choose one
      | Spider-Sense |

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I have 1 damage
    And I am exhausted
    # A boost card is discarded when the activation ends. Asserting it lands in
    # the encounter discard pile is what says it was the boost card rather than
    # the encounter card that gets dealt afterwards -- had it been revealed, its
    # When Revealed choice would have stopped the transcript here.
    And "Sonic Boom" is in the "EncounterDiscardPile"
    And it is round 2

  @card:01123
  Scenario: an activation that deals no damage does not exhaust the hero
    # "If this activation deals damage" is a condition, and this is the case
    # that separates it from "if this activation happens". Both boost cards are
    # Sonic Booms, so the attack totals 0 and lands nothing -- the ability is on
    # the board twice and neither copy fires.
    #
    # An engine that read the boost as an unconditional "exhaust your hero" is
    # indistinguishable from a correct one on the scenario above, and fails
    # here. That is the whole reason this scenario exists.
    Given I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Sonic Boom", "Sonic Boom", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then I am prompted to choose one
      | Spider-Sense |

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I have 0 damage
    And I am not exhausted
    And "Sonic Boom #1" is in the "EncounterDiscardPile"
    And "Sonic Boom #2" is in the "EncounterDiscardPile"
    And it is round 2

  @card:01123
  Scenario: damage taken by a defending ally is not damage to you
    # The other half of the condition: "damage to you". The activation deals its
    # damage, so the scenario above cannot catch this one -- the difference is
    # only in who took it. Black Cat defends, takes the 1, and the hero is
    # untouched and stays ready.
    #
    # Black Cat exhausts because she defended, which is the ordinary rule and
    # not this card. The assertion that carries the claim is "I am not
    # exhausted" next to "Black Cat has 1 damage": the activation did deal
    # damage, and the hero was still not exhausted.
    Given I am in hero form
    And "Black Cat" is in play
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Sonic Boom", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then I am prompted to choose one
      | Spider-Sense |

    When I pass
    Then I am prompted to choose one
      | Defense |
      | Defense |

    When I choose "Defense" on "Black Cat"
    Then I have 0 damage
    And I am not exhausted
    And "Black Cat" has 1 damage
    And "Black Cat" is exhausted
    And "Sonic Boom" is in the "EncounterDiscardPile"
    And it is round 2
