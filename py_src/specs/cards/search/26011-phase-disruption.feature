# Phase Disruption.
#
# Printed: "Play only if Vision is in Intangible mass form. / Hero Action:
# Confuse an enemy. Choose an attachment on that enemy with the text "Hero
# Action" or "Hero Response" and discard that attachment."
#
# This is the *forced* half of `Players.DiscardHeroActionAttachment`
# (`may=False` -> `Player.ChooseAbilities`), and it takes the helper's `enemies`
# branch: the discard is scoped to attachments on the enemy this card just
# confused, not to every attachment on the board. 49008 Electromagnetic Blast,
# which pins the same helper's label, passes `enemies=None` and so only ever
# reaches the `else` branch -- nothing in the suite reached this one, and a
# label change to the `enemies` construction site was invisible. That is what
# these scenarios exist to see.
#
# ---------------------------------------------------------------------------
# Board notes.
#
# Vision is the cheap hero for this: his alter-ego setup puts the mass form
# upgrade into play Intangible side faceup, so the printed play restriction is
# satisfied on turn one with no form manipulation in the transcript.
#
# Enhanced Ivory Horn attaches to Rhino and prints "Hero Action", so it is what
# the discard is offered on. **Two copies are needed to see the prompt at all.**
# A forced choice of one ability over one legal target is not asked -- the
# engine resolves it -- so the single-attachment board discards silently and
# renders no label. The second scenario records that, because it is the reason
# the first has to stock two.
#
# The second scenario also carries Stun Net, which attaches to *your identity*
# and prints "Hero Action", to pin the "on that enemy" half of the clause. It is
# the only assertion here that can see the difference between the two branches'
# target sets: with the discard scoped to the enemy there is one legal target
# and no prompt, and with it scoped board-wide there are two and the engine
# asks. Psychic Inertia (40173) was tried first and cannot do this job --
# `with_texts=["Hero Action"]` reads ability *flags*, not card text, and 40173 is
# scripted as a plain `AbilityType.Action` despite printing "Hero Action:".

Feature: Phase Disruption

  Background:
    Given the scenario is "rhino"
    And the hero is "vision"
    And I am in hero form
    And my hand is "Phase Disruption", "Backflip", "Backflip"

  @card:26011
  Scenario: two attachments on the confused enemy make the discard a choice
    Given the encounter deck is "Enhanced Ivory Horn", "Enhanced Ivory Horn"
    And "Enhanced Ivory Horn #1" is in play
    And "Enhanced Ivory Horn #2" is in play

    When I play "Phase Disruption"
    Then "Rhino" is confused
    And I am prompted to choose one
      | Discard an attachment |
    And the legal targets for "Discard an attachment" are
      | Enhanced Ivory Horn |
      | Enhanced Ivory Horn |

    When I choose "Discard an attachment" targeting "Enhanced Ivory Horn #1"
    Then "Enhanced Ivory Horn #1" is not in play
    And "Enhanced Ivory Horn #2" is in play
    And I am not prompted again

  @card:26011
  Scenario: a lone attachment on the enemy is discarded without being asked about
    Given "Enhanced Ivory Horn" is in play
    And "Stun Net" is in play

    When I play "Phase Disruption"
    Then "Rhino" is confused
    And "Enhanced Ivory Horn" is not in play
    And "Stun Net" is in play
    And I am not prompted again
