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

    ################################################################################
    # Affordability
    #
    # `payments` is the list of resource generators `CheckPlayerCanPayCost`
    # found for this cost -- one entry per way of producing resources, already
    # filtered by `AbilityFactoryResources.CheckThisCanPayCost` for `FromHand`,
    # for form, and for "could this colour contribute at all". What it does not
    # answer is whether any *combination* of them adds up, and that is the
    # question "is this option affordable" needs.

    # A search over subsets is exponential in principle. It is bounded here
    # because the alternative -- a greedy walk, which is what
    # `BotCommand.BuildPaymentInternal` does -- gets `SameType` costs wrong:
    # RRRB greedily accumulated never matches `Cost("RRR", same_type=True)`
    # even though RRR does. Reaching the limit returns "affordable", so the
    # option is offered and the player decides; the filter never withholds an
    # option on a search it did not finish.
    SEARCH_LIMIT = 512

    @staticmethod
    def _Key(res: 'Resources') -> Tuple[int, int, int, int, int]:
        # `Resources.text_legacy` collapses to "-N" whenever `reduce` is set, so
        # it cannot be used as an identity here.
        return (res.rbyg.r, res.rbyg.b, res.rbyg.y, res.rbyg.g, res.reduce)

    def CanPay(self, face: 'CardFace|None') -> bool:
        """Is there a set of the available generators whose total matches the cost?"""
        payment = self.GetPayment(face)
        cost = payment.cost

        zero = Resources("0")
        if zero.IsMatchCost(cost):
            # `UpTo` costs are met by spending nothing, which is always possible.
            return True

        totals: List['Resources'] = [zero]
        seen = {TargetCost._Key(zero)}
        for pay_info in payment.payments:
            for res_text in pay_info.values():
                res = Resources.FromText(res_text or "0")
                for base in totals[:]:
                    combined = base + res
                    key = TargetCost._Key(combined)
                    if key in seen:
                        continue
                    seen.add(key)
                    if combined.IsMatchCost(cost):
                        return True
                    totals.append(combined)
                if len(totals) > TargetCost.SEARCH_LIMIT:
                    return True
        return False

    def CanPayAnyTarget(self) -> bool:
        if self.IsEmpty():
            # Nothing was calculated -- `ignore_resource_cost`, or an ability
            # with no cost at all. Not this filter's business.
            return True
        for face in self.target_cost:
            if self.CanPay(face):
                return True
        return False

    ################################################################################
    # "Do as much as you can"
    def GetAvailableResources(self, face: 'CardFace|None') -> 'Resources':
        total = Resources("0")
        for pay_info in self.GetPayment(face).payments:
            for res_text in pay_info.values():
                total = total + Resources.FromText(res_text or "0")
        return total

    @staticmethod
    def MaxPayableCost(cost: 'Cost', available: 'Resources') -> 'Cost':
        """The largest part of `cost` that `available` can actually cover.

        Spending is the effect, not a cost, for an option that prints only
        "spend X" -- so when X cannot be spent in full, as much of it as
        possible is. Wild pays for any colour, and whatever is left over after
        the coloured demands are met pays the colourless part.
        """
        from game.element.rbyg import ResRBYG, ResRBYGA

        if available.reduce:
            cost = cost - available.reduce

        wild = available.g
        paid = {'r': 0, 'b': 0, 'y': 0}
        spare = 0
        for colour, have, need in (('r', available.r, cost.r),
                                   ('b', available.b, cost.b),
                                   ('y', available.y, cost.y)):
            take = min(need, have)
            use_wild = min(wild, need - take)
            wild -= use_wild
            paid[colour] = take + use_wild
            spare += have - take

        colourless = min(cost.rbyga.a, spare + wild)
        if cost.rule.different_type:
            # "Spend 2 different resources" with only physicals in hand is one
            # resource, not two: the demand is on how many kinds are reachable.
            colourless = min(colourless, available.rbyg.IsDifferentType())
        if cost.rule.same_type:
            colourless = min(colourless, max(available.r, available.b, available.y) + available.g)

        return Cost(ResRBYGA(max(0, colourless), ResRBYG(paid['r'], paid['b'], paid['y'], 0)),
                    rule=cost.rule)

    def ReduceToMaxPayable(self) -> None:
        for face in self.target_cost:
            payment = self.target_cost[face]
            reduced = TargetCost.MaxPayableCost(payment.cost, self.GetAvailableResources(face))
            payment.cost = reduced
            if not self.CanPay(face):
                # The arithmetic above is an upper bound on what is spendable,
                # not a plan. If no actual combination reaches even that, what
                # the player can do is nothing, and nothing is what they do.
                payment.cost = Cost("0")

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

