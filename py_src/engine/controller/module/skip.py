from core import *
from engine.log import Log

CATEGORY_NAME = "SKIP"

class SkipModule:
    def __init__(self) -> None:
        self.is_skipping: bool = False
        self.skip_to: int = 0
        self.skip_to_next: Literal["Round", "Turn", "Phase", "End", False] = False

    def Clean(self):
        self.is_skipping = False
        self.skip_to = 0
        self.skip_to_next = False

    def SetIsSkipping(self, skip: bool) -> bool:
        if skip == self.is_skipping:
            return False
        self.is_skipping = skip
        if not skip:
            self.skip_to = 0
        Log.DebugSilent(CATEGORY_NAME, f"{self.is_skipping=}")
        return True

    def SetSkipTo(self, skip_to: int|Literal["Round", "Turn", "Phase", "End"]):
        if isinstance(skip_to, int):
            self.skip_to = skip_to
        else:
            self.skip_to = 99999
            self.skip_to_next = skip_to
        return True

