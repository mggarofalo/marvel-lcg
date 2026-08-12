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

  # --------------------------------------------------------------------------
  # The crisis icon
  #
  # While a scheme bearing a crisis icon is in play, threat cannot be removed
  # from the main scheme. It is printed as an icon rather than as text, so no
  # card's `text_plain` says so and nothing in a transcript names it: the icon is
  # `stats.scheme_crisis` in the dataset and `Crisis` in the engine's attributes.
  #
  # Four of the eighteen core side schemes carry it -- Crowd Control (01108),
  # Defense Network (01125), Under Attack (01151) and Personal Challenge (01161).
  # The other fourteen do not, which is what makes the pair of controls below
  # worth having: a scenario that only showed the thwart failing would be
  # consistent with "a side scheme in play breaks thwarting", which is false.
  #
  # This section exists because that false reading was filed as an engine bug
  # (MARVEL-86) on exactly this board. The engine was right. What the report
  # actually established was that the harness sends the correct target -- it
  # never asked whether the engine was correct to ignore it.

  Scenario: a crisis icon stops threat coming off the main scheme
    Given the main scheme has 5 threat
    And "Under Attack" is in play

    When I thwart "The Break-In!"
    Then the main scheme has 5 threat

  Scenario: the action is still taken and still costs the exhaust
    # The part that is easy to get wrong, and the reason this is not modelled as
    # a target restriction. The crisis icon does not remove the option or filter
    # the main scheme out of its legal targets -- `Then I cannot thwart` fails
    # here, because the engine will let you do it. It takes the exhaust and
    # removes nothing. A player can spend their whole turn on it.
    Given the main scheme has 5 threat
    And "Under Attack" is in play

    When I thwart "The Break-In!"
    Then the main scheme has 5 threat
    And I am exhausted

  Scenario: a second crisis scheme behaves the same way
    # Under Attack is not special. Crowd Control carries the same icon and no
    # relevant printed text, so what is being measured is the icon.
    Given the main scheme has 5 threat
    And "Crowd Control" is in play

    When I thwart "The Break-In!"
    Then the main scheme has 5 threat

  Scenario: a side scheme without the icon does not stop it
    # The control that makes the three above mean something. Bomb Scare is a
    # side scheme in play, in the same area, and the thwart lands normally.
    Given the main scheme has 5 threat
    And "Bomb Scare" is in play

    When I thwart "The Break-In!"
    Then the main scheme has 4 threat

  Scenario: two side schemes without the icon still do not stop it
    # And it is not the *count* of side schemes either.
    Given the main scheme has 5 threat
    And "Bomb Scare" is in play
    And "Drone Factory" is in play

    When I thwart "The Break-In!"
    Then the main scheme has 4 threat

  Scenario: the crisis scheme itself can still be thwarted
    # The icon locks the main scheme only. Removing threat from the crisis
    # scheme is how a player gets out from under it, so if this were blocked the
    # icon would be unremovable by play.
    Given the main scheme has 5 threat
    And "Under Attack" is in play

    When I thwart "Under Attack"
    Then the main scheme has 5 threat
    And "Under Attack" has 2 threat

  Scenario: an ally is stopped by the icon as well
    # Not a property of the hero's basic thwart. Black Cat is printed THW 1 and
    # her thwart is refused the same way, with her exhaust spent.
    Given the main scheme has 5 threat
    And "Black Cat" is in play
    And "Under Attack" is in play

    When I choose "thwart" on "Black Cat" targeting "The Break-In!"
    Then the main scheme has 5 threat
    And "Black Cat" is exhausted

  Scenario: the same ally thwart lands with no crisis in play
    Given the main scheme has 5 threat
    And "Black Cat" is in play

    When I choose "thwart" on "Black Cat" targeting "The Break-In!"
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
