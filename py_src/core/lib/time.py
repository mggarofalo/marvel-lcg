import time

class Time:

    start_time = time.time()

    @staticmethod
    def Format(format: str = "%Y-%m-%d %H-%M") -> str:
        import datetime
        return f'{datetime.datetime.now().strftime(format)}'

    @staticmethod
    def GetTime() -> float:
        return time.time()

    @staticmethod
    def GetElapsedTime() -> float:
        return time.time() - Time.start_time

