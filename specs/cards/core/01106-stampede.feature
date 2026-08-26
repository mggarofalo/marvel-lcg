# Stampede. Printed:
#
#   "When Revealed (Alter-Ego): This card gains surge.
#    When Revealed (Hero): Rhino attacks you. If a character is damaged by this
#    attack, that character is stunned."
#
# Printed boost 1.
#
# ---------------------------------------------------------------------------
# The form gate is one branch and the stun condition is another.
#
# Four scenarios, and they are four different questions:
#
#   alter-ego            the card surges and there is no attack
#   hero                 the card does not surge and there is an attack
#   a character is damaged   *which* character is stunned
#   no character is damaged  the conditional does not fire
#
# The first two are a matched pair on one board -- same encounter deck, same
# hero, only the form differs -- so Shocker's fate is the form gate and nothing
# else. Both walk a real villain phase, because surge is invisible from a
# `Given`-time reveal: a surged card stops in `DealtEncounterCardsDeck`.
#
# The last two isolate the attack with `Given "Stampede" is revealed` instead,
# and that is deliberate rather than lazy. **Rhino's attack is boosted when
# there is an encounter deck to boost it from** -- measured: with Shocker
# (printed boost 2) under Stampede the attack lands for 4, with a Hydra
# Mercenary (printed boost 1) it lands for 3, and with nothing left it lands for
# Rhino's printed 2. A puzzle scene has no encounter deck, so the isolated board
# is the one where the number on the page is the number in the assertion, and
# the scenarios about the stun are not also making a claim about boosting.
#
# The hero is Iron Man throughout: Spider-Man's printed identity ability is
# "Interrupt: When the villain initiates an attack against you, draw 1 card",
# which opens a decision this transcript has no reason to be about.

Feature: Stampede

  Background:
    Given the scenario is "rhino"
    And the hero is "iron_man"

  @card:01106
  Scenario: revealed to an alter-ego it surges and nobody is attacked
    # The encounter deck is written top-first (MARVEL-82) and a villain
    # activation takes two cards: the mercenary boosts, Stampede is revealed,
    # and Shocker is what the surge reaches. 3 threat on the main scheme is the
    # activation against an alter-ego -- Rhino's printed SCH 1, 1 for the boost
    # card, and the stage's printed 1 acceleration -- and it is here to show the
    # round really ran.
    Given I am in alter-ego form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Stampede", "Shocker"

    When I pass
    Then "Shocker" is in play
    And I have 0 damage
    And "Tony Stark" is not stunned
    And the main scheme has 3 threat
    And it is round 2

  @card:01106
  Scenario: revealed to a hero it does not surge, and Rhino attacks instead
    # The same board in the other form. Shocker never enters play -- it is spent
    # as the boost card for the attack Stampede triggers -- and the hero takes
    # two attacks in the round rather than one.
    #
    # 7 damage: 3 for the villain's own activation (printed ATK 2 plus the
    # mercenary's 1 boost) and 4 for Stampede's attack (printed ATK 2 plus
    # Shocker's 2 boost). Iron Man is printed 9 hit points and declines both
    # defences.
    Given I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Stampede", "Shocker"

    When I pass
    When I pass
    When I pass
    Then "Shocker" is not in play
    And I have 7 damage
    And "Iron Man" is stunned

  @card:01106
  Scenario: the character stunned is the one the attack damaged, not the one it was aimed at
    # "Rhino attacks you" and "that character is stunned" are about different
    # characters as soon as an ally defends. Hellcat is printed 3 hit points and
    # takes Rhino's printed 2 -- no encounter deck, so no boost card -- and it
    # is Hellcat who is stunned. Iron Man, whom the attack was aimed at, is
    # neither damaged nor stunned.
    Given I am in hero form
    And "Hellcat" is in play
    And "Stampede" is revealed

    When I choose "Defense" on "Hellcat"
    Then "Hellcat" has 2 damage
    And "Hellcat" is stunned
    And I have 0 damage
    And "Iron Man" is not stunned
    And I am not prompted again

  @card:01106
  Scenario: a character the attack does not damage is not stunned
    # The conditional. Iron Man carries a tough status card, which cancels the
    # damage entirely rather than reducing it, so no character is damaged by
    # this attack and the stun clause finds nobody. The status is spent doing
    # it, which is how the scenario shows the attack happened at all.
    Given I am in hero form
    And "Iron Man" is tough
    And "Stampede" is revealed

    When I pass
    Then I have 0 damage
    And "Iron Man" is not stunned
    And "Iron Man" is not tough
    And I am not prompted again
