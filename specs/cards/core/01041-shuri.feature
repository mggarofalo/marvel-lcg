# Printed: "Response: After Shuri enters play, search your deck for an upgrade
# and add it to your hand. Shuffle your deck."
#
# Four decision paths, not the two the `imperative` plan budgets, and the reason
# is that the search helper behind this one sentence branches on the board:
#
#   * `Response` is optional -- it is offered as its own option after Shuri
#     arrives, and declining it is an answer the transcript can give.
#   * `Search.PlayerCard` asks *only* when the matches are distinguishable.
#     `SearchInternal.SearchForCardsInternal` sets `skip_choose` False as soon as
#     two legal faces differ in name or in which deck they sit, so one upgrade in
#     the deck is taken silently and two produce a second decision.
#   * A deck with no upgrade in it finds nothing and the sentence resolves to
#     nothing at all.
#
# `Given "Shuri" is in play` runs her enter-play response during setup, which is
# why every scenario opens with a decision and no preceding `When` -- the
# harness documents that trap and this card is a clean example of it.
#
# "your deck" is the whole of the search. `include_player_deck=True` is the only
# zone passed, so a copy in the discard pile is not a match; the third scenario
# puts one there to say so.

Feature: Shuri

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"
    And I am in hero form

  @card:01041
  Scenario: one upgrade in the deck is taken without asking which
    # The lone match is auto-selected, so this scenario's `I am not prompted
    # again` is the assertion that no question was put. Vibranium is deck filler
    # and a resource, not an upgrade, so it never competes.
    Given my deck is "Panther Claws", "Vibranium", "Vibranium"
    And "Shuri" is in play

    When I choose "Response" on "Shuri"
    Then "Panther Claws" is in the "HandsArea"
    And I have 1 cards in hand
    And I have 2 cards in my deck
    And I am not prompted again

  @card:01041
  Scenario: two upgrades in the deck are offered as a choice
    # The card that reaches hand is the one the transcript named, and the other
    # stays in the deck. Without the second assertion an engine that took the
    # first match regardless of the answer would pass.
    Given my deck is "Panther Claws", "Tactical Genius", "Vibranium"
    And "Shuri" is in play

    When I choose "Response" on "Shuri"
    Then the legal targets for "Response" are
      | Panther Claws   |
      | Tactical Genius |
    # "an upgrade" -- Vibranium is a resource card and is in the same deck, so
    # this is the printed noun stated directly rather than inferred from what
    # happened to be picked.
    And I cannot choose "Response" targeting "Vibranium"

    When I choose "Response" on "Shuri" targeting "Tactical Genius"
    Then "Tactical Genius" is in the "HandsArea"
    And "Panther Claws" is in the "PlayerDeck"
    And I have 1 cards in hand
    And I am not prompted again

  @card:01041
  Scenario: an upgrade in the discard pile is not found
    # "search your deck" is the whole of it. The discard pile holds the only
    # upgrade in the game and the response resolves to nothing.
    Given my deck is "Vibranium", "Vibranium"
    And my discard pile is "Panther Claws"
    And "Shuri" is in play

    When I choose "Response" on "Shuri"
    Then "Panther Claws" is in the "DiscardPile"
    And I have 0 cards in hand
    And I have 2 cards in my deck
    And I am not prompted again

  @card:01041
  Scenario: the response can be declined and the deck is left alone
    # A "Response" is an option, not a forced trigger. Declining it is the
    # control for all three scenarios above: the same board, the other answer,
    # and Panther Claws stays where it was.
    Given my deck is "Panther Claws", "Vibranium", "Vibranium"
    And "Shuri" is in play

    When I pass
    Then "Panther Claws" is in the "PlayerDeck"
    And I have 0 cards in hand
    And I have 3 cards in my deck
