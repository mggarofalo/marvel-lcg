from . import *

# * Arc Reactor

def GetAbilities() -> Sequence['Ability']:

    def arc_reactor(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            arc_reactor,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Iron Man", canbe_ready=True)
    ]
