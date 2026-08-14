# Electromagnetic Blast.
#
# Printed: "Hero Action (thwart): Remove 3 threat from a scheme. If this removes
# the last threat from that scheme, you may discard an attachment with the text
# "Hero Action" or "Hero Response.""
#
# This card was listed alongside the six "may search" cards on MARVEL-112, on
# the strength of the `may=True` keyword it passes. It is a different `may`:
# `Players.DiscardHeroActionAttachment(..., may=True)` branches to
# `Player.MayChooseOneAbility` and always has, so the opt-in was already a real
# option with a "Cancel" beside it and the card was never a no-op. It needed no
# repair, and the two scenarios below are the evidence. Phase Strike (32038)
# prints the same clause over the same helper and is the same story.
#
# What it does share with the search cards is the *labelling* half of the bug.
# `DiscardHeroActionAttachment` builds its ability with an empty name, and
# `Effect.Render` falls back to the binding effect's display name when a
# `ForChoiceAbility` has none -- so the opt-in is offered as **"Play"**, the name
# of the event being played, rather than as anything about discarding an
# attachment. That is what the prompt table records. It is answerable, unlike
# the search cards before MARVEL-112, but it does not say what it does.
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
      | Play   |
      | Cancel |

    When I choose "Play"
    Then "Enhanced Ivory Horn" is not in play
    And I am not prompted again

  @card:49008
  Scenario: the attachment survives when the discard is declined
    Given the main scheme has 3 threat

    When I play "Electromagnetic Blast"
    When I choose "Cancel"
    Then "Enhanced Ivory Horn" is in play
    And I am not prompted again
