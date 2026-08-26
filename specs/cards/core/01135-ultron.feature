# Printed (Ultron, stage II): "[star] Forced Interrupt: When Ultron attacks you,
# put the top card of your deck into play facedown, engaged with you as a
# [[Drone]] minion. Until the end of his attack, Ultron gets +1 ATK for each
# [[Drone]] minion engaged with you."
# Printed ATK 2 [star], SCH 2, 22 [star] hit points.
#
# Stage II, not stage I. `the scenario is "ultron_expert"` is how a scenario
# reaches it: the expert scenario is built from Ultron (II) and Ultron (III)
# where the standard one is built from (I) and (II), so this card starts in the
# villain area rather than in the villain deck. Nothing else in the step
# vocabulary sets a villain's stage, and the hit points below are why it matters
# that this is a card of work of its own -- same encounter set, same name, a
# different printed line.
#
# **Rage of Ultron is what makes him attack.** A villain phase would do it too
# and is what 01134-ultron.feature walks, but it deals a boost card first, and a
# boosted attack cannot say what "+1 ATK for each [[Drone]] minion" is worth: the
# printed 2 plus an unknown boost plus the bonus is one number. Revealing Rage of
# Ultron makes him attack outside an activation, for exactly his printed ATK plus
# what this card adds, and then discards one card for each damage dealt -- so the
# damage is measured twice, once on my hero and once on my deck.
#
# Ultron Drones is in play because the drone this card creates is facedown and
# has no printed statistics of its own. Without it the drone is defeated the
# instant it enters play, and the +1 it is supposed to be worth goes with it.

Feature: Ultron II

  Background:
    Given the scenario is "ultron_expert"
    And the hero is "iron_man"
    And "Ultron Drones" is in play

  @card:01135
  Scenario: his attack makes a drone first and is worth 1 more for it
    Given I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip", "Backflip", "Backflip"
    And "Rage of Ultron" is revealed

    # The attack the interrupt is printed to precede.
    When I pass

    # 22, the printed hit points of this stage. Stage I is printed 17 and stage
    # III 27, so this is the assertion that does not transfer.
    Then "Ultron" has 22 health
    # 3 damage: the printed ATK 2, plus 1 for the single drone now engaged with
    # me -- the one the interrupt made a moment earlier.
    And I have 3 damage
    # ...and 3 cards discarded, one per damage dealt. The same number read off a
    # second place, which is what makes it a claim about the attack's strength
    # rather than about my hero's health.
    And I have 3 cards in my discard pile
    # The drone, made from the card that was on top of my deck before any of this
    # -- so my deck is 7 less the 1 it took and the 3 Rage of Ultron discarded.
    And "Aunt May" is in the "EngagedEnemiesArea"
    And "Drone Minion" has 1 health
    And I have 3 cards in my deck
    # "Until the end of his attack." The drone is still engaged with me and the
    # bonus is gone: his ATK reads the printed 2 again.
    And "Ultron" has 2 "attack"
    And I am not prompted again

  @card:01135
  Scenario: the bonus counts every drone minion engaged with me, not only the one he made
    # The same board with an Advanced Ultron Drone already engaged. It is a DRONE
    # minion with printed statistics of its own rather than a facedown drone, and
    # it counts: the attack is worth 4 rather than 3, and the fourth card off my
    # deck says so as well as my hero does.
    Given I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip", "Backflip", "Backflip"
    And "Advanced Ultron Drone" is in play
    And "Rage of Ultron" is revealed

    When I pass

    Then I have 4 damage
    And I have 4 cards in my discard pile
    And "Aunt May" is in the "EngagedEnemiesArea"
    And I have 2 cards in my deck
    And "Ultron" has 2 "attack"
    And I am not prompted again

  @card:01135
  Scenario: an alter-ego is schemed against, so no drone is made and no bonus applies
    # The control for the trigger. "When Ultron attacks you" is the condition, and
    # a villain activating against an alter-ego schemes instead. Rage of Ultron's
    # alter-ego line makes him do exactly that on the same board.
    #
    # The card that would have been the drone is in my discard pile instead,
    # because the only thing that took cards off my deck here was Rage of Ultron
    # discarding one per threat placed.
    Given I am in alter-ego form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip", "Backflip", "Backflip"
    And "Rage of Ultron" is revealed

    # 2 threat, his printed SCH 2 and nothing else: no boost card is dealt outside
    # an activation.
    Then the main scheme has 2 threat
    And "Aunt May" is in the "DiscardPile"
    And "Aunt May" is not in play
    And I have 5 cards in my deck
    And I have 0 damage
    And I am not prompted again
