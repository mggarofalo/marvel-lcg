from core import *
from game.render.symbol import Symbol
from game.element.rbyg import ResRBYGA

class CostRule:

    def __init__(self,
                *,
                up_to: bool|None=None,
                same_type: bool|None=None,
                different_type: bool|None=None,
                from_hand: bool|None=None,
                requirement: str|None=None,
                or_res: 'Cost|None'=None) -> None:
        self.up_to = up_to
        self.same_type = same_type
        self.different_type = different_type
        self.from_hand = from_hand
        self.requirement = requirement
        self.or_res = or_res

        self.rule_texts = [rule for rule, condition in {
            "UpTo": self.up_to,
            "FromHand": self.from_hand,
            "SameType": self.same_type,
            "DifferentType": self.different_type
        }.items() if condition]

class Cost:

    @staticmethod
    def FromText(text: str, up_to: bool|None=None,) -> 'Cost':
        return Cost(Cast(Any, text), up_to=up_to)

    def __init__(self, rbyga: Literal[
                    "R", "B", "Y", "G",
                    "RR", "BB", "YY",
                    "BR", "YR", "YB",
                    "YYY", "BBB", "RRR",
                    "YBR", "RRR", "BRR",
                    "0", "1", "2", "3", "4"]|ResRBYGA,
                 *,
                 rule: CostRule|None=None,
                 up_to          : bool|None=None,   # Can spend 0 ~ `rbyga`
                 same_type      : bool|None=None,
                 different_type : bool|None=None,
                 from_hand      : bool|None=None,
                 or_cost        : 'Cost|None'=None,
                 ) -> None:
        
        if isinstance(rbyga, ResRBYGA):
            self.rbyga = rbyga
        else:
            self.rbyga = ResRBYGA.FromText(rbyga)

        if rule:
            self.rule = rule
        else:
            self.rule = CostRule(
                up_to           = up_to,
                same_type       = same_type,
                different_type  = different_type,
                from_hand       = from_hand,
                or_res          = or_cost,
            )

        # Text
        self.text_legacy = Symbol.Clean(self.color_text)

    @property
    def color_text(self) -> str:
        return self.rbyga.color_text

    @property
    def val(self) -> int:
        return self.rbyga.total

    @property
    def true_val(self) -> int:
        return self.rbyga.true_val

    ################################################################################
    #
    def __mul__(self, val: int) -> 'Cost':
        return Cost(
            self.rbyga * val,
            rule=self.rule
        )

    def __add__(self, other: 'Cost|int') -> 'Cost':
        if isinstance(other, int):
            return Cost(
                self.rbyga + other,
                rule=self.rule
            )
        else:
            return Cost(
                self.rbyga + other.rbyga,
                rule=self.rule
            )

    def __sub__(self, val: int) -> 'Cost':
        return self + (-1 * val)

    ################################################################################
    #
    def GetRuleText(self) -> List[str]:
        return self.rule.rule_texts

    def GetTypeKinds(self) -> int:
        return self.rbyga.rbyg.types

    ################################################################################
    #
    @property
    def r(self) -> int:
        return self.rbyga.r
    @property
    def b(self) -> int:
        return self.rbyga.b
    @property
    def y(self) -> int:
        return self.rbyga.y
    @property
    def g(self) -> int:
        return self.rbyga.g

    ################################################################################
    #
    def GetSpendText(self) -> str:
        texts: List[str] = []
        has_color = False
        if self.rule.up_to:
            texts.append("up to")


        if self.y + self.b + self.r + self.g:
            has_color = True
            texts.append(f"{'[[energy]]' * self.y}{'[[mental]]' * self.b}{'[[physical]]' * self.r}{'[[wild]]' * self.g}")
        elif self.val:
            if self.val > 0:
                texts.append(f"{self.val} resources")
            else:
                texts.append(f"{self.val} resource")

        if not has_color:
            if self.rule.same_type:
                texts.append("of the same type")
            if self.rule.different_type:
                texts.append("of different types")
            else:
                texts.append("of any type")

        if self.rule.from_hand:
            texts.append("from hand")

        return " ".join(texts)

