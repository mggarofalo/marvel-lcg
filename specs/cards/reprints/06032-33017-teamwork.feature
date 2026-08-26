# Teamwork, printed once and scripted twice.
#
# Printed (both ids, byte-identical): "Hero Interrupt: When you use your basic
# thwart power (THW) or basic attack power (ATK), exhaust an ally you control ->
# add that ally's matching power to your hero's power for this use."
#
# There are three Teamworks: 06032 (Thor), 33017 (Cyclops) and 59021
# (Hercules). 59021 is a `full_link` to 06032 in data/cards.json and runs its
# module; 33017 had a script file of its own that reached the same result by a
# different route -- reading `return_exhausted_cards[0]` and calling
# `Hero.GainForThisActive` directly, where 06032 sums the whole returned list
# and calls `Message.WhenUnitUseBasicPower.GainValue`.
#
# The two routes were equivalent, and the reasons are worth recording because
# neither is obvious from the scripts:
#
#   * `CostFunc.Exhaust("YourAlly")` has range (1, 1) and only assigns
#     `return_exhausted_cards` when every target exhausted, so the list is
#     always exactly one ally. The sum and the `[0]` cannot come apart.
#   * `GainValue` dispatches on the would-message type and lands on
#     `attacker.GainForThisActive(..., attack=)` / `trigger.GainForThisActive(
#     ..., thwart=)`. The ability's trigger is "You", so that unit and
#     `Player.GetHero()` are the same card.
#
# 33017 now runs the same body as 06032. The mechanism 06032 uses is the one
# `Message.AddThatMatchingPower` -- the engine helper named after this card's
# printed clause -- is built on, and three other cards print the same clause and
# call it: 49019 "You Got This!", 58020 Unified Strike and 56052 Iron Lad.
#
# Hellcat prints ATK 1 and THW 2, Spider-Man ATK 2 and THW 1. The ally is
# deliberately one whose two powers differ: with Black Cat (ATK 1, THW 1) a
# script that added the wrong power would pass both branches.

Feature: Teamwork

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form

  @card:06032
  Scenario: 06032 adds the ally's ATK to a basic attack
    Given my hand is "06032", "Backflip", "Backflip", "Backflip"
    And "Hellcat" is in play

    When I attack "Rhino"
    Then I am prompted to choose one
      | Play |

    When I choose "Play" on "06032"
    Then "Rhino" has 3 damage
    And "Hellcat" is exhausted
    And I am not prompted again

  @card:33017
  Scenario: 33017 adds the ally's ATK to a basic attack
    Given my hand is "33017", "Backflip", "Backflip", "Backflip"
    And "Hellcat" is in play

    When I attack "Rhino"
    Then I am prompted to choose one
      | Play |

    When I choose "Play" on "33017"
    Then "Rhino" has 3 damage
    And "Hellcat" is exhausted
    And I am not prompted again

  @card:06032
  @card:33017
  Scenario: an unaided basic attack deals the hero's printed ATK
    # The control for the attack branch: 2, not 3. Without it "Rhino has 3
    # damage" is consistent with an engine whose Spider-Man simply hits for 3.
    Given my hand is "Backflip", "Backflip", "Backflip"
    And "Hellcat" is in play

    When I attack "Rhino"
    Then "Rhino" has 2 damage
    And "Hellcat" is not exhausted
    And I am not prompted again

  @card:06032
  Scenario: 06032 adds the ally's THW to a basic thwart
    # "matching power" is the other half of the card, and it is a different code
    # path -- `GainTHWForThisThwart` rather than `GainATKForThisAttack`.
    Given my hand is "06032", "Backflip", "Backflip", "Backflip"
    And "Hellcat" is in play
    And the main scheme has 4 threat

    When I thwart "the main scheme"
    Then I am prompted to choose one
      | Play |

    When I choose "Play" on "06032"
    Then the main scheme has 1 threat
    And "Hellcat" is exhausted
    And I am not prompted again

  @card:33017
  Scenario: 33017 adds the ally's THW to a basic thwart
    Given my hand is "33017", "Backflip", "Backflip", "Backflip"
    And "Hellcat" is in play
    And the main scheme has 4 threat

    When I thwart "the main scheme"
    Then I am prompted to choose one
      | Play |

    When I choose "Play" on "33017"
    Then the main scheme has 1 threat
    And "Hellcat" is exhausted
    And I am not prompted again

  @card:06032
  @card:33017
  Scenario: an unaided basic thwart removes the hero's printed THW
    Given my hand is "Backflip", "Backflip", "Backflip"
    And "Hellcat" is in play
    And the main scheme has 4 threat

    When I thwart "the main scheme"
    Then the main scheme has 3 threat
    And "Hellcat" is not exhausted
    And I am not prompted again
