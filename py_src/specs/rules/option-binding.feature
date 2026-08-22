# Which option an assertion is about. MARVEL-134 / MARVEL-141.
#
# Four `Then` steps inspect a single offered option rather than the board:
# `the legal targets for`, `the target minimum for`, `the target maximum for`,
# and the negative `I am not offered`. Each of them has to answer the question
# "which option?" before it can answer anything else, and the option's **label**
# is not an answer. `Play` is the label of every playable card in hand, so a
# decision routinely offers several, and a scenario naming only `Play` is naming
# a set rather than an option.
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
# So an option assertion takes `on "<card>"` and is matched on the option's
# `bind_id`: the object id of the card the engine attached the option to. The
# unbound spelling is still accepted, because most labels are unique at the
# decision that offers them, but an unbound label matching more than one option
# is **unresolvable** rather than resolved by enumeration order.
#
# ---------------------------------------------------------------------------
# The board, and why it is this one
#
# Two cards in hand that both offer `Play` and disagree about everything else:
#
#   * **Haymaker** (01087) -- "Hero Action (attack): Deal 3 damage to an enemy."
#     Its targets are the enemies in play.
#   * **Wakanda Forever!** (01043a) -- "Hero Action: Resolve the "Special"
#     ability on each [[Black Panther]] upgrade you control in any order."
#     Its targets are upgrades the hero controls.
#
# Nothing is a legal target of both, so a mis-bound assertion cannot pass by
# coincidence: it fails with a target list belonging to the other card. Wakanda
# Forever! also disappears from the menu when the hero controls no
# [[Black Panther]] upgrade, which is the absent-option control -- and it is
# printed behaviour rather than a harness contrivance, pinned in its own right
# by `specs/cards/core/01043a-wakanda-forever.feature`.
#
# The hand carries two Vibranium so both events are affordable at once: since
# MARVEL-130 an unpayable action is not offered at all, and an assertion about
# an option the engine never showed would be unresolvable for the wrong reason.
#
# Under specs/rules/ rather than specs/cards/ because the claim is about how an
# assertion finds its option; the two cards are the sharpest instance of it and
# not the subject. `specs/rules/target-counts.feature` is the sibling file for
# how a selection is bounded once the option has been found.

Feature: Option binding

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"
    And I am in hero form

  # --------------------------------------------------------------------------
  # Ordering: the same two claims, twice, over a hand written both ways.

  @card:01043a @card:01087
  Scenario: two cards offering Play answer for their own targets
    Given my hand is "Haymaker", "01043a", "Vibranium", "Vibranium"
    And "Panther Claws" is in play

    Then the legal targets for "Play" on "01043a" are
      | Panther Claws |
    And the legal targets for "Play" on "Haymaker" are
      | Rhino |

  @card:01043a @card:01087
  Scenario: writing the hand the other way round moves neither answer
    # The control for the scenario above, and the whole of the ordering claim.
    # The board is identical and only the order the two events sit in the hand
    # has changed, so an assertion that read the first matching option would
    # swap both target lists here and pass exactly one of these two scenarios.
    Given my hand is "01043a", "Haymaker", "Vibranium", "Vibranium"
    And "Panther Claws" is in play

    Then the legal targets for "Play" on "01043a" are
      | Panther Claws |
    And the legal targets for "Play" on "Haymaker" are
      | Rhino |

  # --------------------------------------------------------------------------
  # Absence: one of the two Play options is gone and the other is not.

  @card:01043a @card:01087
  Scenario: a filtered-out Play leaves the other card's Play untouched
    # Combat Training is an upgrade the hero controls and is not a
    # [[Black Panther]] upgrade, so Wakanda Forever! has nothing to resolve and
    # the engine removes its `Play` from the menu. Haymaker's `Play` is still
    # offered, with its own targets -- which is exactly the option an unbound
    # `the legal targets for "Play" are` would have read instead.
    Given my hand is "Haymaker", "01043a", "Vibranium", "Vibranium"
    And "Combat Training" is in play

    Then I am not offered "Play" on "01043a"
    And the legal targets for "Play" on "Haymaker" are
      | Rhino |
