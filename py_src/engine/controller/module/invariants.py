"""Runs the world invariants inline, and turns a violation into a repro case.

The rules live in `game/world/invariants.py`; this is the part that knows when
to ask and what to do with the answer. It is a controller module like `replay`,
`skip` and `undo`, so it is constructed once and reset by
`ControllerManager.Setup` -- which runs for a new game, a load, a replay and an
undo alike, and is therefore the right granularity for the rules that remember
the previous decision.

`Controller.ChoiceOne` calls `Check` immediately after it computes the step's
digest, so a violation reported at step *n* describes exactly the state the
replay records against step *n*. Reload the dumped scene, replay it to *n*, and
the same rule fires: the repro is a repro by construction, not by luck.

Off by default; `Engine.Initialize` forces it on for the bot device, because a
self-play run with nothing watching is the case MARVEL-11 exists to fix.
`-no_check_invariants` turns it off again for corpus generation, which has
already paid for it once -- and it is not free: roughly 0.8ms per decision,
which is 1.86x the wall time of a 20000-decision game.

The forcing lives in `Engine.Initialize` rather than in the `bot` arg group,
which would read better and would not work. See the comment there, and
MARVEL-64.
"""

from core import *
from engine.config import ConfigVariables
from engine.file import FileManager
from engine.log import Log

CATEGORY_NAME = "INVARIANT"

CHECK_INVARIANTS = ConfigVariables.Bool('check_invariants', False)
INVARIANT_FOLDER = ConfigVariables.Folder('invariant_folder', "./invariants")

class InvariantModule:

    def __init__(self, manager: 'ControllerManager') -> None:
        from game.world.invariants import Progress

        self.manager = manager
        self.progress = Progress()

    @property
    def is_enabled(self) -> bool:
        return CHECK_INVARIANTS.value

    def Clean(self) -> None:
        """Forget the previous game. Called from `ControllerManager.Setup`."""
        self.progress.Reset()

    ################################################################################
    #
    def Check(self, world: 'World|None') -> None:
        """Assert every rule against `world`. Raises on the first violation.

        Aborting rather than reporting and playing on is deliberate: once a rule
        is broken every later decision is taken on a state already known to be
        wrong, so the reports that follow describe the wreckage rather than the
        crash. One clean repro is worth more than a list of consequences.

        A rule that *raises* is treated the same way, and for a sharper reason.
        `Log.OnCrash` re-raises `EngineIntegrityError` and swallows everything
        else, and `build.py` hardcodes `Build.release = True` -- so a plain
        `AttributeError` out of a rule would be absorbed, the game would carry
        on, and the run manifest would still record `check_invariants: true`.
        A run that cannot check must not be able to claim it did.
        """
        from game.world import invariants

        if not self.is_enabled or world is None:
            return

        try:
            violations = invariants.Check(world, self.progress)
        except invariants.InvariantViolation:
            raise
        except Exception as exc:
            Log.FailedTrace(CATEGORY_NAME, exc)
            raise invariants.InvariantViolation(
                f"the invariant checker itself failed at step "
                f"{self.manager.replay.current_step_id}: "
                f"{type(exc).__name__}: {exc}") from exc

        if not violations:
            return

        step_id = self.manager.replay.current_step_id
        # Dump before logging: the log line is worth more with a path in it, and
        # worth more still if the file is already on disk when the process dies.
        path = self.Dump(world, step_id, violations[0])

        Log.Assert(CATEGORY_NAME,
            f"Invariant violated at step {step_id}\n"
            + invariants.Report(violations)
            + (f"\nRepro: {path}" if path else "\nNo repro was written"))

        raise invariants.InvariantViolation(
            f"{violations[0].rule} at step {step_id}: "
            f"{violations[0].subject} {violations[0].detail}")

    ################################################################################
    #
    def Dump(self, world: 'World', step_id: int, first: 'Violation') -> str:
        """Write the scene as it stands, and return where it went.

        `history_inputs` holds steps `0..step_id-1` at this moment, because
        `ChoiceOne` pushes the current step only after the decision is answered.
        Replaying the file therefore re-executes exactly the inputs that led
        here and stops at the failing step.

        Saved `deterministic=True` for the same reason a bot save is (MARVEL-27):
        a repro that carries a host fingerprint and a timestamp is not one that
        can be committed, compared, or handed to someone else. Nothing here
        reads the clock.

        Everything runs inside one `try`, and the scene comes off the session
        rather than through `Game.scene`. The property asserts instead of
        returning `None`, and `TestRun.Run` does `del game.session.scene`
        between cases -- so both a missing scene and a deleted one raise, and an
        `AssertionError` escaping here would replace the violation with a stack
        trace about bookkeeping. The violation is the news.
        """
        session = self.manager.game.session

        try:
            scene = getattr(session, "scene", None)
            if scene is None or scene.is_puzzle:
                # `Scene.Save` refuses a puzzle board, and a puzzle is authored
                # rather than generated, so there is nothing to reproduce from.
                return ""

            stem = FileManager.SanitizeFilename(
                f"invariant-{scene.GetSaveFileName()}-step{step_id}-{first.rule}".lower())
            folder = INVARIANT_FOLDER.value
            FileManager.MakeDir(folder)

            path = session.SaveScene(FileManager.JoinPath(folder, f"{stem}.json"),
                                     delete_old=False, deterministic=True)
        except Exception as exc:
            Log.FailedTrace(CATEGORY_NAME, exc, no_take_as_error=True)
            return ""

        return path or ""
