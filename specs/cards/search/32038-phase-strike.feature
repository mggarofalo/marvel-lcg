# Phase Strike.
#
# Printed: "Hero Action (attack): Deal 6 damage to an enemy. If you are in
# Phased mass form, you may discard an attachment with the text "Hero Action" or
# "Hero Response" from that enemy."
#
# The *opt-in* half of `Players.DiscardHeroActionAttachment` (`may=True` ->
# `Player.MayChooseOneAbility`) reached through the helper's `enemies` branch --
# "from that enemy", the enemy Phase Strike just hit, rather than any attachment
# anywhere. 49008 Electromagnetic Blast is the same opt-in over the `else`
# branch, so between them the two construction sites in that helper are both
# pinned; before this file only the `else` one was.
#
# ---------------------------------------------------------------------------
# Board notes.
#
# Shadowcat's alter-ego setup puts the mass form upgrade into play Solid side
# faceup, so Phased has to be reached in the transcript. Kitty Pryde's action
# flips it, and changing to hero form afterwards is a separate action in the
# same turn. The third scenario is the control for that preamble: skip the
# Kitty Pryde action and the card deals its damage and asks nothing, which is
# the printed conditional and is also what tells a reader the two extra beats in
# the first two scenarios are load-bearing rather than ceremony.
#
# Two labels below are the engine's rather than the card's, which is the usual
# hazard: an option is named from the Python script, not from printed text.
# Kitty Pryde's flip is printed "Phase Control" and is offered as "Action". The
# lone "Response" in the third scenario is the Solid upgrade's printed "Response:
# After you attack or defend in Solid mass form, flip this card" -- an optional
# response, so it is asked, and it is asked only there: in Phased that clause is
# a Forced Response and the engine resolves it without asking.
#
# Phase Strike costs 3, hence three Backflips as resources. Enhanced Ivory Horn
# attaches to Rhino and prints "Hero Action", so it is the attachment on offer;
# its own "Hero Action" option sits on the turn menu throughout and is not part
# of any prompt this card raises.

Feature: Phase Strike

  Background:
    Given the scenario is "rhino"
    And the hero is "shadowcat"
    And I am in alter-ego form
    And "Enhanced Ivory Horn" is in play
    And my hand is "Phase Strike", "Backflip", "Backflip", "Backflip"

  @card:32038
  Scenario: phased Shadowcat is offered the attachment discard on the enemy hit
    When I choose "Action" on "Kitty Pryde"

    When I change form
    Then I am in hero form

    When I play "Phase Strike"
    Then "Rhino" has 6 damage
    And I am prompted to choose one
      | Discard an attachment |
      | Cancel                |

    When I choose "Discard an attachment"
    Then "Enhanced Ivory Horn" is not in play
    And I am not prompted again

  @card:32038
  Scenario: the attachment survives when the discard is declined
    When I choose "Action" on "Kitty Pryde"

    When I change form

    When I play "Phase Strike"
    When I choose "Cancel"
    Then "Rhino" has 6 damage
    And "Enhanced Ivory Horn" is in play
    And I am not prompted again

  @card:32038
  Scenario: both halves of the card land on the enemy the player named
    # "Deal 6 damage to **an** enemy ... discard an attachment ... **from that
    # enemy**". Every scenario above runs on a board with one enemy on it, where
    # the engine picks the target itself and the two clauses cannot come apart.
    # Here there are two, so the target is a decision the transcript makes, and
    # both clauses have to follow it: the damage goes on Radioactive Man and the
    # horn on Rhino is neither offered nor discarded.
    #
    # This is the assertion that separates `DiscardHeroActionAttachment`'s
    # `enemies` branch from its `else` branch. Scoped to the chosen enemy there
    # is nothing to discard and the opt-in is dropped before the prompt, so no
    # decision is put to the transcript; scoped board-wide the horn would be a
    # legal target and the engine would stop here asking about it.
    #
    # Radioactive Man rather than a Hydra Mercenary: Guard would make Rhino an
    # illegal target and leave one enemy to choose from, which is the board this
    # scenario exists to get away from. His 7 hit points also survive the 6, so
    # the damage is still there to read; his printed Forced Response is on
    # attacks against *you* and does not fire here.
    Given "Radioactive Man" is in play

    When I choose "Action" on "Kitty Pryde"

    When I change form

    When I play "Phase Strike" targeting "Radioactive Man"
    Then "Radioactive Man" has 6 damage
    And "Rhino" has 0 damage
    And "Enhanced Ivory Horn" is in play
    And I am not prompted again

  @card:32038
  Scenario: solid Shadowcat deals the damage and is offered nothing
    When I change form

    When I play "Phase Strike"
    Then "Rhino" has 6 damage
    And I am prompted to choose one
      | Response |

    When I choose "Response"
    Then "Enhanced Ivory Horn" is in play
    And I am not prompted again
