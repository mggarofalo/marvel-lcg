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
# What `BotOption.name` holds, and why matching on it is not string-matching
# against English. `Effect.GetDisplayName` derives these from the ability's
# function tag -- `IsFunction("ATK")`, `("THW")`, and so on -- and returns
# hardcoded ASCII constants, which `Effect.Render` writes into the descriptor
# with `remove_space=True`. They are engine identifiers that happen to read as
# English, and they are what a saved scene records in its effect ids.
#
# An action ability appearing more than once on a card gets a `_<index>` suffix
# (the "10029" fix in `GetDisplayName`), so match the prefix, not the whole
# string.
ASK = "Ask"

# Options whose ability is offered, recorded as a replay step, and legally
# resolves to no state change. `Ask` -- offer a teammate a chance to act, do
# nothing if they decline -- is 621 of the 711 measured cases and the only one
# that is both inert and ubiquitous. The full inventory, and why it cannot be
# completed, is docs/no-op-decisions.md.
NO_OP_VERBS = (ASK,)


class NoOpAwarePolicy(BotPolicy):
    """Engine order, with the known no-op options tried last.

    Registered as `heuristic` because it is the policy slot MARVEL-14 asks for,
    but it is deliberately *less* clever than that issue proposed, because the
    cleverness measured worse. What survives is one change: an option that
    resolves to nothing goes to the back of the queue.

    ## What was measured

    23 games per policy over five scenarios at one, two, three and four heroes,
    identical seeds. Cards resolved is the coverage number that matters; villain
    stage 2 is MARVEL-14's depth criterion.

    | policy | cards resolved | new vs `first` | stage 2 |
    |---|---|---|---|
    | `random` | 406 | 10 | 0/23 |
    | `first` | 429 | -- | **6/23** |
    | graded verbs (attack > thwart > play > ...) | 424 | 10 | 2/23 |
    | **this** | **436** | **20** | 2/23 |

    ## What was tried and rejected

    *Verb preferences.* Scoring attack above thwart above play, with the gaps
    wide enough to matter, reached **fewer** cards than leaving the engine's own
    order alone (424 against 436). The engine offers an identity's basic actions
    before anything in hand, so its order is already a reasonable policy -- it is
    the whole of `FirstLegalPolicy` -- and overriding it with a guess was worse
    than not.

    *Threat-aware thwarting.* Thwarting was promoted over attacking once the main
    scheme passed a share of its threshold. Measured at thresholds 0.5, 0.9 and
    1.1 -- which is to say, including "never" -- and all three produced the same
    2/23. The idea was sound and the effect was nil.

    *Penalising attacks while thwarting.* Strictly harmful: attacks fell 190 to
    149 and stage 2 fell 26.1% to 8.7%. Surviving longer is not the goal; a
    villain is only defeated by damage, and a stage only advances when one is.

    ## What this does not fix

    It does **not** beat `first` on depth: 2/23 against 6/23, and six different
    scoring profiles all landed on 2/23, so that is structural rather than
    untuned. `first` walks the engine's list by `attempt + repeats` from the
    front; anything that re-ranks explores differently and, on these scenarios,
    worse. Finding out why is future work -- see MARVEL-14.

    The value here is not that this policy is better. It is that it is
    **different**: it reaches 20 cards `first` never does, and the union of the
    three policies resolves 453 cards against 429 for the best single one. That
    is what `mixed` is for.
    """

    name = "heuristic"

    def __init__(self) -> None:
        self.guard = RepeatGuard()

    @override
    def OnGameStart(self, seed: int) -> None:
        self.guard.Reset()

    def IsNoOp(self, option: 'BotOption') -> bool:
        name = option.name
        return any(name == verb or name.startswith(verb + "_")
                   for verb in NO_OP_VERBS)

    @override
    def Choose(self, decision: 'BotDecision') -> 'CommandDescriptor':
        # `position` keeps the engine's ordering inside each group. Sorting is
        # stable, so this is a partition rather than a re-rank: everything that
        # does something, in engine order, then everything that does not.
        pairs = []
        for position, option in enumerate(decision.selectable_options):
            command = BotCommand.Build(option)
            if command is not None:
                pairs.append((1 if self.IsNoOp(option) else 0, position, command))

        if not pairs:
            return Fallback(decision)

        repeats = self.guard.Update(decision)
        if not decision.can_cancel:
            # Same rule as `FirstLegalPolicy`: a forced decision is the engine
            # driving the game forward, not the bot spinning.
            repeats = 0

        pairs.sort(key=lambda pair: (pair[0], pair[1]))

        index = decision.attempt + repeats
        if index < len(pairs):
            return pairs[index][2]

        return Fallback(decision)

################################################################################
#
BOT_MIXED_POLICIES = ConfigVariables.Str('bot_mixed_policies', "first,heuristic,random")

class MixedPolicy(BotPolicy):
    """Rotate through several policies, one per game.

    MARVEL-14 asks for this directly, and the measurement is what justifies it:
    the three policies do not merely differ in strength, they reach *different*
    cards. `heuristic` resolves 20 that `first` never does; `first` resolves 13
    that `heuristic` never does; `random` adds 10 of its own. Any single policy
    tops out around 436 cards, and the union reaches 453.

    Rotation is by game index rather than anything random, so a run of N games
    covers the policies evenly and reproducibly, and `-bot_seed` still decides
    the game. The sub-policy for a game is a pure function of how many games
    have started, which `BotRunner` drives in order.
    """

    name = "mixed"

    def __init__(self, seed: int) -> None:
        self.seed = seed
        self.games = -1
        self.policies: List['BotPolicy'] = []
        for name in BOT_MIXED_POLICIES.value.split(","):
            name = name.strip()
            if name and name != MixedPolicy.name:   # no recursion
                self.policies.append(BotPolicyFactory.Create(name, seed))
        if not self.policies:
            self.policies = [FirstLegalPolicy()]
        self.current = self.policies[0]

    @override
    def OnGameStart(self, seed: int) -> None:
        self.games += 1
        self.current = self.policies[self.games % len(self.policies)]
        Log.Info(CATEGORY_NAME,
                 f"Game {self.games}: mixed policy using {self.current.name!r}")
        self.current.OnGameStart(seed)

    @override
    def Choose(self, decision: 'BotDecision') -> 'CommandDescriptor':
        return self.current.Choose(decision)

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
        if policy_name == NoOpAwarePolicy.name:
            return NoOpAwarePolicy()
        if policy_name == MixedPolicy.name:
            return MixedPolicy(policy_seed)

        Log.Warn(CATEGORY_NAME, f"Unknown bot policy {policy_name!r}, using {FirstLegalPolicy.name!r}")
        return FirstLegalPolicy()
