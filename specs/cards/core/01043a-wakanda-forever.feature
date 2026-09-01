# Printed: "Hero Action: Resolve the "Special" ability on each [[Black Panther]]
# upgrade you control in any order. (Resolving each ability is a step in a
# sequence.)"
#
# ---------------------------------------------------------------------------
# Four card ids, one card.
#
# 01043a, 01043b, 01043c and 01043d are four printings of Wakanda Forever! in
# the core set. They agree byte for byte on printed text, they all run
# `cards/pack/core/black_panther/01043a.py`, and `data/cards.json` links b, c
# and d to a with `{"kind": "ability", "card_id": "01043a"}`. They differ in the
# resource icon they print -- energy, mental, physical, wild -- and 01043d is
# the one printed twice, with a deck limit of 2.
#
# `tools.spec.coverage` does not credit a's scenarios to b, c and d. Its
# `Identity()` compares the engine's attribute block, and that block carries
# `RES`, which is exactly what differs. That refusal is defensible rather than a
# bug: the icon is real behaviour, and a cost like Sonic Boom's `Cost("YBR")`
# demands three *specific* icons, so two printings of this card are not
# interchangeable at payment time. Saying so used to be impossible: no step read
# a card's resource icons. the original investigation added one --
# `Then "<card>" has <n> "<icon>" resource icons` -- and
# `specs/rules/resource-icons.feature` now makes that claim for all four
# printings, tagged per id. So the distinction the coverage tool refuses to
# collapse is pinned somewhere, and this file no longer has to carry it.
#
# So the ability is written once, against 01043a, and the last scenario plays
# the other three printings in one turn to establish that they resolve the same
# ability rather than assuming it from the metadata. Writing the sequence
# scenarios out four times would be four copies of one claim.
#
# ---------------------------------------------------------------------------
# The whole event is one decision. Choosing which upgrades to resolve and in
# what order is the `Play` option's target list, so `targeting "A", "B"` is the
# order of the sequence and there is no mid-resolution prompt for it. Each
# upgrade's own target -- an enemy, a scheme, a player -- is a separate decision
# after that, and only when it has more than one legal answer.

Feature: Wakanda Forever!

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"
    And I am in hero form

  @card:01043a
  Scenario: a lone upgrade is the final step of the sequence
    # One upgrade means one step, and that step is also the last, so Panther
    # Claws deals its 4 rather than its 2. Combat Training is in play to say
    # what "[[Black Panther]] upgrade" excludes: it is an upgrade the hero
    # controls and it is not a legal target.
    Given my hand is "01043a", "Vibranium"
    And "Panther Claws" is in play
    And "Combat Training" is in play

    Then the legal targets for "Play" on "01043a" are
      | Panther Claws |
    And I cannot choose "Play" targeting "Combat Training"

    When I play "01043a"
    Then "Rhino" has 4 damage
    And I am not prompted again

  @card:01043a
  Scenario: the order named decides which upgrade gets the final step
    # Panther Claws first, Tactical Genius last: 2 damage (not 4) and 2 threat
    # (not 1). Both halves matter -- an engine that treated every step as final
    # would deal 4 and remove 2, and an engine that treated none as final would
    # deal 2 and remove 1.
    Given my hand is "01043a", "Vibranium"
    And "Panther Claws" is in play
    And "Tactical Genius" is in play
    And the main scheme has 5 threat

    When I choose "Play" on "01043a" targeting "Panther Claws", "Tactical Genius"
    Then "Rhino" has 2 damage
    And the main scheme has 3 threat
    And I am not prompted again

  @card:01043a
  Scenario: the same two upgrades in the other order swap both numbers
    # "in any order", stated as a difference rather than as an adjective. The
    # board is identical to the scenario above and only the target order moved,
    # so an engine that resolved the upgrades in board order -- or in any fixed
    # order of its own -- passes exactly one of these two.
    Given my hand is "01043a", "Vibranium"
    And "Panther Claws" is in play
    And "Tactical Genius" is in play
    And the main scheme has 5 threat

    When I choose "Play" on "01043a" targeting "Tactical Genius", "Panther Claws"
    Then "Rhino" has 4 damage
    And the main scheme has 4 threat
    And I am not prompted again

  @card:01043a
  Scenario: each means each -- naming no order resolves every upgrade
    # the original investigation. The card script spelled its selector `range=(1, "All")`,
    # which means "some or all", so a player controlling three upgrades could
    # resolve one and stop -- and self-play did exactly that, because
    # `BotCommand.Build` always submits `min_targets`. `range="All"` fixes the
    # count at the whole candidate set.
    #
    # The claim is the *count*, so this transcript deliberately names no
    # targets: the numbers below are what a run that resolves both steps
    # produces, and a minimum of 1 resolves only Panther Claws -- as the sole
    # and therefore final step of its sequence, dealing 4 and removing no
    # threat. Every number here moves if the minimum comes back.
    Given my hand is "01043a", "Vibranium"
    And "Panther Claws" is in play
    And "Tactical Genius" is in play
    And the main scheme has 5 threat

    Then the target minimum for "Play" on "01043a" is 2

    When I play "01043a"
    Then "Rhino" has 2 damage
    And the main scheme has 3 threat
    And I am not prompted again

  @card:01043a
  Scenario: with no Black Panther upgrade in play the event is not offered
    # "each [[Black Panther]] upgrade you control" needs at least one, and the
    # engine enforces that by filtering the play out of the menu rather than by
    # letting it resolve to nothing. Combat Training is in play, so this is the
    # trait doing the work and not an empty board.
    Given my hand is "01043a", "Vibranium"
    And "Combat Training" is in play

    Then I am prompted to choose one
      | Attack      |
      | Change Form |

  # --------------------------------------------------------------------------
  # The other three printings, one existence proof each.
  #
  # These are what earn 01043b, 01043c and 01043d their tags: the claim is that
  # each of them runs 01043a's ability, and it is played rather than read off
  # the `{"kind": "ability"}` link in `data/cards.json`. The board is the lone
  # Panther Claws from the first scenario, so each printing resolves the sole
  # and therefore final step of its own sequence and deals 4.
  #
  # It is three scenarios rather than one turn with three plays because payment
  # is chosen by the runner, not by the transcript, and it is greedy over the
  # hand in engine order (`BotCommand.BuildPaymentInternal`). With all three
  # printings in one hand, playing the first one spends the second as its
  # resource: another printing of the same card is a perfectly good 1-icon
  # resource. A hand of exactly one printing and one Vibranium leaves no such
  # ambiguity, and none of these three leans on the order a hand happens to be
  # written in -- which decides nothing else in this format.

  @card:01043b
  Scenario: the mental printing resolves the same ability
    Given my hand is "01043b", "Vibranium"
    And "Panther Claws" is in play

    When I play "01043b"
    Then "Rhino" has 4 damage
    And "01043b" is in the "DiscardPile"
    And I am not prompted again

  @card:01043c
  Scenario: the physical printing resolves the same ability
    Given my hand is "01043c", "Vibranium"
    And "Panther Claws" is in play

    When I play "01043c"
    Then "Rhino" has 4 damage
    And "01043c" is in the "DiscardPile"
    And I am not prompted again

  @card:01043d
  Scenario: the wild printing resolves the same ability
    Given my hand is "01043d", "Vibranium"
    And "Panther Claws" is in play

    When I play "01043d"
    Then "Rhino" has 4 damage
    And "01043d" is in the "DiscardPile"
    And I am not prompted again
