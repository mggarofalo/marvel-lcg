# Printed: "When Revealed: The first player puts the top 2 cards of their deck
# into play facedown, engaged with them as [[Drone]] minions."
# Printed 2 [star] starting threat, 3 boost icons, 1 hazard icon.
#
# "The first player", not "each player" -- the one card in the set that singles
# a player out, and the only thing separating it from the four cards around it
# that all say "each player". A solo scenario cannot tell the two readings apart
# at all, so the second scenario here is not a variant of the first: it is the
# card's actual claim.
#
# Ultron Drones is in play throughout because a facedown drone has no printed
# hit points of its own. Without it both drones enter play at 0 and are defeated
# in the same breath, and the two cards that left the deck land in the discard
# pile instead of standing up as minions.

Feature: Ultron's Imperative

  Background:
    Given the scenario is "ultron"
    And "Ultron Drones" is in play

  @card:01150
  Scenario: two drones, off the top two cards of my deck
    # Two, and the two named on top. The third card is asserted still in the deck
    # so that "the top 2" is a claim about position rather than about a count.
    Given the hero is "iron_man"
    And I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip"
    And "Ultron's Imperative" is revealed

    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Energy" is in the "EngagedEnemiesArea"
    And "Genius" is in the "PlayerDeck"
    And I have 3 cards in my deck
    # Both drones under the name the game displays for a facedown drone, which
    # needs an ordinal now there are two. `#N` is creation order, so #1 is the
    # card the scenario wrote first.
    And "Drone Minion #1" has 1 health
    And "Drone Minion #2" has 1 health
    And "Ultron's Imperative" has 2 threat
    And I am not prompted again

  @card:01150
  Scenario: only the first player's deck is touched
    # The claim the solo scenario cannot make. Both players are in the game, both
    # have a stocked deck, and the second player's is exactly as it was: this
    # card names the first player where Drone Factory, Invasive AI and the three
    # main scheme stages all say "each player".
    Given the heroes are "iron_man", "captain_marvel"
    And I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip"
    And player 2's deck is "Pepper Potts", "Genius", "Genius", "Energy"
    And "Ultron's Imperative" is revealed

    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Energy" is in the "EngagedEnemiesArea"
    And player 1 has 3 cards in their deck
    # Untouched, and still four: no drone was made from it and nothing was
    # discarded off it.
    And player 2 has 4 cards in their deck
    # 4, not 2: the printed starting threat carries a [star] and is per player,
    # which is true of the scheme even though the drones are not.
    And "Ultron's Imperative" has 4 threat
    And I am not prompted again
