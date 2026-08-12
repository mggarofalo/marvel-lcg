# Damage and threat: how each is dealt, prevented, carried and counted, and what
# happens when a scheme fills up. Rulebook behavior. MARVEL-23.
#
# Prevention lives in timing-priority.feature, next to the interrupt window it
# happens in, rather than here -- Backflip is about *when*, and splitting it
# from the windows it answers would leave both halves harder to read.

Feature: Damage and threat

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

  # --------------------------------------------------------------------------
  # Dealing damage

  Scenario: damage stays on the enemy it was dealt to
    # Spider-Man is printed ATK 2 and Hydra Mercenary is printed 3 hit points.
    Given "Hydra Mercenary #1" is in play

    When I attack "Hydra Mercenary #1"
    Then "Hydra Mercenary #1" has 2 damage
    And "Hydra Mercenary #1" has 1 health
    And "Rhino" has 0 damage

  Scenario: excess damage is lost rather than carried to another enemy
    # The minion has 1 hit point left and takes a 2-point attack. Without
    # overkill the extra point goes nowhere -- in particular not to the villain,
    # which is what overkill would change.
    Given "Hydra Mercenary #1" is in play
    And "Hydra Mercenary #1" has 2 damage

    When I attack "Hydra Mercenary #1"
    Then "Hydra Mercenary #1" is not in play
    And "Rhino" has 0 damage

  # --------------------------------------------------------------------------
  # Consequential damage
  #
  # An ally that uses its own ATK or THW takes 1 damage for doing so. Hellcat is
  # the ally used because she is the only core-set ally with no trigger of her
  # own on entering play -- every other one would open a prompt the transcript
  # would have to answer before it could get to the point.

  Scenario: an ally that attacks takes consequential damage
    # Hellcat is printed ATK 1 and 3 hit points.
    Given "Hellcat" is in play

    When I choose "attack" on "Hellcat" targeting "Rhino"
    Then "Rhino" has 1 damage
    And "Hellcat" has 1 damage
    And "Hellcat" has 2 health

  Scenario: an ally that thwarts takes consequential damage too
    # Printed THW 2. The consequential damage is for using the power, not for
    # attacking, so thwarting costs the same 1.
    Given "Hellcat" is in play
    And the main scheme has 5 threat

    When I choose "thwart" on "Hellcat" targeting "The Break-In!"
    Then the main scheme has 3 threat
    And "Hellcat" has 1 damage

  Scenario: the hero takes no consequential damage for the same actions
    # The rule is about allies. A hero attacking pays with an exhaust, not with
    # damage, and the contrast is the point.
    When I attack "Rhino"
    Then "Rhino" has 2 damage
    And I have 0 damage
    And I am exhausted

  # --------------------------------------------------------------------------
  # Threat

  Scenario: thwarting removes threat from the scheme it names
    Given the main scheme has 5 threat

    When I thwart "The Break-In!"
    Then the main scheme has 4 threat

  Scenario: threat accelerates once per round while the villain attacks
    # The Break-In! stage 1B is printed with 1 acceleration. In hero form the
    # villain attacks rather than schemes, so acceleration is the only threat
    # placed all round.
    #
    # Three passes, not two: Spider-Man's identity carries Spider-Sense, so the
    # villain's attack opens an interrupt window before the defence step. The
    # same round under a hero without one is two beats.
    Given the main scheme has 0 threat

    When I pass
    When I pass
    When I pass
    Then the main scheme has 1 threat
    And it is round 2

  Scenario: a completed main scheme ends the game
    # The Break-In! stage 1B is printed at 7 threat to complete, and "If this
    # stage is completed, the players lose the game." Six placed by the
    # scenario, the seventh by acceleration during the villain phase.
    Given the main scheme has 6 threat

    When I pass
    Then the game is over
    And it is the "Main Scheme Place Threat" phase

  Scenario: one short of the threshold does not end the game
    # The control. Same transcript, one less threat, and the round completes
    # normally -- so it is the threshold that ended the game above and not
    # merely the passage of a round.
    Given the main scheme has 5 threat

    When I pass
    When I pass
    When I pass
    Then the game is not over
    And the main scheme has 6 threat
    And it is round 2
