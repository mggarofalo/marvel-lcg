# Printed: "When Revealed: Each player discards the top 3 cards of their deck."
# Printed 3 [star] starting threat, 3 boost icons, 1 hazard icon.
#
# The [star] on the starting threat is per player, so a two-player game is a
# claim about the side scheme as well as about the discard: 3 in the solo
# scenario, 6 in the two-player one.
#
# Both decks are stocked deeper than the three cards the card takes. A player
# whose deck runs out is eliminated, which ends the game before the transcript
# can assert anything, and a scenario that failed for that reason would be
# describing the deck rather than the card.

Feature: Invasive AI

  Background:
    Given the scenario is "ultron"

  @card:01149
  Scenario: it takes the top three cards of my deck and stops there
    # Named cards rather than a count. "The top 3" is the claim, so the three
    # that go are named and the fourth is asserted still in the deck -- a
    # scenario that only counted the deck down by three would pass against an
    # engine that reached anywhere into it.
    Given the hero is "iron_man"
    And I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip", "Backflip"
    And "Invasive AI" is revealed

    Then "Aunt May" is in the "DiscardPile"
    And "Energy" is in the "DiscardPile"
    And "Genius" is in the "DiscardPile"
    And "Pepper Potts" is in the "PlayerDeck"
    And I have 3 cards in my deck
    And I have 3 cards in my discard pile
    # The side scheme itself, in play with its printed starting threat for one
    # player.
    And "Invasive AI" is in the "SideSchemesArea"
    And "Invasive AI" has 3 threat
    And I am not prompted again

  @card:01149
  Scenario: every player discards from their own deck, not one deck twice
    # The "each" in "each player discards the top 3 cards of their deck". The
    # solo scenario above is equally consistent with an engine that resolved this
    # once, for whoever revealed it, or one that took six cards off the revealing
    # player's deck.
    #
    # The two decks lead with different cards on purpose: the counts alone would
    # not separate "three off each" from "six off one", and naming the printed
    # identity of a card in each discard pile does.
    Given the heroes are "iron_man", "captain_marvel"
    And I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip", "Backflip"
    And player 2's deck is "Pepper Potts", "Genius", "Genius", "Energy", "Energy", "Energy"
    And "Invasive AI" is revealed

    Then "Aunt May" is in the "DiscardPile"
    And player 1 has 3 cards in their deck
    And player 2 has 3 cards in their deck
    # 6, not 3: the printed starting threat carries a [star] and is per player.
    And "Invasive AI" has 6 threat
    And I am not prompted again
