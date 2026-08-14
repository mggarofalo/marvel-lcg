# Concussive Blow, printed once and scripted twice.
#
# Printed (both ids, byte-identical): "Hero Action (attack): Confuse an enemy.
# If you paid for this card using a [physical] resource, deal 3 damage to that
# enemy."
#
# 05031 (Ms. Marvel) and 41014 (Psylocke) are one printed card with two script
# files. They disagreed on which enemies are legal targets: 05031 restricted the
# target to enemies that can *take* a confused status, so an enemy that is
# already confused -- and every STALWART villain, which can never be confused at
# all -- was not a legal target and the card could not be played at it.
#
# The printed card has a second clause. Three damage to an enemy that cannot be
# confused is a real effect and a reason a player would choose that target, so
# the restriction is wrong. The engine reserves `canbe_confused=True` for a
# target whose only payoff is the status: 01011 Spider-Woman ("confuse the
# villain"), 37012 Dazzler ("confuse an enemy"), and the *second* target of
# 42003 Adaptive Plumage, where the confuse clause has its own target.
#
# Concussive Blow costs 3 and the [physical] clause needs one physical resource
# among what paid for it. Backflip prints a physical resource, so a hand of
# three Backflips pays the cost and satisfies the clause at the same time.

Feature: Concussive Blow

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form

  @card:05031
  Scenario: 05031 confuses the enemy and deals 3 damage when a physical resource paid for it
    Given my hand is "05031", "Backflip", "Backflip", "Backflip"

    When I play "05031" targeting "Rhino"
    Then "Rhino" is confused
    And "Rhino" has 3 damage
    And I am not prompted again

  @card:41014
  Scenario: 41014 confuses the enemy and deals 3 damage when a physical resource paid for it
    Given my hand is "41014", "Backflip", "Backflip", "Backflip"

    When I play "41014" targeting "Rhino"
    Then "Rhino" is confused
    And "Rhino" has 3 damage
    And I am not prompted again

  @card:05031
  Scenario: 05031 can still be played at an enemy that is already confused
    # The scenario the two files disagreed on. An already-confused Rhino cannot
    # take the status a second time, so under the restriction it was not a legal
    # target and the play was refused outright. The damage clause is the whole
    # point of choosing it.
    Given my hand is "05031", "Backflip", "Backflip", "Backflip"
    And "Rhino" is confused

    When I play "05031" targeting "Rhino"
    Then "Rhino" has 3 damage
    And "Rhino" is confused
    And I am not prompted again

  @card:41014
  Scenario: 41014 can still be played at an enemy that is already confused
    Given my hand is "41014", "Backflip", "Backflip", "Backflip"
    And "Rhino" is confused

    When I play "41014" targeting "Rhino"
    Then "Rhino" has 3 damage
    And "Rhino" is confused
    And I am not prompted again

  @card:05031
  @card:41014
  Scenario: paid without a physical resource, it confuses and deals nothing
    # The control for the damage clause. Enhanced Spider-Sense, Crisis
    # Interdiction and Alpha Flight Station print [energy], [mental] and
    # [energy] -- three resources, none physical, exactly the cost. Without this
    # scenario "Rhino has 3 damage" above is consistent with an engine that
    # deals 3 unconditionally.
    #
    # Spider-Woman is deliberately *not* the filler here: she prints a wild
    # resource, which satisfies "using a [physical] resource" and pays the
    # damage out anyway.
    Given my hand is "05031", "Enhanced Spider-Sense", "Crisis Interdiction", "Alpha Flight Station"

    When I play "05031" targeting "Rhino"
    Then "Rhino" is confused
    And "Rhino" has 0 damage
    And I am not prompted again
