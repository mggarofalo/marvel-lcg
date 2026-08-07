
class Timer:

    def __init__(self) -> None:
        self.max_timeout: float = 0
        self.start_time: float|None = None

    def UpdateMaxTimeout(self, max_timeout: float):
        self.max_timeout = max_timeout

    def GetRemainingTime(self) -> float|None:
        from core.lib import Time
        if self.start_time is None:
            return None
        elapsed_time = Time.GetTime() - self.start_time
        remaining_time = self.max_timeout - elapsed_time
        return max(0, remaining_time)

