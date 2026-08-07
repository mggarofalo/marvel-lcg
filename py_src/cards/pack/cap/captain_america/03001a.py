from . import *

# * Captain America

def GetAbilities() -> Sequence['Ability']:

    def i_can_do_this_all_day(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            i_can_do_this_all_day,
        ).SetName("I Can Do This All Day!")
        .SetCostFunc(CostFunc.Discard("YourHandCards"))
        .SetTarget("This", canbe_ready=True)
        .LimitOncePerRound()
    ]

