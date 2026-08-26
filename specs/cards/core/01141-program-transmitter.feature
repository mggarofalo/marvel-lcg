# Printed: "Attach to Ultron.
# [star] Forced Response: After Ultron schemes, place 1 threat on each side scheme.
# Hero Action: Exhaust your hero and spend [mental] [mental] resources ->
# discard this card."
# 1 boost icon, and a [star] SCH of 1.
#
# Three printed lines and three scenarios, because the card does three separable
# things: it attaches, it adds to what Ultron schemes for and then spreads threat
# after he does, and it can be paid off.
#
# The [star] SCH is the easiest of the three to lose sight of and the easiest to
# check: Ultron (I) is printed SCH 1, so a scheme with this attached is worth 2,
# and the main scheme reads that in the same beat the side schemes read the
# Forced Response. An engine that implemented the response and dropped the
# statistic would put 1 on the main scheme and 1 on each side scheme, and only
# the main scheme number tells them apart.
#
# Rage of Ultron is what makes Ultron scheme here. The alternative is a villain
# phase, and on this board that is worse rather than merely longer: The Crimson
# Cowl 1B is printed 3 to complete, and a villain phase places its 1 acceleration
# *and* Ultron's boosted 2, which completes the stage and advances the main
# scheme out from under the assertion. Rage of Ultron makes him scheme once,
# outside a villain phase, for exactly his printed SCH plus this card's.
#
# The two side schemes are put into play rather than revealed, so each carries
# its printed starting threat and nothing else: Crowd Control 2, Invasive AI 3.
# Two different numbers on purpose -- "place 1 threat on each" and "set each to
# 3" would be the same board if they started level.

Feature: Program Transmitter

  Background:
    Given the scenario is "ultron"
    And the hero is "iron_man"

  @card:01141
  Scenario: it attaches to Ultron, adds 1 to his scheme, and spreads 1 to each side scheme
    Given I am in alter-ego form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip"
    And "Program Transmitter" is in play
    And "Crowd Control" is in play
    And "Invasive AI" is in play
    And "Rage of Ultron" is revealed

    # The attachment found the villain named on it.
    Then "Program Transmitter" is in the "UpgradesArea"
    # 2 on the main scheme: Ultron (I)'s printed SCH 1 plus this card's [star] 1.
    And the main scheme has 2 threat
    # 1 on each side scheme, from two different starting numbers.
    And "Crowd Control" has 3 threat
    And "Invasive AI" has 4 threat
    And I am not prompted again

  @card:01141
  Scenario: the response follows Ultron scheming, not any threat he causes to be placed
    # The control for the trigger, and it is a sharper one than "nothing
    # happened": Ultron does attack here, and his own printed Forced Response
    # then places 1 threat on the main scheme. So threat *is* placed by Ultron in
    # this transcript, and the side schemes still do not move -- "after Ultron
    # schemes" means the scheme activation and not threat placement in general.
    Given I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip"
    And "Ultron Drones" is in play
    And "Program Transmitter" is in play
    And "Crowd Control" is in play
    And "Invasive AI" is in play
    And "Rage of Ultron" is revealed

    When I pass
    When I choose "Place 1 threat on the main scheme"

    Then the main scheme has 1 threat
    And "Crowd Control" has 2 threat
    And "Invasive AI" has 3 threat
    And I have 2 damage
    And I am not prompted again

  @card:01141
  Scenario: the hero action exhausts my hero, spends two mental resources and discards it
    # One Genius is [mental][mental], so the printed cost is paid by a single
    # card and the hand is empty afterwards rather than one card lighter.
    #
    # Ultron's scheme value falling back to his printed 1 is what says the
    # attachment is really gone: the card leaving the upgrades area and the
    # statistic it was granting are two separate things, and an engine could
    # drop one without the other.
    Given I am in hero form
    And my hand is "Genius"
    And "Program Transmitter" is in play
    Then "Ultron" has 2 "scheme"

    When I choose "Hero Action" on "Program Transmitter"
    Then "Program Transmitter" is not in play
    And "Program Transmitter" is in the "EncounterDiscardPile"
    And "Ultron" has 1 "scheme"
    And I am exhausted
    And I have 0 cards in hand
    And I am not prompted again
