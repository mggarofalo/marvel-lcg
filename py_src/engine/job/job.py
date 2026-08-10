from typing import Final
from core import *
import asyncio
from concurrent.futures import ThreadPoolExecutor, Future
from engine.log import Log
from engine.task.condition import Condition
from engine.config import ConfigVariables

MAX_WORKERS = ConfigVariables.Int('max_workers', 5)

CATEGORY_NAME = "JOB"

class Job:
    def __init__(self, job_id: int,
                func: Callable[..., Any], *args: Tuple[Any],
                name: str|None,
                condition: 'Condition',
                on_complete: Callable[['Job'], None],
                **kwargs: Any,
                ) -> None:
        self.job_id: Final = job_id
        self.condition: Final = condition
        self.name: Final = name
        self.is_coroutine: Final = asyncio.iscoroutinefunction(func)
        self.on_complete: Final = on_complete

        self.is_done = False
        self.return_value: Any = None
        self.future: Future[Any]|None = None

        # An `EngineIntegrityError` this job died of, held until a waiter can be
        # given it. See `RaiseIfFailed`.
        self.integrity_error: 'EngineIntegrityError|None' = None

        def run_job() -> None:
            try:
                if self.is_coroutine:
                    """Run the asynchronous job function and mark it as done."""
                    async def run_job_async() -> None:
                        self.return_value = await func(*args, **kwargs)
                    asyncio.run(run_job_async())
                else:
                    """Run the job function and mark it as done."""
                    self.return_value = func(*args, **kwargs)
            except EngineIntegrityError as exc:
                # Not absorbed -- deferred. This is a worker thread, so raising
                # here would only set the future's exception, which nothing
                # reads. Hold it instead and let whoever waits for the job raise
                # it on their own thread. See MARVEL-54.
                self.integrity_error = exc
                Log.FailedTrace(CATEGORY_NAME, exc)
            except Exception as exc:
                # Everything else stays absorbed on purpose: a job that fails
                # must not take the process down. That is the right call for a
                # broken card and the wrong one for an error that says the
                # recorded output is already corrupt, which is the whole
                # distinction `EngineIntegrityError` exists to draw.
                Log.FailedTrace(CATEGORY_NAME, exc)
            finally:
                self.is_done = True
                self.on_complete(self)
                # Log.DebugSilent(CATEGORY_NAME, f"{self} done")
                self.condition.NotifyAll()
        self.run_job = run_job

    def Start(self, executor: ThreadPoolExecutor) -> None:
        """Start the job in a thread pool."""
        self.future = executor.submit(self.run_job)

    def CheckDone(self) -> bool:
        """Check if the job is done."""
        return self.is_done

    def RaiseIfFailed(self) -> None:
        """Re-raise an `EngineIntegrityError` the job died of, on this thread.

        Deliberately not cleared afterwards: every waiter for this job gets it.
        An integrity error means something has already been produced that must
        not be trusted, so "one caller has heard about it" is not a reason for
        the next one to carry on. See MARVEL-54.
        """
        if self.integrity_error != None:
            raise self.integrity_error

    def WaitFinished(self) -> None:
        """Wait until the job is finished without blocking the main thread."""
        self.condition.Wait(lambda: self.CheckDone())
        self.RaiseIfFailed()

    def __repr__(self) -> str:
        return f"({self.job_id}) {self.name}{' (Async)' if self.is_coroutine else ''}"

