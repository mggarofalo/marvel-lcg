from . import *

# Hawkeye

def GetAbilities() -> Sequence['Ability']:

    def quick_draw(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            quick_draw,
        ).SetName("Quick Draw")
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Upgrade, name=HAWKEYE_BOW_NAME, canbe_ready=True)
    ]
