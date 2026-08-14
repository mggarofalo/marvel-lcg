# Klaw, stage III. Printed: "Toughness. (This character enters play with a
# tough status card.)"
# "[star] Forced Interrupt: When Klaw attacks, give him 1 additional boost card
#  for this activation."
# Printed statistics: 22 hit points per hero, ATK 2, SCH 3.
#
# Stage III is the expert-mode card: `data/scenarios/klaw.json` stacks the
# villain deck as stage I then stage II, and `klaw_expert.json` as stage II
# then stage III, which is the printed "Klaw (II) and Klaw (III) instead for
# expert mode". So both transcripts here play the expert scenario and defeat
# stage II to advance -- 18 hit points against one hero, so 17 damage from a
# `Given` plus Captain Marvel's printed ATK 2 takes him off.
#
# What is specced here is what stage III does not share with the other two
# stages: its statistics, and Toughness. The additional boost card is the same
# script file 01113 runs and is pinned there against stage I's numbers;
# repeating it here would be one claim written twice.
#
# Toughness is worth pinning on this card rather than leaning on
# `specs/rules/keywords.feature`, because the keyword arrives here by a route
# no minion takes: nothing puts this card into play, it is the next stage of a
# villain already on the board, and "enters play with a tough status card" has
# to survive that.

Feature: Klaw (III)

  Background:
    Given the scenario is "klaw_expert"
    And the hero is "captain_marvel"

  @card:01115
  Scenario: advancing to stage III stands the villain up tough on his printed stats
    # The ally is here to land the second attack of the transcript: the hero
    # who defeated stage II is exhausted, and a status nothing tests is not
    # evidence that the status is real. Black Cat's printed ATK 1 is cancelled
    # in full and the tough card is spent absorbing it, which is what separates
    # a live status card from a flag the render happened to set.
    Given I am in hero form
    And "Black Cat" is in play
    And "Klaw" has 17 damage

    When I attack "Klaw"
    Then "Klaw" has 3 "printed_stage"
    And "Klaw" has 22 health
    And "Klaw" has 2 "attack"
    And "Klaw" has 3 "scheme"
    And "Klaw" is tough

    When I choose "attack" on "Black Cat"
    Then "Klaw" has 0 damage
    And "Klaw" is not tough

  @card:01115
  Scenario: the printed hit points are per hero
    # `22*`. A one-hero board cannot tell 22 from 22-per-hero, so this one is
    # played two-handed and the villain stands up on 44. Stage II is 18 per
    # hero, so two-handed he has 36 and 35 damage leaves him one short of the
    # advance -- which is why the damage figure differs from the scenario
    # above.
    Given the heroes are "captain_marvel", "iron_man"
    And I am in hero form
    And "Klaw" has 35 damage

    When I attack "Klaw"
    Then "Klaw" has 44 health
    And "Klaw" has 2 "attack"
    And "Klaw" has 3 "scheme"
    And "Klaw" is tough
