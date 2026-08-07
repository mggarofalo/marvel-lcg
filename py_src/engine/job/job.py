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
            except Exception as exc:
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

    def WaitFinished(self) -> None:
        """Wait until the job is finished without blocking the main thread."""
        self.condition.Wait(lambda: self.CheckDone())

    def __repr__(self) -> str:
        return f"({self.job_id}) {self.name}{' (Async)' if self.is_coroutine else ''}"

