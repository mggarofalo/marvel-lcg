@core
Feature: Canonical Core setup
  A behavioral scene begins from the printed Core deal before any transcript
  arranges a boundary state. The deal itself is therefore executable authority.

  @behavior:rr:appendix-ii-setup.step.1:published-result
  @covers:behavior:rr:appendix-ii-setup.step.2:published-result
  @covers:behavior:rr:appendix-ii-setup.step.3:published-result
  @covers:behavior:rr:appendix-ii-setup.step.4:published-result
  @covers:behavior:rr:appendix-ii-setup.step.5:published-result
  @covers:behavior:rr:appendix-ii-setup.step.8:published-result
  @covers:behavior:rr:appendix-ii-setup.step.9:published-result
  @covers:behavior:rr:appendix-ii-setup.step.10:published-result
  @covers:behavior:rr:appendix-ii-setup.step.14:published-result
  @rr:appendix-ii-setup.step.1 @rr:appendix-ii-setup.step.2
  @rr:appendix-ii-setup.step.3 @rr:appendix-ii-setup.step.4
  @rr:appendix-ii-setup.step.5 @rr:appendix-ii-setup.step.8
  @rr:appendix-ii-setup.step.9 @rr:appendix-ii-setup.step.10
  @rr:appendix-ii-setup.step.14
  Scenario: The printed Spider-Man and Rhino deal reaches its opening state
    # Setup selects identities, sets their hit points, chooses a first player,
    # sets aside obligations and nemesis sets, selects the scenario, sets the
    # villain's hit points, creates the encounter deck, and draws opening hands.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 801  |
    When the dealt Core scene is inspected
    Then the game has 1 player
    And seat 1 is in alter-ego form
    And seat 1 has the first player token
    And seat 1 has 6 cards in hand
    And seat 1 has 34 cards in their player deck
    And card 01001b copy 0 has 10 remaining hit points
    And card 01094 copy 0 is the faceup villain
    And card 01094 copy 0 has 14 remaining hit points
    And card 01097b copy 0 is the faceup main scheme
    And card 01165 copy 0 is in the encounter deck
    And card 01166 copy 0 is in seat 1's set-aside nemesis pile
