# Rhino, stage III. Printed:
#
#   "Toughness. (This character enters play with a tough status card.)
#    When Revealed: Stun each hero."
#
# Printed 16 hit points per hero, ATK 4, SCH 1.
#
# ---------------------------------------------------------------------------
# This header used to say that stage III could not be put into play by any means
# the vocabulary offers, so its printed statistics and its Toughness had nothing
# to be asserted against. **That was false**, and the mistake is kept on the
# record because it is an easy one to make again.
#
# The premises were right. Stage III is expert-mode content -- The Break-In!
# prints "Rhino (I) and Rhino (II). (Rhino (II) and Rhino (III) instead for
# expert mode.)" -- and on the standard board 01096 really is in no zone at all,
# so `Given "01096" is in play` leaves it in the encounter discard pile and
# `Given the difficulty is expert` does not swap the villain deck for the expert
# pair. What does not follow is the conclusion, because `the difficulty is
# expert` is not how expert content is reached: it flips `campaign.expert` on the
# **standard** villain deck, which is a board no real game produces.
#
# An expert scenario is a different file with a different villain deck.
# `data/scenarios/rhino_expert.json` stacks the villain deck as 01095 then 01096,
# which is the printed "Rhino (II) and Rhino (III) instead", so
#
#     Given the scenario is "rhino_expert"
#
# opens with stage II standing in the villain area on 15 hit points and stage III
# behind it in the villain deck. Defeating stage II advances stage III into play
# with its printed hit points, ATK, SCH and its Toughness, which is what the last
# two scenarios below do. the original investigation measured 46 villain ids that appear only in
# an expert scenario file; 37 of them, this one included, are reached by
# defeating the stage before them, and 9 stand in the villain area at setup.
#
# ---------------------------------------------------------------------------
# "Each hero" is one branch, not two, on every board a scenario can build.
#
# The engine reads it as `Worlds.GetOnFieldHeroes`, which is the heroes in hero
# form. The first two scenarios are the two answers that has: a hero is stunned
# and an alter-ego is not. That second one is a real reading of the printed word
# "hero", not a technicality -- an engine that stunned the identity whatever
# form it was in would fail it.
#
# A second player cannot distinguish the loop from a single resolution here,
# because `I am in hero form` speaks for seat 1 only and there is no step that
# puts another player into hero form (the same limitation Under Attack's file
# records). So there is no third scenario for the "each" of "each hero"; a
# two-player board would measure the same one hero. **That gap is still real**
# and is recorded in specs/unreachable.json; nothing the original investigation added reaches it.
#
# ---------------------------------------------------------------------------
# There is no shared Background: the first two scenarios play the standard
# scenario, where revealing the card is the cheapest way to fire its ability, and
# the last two play the expert one, where the card is in the villain deck and
# arrives by advancing. The hero counts differ as well, and a per-hero hit point
# total cannot be pre-loaded once for boards with different numbers of players.

Feature: Rhino (III)

  @card:01096
  Scenario: revealing stage III stuns the hero
    # Stun is a status card and not damage: the hero carries it and his hit
    # points are untouched. `I cannot attack` is what the status actually does
    # -- the engine enforces stun by emptying the Attack option's legal targets,
    # so without that step an engine that handed out a status card nothing reads
    # would satisfy the assertion above it.
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And "01096" is revealed

    Then "Spider-Man" is stunned
    And I have 0 damage
    And I cannot attack "Rhino"
    And I am not prompted again

  @card:01096
  Scenario: an alter-ego is not a hero and is not stunned
    # The control on the printed word. Peter Parker is the same identity card
    # in the other form, and he keeps his ready status through the reveal.
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in alter-ego form
    And "01096" is revealed

    Then "Peter Parker" is not stunned
    And I am not in hero form
    And I am not prompted again

  @card:01096
  Scenario: advancing to stage III stands the villain up tough on its printed stats
    # The expert board. Stage II is printed 15 hit points at one hero, so 14
    # damage plus Spider-Man's printed ATK 2 defeats it and stage III advances
    # into play -- which is the only route this card has, since nothing puts a
    # villain stage into play directly.
    #
    # Black Cat is here to land the second attack of the transcript. The hero who
    # defeated stage II is exhausted *and* stunned by the reveal, and a status
    # nothing tests is not evidence that the status is real; the ally's printed
    # ATK 1 is cancelled in full and the tough card is spent absorbing it, which
    # is what separates a live status card from a flag the render happened to
    # set.
    Given the scenario is "rhino_expert"
    And the hero is "spider_man"
    And I am in hero form
    And "Black Cat" is in play
    And "Rhino" has 14 damage

    When I attack "Rhino"
    Then "Rhino" has 3 "printed_stage"
    And "Rhino" has 16 health
    And "Rhino" has 4 "attack"
    And "Rhino" has 1 "scheme"
    And "Rhino" is tough
    # The When Revealed again, this time fired by a real advance rather than by
    # a `Given`. The two scenarios above prove what it does; this proves it
    # happens on the board the card is actually printed for.
    And "Spider-Man" is stunned

    When I choose "attack" on "Black Cat"
    Then "Rhino" has 0 damage
    And "Rhino" is not tough

  @card:01096
  Scenario: the printed hit points are per hero
    # `16*`. A one-hero board cannot tell 16 from 16-per-hero, so this one is
    # played two-handed and the villain stands up on 32. Stage II is 15 per hero,
    # so two-handed it has 30 and 29 damage plus Spider-Man's printed ATK 2 is
    # what defeats it -- which is why the damage figure differs from the scenario
    # above.
    Given the scenario is "rhino_expert"
    And the heroes are "spider_man", "captain_marvel"
    And I am in hero form
    And "Rhino" has 29 damage

    When I attack "Rhino"
    Then "Rhino" has 32 health
    And "Rhino" has 4 "attack"
    And "Rhino" has 1 "scheme"
