"""An integrity error is never absorbed, including by a thread.

`core.errors.EngineIntegrityError` means the run has already produced something
that must not be trusted. The engine absorbs exceptions everywhere on purpose --
a card script that raises should not end the game -- so MARVEL-32 made
`Log.OnCrash` re-raise this one class regardless of `Build.release`. That covered
the six catch-all sites that funnel through `Log.OnCrash`.

`Job.run_job` and `Task.run` do not funnel through it. Both wrap the work in
`except Exception` and log a traceback, so an integrity error raised on a worker
thread was logged and dropped. It is not live today -- player-decision dispatch
goes through `JobManager.Simultaneous`, a plain synchronous loop -- but it would
become live the moment anything moves dispatch onto the async path, and it would
do so *invisibly*: the symptom is an exception that stops appearing, not one that
starts. That is MARVEL-54.

A worker thread cannot raise at its caller, so the fix is to hold the error and
re-raise it on whoever waits. These tests pin that, and `TestNoNewSiteAbsorbs`
pins the rule itself: any new broad `except` in `engine/` or `game/` that neither
re-raises, nor routes through `Log.OnCrash`, nor lets `EngineIntegrityError`
past first, has to be reviewed and listed below before the suite goes green
again. The convention used to live only in AGENTS.md, which is how this gap
survived MARVEL-32's review.
"""

import ast
import unittest
from pathlib import Path

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from core.errors import EngineIntegrityError
from engine.device.manager.base import FabricatedInputError
from engine.job import JobManager
from engine.log import Log
from engine.task import TaskManager


PY_SRC = Path(__file__).resolve().parent.parent

# Packages the rule is asserted over. `core/` is deliberately out: nothing there
# knows what an engine artefact is, and `EngineIntegrityError` is defined in it.
GUARDED_PACKAGES = ("engine", "game")


class Boom(EngineIntegrityError):
    """Stands in for a real one. Any subclass must travel the same way."""


class JobTestCase(unittest.TestCase):
    """Runs real threads through a private `JobManager`.

    The manager is class-level state shared with whatever else ran in this
    process, so it is swapped out and put back rather than re-initialised in
    place.
    """

    @classmethod
    def setUpClass(cls):
        cls.saved = (getattr(JobManager, "executor", None),
                     getattr(JobManager, "condition", None),
                     JobManager.jobs)
        JobManager.jobs = []
        JobManager.Initialize()

    @classmethod
    def tearDownClass(cls):
        JobManager.Shutdown()
        JobManager.executor, JobManager.condition, JobManager.jobs = cls.saved

    def setUp(self):
        self.addCleanup(setattr, Log, "log_statistics", Log.log_statistics)
        Log.log_statistics = {}

    @staticmethod
    def Raising(exc):
        def Work():
            raise exc
        return Work


class TestAThreadedJobHandsBackAnIntegrityError(JobTestCase):

    def test_it_reaches_the_thread_that_waited_for_all(self):
        job = JobManager.AddJob(self.Raising(Boom("the corpus is already wrong")),
                                name="raising")

        with self.assertRaises(Boom):
            JobManager.WaitForAllJobsToComplete(job)

    def test_it_reaches_the_thread_that_waited_for_any(self):
        job = JobManager.AddJob(self.Raising(Boom("the corpus is already wrong")),
                                name="raising")

        with self.assertRaises(Boom):
            JobManager.WaitForAnyJobToComplete(job)

    def test_it_reaches_the_thread_that_waited_on_the_job_itself(self):
        job = JobManager.AddJob(self.Raising(Boom("the corpus is already wrong")),
                                name="raising")

        with self.assertRaises(Boom):
            job.WaitFinished()

    def test_a_subclass_travels_the_same_way(self):
        # `FabricatedInputError` is the one MARVEL-32 exists for, and the reason
        # a base-class check is not enough on its own.
        job = JobManager.AddJob(
            self.Raising(FabricatedInputError("a decline nobody made")), name="raising")

        with self.assertRaises(FabricatedInputError):
            JobManager.WaitForAllJobsToComplete(job)

    def test_the_message_survives(self):
        job = JobManager.AddJob(self.Raising(Boom("step 12 digest")), name="raising")

        with self.assertRaises(Boom) as caught:
            JobManager.WaitForAllJobsToComplete(job)

        self.assertIn("step 12 digest", str(caught.exception))

    def test_the_job_still_finishes_and_is_removed(self):
        # The `finally` block has to run whichever branch caught: a job that
        # never marks itself done deadlocks every waiter.
        job = JobManager.AddJob(self.Raising(Boom("boom")), name="raising")
        with self.assertRaises(Boom):
            JobManager.WaitForAllJobsToComplete(job)

        self.assertTrue(job.is_done)
        self.assertNotIn(job, JobManager.jobs)

    def test_a_second_waiter_is_told_as_well(self):
        # Deliberately not cleared on the way out. "One caller has heard about
        # it" is not a reason for the next one to carry on.
        job = JobManager.AddJob(self.Raising(Boom("boom")), name="raising")
        with self.assertRaises(Boom):
            JobManager.WaitForAllJobsToComplete(job)

        with self.assertRaises(Boom):
            JobManager.WaitForAllJobsToComplete(job)

    def test_one_failure_is_raised_and_the_rest_are_logged(self):
        # `RunProcesses` gives every controller the same work, so all of them
        # failing the same way is one finding -- but the others must not vanish.
        jobs = [JobManager.AddJob(self.Raising(Boom(f"boom {i}")), name=f"raising {i}")
                for i in range(3)]

        with self.assertRaises(Boom):
            JobManager.WaitForAllJobsToComplete(*jobs)

        self.assertTrue(Log.HasError(error=True))


