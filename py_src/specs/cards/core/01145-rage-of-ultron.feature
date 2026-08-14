# Printed: "When Revealed (Alter-Ego): Ultron schemes. Discard the top card of
# your deck for each threat placed this way.
# When Revealed (Hero): Ultron attacks you. Discard the top card of your deck for
# each damage dealt by this attack."
# 2 boost icons.
#
# One card with two When Revealed lines and the identity's form picks which one
# runs, so there are exactly two decision paths and they are these two. The
# discard is a *count* in both cases and the counts are different -- 1 against 2
# -- because they are read off two different printed statistics of the same
# villain: Ultron (I) is printed SCH 1 and ATK 2. A scenario that only asserted
# "some cards were discarded" would be satisfied by an engine that discarded a
# fixed number, or that read the wrong statistic.
#
# Neither branch is boosted. Boost cards are dealt during a villain activation in
# the villain phase; this card makes Ultron scheme or attack outside one, so the
# printed values stand alone and the numbers below are readable.
#
# The hero branch also fires Ultron (I)'s own Forced Response -- "After Ultron
# attacks you, choose to either..." -- which is a real decision and is answered
# in the transcript rather than being set aside. Its own behaviour is specced in
# 01134-ultron.feature.

Feature: Rage of Ultron

  Background:
    Given the scenario is "ultron"
    And the hero is "iron_man"

  @card:01145
  Scenario: against an alter-ego Ultron schemes, and one threat costs one card
    Given I am in alter-ego form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip"
    And "Rage of Ultron" is revealed

    # 1 threat: Ultron (I) is printed SCH 1 and nothing here boosts him.
    Then the main scheme has 1 threat
    # ...so one card, and the one that was on top.
    And "Aunt May" is in the "DiscardPile"
    And "Energy" is in the "PlayerDeck"
    And I have 4 cards in my deck
    And I have 1 cards in my discard pile
    # No attack happened, which is the other branch not having run.
    And I have 0 damage
    And I am not prompted again

  @card:01145
  Scenario: against a hero Ultron attacks, and two damage costs two cards
    # A `Given`-time reveal runs the whole reveal pipeline, so the transcript
    # opens on the defence prompt the attack put in front of me.
    Given I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip"
    And "Ultron Drones" is in play
    And "Rage of Ultron" is revealed

    Then I am prompted to choose one
      | Defense |

    When I pass
    # Ultron (I)'s printed Forced Response follows his attack. It is answered
    # here so the transcript covers every decision the card put in front of me;
    # the threat branch is chosen because the other one would take a third card
    # off the deck this scenario is counting.
    Then I am prompted to choose one
      | Place 1 threat on the main scheme                                                    |
      | Put the top card of your deck into play facedown, engaged with you as a Drone minion |

    When I choose "Place 1 threat on the main scheme"
    # 2 damage from the printed ATK 2, and so two cards off the top of my deck --
    # the top two, with the third still in place.
    Then I have 2 damage
    And "Aunt May" is in the "DiscardPile"
    And "Energy" is in the "DiscardPile"
    And "Genius" is in the "PlayerDeck"
    And I have 3 cards in my deck
    And I have 2 cards in my discard pile
    And I am not prompted again
