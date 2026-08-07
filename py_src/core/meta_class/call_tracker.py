from core import *
from build import Build

class CallTracker:

    method_call_count: Dict[str, Dict[str, int]] = {}

    FuncT = TypeVar("FuncT", bound=Callable[..., Any])

    class Meta(type):
        pass

    @staticmethod
    def count_calls(method: 'CallTracker.FuncT') -> 'CallTracker.FuncT':
        return method

    @staticmethod
    def count_calls_debug(method: 'CallTracker.FuncT') -> 'CallTracker.FuncT':
        def wrapper(self: Any, *args: Any, **kwargs: Any) -> Any:
            caller = GetCallStack(3)
            if method.__name__ not in CallTracker.method_call_count:
                CallTracker.method_call_count[method.__name__] = {}
            if caller not in CallTracker.method_call_count[method.__name__]:
                CallTracker.method_call_count[method.__name__][caller] = 1
            else:
                CallTracker.method_call_count[method.__name__][caller] += 1
            return method(self, *args, **kwargs)
        return wrapper

    if not Build.release:
        count_calls = count_calls_debug

    @classmethod
    def PrintStats(cls) -> str:
        import json
        method_call_count = json.dumps(CallTracker.method_call_count, indent=4)
        text = f"""method_call_count: {method_call_count}
"""
        return text

    @classmethod
    def ResetStats(cls) -> None:
        CallTracker.method_call_count = {}

