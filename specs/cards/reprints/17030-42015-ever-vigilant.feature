# Ever Vigilant, printed once and scripted twice.
#
# Printed (both ids, byte-identical): "Play only if your identity has the
# [[aerial]] trait. Hero Action: Ready your hero and remove 2 threat from the
# main scheme."
#
# 17030 (Sinister Motives / Star-Lord) and 42015 (Angel) are one printed card
# with two script files. The only difference between them was a `.SetLabel()`
# call with no arguments on 42015, and that is a **no-op**: `Ability.labels` is
# initialised to `[]`, `SetLabel` asserts it is still `[]` and then assigns
# `list(labels)`, which for zero arguments is `[]` again. Nothing records that
# the method was called. Building both ability lists confirms it -- both come
# out `(AbilityType.HeroAction,Play)` with `labels == []`.
#
# So this pair belongs with the four cosmetic ones, not with the behavioural
# six. The redundant call has been removed so the two files agree, and the
# scenarios below are the pair's regression net rather than a fix's proof.

Feature: Ever Vigilant

  Background:
    Given the scenario is "rhino"
    And the hero is "falcon"
    And I am in hero form
    And the main scheme has 4 threat

  @card:17030
  Scenario: 17030 readies the exhausted hero and removes 2 threat
    # The hero has to be exhausted to be a legal target, so the transcript
    # spends the basic attack first. Falcon is the AERIAL identity with the
    # quietest board: Angel's own hero form responds to every card played, which
    # would put a beat in the middle of this that has nothing to do with the
    # card. The "Eagle-Eyed" response after the event resolves is Falcon's and
    # is declined explicitly.
    Given my hand is "17030", "Backflip", "Backflip", "Backflip"

    When I attack "Rhino"
    Then I am exhausted

    When I play "17030"
    Then I am not exhausted
    And the main scheme has 2 threat

    When I pass
    Then I am not prompted again

  @card:42015
  Scenario: 42015 readies the exhausted hero and removes 2 threat
    Given my hand is "42015", "Backflip", "Backflip", "Backflip"

    When I attack "Rhino"
    Then I am exhausted

    When I play "42015"
    Then I am not exhausted
    And the main scheme has 2 threat

    When I pass
    Then I am not prompted again

  @card:17030
  Scenario: 17030 will not take a hero that is already ready
    # "Ready your hero" has nothing to do on a ready hero, and the option is
    # still offered -- the engine only skips the prompt when there is neither an
    # option nor a target left. So this is a claim about the target set, which
    # `I am prompted to choose one` cannot make.
    Given my hand is "17030", "Backflip", "Backflip", "Backflip"

    Then I am prompted to choose one
      | Attack       |
      | Thwart       |
      | Change Form  |
      | Play         |
    And I cannot choose "Play" targeting "me"

  @card:42015
  Scenario: 42015 will not take a hero that is already ready
    Given my hand is "42015", "Backflip", "Backflip", "Backflip"

    Then I am prompted to choose one
      | Attack       |
      | Thwart       |
      | Change Form  |
      | Play         |
    And I cannot choose "Play" targeting "me"
