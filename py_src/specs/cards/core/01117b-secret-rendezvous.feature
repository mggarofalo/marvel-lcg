# Secret Rendezvous, stage 2B. Printed: "If this stage is completed, the
# players lose the game."
# Printed statistics: starting threat 0, 8 threat per hero to complete,
# escalation 1 per hero.
#
# The card has no script: the losing condition and the starred numbers are
# implemented generically, for every main scheme, in
# `game/card/face/attribute/`. That does not make them unassertable -- it makes
# them the whole card, and the numbers are what a port would get wrong.
#
# Two claims that need separate boards: the threshold is a threshold (7 is not
# 8), and it is per hero. The advance chain reaches 2B the way a game does, by
# completing 1B at its own per-hero threshold and letting stage 2A resolve and
# advance; the encounter deck is stocked first because 2A digs through it.

Feature: Secret Rendezvous (2B)

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"
    And I am in hero form

  @card:01117b
  Scenario: completing the stage loses the game rather than winning it
    # The one thing the card says. Note which way round the outcome is: every
    # other completed scheme in this game is progress for somebody, and this
    # one ends the game against the players -- an engine that treated a
    # completed main scheme as a completed main scheme reports the wrong side.
    Given the encounter deck is "Armored Guard", "Armored Guard", "Armored Guard"
    And the main scheme has 6 threat
    And the main scheme has 8 threat

    Then the game is over
    And the players lost

  @card:01117b
  Scenario: one short of the threshold is not completion
    # The control. Without it the scenario above is satisfied by an engine that
    # ends the game whenever threat is placed on this stage at all.
    Given the encounter deck is "Armored Guard", "Armored Guard", "Armored Guard"
    And the main scheme has 6 threat
    And the main scheme has 7 threat

    Then the game is not over
    And the main scheme has 7 threat
    And "the main scheme" has 8 "target_threat"
    And "the main scheme" has 1 "escalation_threat"
    And "the main scheme" has 2 "printed_stage"

  @card:01117b
  Scenario: the threshold and the escalation are both per hero
    # `8*` and `1*`. At two heroes the stage completes at 16 and escalates by
    # 2, and 12 threat -- which would have ended the game at one hero, twice
    # over -- leaves it standing.
    Given the heroes are "captain_marvel", "iron_man"
    And the encounter deck is "Armored Guard", "Armored Guard", "Armored Guard"
    And the main scheme has 12 threat

    Then the game is not over
    And "the main scheme" has 16 "target_threat"
    And "the main scheme" has 2 "escalation_threat"
    And "the main scheme" has 2 "printed_stage"
