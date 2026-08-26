# Superpower Training.
#
# Printed: "Victory 0. When Defeated: Each player may search their deck and
# discard pile for an identity-specific upgrade and put it into play.
# (Shuffle.)"
#
# One of the six cards whose "may search" clause did nothing until MARVEL-112:
# `Search.PlayerCard(..., may=True)` widened the selector range to (0, max), and
# every automated player picks the minimum, so the search always came back
# empty. `SearchInternal` now offers the decline as its own option instead.
#
# ---------------------------------------------------------------------------
# Board notes.
#
# "Identity-specific" is `ClassCard.CARD_CLASS` "IdentitySpecific", which is the
# card's printed `Hero` class. Web-Shooter and Webbed Up are Spider-Man's;
# Tenacity is a basic upgrade and is what the class filter has to leave behind.
#
# The card prints no cost bound, and Webbed Up costs 4 -- the pair below is what
# says so. Contrast Lock and Load, which prints "with a cost of 3 or less" over
# the same search.

Feature: Superpower Training

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And my hand is "Backflip", "Backflip", "Backflip"

  @card:40059
  Scenario: the search puts the identity-specific upgrade into play
    Given my deck is "Web-Shooter", "Backflip", "Backflip"
    And "40059" is in play
    And "40059" has 1 threat

    When I thwart "40059"
    Then I am prompted to choose one
      | Search |
      | Cancel |

    When I choose "Search"
    Then "Web-Shooter" is in the "UpgradesArea"
    And "40059" is in the "VictoryDisplay"
    And I am not prompted again

  @card:40059
  Scenario: the deck is left alone when the search is declined
    Given my deck is "Web-Shooter", "Backflip", "Backflip"
    And "40059" is in play
    And "40059" has 1 threat

    When I thwart "40059"
    When I choose "Cancel"
    Then "Web-Shooter" is in the "PlayerDeck"
    And "40059" is in the "VictoryDisplay"
    And I am not prompted again

  @card:40059
  Scenario: a basic upgrade is not identity-specific
    # Tenacity is an upgrade of cost 2 with the basic class. Webbed Up costs 4
    # and is offered anyway, because this card sets no cost bound.
    Given my deck is "Web-Shooter", "Webbed Up", "Tenacity", "Backflip"
    And "40059" is in play
    And "40059" has 1 threat

    When I thwart "40059"
    Then the legal targets for "Search" are
      | Web-Shooter |
      | Webbed Up   |

    When I choose "Search" targeting "Web-Shooter"
    Then "Web-Shooter" is in the "UpgradesArea"
    And "Tenacity" is in the "PlayerDeck"
    And I am not prompted again

  @card:40059
  Scenario: nobody is asked when there is no identity-specific upgrade to find
    Given my deck is "Tenacity", "Backflip", "Backflip"
    And "40059" is in play
    And "40059" has 1 threat

    When I thwart "40059"
    Then "40059" is in the "VictoryDisplay"
    And "Tenacity" is in the "PlayerDeck"
    And I am not prompted again
