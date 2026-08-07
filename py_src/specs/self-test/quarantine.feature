# These scenarios are wrong on purpose. They are the proof that the quarantine
# works: a validation run must classify them and keep them out of trusted.json,
# and `--trusted-only` must never execute them.
#
# Do not "fix" them. If one of these starts passing, the harness has stopped
# telling the truth.

Feature: Quarantine self-test

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  @self-test
  Scenario: a wrong expected value is FAIL-engine-suspected
    # The transcript runs cleanly and the assertion is simply wrong, which is
    # the shape of a real spec-versus-engine disagreement.
    Given I am in hero form

    When I attack "Rhino"
    Then "Rhino" has 99 health

  @self-test
  Scenario: an action the engine never offers is FAIL-spec-wrong
    # Attacking from alter-ego form is not a legal action, so the transcript
    # describes a game the engine will not play.
    Given I am in alter-ego form

    When I attack "Rhino"
    Then "Rhino" has 12 health

  @self-test
  Scenario: an assertion about a card outside the game is FAIL-spec-wrong
    Given I am in hero form
    Then "Galactus" has 1 health

  @self-test
  Scenario: a mid-resolution choice the transcript does not answer is FAIL-spec-wrong
    # The point of the transcript format. A batched format would silently pick
    # one of Nick Fury's three options and report a pass.
    Given I am in hero form
    And my hand is "Nick Fury", "Backflip", "Backflip", "Webbed Up", "Enhanced Spider-Sense"
    And "Shocker" is in play

    When I play "Nick Fury"
    Then "Shocker" has 4 damage

  @self-test
  Scenario: an unexpected extra prompt is FAIL-engine-suspected
    Given I am in hero form
    And my hand is "Nick Fury", "Backflip", "Backflip", "Webbed Up", "Enhanced Spider-Sense"
    And "Shocker" is in play

    When I play "Nick Fury"
    Then I am not prompted again
