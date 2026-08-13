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

## Two counters, because a loop need not stand still

Counting *consecutive identical* digests only sees a cycle of period 1. MARVEL-99
found the other shape: a policy re-picking "swap these two encounter cards" on
every pass of `PlayerAction.SwapTheseCards`'s `while True`, which alternates
between exactly two board states for ever. Every step changes the digest, so the
counter above resets on every step and the loop ran to the 20,000-step
`bot_max_steps` wall instead -- 14 of 321 corpus cases, each paying the full
budget to yield nothing.

The generalisation is **decisions since the last state the game had never been in
before**. A cycle of any period stops producing novel states as soon as it closes;
the run above is the special case where the period is 1. It needs no knowledge of
what the loop is, which is the same argument that made the digest the right thing
to watch in the first place.

Measured over 902 completed scenes and 105,633 decisions from the MARVEL-96
corpus run, the longest legitimate run with no novel state is **10** (751 runs of
1, 227 of 4, 50 of 5, 5 of 6, one of 7, one of 10; nothing between 10 and the
stalls). `bot_stall_limit` defaults to **256**, twenty-five times that -- and
still two orders of magnitude cheaper than the wall it replaces. The margin is
wider than the tight limit's because a revisited state is a weaker signal than a
frozen one.
"""

import hashlib

from core import *
from core.errors import EngineIntegrityError
from engine.config import ConfigVariables

CATEGORY_NAME = "BOT"

BOT_NO_PROGRESS_LIMIT = ConfigVariables.Int('bot_no_progress_limit', 32)
BOT_STALL_LIMIT       = ConfigVariables.Int('bot_stall_limit', 256)


class NoProgressError(EngineIntegrityError):
    """A run of decisions changed nothing, so the game is not advancing.

    `EngineIntegrityError` rather than a plain exception because the handlers
    between here and the runner catch broadly -- `EffectInvoker`, `Message2.Send`
    and `Engine.EngineRun` all absorb so one bad card cannot end a game, and
    `Log.OnCrash` re-raises only off a release build. A spinning run produces a
    wrong artefact, not a wrong frame. See AGENTS.md, "Headless bot".
    """


def DigestKey(digest: str) -> str:
    """A short, stable name for one board state.

    Stable is the operative word: this decides whether the guard fires, so it
    must not vary between processes. `hash()` on a `str` is salted per process,
    which would make two runs of one seed disagree about whether two states
    collided -- rare, but the corpus rests on the claim that they never do.
    """
    return hashlib.blake2b(digest.encode("utf-8"), digest_size=16).hexdigest()


class NoProgressGuard:
    """Watches the state digest for a bot that has stopped advancing the game.

    Two counters over the same observation, because a loop need not stand still:

    - `unchanged` -- consecutive decisions leaving the digest **identical**. The
      tight form, and the cheaper diagnosis: it names one recurring decision.
    - `stale` -- decisions since the last **novel** digest. The loose form, where
      the cycle has a period greater than 1 so every step changes the digest and
      `unchanged` resets every time. This is what MARVEL-99 was.

    Only whole decisions count. `BotDeviceManager.SupplyInput` runs again for
    the same step when the engine rejects an answer, and a rejected answer has
    not had its chance to change anything yet.
    """

    def __init__(self, limit: int|None=None, stall_limit: int|None=None) -> None:
        self.limit = limit if limit != None else BOT_NO_PROGRESS_LIMIT.value
        self.stall_limit = stall_limit if stall_limit != None else BOT_STALL_LIMIT.value
        self.Reset()

    def Reset(self) -> None:
        self.last_digest: str|None = None
        self.unchanged = 0
        self.since_step = 0
        self.recent: List[str] = []

        # One key per board state this game has ever been in. Bounded by the
        # decision count, so at worst `bot_max_steps` entries of 32 bytes.
        self.seen: Set[str] = set()
        self.stale = 0
        self.stale_since_step = 0
        self.cycle: List[str] = []

        # The loose form needs its own tail. `recent` above is emptied by every
        # digest change, and in a cycle of period > 1 that is every decision --
        # so it would report exactly one line, which is not a diagnosis.
        self.last_decisions: List[str] = []

    def Observe(self, digest: str, step_id: int, description: str) -> None:
        """Record one decision. Raises `NoProgressError` once a run is too long.

        `digest` is `replay.calculated_digest`, which `Controller.ChoiceOne` has
        already computed for this step -- the comparison below is a hash of a
        string the engine already built, not a second serialisation of the board.
        """
        if digest != self.last_digest:
            self.last_digest = digest
            self.unchanged = 0
            self.since_step = step_id
            self.recent = [description]
        else:
            self.unchanged += 1
            self.recent.append(description)
            if len(self.recent) > 8:
                self.recent.pop(0)

        self.last_decisions.append(description)
        if len(self.last_decisions) > 8:
            self.last_decisions.pop(0)

        key = DigestKey(digest)
        if key in self.seen:
            self.stale += 1
            self.cycle.append(key)
        else:
            self.seen.add(key)
            self.stale = 0
            self.stale_since_step = step_id
            self.cycle = [key]

        # The tight check first: it fires eight times sooner and its report
        # names a single decision rather than a cycle of them.
        if self.limit > 0 and self.unchanged >= self.limit:
            raise NoProgressError(self.Report(step_id))

        if self.stall_limit > 0 and self.stale >= self.stall_limit:
            raise NoProgressError(self.StallReport(step_id))

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

    def StallReport(self, step_id: int) -> str:
        # How many distinct states the loop moves between. 2 is the MARVEL-99
        # shape -- swap two cards, swap them back -- and it is the number that
        # says "cycle" rather than "slow game" at a glance.
        period = len(set(self.cycle))
        tail = self.last_decisions[-4:]
        cycle = "\n  ".join(tail)
        return (
            f"No new state for {self.stale} decisions "
            f"(steps {self.stale_since_step}-{step_id}): the game is cycling "
            f"between {period} board state(s) it has already been in.\n"
            f"The last {len(tail)} decision(s) were:\n  {cycle}\n"
            f"Every one of them changed the digest, which is why the "
            f"consecutive-identical check did not fire -- the loop moves, it "
            f"just does not go anywhere. See docs/no-op-decisions.md and "
            f"MARVEL-99. Raise -bot_stall_limit only with a scene that shows the "
            f"run is legitimate; the longest measured legitimate run is 10."
        )
