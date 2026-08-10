from core import *
from concurrent.futures import ThreadPoolExecutor
from engine.log import Log
from engine.task.condition import Condition
from engine.job.job import Job
from engine.config import ConfigVariables

MAX_WORKERS = ConfigVariables.Int('max_workers', 5)

CATEGORY_NAME = "JOB"

"""
Differences between `JobManager` and `TaskManager`

If `JobManager` can't handle a case, then use `TaskManager`
Our goal is to remove `TaskManager`
"""

class JobManager:

    jobs: List[Job] = []
    next_job_id: int = 1
    executor: 'ThreadPoolExecutor'
    condition: 'Condition'

    @staticmethod
    def Initialize():
        JobManager.executor = ThreadPoolExecutor(max_workers=MAX_WORKERS.value)
        JobManager.condition = Condition("Job")

    @staticmethod
    def AddJob(func: Callable[..., Any], *args: Any,
                name: str|None,
                **kwargs: Any) -> Job:
        new_job = Job(JobManager.next_job_id, func, *args, condition=JobManager.condition, name=name, on_complete=JobManager.RemoveJob, **kwargs)
        JobManager.jobs.append(new_job)
        new_job.Start(JobManager.executor)  # Start the job in the thread pool
        Log.DebugSilent(CATEGORY_NAME, f"Added job: {new_job}")
        JobManager.next_job_id += 1
        return new_job

    @staticmethod
    def RemoveJob(job: 'Job') -> None:
        if job in JobManager.jobs:
            JobManager.jobs.remove(job)
            Log.DebugSilent(CATEGORY_NAME, f"Removed job: {job}")
        else:
            Log.Warn(CATEGORY_NAME, f"{job} not found")
        Log.DebugSilent(CATEGORY_NAME, f"{JobManager.jobs=}")

    @staticmethod
    def WaitForAllJobsToComplete(*jobs: Job) -> None:
        def check():
            return all(job.is_done for job in jobs)
        JobManager.condition.Wait(check)
        JobManager.RaiseIfAnyFailed(*jobs)

    @staticmethod
    def WaitForAnyJobToComplete(*jobs: Job) -> None:
        def check():
            for job in jobs:
                if job.is_done:
                    return True
            return False
        JobManager.condition.Wait(check)
        JobManager.RaiseIfAnyFailed(*jobs)

    @staticmethod
    def RaiseIfAnyFailed(*jobs: Job) -> None:
        """Hand the waiting thread the first integrity error among `jobs`.

        A worker thread cannot raise at its caller, so `Job.run_job` holds the
        error and this is where it lands. Ordinary exceptions are not re-raised:
        a job that failed for an ordinary reason is still just a failed job.
        See MARVEL-54.

        Later ones are logged rather than lost -- with `RunProcesses` every
        controller runs the same code, so all of them failing the same way is
        one finding, and only the first is worth a traceback.
        """
        failed = [job for job in jobs if job.integrity_error != None]
        for job in failed[1:]:
            Log.Assert(CATEGORY_NAME,
                f"{job} also failed: {type(job.integrity_error).__name__}: {job.integrity_error}")
        if failed:
            failed[0].RaiseIfFailed()

    @staticmethod
    def Shutdown() -> None:
        JobManager.executor.shutdown(wait=False, cancel_futures=True)

    ################################################################################
    #
    T = TypeVar("T")

    @staticmethod
    def Simultaneous(process: Callable[[T], None], objects: List[T]):
        for object in objects:
            process(object)

    @staticmethod
    def RunProcesses(process: Callable[[T], None], objects: List[T], *, name: str) -> List[Job]:
        jobs: List[Job] = []
        for index, object in enumerate(objects):
            job = JobManager.AddJob(process, object, name=f"{name} {index}")
            jobs.append(job)
        return jobs


"""
import time
from engine.job import JobManager

JobManager.Initialize()

def my_job(a: int, b: int) -> int:
    print(f"Job started with parameters: {a}, {b}")
    time.sleep(a)  # Simulate a long-running job
    print("Job finished.")
    return a + b

# Adding jobs with positional and keyword arguments
job1 = JobManager.AddJob(my_job, 1, b=2)
job2 = JobManager.AddJob(my_job, 2, 2)
job3 = JobManager.AddJob(my_job, 3, b=2)

JobManager.WaitForAllJobsToComplete(job1, job2, job3)

# Adding jobs with positional and keyword arguments
job1 = JobManager.AddJob(my_job, 1, b=2)
job2 = JobManager.AddJob(my_job, 2, 2)
job3 = JobManager.AddJob(my_job, 3, b=2)

JobManager.WaitForAnyJobToComplete(job1, job2, job3)

print(f"Return value: {job1.return_value}")  # Access the return value
"""

