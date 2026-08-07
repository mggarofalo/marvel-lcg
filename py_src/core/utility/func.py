from core import *
import inspect
import os

ROOT_DIR = os.path.abspath(os.curdir)

def GetFuncLines(fn: Callable[..., Any], replace_path: bool=False) -> str:
    file_path = fn.__code__.co_filename
    if replace_path:
        from engine.file import FileManager
        file_path = FileManager.ReplacePath(file_path, ROOT_DIR)
    return file_path + ":" + str(fn.__code__.co_firstlineno)

def GetFuncName(fn: Callable[..., Any], replace_path: bool=False) -> str:
    from build import Build
    if Build.release:
        return fn.__name__
    else:
        return fn.__name__ + " " + GetFuncLines(fn, replace_path)

def GetCallStack(deep: int) -> str:
    from build import Build

    frame = inspect.currentframe()
    for _ in range(deep):
        if frame:
            frame = frame.f_back
        else:
            break
    if frame:
        if not Build.release:
            file_path = os.path.relpath(frame.f_code.co_filename, ROOT_DIR)
        else:
            file_path = frame.f_code.co_filename
        text = f"{file_path}:{frame.f_lineno}({frame.f_code.co_name})"
    else:
        text = "(None)"

    # caller = getframeinfo(stack()[deep][0])
    # if not Build.release:
    #     file_path = os.path.relpath(caller.filename, ROOT_DIR)
    # else:
    #     file_path = caller.filename
    # text = f"{file_path}:{caller.lineno}({caller.function})"
    return text

"""
import inspect
import time

def test_snippet_a(deep):
    frame = inspect.currentframe()
    for _ in range(deep):
        if frame:
            frame = frame.f_back
        else:
            break

def test_snippet_b(deep):
    frame_info = inspect.stack()[deep]
    return inspect.getframeinfo(frame_info[0])

def measure_time(snippet_func, deep, iterations=10000):
    start_time = time.time()
    for _ in range(iterations):
        snippet_func(deep)
    end_time = time.time()
    return end_time - start_time

if __name__ == "__main__":
    deep = 5  # Adjust the depth as needed
    iterations = 10000  # Number of iterations for each test

    # Measure time for Snippet A
    time_a = measure_time(test_snippet_a, deep, iterations)
    print(f"Time for Snippet A (depth {deep}, iterations {iterations}): {time_a:.6f} seconds")

    # Measure time for Snippet B
    time_b = measure_time(test_snippet_b, deep, iterations)
    print(f"Time for Snippet B (depth {deep}, iterations {iterations}): {time_b:.6f} seconds")
"""

# inspect.iscoroutinefunction(func)
T = TypeVar("T")
def IsAsyncFunction(func: Callable[..., T]|Callable[..., Awaitable[T]]) -> TypeGuard[Callable[..., Awaitable[T]]]:
    return hasattr(func, '__code__') and func.__code__.co_flags & 0x80 != 0

def IsNonAsyncFunction(func: Callable[..., T]|Callable[..., Awaitable[T]]) -> TypeGuard[Callable[..., T]]:
    return not IsAsyncFunction(func)

