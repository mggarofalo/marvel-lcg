# Which option an assertion is about. MARVEL-134 / MARVEL-141.
#
# Three `Then` steps inspect a single offered option rather than the board:
# `the legal targets for`, `the target minimum for` and `the target maximum
# for`. Each has to answer the question "which option?" before it can answer
# anything else, and the option's **label** is not an answer. `Play` is the
# label of every playable card in hand, so a decision routinely offers several,
# and a scenario naming only `Play` is naming a set rather than an option.
#
# The two ways that goes wrong are different, and only one of them is loud:
#
#   * **ordering.** With two `Play` options the assertion was answered by
#     whichever the engine enumerated first, so the same claim about the same
#     board passed or failed on the order a hand happened to be written in.
#   * **absence.** With the intended card's `Play` filtered out of the menu --
#     which is how the engine says "you cannot do this" -- an unrelated `Play`
#     was still there to answer in its place. That one is silent: the assertion
#     resolves, against the wrong card.
#
# So all three take `on "<card>"` and match on the option's `bind_id`: the
# object id of the card the engine attached the option to. The unbound spelling
# is still accepted, because most labels are unique at the decision that offers
# them, but an unbound label matching more than one option is **unresolvable**
# rather than resolved by enumeration order.
#
# ---------------------------------------------------------------------------
# The board, and why it is this one
#
# Two cards in hand that both offer `Play` and disagree about everything else:
#
#   * **Haymaker** (01087) -- "Hero Action (attack): Deal 3 damage to an enemy."
#     Its targets are the enemies in play.
#   * **Wakanda Forever!** (01043a) -- "Hero Action: Resolve the "Special"
#     ability on each [[Black Panther]] upgrade you control in any order.
#     (Resolving each ability is a step in a sequence.)" Its targets are the
#     [[Black Panther]] upgrades the hero controls.
#
# Nothing is a legal target of both, so a mis-bound assertion cannot pass by
# coincidence: it fails with a target list belonging to the other card. Wakanda
# Forever! also disappears from the menu when the hero controls no
# [[Black Panther]] upgrade -- printed behaviour rather than a harness
# contrivance, pinned in its own right by
# `specs/cards/core/01043a-wakanda-forever.feature`.
#
# The hand holds nothing but the two events, and neither is affordable. That is
# deliberate and it is worth knowing: **MARVEL-130's affordability filter does
# not apply to a play.** `EffectChecker` sets `requires_affordability` for a
# choose ability or a non-play action, so an unaffordable `Play` is still
# offered and only fails when it is chosen. Both options are therefore present
# to be told apart, which is all these scenarios are about.
#
# ---------------------------------------------------------------------------
# What each scenario controls for, and what it cannot
#
# The **ordering** hazard has a passing spelling, so the two scenarios below are
# a real control: each states both cards' target lists, so a resolver that read
# the first matching option fails one assertion in the first scenario and the
# other assertion in the second, and passes neither.
#
# The **absence** hazard does not. A bound assertion whose card has left the
# menu is *unresolvable*, and this vocabulary has no way to assert that a step
# is unresolvable -- by design, since a scenario that could would be asserting
# its own failure. The third scenario states what the board looks like when one
# of two shared-label options is gone, which is the board the hazard needs, but
# it does not discriminate: with Wakanda Forever! filtered out only one `Play`
# remains, so a first-match resolver reads the same option a bound one does.
# The control for that direction is
# `unit_test/test_spec_harness.py::test_a_card_bound_target_list_does_not_inspect_another_play_option`,
# which asserts the run comes back unresolvable.
#
# ---------------------------------------------------------------------------
# Under specs/rules/ rather than specs/cards/, and untagged, because the claim
# is about how an assertion finds its option and not about either card. Neither
# scenario resolves an ability, so crediting `@card:` coverage here would report
# work that has not been done. `specs/rules/target-counts.feature` is the
# sibling file for how a selection is bounded once the option has been found.

Feature: Option binding

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"
    And I am in hero form

  # --------------------------------------------------------------------------
  # Ordering: the same two claims, twice, over a hand written both ways.

  Scenario: two cards offering Play answer for their own targets
    Given my hand is "Haymaker", "01043a"
    And "Panther Claws" is in play

    Then the legal targets for "Play" on "01043a" are
      | Panther Claws |
    And the legal targets for "Play" on "Haymaker" are
      | Rhino |

  Scenario: writing the hand the other way round moves neither answer
    # The control for the scenario above, and the whole of the ordering claim.
    # The board is identical and only the order the two events sit in the hand
    # has changed. Hand order decides which card the first `Play` belongs to, so
    # a resolver that read the first matching option answers Rhino for Wakanda
    # Forever! in one of these two scenarios and Panther Claws for Haymaker in
    # the other -- failing one assertion in each and passing neither.
    Given my hand is "01043a", "Haymaker"
    And "Panther Claws" is in play

    Then the legal targets for "Play" on "01043a" are
      | Panther Claws |
    And the legal targets for "Play" on "Haymaker" are
      | Rhino |

  # --------------------------------------------------------------------------
  # Absence: one of the two Play options is gone and the other is not. Stated
  # rather than controlled -- see the header for why the failing direction has
  # no passing spelling.

  Scenario: a filtered-out Play leaves the other card's Play untouched
    # Combat Training is an upgrade the hero controls and is not a
    # [[Black Panther]] upgrade, so Wakanda Forever! has nothing to resolve and
    # the engine removes its `Play` from the menu. Haymaker's `Play` is still
    # offered, with its own targets -- which is exactly the option an unbound
    # `the legal targets for "Play" are` would read in Wakanda Forever!'s place.
    Given my hand is "Haymaker", "01043a"
    And "Combat Training" is in play

    Then I am not offered "Play" on "01043a"
    And the legal targets for "Play" on "Haymaker" are
      | Rhino |
