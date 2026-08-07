from . import *

# Organic Webbing

def GetAbilities() -> Sequence['Ability']:

    def organic_webbing(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        effect.targets[0].GainUntilRoundEnd(
            effect,
            trait="AERIAL"
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            organic_webbing
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Discard("TuckUnderYourIdentityCard"))
        .SetTarget(name="Silk"),
    ]

