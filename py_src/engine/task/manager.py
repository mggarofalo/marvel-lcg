from functools import partial
import sys
import asyncio
from core import *
from build import Build
from engine.task.condition import Condition
from engine.task.task import Task
from engine.config import ConfigVariables

if sys.platform == 'win32':
    asyncio.set_event_loop_policy(asyncio.WindowsSelectorEventLoopPolicy())

ENABLE_MULTIPLE_THREADS = ConfigVariables.Bool('enable_multiple_threads', False)

CATEGORY_NAME = "TASK"

class TaskManager:

    tasks: List[Task] = []
    id = 0
    condition = Condition("Task")

    @staticmethod
    def Initialize():
        pass

    @staticmethod
    def AddTask(process: Callable[..., Awaitable[Any]], *args: Any,
                name: str|None,
                run_forever: bool=False,
                **kwargs: Any) -> 'Task':
        TaskManager.id += 1
        bound_process = partial(process, *args, **kwargs)
        task = Task(bound_process, name, run_forever, TaskManager.condition)
        TaskManager.tasks.append(task)
        task.Start()
        return task

    @staticmethod
    def WaitTasksFinished(tasks: List[Task]):
        def check():
            return all(task.is_finished for task in tasks)

        TaskManager.condition.Wait(check)

    @staticmethod
    def Shutdown():
        for thread in TaskManager.tasks:
            thread.Stop()

    ################################################################################
    #
    T = TypeVar("T")

    @staticmethod
    def RunAwaitProcesses(process: Callable[[T], Awaitable[None]], objects: List[T], wait_finished: bool=True):
        tasks: List[Task] = []
        if not Build.release:
            name = GetCallStack(2)
        else:
            name = "Run Await"

        for object in objects:
            task = TaskManager.AddTask(process, object, name=name)
            tasks.append(task)

        if wait_finished:
            TaskManager.WaitTasksFinished(tasks)

    @staticmethod
    async def ToThread(process: Callable[..., T], *args: Any, **kwargs: Any) -> T:
        value = await asyncio.to_thread(process, *args, **kwargs)
        return value

