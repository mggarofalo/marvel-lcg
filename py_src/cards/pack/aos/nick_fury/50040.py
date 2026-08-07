from . import *

# * Fury's Flying Car

def GetAbilities() -> Sequence['Ability']:

    def furys_flying_car(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        for face in effect.targets:
            face.GainUntilRoundEnd(
                effect,
                trait="AERIAL"
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            furys_flying_car
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.RemoveTokens("YourSuitFormUpgrade", 1, 'threat'))
        .SetTarget(name="Nick Fury", canbe_ready=True),
    ]

