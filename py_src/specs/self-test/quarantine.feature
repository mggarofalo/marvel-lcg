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

  @self-test
  Scenario: an ordinal over cards the scenario did not create is FAIL-spec-wrong
    # Both cards named "Rhino" -- the stage-1 villain and the stage-2 card in
    # the villain deck -- were allocated by the engine during setup, so "#2"
    # would mean whichever the allocator reached second. Naming a zone is the
    # only honest way to say which one (MARVEL-42).
    Given I am in hero form
    Then "Rhino #2" has 0 damage

  @self-test
  Scenario: saying a card is in play twice is FAIL-spec-wrong
    # Given is declarative, so the second step resolves to the card the first
    # created and changes nothing. Left alone this reads as two minions and
    # runs as one, which is a spec that passes while proving the wrong thing.
    Given I am in hero form
    And "Hydra Mercenary" is in play
    And "Hydra Mercenary" is in play

    Then "Hydra Mercenary #2" has 3 health

  @self-test
  Scenario: a restriction the engine does not impose is FAIL-engine-suspected
    # `I cannot attack` has to be capable of failing, or every Guard, stun and
    # confuse scenario in specs/rules/ establishes nothing. Nothing here blocks
    # the hero, so the engine offers Rhino as a legal target and the claim is
    # refused with the targets it would have allowed.
    Given I am in hero form
    Then I cannot attack "Rhino"

  @self-test
  Scenario: a restriction about a card outside the game is FAIL-spec-wrong
    # "You cannot attack a card that is not in this game" is true and worthless.
    # An unresolvable subject is the one way a `cannot` could pass while saying
    # nothing at all, so it is refused rather than granted.
    Given I am in hero form
    Then I cannot attack "Galactus"
