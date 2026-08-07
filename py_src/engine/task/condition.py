import threading
from core import *

class Condition:

    def __init__(self, name: str) -> None:
        self.condition = threading.Condition()
        self.name = name
        self.func: Any|None = None

    def Wait(self, check: Callable[[], bool], timeout: float|None=None) -> bool:
        # Wait for the check to be True
        no_time_out = True
        with self.condition:
            self.func = check
            no_time_out = self.condition.wait_for(check, timeout=timeout)
        return no_time_out

    def NotifyAll(self) -> None:
        with self.condition:
            self.condition.notify_all()

