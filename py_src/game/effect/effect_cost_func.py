from typing import Final
from core import *
from game.ability import *
from game.effect import *

class EffectCostFunction:
    TF = TypeVar("TF", bound='CostFunc.Base')

    def __init__(self, effect: 'Effect') -> None:
        self.effect: Final = effect
        self.additional_cost_func_list: List[CostFunc.Base] = []
        self.Reset()

    def Get(self, cost_fn_type: Type[TF], *, index: int=0) -> TF:
        fn = self.Has(cost_fn_type, index=index)
        if fn == None:
            assert False
        return fn

    def Has(self, cost_fn_type: Type[TF], *, index: int=0) -> TF|None:
        check_index = 0
        for cost_fn in self.GetAll():
            if isinstance(cost_fn, cost_fn_type):
                if check_index == index:
                    return cost_fn
                check_index += 1
        return None

    def Reset(self):
        self.cost_func_list: List[CostFunc.Base] = []

        if not self.effect.ability.flags.is_delay_ability:
            self.cost_func_list = self.effect.ability.cost_funcs[:]

    ################################################################################
    #
    def Add(self, cost_func: 'CostFunc.Base'):
        self.additional_cost_func_list.append(cost_func)

    def Del(self, cost_func: 'CostFunc.Base'):
        self.additional_cost_func_list.remove(cost_func)

    def GetAll(self) -> List['CostFunc.Base']:
        return self.cost_func_list + self.additional_cost_func_list

