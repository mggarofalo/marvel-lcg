# Printed (The Crimson Cowl, stage 1A): "Contents: Ultron (I) and Ultron (II).
# (Ultron (II) and Ultron (III) instead for expert mode.) Ultron and Standard
# encounter sets. One modular encounter set (recommended: Under Attack).
# Setup: Put the Ultron Drones environment into play. Shuffle the encounter deck.
# Advanced to stage 1B."
#
# ---------------------------------------------------------------------------
# This header used to say the card had one reachable claim and not two, and that
# the setup line's first sentence was the missing one. **It is reachable now**,
# and the old reasoning is kept because it explains why every other file in this
# batch opens with `Given "Ultron Drones" is in play`.
#
# The ability hangs off `AbilityFactory.WhenCardSetup`, which fires inside
# `GameSetup()` at `World` step 12. A puzzle scene was built with an empty
# `set_aside`, an empty encounter deck and no modular sets, so that the board
# holds exactly what a scenario asks for -- and the harness applies every `Given`
# *after* `GameSetup()` returns. So the handler ran, `SetupCards.PutIntoPlay`
# searched for an Environment named "Ultron Drones", found nothing, and put
# nothing into play. Not because the card is optional in a real game, but because
# there was no way to hand the scene a card before setup.
#
# the original investigation added one (the same step 01116a needs, for the same reason):
#
#     Given the encounter deck at setup is "Ultron Drones", ...
#
# It is part of the scene the engine sets up from rather than a `Given` that runs
# earlier. `SetupCards.PutIntoPlay` goes through `SearchInternal.FindCards`,
# which reaches the encounter deck, so the setup deck is enough -- the printed
# card is set aside rather than shuffled in, and that difference is invisible to
# a search that finds it either way.
#
# The second and third printed sentences are unchanged from what this file
# already said. "Advance to stage 1B" is what the first scenario asserts, by the
# B face's own numbers. "Shuffle the encounter deck" is not assertable at all:
# the order of a shuffled deck is the RNG's, and a spec that pinned it would be
# pinning the RNG rather than the card.

Feature: The Crimson Cowl 1A

  @card:01137a
  Scenario: the game opens on stage 1B rather than on the setup face
    Given the scenario is "ultron"
    And the hero is "iron_man"

    # The B face's own numbers, which is how a scenario can tell which face is
    # in play: 1A carries no threat line at all.
    Then the main scheme has 0 threat
    And "the main scheme" has 3 "target_threat"
    And "the main scheme" has 1 "escalation_threat"
    And "the main scheme" has 1 "printed_stage"
    And "the main scheme" is in play
    And I am not prompted again

  @card:01137a
  Scenario: the setup line puts the Ultron Drones environment into play
    # Advanced Ultron Drone and Hydra Mercenary are the control on the search:
    # both are in the same deck, neither is an Environment named "Ultron Drones",
    # and neither is put into play. An engine that put the whole deck into play,
    # or the top card of it, fails on them.
    #
    # The drone is what says "into play" means in play rather than "in the
    # environment area". A facedown [[Drone]] has no printed statistics of its
    # own -- Ultron Drones is the permanent that gives it a base 1/1/1 -- so 1B's
    # own reveal, "each player puts the top card of their deck into play facedown
    # as a [[Drone]] minion", produces a minion with 1 hit point only because
    # this setup line already ran. Without it the drone enters play on 0 and is
    # defeated in the same breath, and there is nothing left to name.
    Given the scenario is "ultron"
    And the hero is "iron_man"
    And the encounter deck at setup is "Ultron Drones", "Advanced Ultron Drone", "Hydra Mercenary"
    And my deck at setup is "Energy", "Energy"

    Then "Ultron Drones" is in the "EnvironmentArea"
    And "Ultron Drones" is in play
    And "Advanced Ultron Drone" is not in play
    And "Hydra Mercenary" is not in play
    And "Drone Minion" has 1 health
    And "Drone Minion" has 1 "attack"
    And I am not prompted again
