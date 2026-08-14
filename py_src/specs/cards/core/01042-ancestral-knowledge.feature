# Printed: "Alter-Ego Action: Choose up to 3 different cards in your discard
# pile and shuffle them into your deck."
#
# Three claims in one sentence, and they fail independently, so they are three
# scenarios plus the restriction that gates the action:
#
#   * "up to 3"       -- fewer than three is a legal answer
#   * "3"             -- the fourth card in the pile is not reached
#   * "different"     -- two copies of one title are one card
#   * "Alter-Ego Action" -- the option is absent in hero form
#
# The cards shuffled in cannot be named individually afterwards: "shuffle them
# into your deck" randomises the order, so what a scenario can say is which zone
# each card ended in and how many are in each. That is deliberate -- a scenario
# that pinned deck positions here would be pinning the RNG.
#
# ---------------------------------------------------------------------------
# The last scenario in this file is expected to FAIL, and is left in quarantine
# on purpose.
#
# `select_rule="DifferentCards"` is passed by the card script and is declared in
# `SelectorRule.RULE_BASE`, but nothing in `game/selector/selector_rule.py`
# enforces it -- neither `Process` nor `AfterSelectTargets` looks at it. The only
# implementation in the repository is in the web client, at
# `public/js/marvel/effect.ts:377`. So a player who is not a browser -- the bot,
# the spec harness, and any future C# runner -- can choose two copies of the same
# title, which the card's own script comments quote the FAQ to forbid:
#
#     # A: Can Ancestral Knowledge shuffle different versions of Wakanda Forever
#     #    into Black Panther's deck?
#     # Q: No. Cards with the same title are considered to be the same card for
#     #    the purpose of card abilities.
#
# This is not the only card affected -- `TeamUp` targets carry the same rule --
# so it is reported rather than written around.

Feature: Ancestral Knowledge

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"

  @card:01042
  Scenario: three chosen cards leave the discard pile and the fourth stays
    # Four cards in the pile, three chosen. The discard ends at 3 rather than 1
    # because Ancestral Knowledge itself and the Vibranium that paid for it both
    # arrive there -- so the assertion that carries the claim is Vibranium Suit
    # still being in the pile, not the count.
    Given I am in alter-ego form
    And my hand is "01042", "Vibranium"
    And my discard pile is "Panther Claws", "Tactical Genius", "Energy Daggers", "Vibranium Suit"

    When I choose "Play" on "01042" targeting "Panther Claws", "Tactical Genius", "Energy Daggers"
    Then I have 3 cards in my deck
    And "Vibranium Suit" is in the "DiscardPile"
    And "Panther Claws" is in the "PlayerDeck"
    And "Tactical Genius" is in the "PlayerDeck"
    And "Energy Daggers" is in the "PlayerDeck"
    And I have 3 cards in my discard pile
    And I am not prompted again

  @card:01042
  Scenario: "up to" allows fewer than three
    # The same board, one card chosen. Three cards stay in the pile, so an
    # engine that read "3" as a fixed number rather than a maximum fails here
    # while passing the scenario above.
    Given I am in alter-ego form
    And my hand is "01042", "Vibranium"
    And my discard pile is "Panther Claws", "Tactical Genius", "Energy Daggers", "Vibranium Suit"

    When I choose "Play" on "01042" targeting "Panther Claws"
    Then I have 1 cards in my deck
    And "Panther Claws" is in the "PlayerDeck"
    And "Tactical Genius" is in the "DiscardPile"
    And "Energy Daggers" is in the "DiscardPile"
    And "Vibranium Suit" is in the "DiscardPile"
    And I have 5 cards in my discard pile
    And I am not prompted again

  @card:01042
  Scenario: a hero is not offered the action at all
    # "Alter-Ego Action". The restriction is enforced by the option never
    # appearing, so the whole menu is asserted rather than just the absence --
    # the hero has an Attack and a Change Form and nothing else, and there is no
    # `Play` bound to the card sitting in hand.
    Given I am in hero form
    And my hand is "01042", "Vibranium"
    And my discard pile is "Panther Claws", "Tactical Genius", "Energy Daggers"

    Then I am prompted to choose one
      | Attack      |
      | Change Form |

  @card:01042
  Scenario: two copies of one title are one card and cannot both be chosen
    # EXPECTED TO FAIL -- see the note at the top of this file. The engine
    # accepts both copies and shuffles both in; under the printed rule the
    # second copy is not an available second choice, so at most one of the pair
    # can reach the deck.
    Given I am in alter-ego form
    And my hand is "01042", "Vibranium"
    And my discard pile is "Panther Claws", "Panther Claws", "Tactical Genius"

    When I choose "Play" on "01042" targeting "Panther Claws #1", "Panther Claws #2"
    Then "Panther Claws #2" is in the "DiscardPile"
    And I have 1 cards in my deck
