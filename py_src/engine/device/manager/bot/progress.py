"""Structural detection of a bot that has stopped making progress.

Some abilities are offered, recorded as a replay step, and legally resolve to no
state change at all -- the alter-ego "Ask" action is the canonical one, offering
a teammate a chance to act and doing nothing if they decline. A policy that
always answers the same way rides one of those forever while the step counter
climbs. `RepeatGuard` in `policies.py` works around it by moving `FirstLegalPolicy`
further down the option list when a question recurs, and the web client
hard-codes the same special case in `AutoActivate.isHasAutoActivate`.

Both of those are heuristics over the *question*. This is a check on the
*answer*: the state digest is the engine's own account of what changed, so a run
of decisions that all leave it identical has, by definition, made no progress.
Nothing about which ability was offered has to be known in advance, which is why
this is the backstop and `RepeatGuard` is not -- an inventory of no-op abilities
can be incomplete, and `docs/no-op-decisions.md` says why it probably is.

Failing loudly is the point. `bot_max_steps` already ends a spinning game, but
it ends it as a warning after thousands of wasted steps, with no scene saved and
nothing naming the cause -- a silently masked infinite loop, which during corpus
generation is worse than a crash. See MARVEL-37.

## The limit

Measured over 44 generated scenes and 4759 decisions, across solo, two-, three-
and four-hero games: the longest run of consecutive decisions that left the
digest unchanged was **4**. The distribution has no tail -- 751 runs of 1, 321 of
2, 58 of 3, 8 of 4, nothing longer.

The default of 32 is eight times that, so it is not a threshold real play is
expected to approach. It is deliberately not tight: a false positive here aborts
a generation run, and the failure it exists to catch is unbounded, so it will be
reached whatever the margin.
"""

from core import *
from core.errors import EngineIntegrityError
from engine.config import ConfigVariables

CATEGORY_NAME = "BOT"

BOT_NO_PROGRESS_LIMIT = ConfigVariables.Int('bot_no_progress_limit', 32)


class NoProgressError(EngineIntegrityError):
    """A run of decisions changed nothing, so the game is not advancing.

    `EngineIntegrityError` rather than a plain exception because the handlers
    between here and the runner catch broadly -- `EffectInvoker`, `Message2.Send`
    and `Engine.EngineRun` all absorb so one bad card cannot end a game, and
    `Log.OnCrash` re-raises only off a release build. A spinning run produces a
    wrong artefact, not a wrong frame. See AGENTS.md, "Headless bot".
    """


class NoProgressGuard:
    """Counts consecutive decisions that left the state digest unchanged.

    Only whole decisions count. `BotDeviceManager.SupplyInput` runs again for
    the same step when the engine rejects an answer, and a rejected answer has
    not had its chance to change anything yet.
    """

    def __init__(self, limit: int|None=None) -> None:
        self.limit = limit if limit != None else BOT_NO_PROGRESS_LIMIT.value
        self.Reset()

    def Reset(self) -> None:
        self.last_digest: str|None = None
        self.unchanged = 0
        self.since_step = 0
        self.recent: List[str] = []

    def Observe(self, digest: str, step_id: int, description: str) -> None:
        """Record one decision. Raises `NoProgressError` once the run is too long.

        `digest` is `replay.calculated_digest`, which `Controller.ChoiceOne` has
        already computed for this step -- this is a comparison, not a second
        serialisation of the board.
        """
        if digest != self.last_digest:
            self.last_digest = digest
            self.unchanged = 0
            self.since_step = step_id
            self.recent = [description]
            return

        self.unchanged += 1
        self.recent.append(description)
        if len(self.recent) > 8:
            self.recent.pop(0)

        if self.limit > 0 and self.unchanged >= self.limit:
            raise NoProgressError(self.Report(step_id))

    def Report(self, step_id: int) -> str:
        cycle = "\n  ".join(self.recent)
        return (
            f"No progress for {self.unchanged} decisions "
            f"(steps {self.since_step}-{step_id}): every one left the state "
            f"digest identical.\n"
            f"The last {len(self.recent)} were:\n  {cycle}\n"
            f"This is a policy riding a no-op ability, not a slow game -- see "
            f"docs/no-op-decisions.md. Raise -bot_no_progress_limit only with a "
            f"scene that shows the run is legitimate."
        )