class TestAnOrdinaryJobFailureIsStillAbsorbed(JobTestCase):
    """The distinction is the whole point. A job that fails for an ordinary
    reason is still just a failed job, and must not take the process down."""

    def test_an_ordinary_exception_does_not_reach_the_waiter(self):
        job = JobManager.AddJob(self.Raising(ValueError("a card script blew up")),
                                name="raising")

        JobManager.WaitForAllJobsToComplete(job)

        self.assertIsNone(job.integrity_error)

    def test_it_is_still_logged_as_an_error(self):
        # Absorbed is not the same as unnoticed: `Log.HasError` is what the
        # corpus gate reads. See MARVEL-65.
        job = JobManager.AddJob(self.Raising(ValueError("a card script blew up")),
                                name="raising")

        JobManager.WaitForAllJobsToComplete(job)

        self.assertTrue(Log.HasError(error=True))

    def test_a_job_that_succeeds_returns_its_value(self):
        job = JobManager.AddJob(lambda: 42, name="fine")

        JobManager.WaitForAllJobsToComplete(job)

        self.assertEqual(job.return_value, 42)
        self.assertIsNone(job.integrity_error)


class TestAThreadedTaskHandsBackAnIntegrityError(unittest.TestCase):
    """`TaskManager` is the older mechanism `JobManager` is meant to replace,
    and it had the identical hole."""

    def setUp(self):
        self.addCleanup(setattr, TaskManager, "tasks", TaskManager.tasks)
        TaskManager.tasks = []
        self.addCleanup(setattr, Log, "log_statistics", Log.log_statistics)
        Log.log_statistics = {}

    @staticmethod
    def Raising(exc):
        async def Work():
            raise exc
        return Work

    def test_it_reaches_the_thread_that_waited(self):
        task = TaskManager.AddTask(self.Raising(Boom("the corpus is already wrong")),
                                   name="raising")

        with self.assertRaises(Boom):
            TaskManager.WaitTasksFinished([task])

    def test_it_reaches_the_thread_that_waited_on_the_task_itself(self):
        task = TaskManager.AddTask(self.Raising(Boom("boom")), name="raising")

        with self.assertRaises(Boom):
            task.WaitFinished()

    def test_the_task_still_finishes(self):
        task = TaskManager.AddTask(self.Raising(Boom("boom")), name="raising")
        with self.assertRaises(Boom):
            TaskManager.WaitTasksFinished([task])

        self.assertTrue(task.is_finished)

    def test_an_ordinary_exception_does_not_reach_the_waiter(self):
        task = TaskManager.AddTask(self.Raising(ValueError("something ordinary")),
                                   name="raising")

        TaskManager.WaitTasksFinished([task])

        self.assertIsNone(task.integrity_error)
        self.assertTrue(Log.HasError(error=True))

    def test_a_task_that_succeeds_returns_its_value(self):
        async def Work():
            return 42
        task = TaskManager.AddTask(Work, name="fine")

        TaskManager.WaitTasksFinished([task])

        self.assertEqual(task.return_value, 42)


################################################################################
# The rule itself
#
BROAD = {"Exception", "BaseException"}
INTEGRITY = "EngineIntegrityError"


def HandlerTypes(handler):
    """The bare names an `except` clause catches. `x.Y` counts as `x`."""
    node = handler.type
    if node is None:
        return set()
    parts = node.elts if isinstance(node, ast.Tuple) else [node]
    names = set()
    for part in parts:
        while isinstance(part, ast.Attribute):
            part = part.value
        if isinstance(part, ast.Name):
            names.add(part.id)
    return names


