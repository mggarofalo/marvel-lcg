"""Trivial reference policies.

These exist to prove the device works end to end, nothing more. Neither one
plays well and neither is meant to. A policy that actually plays the game is
MARVEL-10 / MARVEL-14; it plugs in here by subclassing `BotPolicy` and being
registered in `BotPolicyFactory`.
"""

from core import *
from engine.config import ConfigVariables
from engine.log import Log
from engine.device.manager.bot.command import BotCommand
from engine.device.manager.bot.policy import BotDecision, BotPolicy, BotStuck
from game.scene.replay.operation import CommandDescriptor

CATEGORY_NAME = "BOT"

BOT_POLICY      = ConfigVariables.Str('bot_policy', "first")
BOT_POLICY_SEED = ConfigVariables.Int('bot_policy_seed', 0)

################################################################################
#
def Fallback(decision: 'BotDecision') -> 'CommandDescriptor':
    """What to answer when no option is left to try."""
    if decision.can_cancel:
        return BotCommand.Cancel()

    if BotCommand.IsForcedCancelAllowed(decision.options):
        # `Controller.ChoiceOne` accepts `id == 0` for a forced decision when it
        # is the only option and needs no targets. This is the "End Phase" shape.
        return BotCommand.Cancel()

    raise BotStuck(
        f"No answer left for '{decision.event_name}' "
        f"(step {decision.step_id}, attempt {decision.attempt}, "
        f"{len(decision.options)} options, cancel not allowed)"
    )

################################################################################
#
BOT_REPEAT_WINDOW = ConfigVariables.Int('bot_repeat_window', 32)

class RepeatGuard:
    """Counts how often an identical decision has recurred in the recent past.

    Some abilities are repeatable and change nothing when they resolve. The
    multiplayer "Ask" action is the clearest example — it offers a teammate a
    chance to act, and does nothing at all if the teammate declines. The web
    client special-cases it too (`AutoActivate.isHasAutoActivate`). A policy that
    always answers the same way loops on those forever.

    A sliding window rather than "same as last time" because the loops are
    cycles, not repeats: "Ask" bounces between two players, so the identical
    question only comes back every other decision.

    Seeing the same question again means the previous answer made no progress,
    so the policy should move further down the option list.
    """

    def __init__(self, window: int|None=None) -> None:
        self.window = window if window != None else BOT_REPEAT_WINDOW.value
        self.recent: List[Tuple[Any, ...]] = []

    def Reset(self) -> None:
        self.recent = []

    def Update(self, decision: 'BotDecision') -> int:
        signature = (
            decision.player_id,
            decision.event_name,
            decision.ability_type,
            tuple(option.id for option in decision.options),
        )
        count = self.recent.count(signature)

        self.recent.append(signature)
        if len(self.recent) > self.window:
            self.recent.pop(0)

        return count

################################################################################
#
class FirstLegalPolicy(BotPolicy):
    """Answer with the first option the engine will accept.

    Two things move the choice down the list:

    - `decision.attempt`, when the engine rejected the previous answer
    - `RepeatGuard`, when the same question comes back unchanged
    """

    name = "first"

    def __init__(self) -> None:
        self.guard = RepeatGuard()

    @override
    def OnGameStart(self, seed: int) -> None:
        self.guard.Reset()

    @override
    def Choose(self, decision: 'BotDecision') -> 'CommandDescriptor':
        commands = BotCommand.BuildAll(decision.selectable_options)

        repeats = self.guard.Update(decision)
        if not decision.can_cancel:
            # A forced decision is the engine driving the game forward, not the
            # bot spinning. "End Turn" recurs every turn with the same option and
            # is the only legal answer, so the guard must not skip past it.
            repeats = 0

        index = decision.attempt + repeats
        if index < len(commands):
            return commands[index]

        return Fallback(decision)

################################################################################
#
class SeededRandomPolicy(BotPolicy):
    """Answer with a random legal option, from a private seeded RNG stream.

    The RNG is a `random.Random` instance owned by this policy. It never touches
    `engine.lib.Random`, so the game's own shuffles and draws are unaffected and
    a given (game seed, policy seed) pair always replays identically.

    No `RepeatGuard`: a random policy cannot loop forever on a no-op ability
    because it re-rolls each time. Its only backstop against a very long game is
    `bot_max_steps`.
    """

    name = "random"

    def __init__(self, seed: int) -> None:
        import random
        self.seed = seed
        self.rng = random.Random(seed)

    @override
    def OnGameStart(self, seed: int) -> None:
        import random
        # Re-seed per game so game N does not depend on how long game N-1 ran.
        self.rng = random.Random(self.seed * 1000003 + seed)

    @override
    def Choose(self, decision: 'BotDecision') -> 'CommandDescriptor':
        commands = BotCommand.BuildAll(decision.selectable_options)

        if decision.attempt == 0 and commands:
            if decision.can_cancel:
                # Include "do nothing" as one of the choices so turns end.
                index = self.rng.randrange(len(commands) + 1)
                if index == len(commands):
                    return BotCommand.Cancel()
                return commands[index]
            return commands[self.rng.randrange(len(commands))]

        # A retry: fall back to a deterministic walk so we always terminate.
        if decision.attempt < len(commands):
            return commands[decision.attempt]

        return Fallback(decision)

################################################################################
#
class BotPolicyFactory:

    @staticmethod
    def Create(name: str|None=None, seed: int|None=None) -> 'BotPolicy':
        policy_name = name if name != None else BOT_POLICY.value
        policy_seed = seed if seed != None else BOT_POLICY_SEED.value

        if policy_name == FirstLegalPolicy.name:
            return FirstLegalPolicy()
        if policy_name == SeededRandomPolicy.name:
            return SeededRandomPolicy(policy_seed)

        Log.Warn(CATEGORY_NAME, f"Unknown bot policy {policy_name!r}, using {FirstLegalPolicy.name!r}")
        return FirstLegalPolicy()
