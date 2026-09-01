# Status cards: stunned, confused, tough. Rulebook behavior. the original investigation.
#
# Toughness is a printed keyword as well as a status, so the keyword half lives
# in keywords.feature; what is here is the status a card holds.
#
# ---------------------------------------------------------------------------
# Stun and confuse are restrictions, and restrictions need their own assertion.
#
# A stunned hero is still offered `Attack`. The engine does not remove the
# option -- it empties the option's legal targets, so the restriction shows up
# in neither the option set nor any card's state, and both `I am prompted to
# choose one` and every `Then` about the board pass straight over it. `I cannot
# attack "<card>"` is the step that sees it (the original investigation).
#
# Each restriction is written next to the action it does *not* touch. "A stunned
# hero cannot attack" is only worth asserting alongside "a stunned hero can
# still thwart"; without the second, an engine that had forgotten how to do
# anything at all would satisfy the first.

Feature: Status cards

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

  # --------------------------------------------------------------------------
  # Stun

  @rr:stun-stunned.1
  @rr:attack-player-ability-type.1.1
  Scenario: a stunned hero cannot attack
    Given "Spider-Man" is stunned

    Then I cannot attack "Rhino"

  @rr:stun-stunned
  @rr:thwart.1
  Scenario: a stunned hero can still thwart
    # Stun is about attacking. Spider-Man is printed THW 1.
    Given "Spider-Man" is stunned
    And the main scheme has 5 threat

    When I thwart "The Break-In!"
    Then the main scheme has 4 threat

  @rr:attack-player-ability-type.1
  Scenario: an unstunned hero can attack
    # The control for the restriction: `I cannot attack` has to be capable of
    # failing, and on this board the same hero takes his printed 2 off Rhino.
    When I attack "Rhino"
    Then "Rhino" has 2 damage

  @rr:stun-stunned.1
  @rr:status-cards.2
  Scenario: a stunned villain's attack is cancelled and the status spent
    # The other direction. The villain activates, the stun cancels the attack,
    # and the status card is discarded doing it -- so the hero takes nothing and
    # Rhino is no longer stunned.
    #
    # One beat, and the whole villain phase happens inside it: the hero ends
    # their turn and is never asked to defend, because there is no attack to
    # defend against. The unstunned round is three beats (see
    # timing-priority.feature) -- interrupt window, defence, damage window.
    Given "Rhino" is stunned

    When I pass
    Then I have 0 damage
    And "Rhino" is not stunned
    And it is round 2

  # --------------------------------------------------------------------------
  # Confuse

  @rr:confuse-confused.1
  @rr:thwart.1.1
  Scenario: a confused hero cannot thwart
    Given "Spider-Man" is confused
    And the main scheme has 5 threat

    Then I cannot thwart "The Break-In!"

  @rr:confuse-confused
  @rr:attack-player-ability-type.1
  Scenario: a confused hero can still attack
    # Confuse is about thwarting, and the mirror of the stun pair above.
    Given "Spider-Man" is confused

    When I attack "Rhino"
    Then "Rhino" has 2 damage

  @rr:thwart.1
  Scenario: an unconfused hero can thwart
    # The control.
    Given the main scheme has 5 threat

    When I thwart "The Break-In!"
    Then the main scheme has 4 threat
