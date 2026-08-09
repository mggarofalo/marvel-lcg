import threading
import asyncio
from core import *
from engine.task.condition import Condition
from engine.log import Log

CATEGORY_NAME = "TASK"

class Task:

    def __init__(self, process: Callable[[], Awaitable[Any]],
                name: str|None,
                run_for_ever: bool,
                condition: 'Condition') -> None:
        self.loop: asyncio.AbstractEventLoop = asyncio.new_event_loop()
        self.is_finished = False
        self.run_forever = run_for_ever
        self.condition = condition
        self.return_value: Any = None

        # An `EngineIntegrityError` this task died of, held until a waiter can
        # be given it. Same reasoning as `Job`; see `RaiseIfFailed` and
        # MARVEL-54.
        self.integrity_error: 'EngineIntegrityError|None' = None

        def run() -> None:
            try:
                asyncio.set_event_loop(self.loop)
                self.return_value = self.loop.run_until_complete(process())
                if self.run_forever:
                    self.loop.run_forever()
            except EngineIntegrityError as exc:
                # Not absorbed -- deferred to whoever waits for this task. A
                # thread cannot raise at the code that started it.
                self.integrity_error = exc
                Log.FailedTrace(CATEGORY_NAME, exc)
            except Exception as exc:
                # Still absorbed on purpose: a task that fails must not take the
                # process down with it.
                Log.FailedTrace(CATEGORY_NAME, exc)
            finally:
                self.is_finished = True
                self.loop.close()
                self.condition.NotifyAll()

        self.thread = threading.Thread(target=run, name=name)

    def Start(self) -> None:
        self.thread.start()

    def Stop(self) -> None:
        if self.loop.is_running():
            self.loop.call_soon_threadsafe(self.loop.stop)
        self.thread.join()

    def RaiseIfFailed(self) -> None:
        """Re-raise an `EngineIntegrityError` the task died of, on this thread.

        Not cleared afterwards: every waiter gets it. See `Job.RaiseIfFailed`.
        """
        if self.integrity_error != None:
            raise self.integrity_error

    def WaitFinished(self) -> None:
        def check():
            return self.is_finished
        self.condition.Wait(check)
        self.RaiseIfFailed()

