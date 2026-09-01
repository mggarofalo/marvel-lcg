# Electromagnetic Blast.
#
# Printed: "Hero Action (thwart): Remove 3 threat from a scheme. If this removes
# the last threat from that scheme, you may discard an attachment with the text
# "Hero Action" or "Hero Response.""
#
# This card was listed alongside the six "may search" cards on the original investigation, on
# the strength of the `may=True` keyword it passes. It is a different `may`:
# `Players.DiscardHeroActionAttachment(..., may=True)` branches to
# `Player.MayChooseOneAbility` and always has, so the opt-in was already a real
# option with a "Cancel" beside it and the card was never a no-op. It needed no
# repair, and the two scenarios below are the evidence. Phase Strike (32038)
# prints the same clause over the same helper and is the same story.
#
# What it did share with the search cards is the *labelling* half of the bug.
# `DiscardHeroActionAttachment` built its ability with an empty name, and
# `Effect.Render` falls back to the binding effect's display name when a
# `ForChoiceAbility` has none -- so the opt-in was offered as **"Play"**, the
# name of the event being played, rather than as anything about discarding an
# attachment. It was answerable, unlike the search cards before the original investigation, but
# it did not say what it did.
#
# This scenario was written recording that wrong label rather than papering over
# it, so that fixing the engine would fail it on purpose. the original investigation is that fix:
# the ability now carries `Players.DISCARD_ATTACHMENT_PROMPT`, the same shape as
# `SearchInternal.MAY_SEARCH_PROMPT`, and the prompt table below is what the
# label change is pinned by.
#
# ---------------------------------------------------------------------------
# Board notes.
#
# Enhanced Ivory Horn is Rhino's attachment and prints "Hero Action", so it is
# what the discard is offered on. The main scheme is set to exactly 3 threat:
# removing 3 removes the last, which is the condition the second clause hangs
# on. With one legal attachment the engine picks it without a further prompt, so
# choosing the opt-in is the whole transcript.

Feature: Electromagnetic Blast

  Background:
    Given the scenario is "rhino"
    And the hero is "magneto"
    And I am in hero form
    And "Enhanced Ivory Horn" is in play
    And my hand is "Electromagnetic Blast", "Backflip", "Backflip"

  @card:49008
  Scenario: removing the last threat offers the attachment discard
    Given the main scheme has 3 threat

    When I play "Electromagnetic Blast"
    Then the main scheme has 0 threat
    And I am prompted to choose one
      | Discard an attachment |
      | Cancel                |

    When I choose "Discard an attachment"
    Then "Enhanced Ivory Horn" is not in play
    And I am not prompted again

  @card:49008
  Scenario: the attachment survives when the discard is declined
    Given the main scheme has 3 threat

    When I play "Electromagnetic Blast"
    When I choose "Cancel"
    Then "Enhanced Ivory Horn" is in play
    And I am not prompted again

  @card:49008
  Scenario: threat left behind is offered nothing at all
    # "**If** this removes the last threat from that scheme". The two scenarios
    # above both empty the scheme, so both are equally consistent with an engine
    # that ignored the condition and offered the discard unconditionally. Five
    # threat minus three is two, and two is not none, so the opt-in never
    # appears -- and there is no `When` here to answer one, which is what makes
    # the claim load-bearing: an engine that asked anyway halts the transcript on
    # a decision it does not answer.
    Given the main scheme has 5 threat

    When I play "Electromagnetic Blast"
    Then the main scheme has 2 threat
    And "Enhanced Ivory Horn" is in play
    And I am not prompted again

  @card:49008
  Scenario: the three threat comes off the scheme the player named
    # "Remove 3 threat from **a** scheme", and then "the last threat from **that**
    # scheme" -- two clauses about the same chosen scheme, and neither is
    # observable on a board with only one scheme on it. Usurp The Throne is the
    # cheap second scheme: it prints no text at all, so nothing it does can be
    # mistaken for something this card did.
    #
    # The side scheme is emptied and the main scheme is untouched, so the opt-in
    # that follows is evidence about the *side* scheme's last threat. An engine
    # reading the condition off the main scheme instead would see five threat
    # standing and offer nothing.
    Given the main scheme has 5 threat
    And "Usurp The Throne" is in play
    And "Usurp The Throne" has 3 threat

    # The opt-in appearing here *is* the claim that the side scheme lost its
    # last threat, so nothing needs to say so twice. Where the side scheme ended
    # up is asserted at the end of the transcript rather than at this beat: the
    # engine processes a defeated scheme's removal once the event has finished
    # resolving, so at this point it is emptied but still standing in
    # `SideSchemesArea`, and a scenario should not be pinned to which side of
    # that line the prompt falls on.
    When I play "Electromagnetic Blast" targeting "Usurp The Throne"
    Then the main scheme has 5 threat
    And I am prompted to choose one
      | Discard an attachment |
      | Cancel                |

    When I choose "Discard an attachment"
    Then "Enhanced Ivory Horn" is not in play
    And "Usurp The Throne" is in the "EncounterDiscardPile"
    And I am not prompted again