class AbsorberScan(ast.NodeVisitor):
    """Broad `except` clauses that swallow whatever they caught.

    A handler is fine -- and not reported -- when any of these hold:

    - the same `try` has an earlier `except EngineIntegrityError`, so the broad
      clause can never see one;
    - the handler re-raises;
    - the handler reports through `Log.OnCrash`, which re-raises integrity
      errors itself.
    """

    def __init__(self):
        self.scope = []
        self.found = []

    def PushScope(self, node):
        self.scope.append(node.name)
        self.generic_visit(node)
        self.scope.pop()

    visit_FunctionDef = PushScope
    visit_AsyncFunctionDef = PushScope
    visit_ClassDef = PushScope

    def visit_Try(self, node):
        guarded = any(INTEGRITY in HandlerTypes(handler) for handler in node.handlers)
        for handler in node.handlers:
            if guarded:
                continue
            if handler.type != None and not HandlerTypes(handler) & BROAD:
                continue
            if any(isinstance(inner, ast.Raise) for inner in ast.walk(handler)):
                continue
            if any(isinstance(inner, ast.Call)
                   and isinstance(inner.func, ast.Attribute)
                   and inner.func.attr == "OnCrash"
                   for inner in ast.walk(handler)):
                continue
            self.found.append(".".join(self.scope) or "<module>")
        self.generic_visit(node)


def FindAbsorbers():
    """{(module path, qualified name): how many broad handlers absorb there}."""
    counts = {}
    for package in GUARDED_PACKAGES:
        for path in sorted((PY_SRC / package).rglob("*.py")):
            scan = AbsorberScan()
            scan.visit(ast.parse(path.read_text(encoding="utf-8")))
            for qualname in scan.found:
                key = (path.relative_to(PY_SRC).as_posix(), qualname)
                counts[key] = counts.get(key, 0) + 1
    return counts


# Every place in `engine/` and `game/` that swallows a broad exception, with the
# reason it is allowed to. Reviewed for MARVEL-54; none of them can be reached
# by an `EngineIntegrityError`, or the swallow is itself the failure handling.
#
# Adding a site here is a decision, not a formality. If the code inside the
# `try` can raise an integrity error, add `except EngineIntegrityError: raise`
# ahead of the broad clause instead -- the scan stops reporting it either way,
# and only one of the two is correct.
REVIEWED_ABSORBERS = {
    # Best-effort remap of a recorded input onto this run's effect ids. It calls
    # `CommandDescriptor` helpers only; failing means the step replays on its
    # raw recorded input, which is the intended fallback.
    ("engine/controller/controller.py", "Controller.ChoiceOne"): 2,
    # Building the human-readable violation report. The violation itself has
    # already been raised; failing to describe it must not replace it.
    ("engine/controller/module/invariants.py", "InvariantModule.Dump"): 1,
    # `packaging.version.Version` rejecting a string that came out of a file.
    ("engine/controller/module/replay.py", "InputModule.MissingDigestReason"): 1,
    # A cost that cannot be re-derived from its rendered text. The option is
    # treated as unaffordable and the policy moves on.
    ("engine/device/manager/bot/command.py", "BotCommand.BuildPayment"): 1,
    # The scene is the best crash artefact, not the only one; the sidecar still
    # carries the seed, step, digest and traceback.
    ("engine/device/manager/bot/crash.py", "CrashCollector.SaveSceneFor"): 1,
    # Reporting steps that run after the games are already on disk. A report
    # that cannot be written is worth a warning, not the run.
    ("engine/device/manager/bot/runner.py", "BotRunner.Guarded"): 1,
    # The per-game boundary: it captures the failure and returns None, so the
    # game is discarded and never enters the corpus. Absorbing is the handling.
    ("engine/device/manager/bot/runner.py", "BotRunner.RunOne"): 1,
    # `int()` on a query string.
    ("engine/device/web/server/server_new_game.py",
     "GameServerNewGame.load_replay_data"): 1,
    # A websocket client that went away mid-send. Transport, not game state.
    ("engine/device/web/server/server_socket.py",
     "GameServerSocket.WebSendRender.process_client"): 1,
    ("engine/device/web/server/server_socket.py",
     "GameServerSocket.websocket_handler"): 1,
    # Filesystem probes: the answer to "could this be moved" is a bool.
    ("engine/file/manager.py", "FileManager.MoveFile"): 1,
    # An HTTP call to a version endpoint, on startup, over the network.
    ("engine/lib/check_new_version.py", "CheckForNewVersion.Check"): 1,
    # A missing or malformed translation file. The engine runs untranslated.
    ("engine/lib/translate.py", "TransText.__init__"): 1,
    # A console that cannot encode a card name. Printing must never be fatal --
    # and this one is the logger, so it cannot report through the logger.
    ("engine/log/log.py", "PrintUtf8"): 1,
    # A crash reporter must never become the crash. It uninstalls the observer
    # and says so.
    ("engine/log/log.py", "Log.NotifyCrashObserver"): 1,
    # Binding a socket to find out whether a port is free.
    ("engine/network/net_lib.py", "NetLib.IsPortAvailable"): 1,
    # Serving a file that is not there, and binding the listener. Transport.
    ("engine/network/web_server.py", "WebServer.ReadFile.read_file"): 1,
    ("engine/network/web_server.py", "WebServer.ReadFile"): 1,
    ("engine/network/web_server.py", "WebServer.Run.start_server"): 1,
    # `issubclass` against an ability's `when` that is not a class. Skipping the
    # effect is the fallback the comment on it describes.
    ("game/card/face/effect/face_effect.py", "FaceEffect.Find"): 1,
    # The cheat console executes arbitrary text the developer typed. Its errors
    # belong to the typist.
    ("game/cheat/cheat.py", "Cheat.TryPreDebugExec"): 1,
    ("game/cheat/cheat.py", "Cheat.PreDebugExec"): 1,
    # A statistics file that will not parse. It logs an error and plays on
    # without recording -- statistics are not game state.
    ("game/statistics/game_statistics.py", "GameStatistics.Load"): 1,
    # Deciding whether a file in a corpus folder is a scene at all. An
    # unreadable file is not a scene; that is the answer, not an error.
    ("game/test/verify.py", "ReplayVerifier.IsSceneDocument"): 1,
}


