from core import *
from game.card.face import *
from game.effect import *
from game.element.resources import Resources
from game.element.cost import Cost

class TargetCost:

    @dataclass
    class Payment:
        cost: 'Cost'
        # str, "RYB", ~~UI only~~, show can generate res of each effect
        # these effects can do pay, and the str store the resources they can generate
        payments: List[Dict['Effect', str]]
        cost_check: Dict['Effect', 'Effect']

    def __init__(self) -> None:
        self.target_cost: Dict['CardFace|None', 'TargetCost.Payment'] = {}
        self.only_none_target = False

    def SetNoneTargetOnly(self):
        self.only_none_target = True

    def IsEmpty(self) -> bool:
        return self.target_cost == {}

    def HasTarget(self, face: 'CardFace|None') -> bool:
        return face in self.target_cost

    def GetCost(self, face: 'CardFace|None') -> 'Cost':
        if self.only_none_target:
            return self.target_cost[None].cost
        return self.target_cost[face].cost

    def GetPayment(self, face: 'CardFace|None') -> 'TargetCost.Payment':
        if self.only_none_target:
            return self.target_cost[None]
        return self.target_cost[face]

    def UpdateCost(self, face: 'CardFace|None', diff: 'int'):
        self.target_cost[face].cost += diff

    # def GetEffectsTotalResText(self, face: 'CardFace|None', paid_effects: List['Effect']) -> 'Resources':
    #     res = Resources("0")
    #     for pay_info in self.target_cost[face].payments:
    #         for effect in paid_effects:
    #             if effect in pay_info:
    #                 res += Resources(pay_info[effect])
    #                 break
    #     return res

    def AddTarget(self, face: 'CardFace|None', cost: 'Cost') -> None:
        self.target_cost[face] = TargetCost.Payment(cost, [], {})

    def AddPayment(self, face: 'CardFace|None', cost_effect: 'Effect', cost: 'Resources', check_effect: 'Effect') -> None:
        # pay_info = {effect: res.text_legacy}
        # # assert self.for_targets != []
        # self.for_effect.for_select_target_dict[].pay_info.append(pay_info)
        self.target_cost[face].payments.append({cost_effect: cost.text_legacy})
        self.target_cost[face].cost_check[cost_effect] = check_effect

    def GetAllPayEffects(self) -> List['Effect']:
        effects: List['Effect'] = []
        for face in self.target_cost:
            for effect_str in self.target_cost[face].payments:
                for effect in effect_str:
                    if effect not in effects:
                        effects.append(effect)
        return effects

    def FindPayEffect(self, target: 'CardFace|None', effect_id: int) -> 'Effect|None':
        for effect_res in self.GetPayment(target).payments:
            for effect in effect_res:
                if effect.object_id == effect_id:
                    return effect
        return None

