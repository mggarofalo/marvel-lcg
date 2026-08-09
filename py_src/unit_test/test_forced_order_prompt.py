"""`EventManager.AskForcedOrder` against a live world (MARVEL-40).

`SelectForcedEffect` and `ForcedOrderLabels` are pure and covered by
`test_forced_effect_selection.py`. The prompt itself is not: it builds real
abilities through `AbilityFactory`, carries a real `Ties` rule as its
`by_effect`, and goes through `Player.ChooseAbilities`.

Nothing in self-play reaches it -- `tools/determinism/probe_forced_selection.py`
reports zero multi-candidate batches across the whole wide matrix -- so without
this file the prompt would ship having never executed. These boot the engine, so
they belong in the slower tier and must be run from `py_src/`.

Only the decision is stubbed. Everything the prompt constructs is real.
"""

import unittest

# `engine` first, and not for its side effects: `game.*` modules import each
# other in a cycle that only resolves if `engine/__init__.py` has already walked
# it.
import engine  # noqa: F401

from game.event.manager import EventManager
from tools.spec.case import SpecCase, ThenStep
from tools.spec.harness import EnsureEngine, NewGameForCase
from tools.spec.policy import TranscriptPolicy


def NewWorld():
    """A solo Rhino board -- villain, main scheme, identity, nothing else."""
    EnsureEngine()
    case = SpecCase(
        name="forced ability order prompt",
        scenario="rhino",
        heroes=("spider_man",),
        beats=(ThenStep("Rhino", "health", 14),),
    )
    game = NewGameForCase(case, TranscriptPolicy())
    assert game.GameSetup()
    return game.world


def TwoRealEffects(world):
    """Two registered effects from one card in play.

    Same card on purpose: that is the case MARVEL-40 makes selectable and the
    one a face selector could never express.
    """
    for card in world.object_manager.card_dict.values():
        effects = list(card.GetEffects())
        if len(effects) >= 2:
            return effects[0], effects[1]
    raise AssertionError("no card in play carries two effects")


class RecordingPlayer:
    """The first player, with only `ChooseAbilities` replaced.

    Returns an effect wrapping the ability at `pick`, which is the shape
    `ChooseAbilitiesHelper` really returns -- one `Effect` per chosen ability,
    carrying the very `Ability` object it was handed.
    """

    def __init__(self, real, pick: int | None) -> None:
        self.real = real
        self.pick = pick
        self.by_effect = None
        self.labels: list[str] = []

    def ChooseAbilities(self, by_effect, *abilities, **kwargs):
        self.by_effect = by_effect
        self.labels = [ability.name for ability in abilities]
        if self.pick is None:
            return []
        chosen = abilities[self.pick]
        return [type("ChosenEffect", (), {"ability": chosen})()]

    def __getattr__(self, name):
        return getattr(self.real, name)


class TestAskForcedOrder(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.world = NewWorld()

    def setUp(self):
        self.manager = self.world.event_manager
        self.first, self.second = TwoRealEffects(self.world)

    def test_it_returns_the_candidate_whose_option_was_picked(self):
        for pick, expected in ((0, self.first), (1, self.second)):
            player = RecordingPlayer(self.world.GetFirstPlayer(), pick)
            chosen = self.manager.AskForcedOrder(player, [self.first, self.second])
            self.assertIs(chosen, expected)

    def test_it_offers_one_option_per_candidate(self):
        player = RecordingPlayer(self.world.GetFirstPlayer(), 0)
        self.manager.AskForcedOrder(player, [self.first, self.second])

        self.assertEqual(len(player.labels), 2)
        self.assertEqual(len(set(player.labels)), 2, player.labels)

    def test_the_prompt_names_the_rule_it_is_applying(self):
        # `Ties` rather than a bare `GameRule`, so the log and the client say why
        # the player is being asked.
        player = RecordingPlayer(self.world.GetFirstPlayer(), 0)
        self.manager.AskForcedOrder(player, [self.first, self.second])

        self.assertEqual(
            player.by_effect.GetDisplayName(),
            "Forced abilities would initiate at the same moment",
        )

    def test_declining_returns_none_so_the_caller_falls_back(self):
        player = RecordingPlayer(self.world.GetFirstPlayer(), None)
        self.assertIsNone(
            self.manager.AskForcedOrder(player, [self.first, self.second])
        )

    def test_the_whole_selection_runs_end_to_end_on_a_live_world(self):
        # The path `ProcessForcedEffect` actually takes, with a real world behind
        # it: two abilities on one card, resolved to the second.
        player = RecordingPlayer(self.world.GetFirstPlayer(), 1)
        chosen = EventManager.SelectForcedEffect(
            [self.first, self.second],
            lambda candidates: self.manager.AskForcedOrder(player, candidates),
        )
        self.assertIs(chosen, self.second)


if __name__ == "__main__":
    unittest.main()
