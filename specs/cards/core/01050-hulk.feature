# Hulk. the original investigation, the first card of the core shard.
#
# > Forced Response: After Hulk attacks, discard the top card of your deck. If
# > that card's printed resource has:
# > [physical] - Deal 2 damage to an enemy.
# > [energy]   - Deal 1 damage to each character.
# > [mental]   - Discard Hulk.
# > [wild]     - All of the above.
#
# Four branches off one attack, chosen by a card the scenario controls entirely:
# `my deck is` writes the deck top-first (the original investigation), so the first card named is
# the one discarded and the branch is deterministic. Without that this card would
# need the RNG pinned; with it, each branch is one scenario.
#
# The filler behind the trigger card is always Backflip, so nothing but the top
# card differs between these five scenarios.
#
# Hulk is printed ATK 3 with 5 hit points, and he is an ally, so every attack
# also costs him 1 consequential damage (see damage-and-threat.feature). Rhino
# stage 1 is printed 14 hit points against one player. Spider-Man never acts, so
# every point below is Hulk's.

Feature: Hulk

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And "Hulk" is in play

  @card:01050
  Scenario: a physical top card adds 2 damage to an enemy
    # 5 on Rhino: 3 for the attack, 2 for the branch. The branch says "an enemy"
    # and Rhino is the only one, so the engine selects him itself and asks
    # nothing -- a scenario that named him would be asserting the absence of a
    # choice that was never offered.
    Given my deck is "Backflip", "Backflip"

    When I choose "attack" on "Hulk" targeting "Rhino"
    Then "Rhino" has 5 damage
    And "Hulk" has 1 damage
    And "Hulk" is in play
    And I am not prompted again

  @card:01050
  Scenario: a mental top card discards Hulk
    # 3 on Rhino and no more: the attack lands, then Hulk is discarded. He takes
    # no consequential damage because he is no longer in play to take it.
    Given my deck is "Enhanced Spider-Sense", "Backflip"

    When I choose "attack" on "Hulk" targeting "Rhino"
    Then "Rhino" has 3 damage
    And "Hulk" is not in play
    And "Hulk" is in the "DiscardPile"
    And I am not prompted again

  @card:01050
  Scenario: an energy top card asks before dealing 1 to each character
    # The branch that is not automatic. "Each character" has no single target
    # for the engine to pick, so it stops and asks -- which is the whole reason
    # a scenario is a transcript. A batched format would record the damage and
    # lose the fact that the player was asked at all.
    Given my deck is "Aunt May", "Backflip"

    When I choose "attack" on "Hulk" targeting "Rhino"
    Then I am prompted to choose one
      | Deal 1 damage to each character |

    When I choose "Deal 1 damage to each character"
    Then "Rhino" has 4 damage
    And I have 1 damage
    And "Hulk" has 2 damage
    And I am not prompted again

  @card:01050
  Scenario: a wild top card does all of it
    # 3 for the attack, 2 for the physical branch and 1 for the energy branch is
    # 6 on Rhino; the hero takes the energy branch's 1; Hulk takes his
    # consequential 1, the energy branch's 1, and is then discarded by the
    # mental branch.
    Given my deck is "Hellcat", "Backflip"

    When I choose "attack" on "Hulk" targeting "Rhino"
    Then I am prompted to choose one
      | Deal 1 damage to each character |

    When I choose "Deal 1 damage to each character"
    Then "Rhino" has 6 damage
    And I have 1 damage
    And "Hulk" is not in play
    And I am not prompted again

  @card:01050
  Scenario: an empty deck discards nothing and the response still finishes
    # the original investigation. A puzzle board starts with no player deck, and with the
    # discard pile empty as well there is nothing to reshuffle -- so the forced
    # response is asked to discard a card that does not exist. The rule is that
    # the ability does as much as it can: nothing is discarded, no printed
    # resource is seen, and none of the four branches fires.
    #
    # `PlayerAction.DiscardDeckTopCard` indexed the result of
    # `DiscardDeckTopCards` unguarded, so this raised `IndexError` in the middle
    # of the response. Nothing about the board says so -- every assertion below
    # held before the fix as well, because all four branches are conditional on
    # a card that was never discarded, so the abort and the correct resolution
    # land on the same state. What fails is the *verdict*: the engine's broad
    # handlers log the traceback and play on, and `Log.HasError` demotes the
    # case to ERROR. That demotion is the whole guard here, which is why this
    # scenario is paired with 40040 A Good Workout in specs/cards/search/ --
    # there the same bug is visible on the board.
    #
    # This is the one scenario in the file that stocks no deck, deliberately.

    When I choose "attack" on "Hulk" targeting "Rhino"
    Then "Rhino" has 3 damage
    And "Hulk" has 1 damage
    And "Hulk" is in play
    And I have 0 cards in my deck
    And I have 0 cards in my discard pile
    And I am not prompted again

  @card:01050
  Scenario: the forced response fires on every attack, not only the first
    # "Forced" means the player never gets to decline it, and the transcript is
    # how that is checked: a second attack would have to be answered a second
    # time. Hulk exhausts after attacking, so the second attack is Spider-Man's
    # and fires nothing -- the response is Hulk's, not the attack's.
    Given my deck is "Backflip", "Backflip"

    When I choose "attack" on "Hulk" targeting "Rhino"
    Then "Rhino" has 5 damage
    And "Hulk" is exhausted

    When I attack "Rhino"
    Then "Rhino" has 7 damage
    And I am not prompted again
