from core.meta_class.instance_tracker import InstanceTracker
from core.meta_class.call_tracker import CallTracker

class Tracker:

    # class Meta(type): ...

    Meta = InstanceTracker.Meta

    @staticmethod
    def count_calls(method: 'CallTracker.FuncT') -> 'CallTracker.FuncT': ...

    count_calls = CallTracker.count_calls

    @classmethod
    def PrintStats(cls) -> str:
        return InstanceTracker.PrintStats() + CallTracker.PrintStats()

    @classmethod
    def ResetStats(cls) -> None:
        InstanceTracker.ResetStats()
        CallTracker.ResetStats()

