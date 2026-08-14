# Armored Rhino Suit. Printed:
#
#   "Attach to Rhino.
#    Forced Interrupt: When any amount of damage would be dealt to Rhino, place
#    it here instead. Then, if there is at least 5 damage here, discard Armored
#    Rhino Suit."
#
# ---------------------------------------------------------------------------
# The redirected damage is counters on the attachment, not damage on it.
#
# An attachment has no hit points, so the engine has nowhere to put damage as
# damage: `Faces.PlaceCountersOn([this], damage, 'damage', ...)` puts it on as a
# named counter, and `"Armored Rhino Suit" has <n> damage` is refused with "it
# is a UpgradesArea card with no such value". The step that reads it is
# `has <n> "damage" counters`. That distinction is the reason the card can hold
# 8 when its threshold is 5 -- there is no pool to overflow.
#
# ---------------------------------------------------------------------------
# Three branches, from two printed sentences.
#
#   under the threshold   the damage moves and the suit stays on
#   at or over it         the damage still moves, and the suit goes
#   after it is gone      the villain takes damage normally again
#
# "Any amount" is what the second one is about, and it is why the big scenario
# uses Swinging Web Kick rather than two basic attacks: 8 in one blow is more
# than the threshold and more than the suit could plausibly be thought to
# absorb, and every point of it still lands on the attachment. The villain takes
# nothing.
#
# The third branch needs two attacks in one turn, which one hero cannot make --
# attacking exhausts him. Hulk is printed ATK 3 and takes the first swing, which
# is what leaves the hero ready for the second.

Feature: Armored Rhino Suit

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form

  @card:01098
  Scenario: it attaches to the villain and takes the damage in its place
    # Spider-Man's printed ATK 2. Two counters on the suit, nothing on Rhino,
    # and the suit is under the threshold so it is still attached.
    Given "Armored Rhino Suit" is in play

    Then "Armored Rhino Suit" is in the "UpgradesArea"

    When I attack "Rhino"
    Then "Armored Rhino Suit" has 2 "damage" counters
    And "Rhino" has 0 damage
    And "Rhino" has 14 health
    And "Armored Rhino Suit" is in play
    And I am not prompted again

  @card:01098
  Scenario: any amount means any amount, and 5 or more discards the suit
    # Swinging Web Kick is printed "Hero Action (attack): Deal 8 damage to an
    # enemy", cost 3, paid by Strength (2 physical) and Genius (2 mental). All 8
    # go on the attachment -- not 5, not the 4 that would be left after the
    # threshold -- and the suit leaves play carrying them.
    Given "Armored Rhino Suit" is in play
    And my hand is "Swinging Web Kick", "Strength", "Genius"

    When I play "Swinging Web Kick" targeting "Rhino"
    Then "Armored Rhino Suit" has 8 "damage" counters
    And "Armored Rhino Suit" is not in play
    And "Armored Rhino Suit" is in the "EncounterDiscardPile"
    And "Rhino" has 0 damage
    And "Rhino" has 14 health
    And I am not prompted again

  @card:01098
  Scenario: the threshold counts damage already on the suit, and the villain is exposed once it goes
    # "At least 5" measured at exactly 5. Four counters are on the suit before
    # the turn starts and Hellcat is printed ATK 1, so the fifth is the one that
    # discards it -- an engine reading the printed threshold as "more than 5"
    # leaves the suit attached here and nowhere else in this file.
    #
    # Hellcat also keeps the hero free. Attacking exhausts a character, so a
    # second attack in one turn needs a second attacker, and she is printed
    # "Action: Return Hellcat to your hand" -- no forced response, nothing that
    # reads a deck. Spider-Man is still ready, and his printed 2 now reaches the
    # villain, which is the assertion that says the interrupt went away with the
    # card rather than outliving it.
    Given "Armored Rhino Suit" is in play
    And "Armored Rhino Suit" has 4 "damage" counters
    And "Hellcat" is in play

    When I choose "attack" on "Hellcat" targeting "Rhino"
    Then "Armored Rhino Suit" has 5 "damage" counters
    And "Armored Rhino Suit" is not in play
    And "Rhino" has 0 damage

    When I attack "Rhino"
    Then "Rhino" has 2 damage
    And "Rhino" has 12 health
    And I am not prompted again