class TestNoNewSiteAbsorbsAnIntegrityError(unittest.TestCase):
    """The rule, made enforceable.

    "Integrity errors are never absorbed" lived only in AGENTS.md, and the two
    threaded sites this issue fixes were written before it and never revisited.
    A convention nothing checks is a convention that drifts.
    """

    def test_every_absorbing_site_has_been_reviewed(self):
        new = {key: count for key, count in FindAbsorbers().items()
               if count > REVIEWED_ABSORBERS.get(key, 0)}

        self.assertEqual(new, {}, self.Explain(new))

    def test_the_reviewed_list_has_no_stale_entries(self):
        # A site that was fixed or deleted should leave the list, or the next
        # reader trusts a note about code that is not there.
        found = FindAbsorbers()
        stale = {key: count for key, count in REVIEWED_ABSORBERS.items()
                 if count > found.get(key, 0)}

        self.assertEqual(stale, {}, f"listed but no longer absorbing: {sorted(stale)}")

    def test_the_scan_finds_something(self):
        # Everything above is vacuously true of a scan that walks no files.
        self.assertTrue(FindAbsorbers())

    def test_a_handler_that_lets_integrity_errors_past_is_not_reported(self):
        scan = self.Scan("""
            try:
                Work()
            except EngineIntegrityError:
                Hold()
            except Exception:
                Absorb()
        """)

        self.assertEqual(scan, [])

    def test_a_handler_that_re_raises_is_not_reported(self):
        scan = self.Scan("""
            try:
                Work()
            except Exception:
                raise
        """)

        self.assertEqual(scan, [])

    def test_a_handler_that_reports_through_on_crash_is_not_reported(self):
        scan = self.Scan("""
            try:
                Work()
            except Exception as exc:
                Log.OnCrash("GAME", exc, "", None)
        """)

        self.assertEqual(scan, [])

    def test_a_plain_swallow_is_reported(self):
        # The shape `Job.run_job` had.
        scan = self.Scan("""
            try:
                Work()
            except Exception as exc:
                Log.FailedTrace("JOB", exc)
        """)

        self.assertEqual(scan, ["<module>"])

    def test_a_bare_except_is_reported(self):
        scan = self.Scan("""
            try:
                Work()
            except:
                pass
        """)

        self.assertEqual(scan, ["<module>"])

    def test_a_narrow_handler_is_not_reported(self):
        # The rule is about handlers broad enough to catch an integrity error
        # by accident, not about every `except`.
        scan = self.Scan("""
            try:
                Work()
            except ValueError:
                pass
        """)

        self.assertEqual(scan, [])

    def test_the_reported_name_is_the_innermost_scope(self):
        scan = self.Scan("""
            class Outer:
                def Inner(self):
                    try:
                        Work()
                    except Exception:
                        pass
        """)

        self.assertEqual(scan, ["Outer.Inner"])

    @staticmethod
    def Scan(source):
        import textwrap
        visitor = AbsorberScan()
        visitor.visit(ast.parse(textwrap.dedent(source)))
        return visitor.found

    @staticmethod
    def Explain(new):
        lines = ["broad `except` clauses that swallow, and are not in REVIEWED_ABSORBERS:"]
        for path, qualname in sorted(new):
            lines.append(f"  {path}  {qualname}")
        lines.append("If an EngineIntegrityError can reach it, add "
                     "`except EngineIntegrityError: raise` ahead of the broad clause.")
        lines.append("If it cannot, add it to REVIEWED_ABSORBERS with the reason why.")
        return "\n".join(lines)


if __name__ == "__main__":
    unittest.main()
