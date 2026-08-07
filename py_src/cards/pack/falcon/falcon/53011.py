from . import *

# Draw Their Fire

def GetAbilities() -> Sequence['Ability']:

    def draw_their_fire(effect: 'Effect', message: 'Message.AfterPhaseBegin') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        defend_effects: List['Effect'] = []
        for face in effect.targets:
            defend_effects.extend(face.effect.Find(func_name="DEF"))

        for defend_effect in defend_effects:
            cost_func_list = defend_effect.cost_func.GetAll()
            for cost_func in cost_func_list:
                if isinstance(cost_func, CostFunc.Exhaust):
                    cost_func.not_need_to_exhaust.append(this)

        def action():
            for defend_effect in defend_effects:
                cost_func_list = defend_effect.cost_func.GetAll()
                for cost_func in cost_func_list:
                    if isinstance(cost_func, CostFunc.Exhaust):
                        cost_func.not_need_to_exhaust.remove(this)

        RunAt.TheEndOfThePhase(effect, action)


    return [
        AbilityFactory.AfterPhaseBegin(
            AbilityType.HeroResponse,
            "Villain",
            draw_their_fire
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget(name="Falcon"),
    ]

