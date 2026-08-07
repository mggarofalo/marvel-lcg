from . import *

# * X-23's Claws

def GetAbilities() -> Sequence['Ability']:

    def x_23s_claws(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        for face in effect.targets:
            face.GainUntilRoundEnd(
                effect,
                attack=2
            )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            x_23s_claws
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.TakeDamage(2, "YourIdentity"))
        .SetTarget(name="X-23"),
    ]

