# Rhino, stage II. Printed:
#
#   "When Revealed: Search the encounter deck and discard pile for the
#    Breakin' & Takin' side scheme and reveal it. Shuffle the encounter deck."
#
# Printed 15 hit points per hero, ATK 3, SCH 1.
#
# ---------------------------------------------------------------------------
# The board is reached by defeating stage I, not by revealing the card.
#
# `Given "01095" is revealed` also fires the ability, and it is cheaper, but it
# leaves the stage-II card in the encounter discard pile and stage I still
# standing in the villain area -- so none of stage II's printed statistics can
# be asserted on that board, and the villain the scenario is about is not the
# villain in play. Stage I is printed 14 hit points at one hero, so 12 damage
# plus Spider-Man's printed ATK 2 defeats it exactly and the villain advances.
#
# That also makes `"Rhino"` unambiguous after the advance: two cards are printed
# with that name, and the rule the harness applies is that a name matching
# several cards, only one of which is on the board, means the one on the board.
#
# The last scenario sets up its own hero count and its own damage, so nothing
# below shares a Background beyond the scenario name -- a stage-I hit point
# total that is per hero cannot be pre-loaded once for boards with different
# numbers of players.
#
# ---------------------------------------------------------------------------
# The first three scenarios are three branches of one printed sentence. It names
# two places to look, which the engine's
# `Search.EncounterCard(include_discard_pile=True)` reads as two code paths, and
# the third is what happens when the search comes back empty -- a branch the
# printed text does not spell out and the one a port is most likely to get wrong
# by raising or by revealing something else instead.
#
# "Shuffle the encounter deck" is not assertable: the vocabulary has no step for
# deck order, and order does not survive a shuffle anyway (MARVEL-82), so there
# is nothing a scenario could observe. It is called out here rather than quietly
# dropped.

Feature: Rhino (II)

  Background:
    Given the scenario is "rhino"

  @card:01095
  Scenario: advancing to stage II pulls the side scheme out of the encounter deck
    # Breakin' & Takin' is printed with 2 fixed starting threat and places an
    # additional 1 per hero when revealed, so 3 at one hero. The Hydra Mercenary
    # sitting underneath it is the control on "search": a search that revealed
    # the top card, or every card, would put the minion into play too.
    Given the hero is "spider_man"
    And I am in hero form
    And "Rhino" has 12 damage
    And the encounter deck is "Breakin' & Takin'", "Hydra Mercenary"

    When I attack "Rhino"
    Then "Rhino" has 15 health
    And "Rhino" has 15 "max_health"
    And "Rhino" has 0 damage
    And "Breakin' & Takin'" is in the "SideSchemesArea"
    And "Breakin' & Takin'" has 3 threat
    And "Hydra Mercenary" is not in play
    And I am not prompted again

  @card:01095
  Scenario: the search reaches the encounter discard pile as well as the deck
    # The second half of "search the encounter deck and discard pile". The
    # encounter deck holds only minions here, so the side scheme is found in the
    # discard pile or not at all.
    Given the hero is "spider_man"
    And I am in hero form
    And "Rhino" has 12 damage
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
    And the encounter discard pile is "Breakin' & Takin'"

    When I attack "Rhino"
    Then "Breakin' & Takin'" is in the "SideSchemesArea"
    And "Breakin' & Takin'" has 3 threat
    And "Hydra Mercenary #1" is not in play
    And "Hydra Mercenary #2" is not in play
    And I am not prompted again

  @card:01095
  Scenario: with the side scheme in neither place the advance reveals nothing
    # The empty branch. The villain still advances and still has its stage-II
    # statistics; nothing enters play, which is the assertion that a search
    # falling back on "reveal the top card instead" would fail.
    Given the hero is "spider_man"
    And I am in hero form
    And "Rhino" has 12 damage
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary"

    When I attack "Rhino"
    Then "Rhino" has 15 health
    And "Hydra Mercenary #1" is not in play
    And "Hydra Mercenary #2" is not in play
    And the main scheme has 0 threat
    And I am not prompted again

  @card:01095
  Scenario: at two heroes stage II has 30 hit points
    # The printed star, and a separate claim from the one 01094 makes for stage
    # I: a scenario asserting hit points does not transfer between stages.
    # Stage I is 14 per hero, so 26 damage plus Spider-Man's printed 2 defeats
    # it at two heroes.
    Given the heroes are "spider_man", "captain_marvel"
    And I am in hero form
    And "Rhino" has 26 damage
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary"

    When I attack "Rhino"
    Then "Rhino" has 30 health
    And "Rhino" has 30 "max_health"
