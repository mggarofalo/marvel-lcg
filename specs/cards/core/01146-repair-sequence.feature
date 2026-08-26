# Printed: "When Revealed: Ultron heals 2 damage for each [[Drone]] minion
# engaged with you. If no damage was healed this way, this card gains surge.
# [star] Boost: Ultron heals 1 damage for each [[Drone]] minion engaged with you."
#
# Every scenario here walks a real villain phase, which is more expensive than
# the `Given "<card>" is revealed` the rest of this batch uses, and there are two
# reasons neither of which is optional.
#
# **Surge is invisible from a `Given`-time reveal.** A surged card stops in
# `DealtEncounterCardsDeck` rather than reaching the encounter deck for another
# card to be dealt, so the only way to see a surge is for the reveal to happen
# during a villain phase with the encounter deck stacked. Crowd Control sits
# third in every encounter deck below and is the whole test: it is in play when
# this card surged and still in the encounter deck when it did not.
#
# **The boost ability only exists during an activation.** `WhenCardBecomeBoost`
# fires when the card is dealt face down as a boost card, which nothing but a
# villain activation does.
#
# The condition is "if no damage was *healed*", and it is reached two different
# ways -- no drones to heal for, and drones but nothing to heal. Both are here,
# because an engine that read the condition as "if no drone was engaged" passes
# the first and fails the second.
#
# Board notes. Iron Man carries no interrupt of his own, so nothing but the cards
# under test puts a decision in these transcripts. Hard to Keep Down is the boost
# card wherever the boost is not what is being measured: it prints no boost
# icons, so Ultron's attack is exactly his printed 2 and the damage numbers are
# readable. The drones are made by the two Android Efficiency faces, which take
# one card each off the top of my deck; two reveals of one face would be a
# `Given` acting on the same card twice, which the harness refuses.

Feature: Repair Sequence

  Background:
    Given the scenario is "ultron"
    And the hero is "iron_man"
    And I am in hero form
    And "Ultron Drones" is in play

  @card:01146
  Scenario: the reveal heals 2 for each drone engaged with me, and does not surge
    Given my deck is "Aunt May", "Energy", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And "Ultron" has 6 damage
    And "01144a" is revealed
    And "01144b" is revealed
    And the encounter deck is "Hard to Keep Down", "Repair Sequence", "Crowd Control", "Hydra Mercenary"

    When I pass
    # Ultron attacks, and I decline to defend.
    When I pass
    # Ultron (I)'s own Forced Response follows his attack. The threat branch is
    # answered rather than the drone one, because a third drone would change the
    # number this scenario is measuring.
    When I choose "Place 1 threat on the main scheme"
    # Both drones are engaged with me, so the engine asks which order they
    # activate in. A facedown drone has no printed name, so each is named by the
    # card underneath it.
    When I choose "Minion Activates Order" targeting "Aunt May", "Energy"
    When I pass
    When I pass

    # 4 healed off 6: 2 for each of the two drones engaged with me.
    Then "Ultron" has 2 damage
    # No surge. The card behind Repair Sequence in the encounter deck is still in
    # the encounter deck, which is what "this card did not gain surge" looks like
    # from outside.
    And "Crowd Control" is in the "EncounterDeck"
    And "Repair Sequence" is in the "EncounterDiscardPile"
    And it is round 2

  @card:01146
  Scenario: with no drone engaged with me nothing is healed and the card surges
    # The same villain phase with the two drones left off the board. Nothing is
    # healed, so the card surges and a second encounter card is dealt and
    # revealed -- Crowd Control, which is in play here and was not above.
    Given my deck is "Aunt May", "Energy", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And "Ultron" has 6 damage
    And the encounter deck is "Hard to Keep Down", "Repair Sequence", "Crowd Control", "Hydra Mercenary"

    When I pass
    When I pass
    When I choose "Place 1 threat on the main scheme"

    Then "Ultron" has 6 damage
    And "Crowd Control" is in the "SideSchemesArea"
    And "Repair Sequence" is in the "EncounterDiscardPile"
    And it is round 2

  @card:01146
  Scenario: drones engaged but nothing to heal still counts as no damage healed
    # The board of the first scenario with one number changed: Ultron is
    # undamaged, so the two drones are worth 4 healing that has nowhere to go.
    # "If no damage was healed this way" is about the healing and not about the
    # drones, and this is the only scenario that can tell those apart -- an
    # engine reading the condition as "if no drone was engaged" surges in the
    # scenario above and does nothing here.
    Given my deck is "Aunt May", "Energy", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And "01144a" is revealed
    And "01144b" is revealed
    And the encounter deck is "Hard to Keep Down", "Repair Sequence", "Crowd Control", "Hydra Mercenary"

    When I pass
    When I pass
    When I choose "Place 1 threat on the main scheme"
    When I choose "Minion Activates Order" targeting "Aunt May", "Energy"
    When I pass
    When I pass

    Then "Ultron" has 0 damage
    And "Crowd Control" is in the "SideSchemesArea"
    And it is round 2

  @card:01146
  Scenario: as a boost card it heals 1 for each drone rather than 2
    # Repair Sequence is written first in the encounter deck, so it is the card
    # dealt face down to boost Ultron's activation and the [star] boost ability
    # is what runs. Half the reveal's rate on the same board: 2 healed off 6
    # rather than 4.
    #
    # The boost card also carries 1 boost icon, so Ultron's attack is 3 rather
    # than the 2 the other scenarios see.
    Given my deck is "Aunt May", "Energy", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And "Ultron" has 6 damage
    And "01144a" is revealed
    And "01144b" is revealed
    And the encounter deck is "Repair Sequence", "Crowd Control", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I pass
    When I choose "Place 1 threat on the main scheme"
    When I choose "Minion Activates Order" targeting "Aunt May", "Energy"
    When I pass
    When I pass

    Then "Ultron" has 4 damage
    # 5 damage to me: Ultron's printed 2 plus the 1 boost icon on this card, and
    # 1 from each drone.
    And I have 5 damage
    And "Repair Sequence" is in the "EncounterDiscardPile"
    And it is round 2
