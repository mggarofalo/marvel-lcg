# These scenarios are wrong on purpose. They are the proof that the quarantine
# works: a validation run must classify them and keep them out of trusted.json,
# and `--trusted-only` must never execute them.
#
# Do not "fix" them. If one of these starts passing, the harness has stopped
# telling the truth.

Feature: Quarantine self-test

  Background:
    Given the scenario "rhino"
    And the hero "spider_man"

  @self-test
  Scenario: A wrong expected value is FAIL-engine-suspected
    # The scenario runs cleanly and the assertion is simply wrong, which is the
    # shape of a real spec-versus-engine disagreement.
    Given "01001a" is in hero form
    When the player attacks "Rhino in VillainArea"
    Then "Rhino in VillainArea" has 99 health

  @self-test
  Scenario: An action the engine never offers is FAIL-spec-wrong
    # Attacking from alter-ego form is not a legal action, so the scenario
    # describes a game the engine will not play.
    Given "01001a" is in alter-ego form
    When the player attacks "Rhino in VillainArea"
    Then "Rhino in VillainArea" has 12 health

  @self-test
  Scenario: An assertion about a card outside the game is FAIL-spec-wrong
    Given "01001a" is in hero form
    Then "Galactus" has 1 health
