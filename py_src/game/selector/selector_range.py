from typing import Final
from core import *
from game.card.face import *
from game.effect import *
from game.selector.selector_rule import *

class SelectorRange:

    # Zero means: `lambda effect: len(effect.all_legal_targets)`
    # if existed legal targets, must select them, if not, can be zero
    RANGE_MIN = int|Callable[['Effect'], int]|Literal["Zero"]
    RANGE_MAX = int|Callable[['Effect'], int]|Literal["All"]

    # "All" means all legal targets
    RANGE_INT_TYPE = Tuple[int, int]
    RANGE_TYPE = RANGE_INT_TYPE|Tuple[RANGE_MIN, RANGE_MAX]|Literal["All", "Default"]|Callable[['Effect'], int]

    def __init__(self, range: 'RANGE_TYPE', selector_rule: 'SelectorRule') -> None:
        self.min_fn: SelectorRange.RANGE_MIN
        self.max_fn: SelectorRange.RANGE_MAX

        self.has_combined_rules: Final = selector_rule.has_combined_rules
        self.has_repeat_rules: Final = selector_rule.repeat_rules != []
        self.select_rule: Final = selector_rule.select_rule

        self.SetRange(range)

    def SetRange(self, range: 'RANGE_TYPE'):
        self.raw_range = range

        if self.has_combined_rules:
            assert range == "Default"
            max_fn = "All"
            min_fn = 1
            select_all = False
        elif range == "Default":
            min_fn = 1
            max_fn = 1
            select_all = False
        elif range == "All":
            min_fn = 0
            max_fn = 0
            select_all = True
        elif isinstance(range, Tuple):
            min_fn, max_fn = range
            select_all = False
        else:
            min_fn = range
            max_fn = min_fn
            select_all = False

        if self.select_rule != "":
            assert select_all or \
                isinstance(max_fn, int) and max_fn > 1 or \
                max_fn == "All"

        self.max_fn = max_fn
        self.min_fn = min_fn
        self.select_all = select_all
        self.can_be_zero = self.min_fn == "Zero"

    def GetTargetMax(self, effect: 'Effect', targets: Sequence['CardFace']) -> int:
        if self.select_all or self.max_fn == "All":
            return len(targets) if not self.has_repeat_rules else Math.INT_INF
        elif isinstance(self.max_fn, int):
            return self.max_fn
        else:
            return self.max_fn(effect)

    def GetTargetMin(self, effect: 'Effect', targets: Sequence['CardFace']) -> int:
        if self.select_all:
            return max(1, len(targets))
        if self.min_fn == "Zero":
            return len(targets)
        elif isinstance(self.min_fn, int):
            return self.min_fn
        else:
            return self.min_fn(effect)

