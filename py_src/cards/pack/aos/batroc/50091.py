from . import *

# Rescued Captive

def GetAbilities() -> Sequence['Ability']:

    def rescued_captive(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, "1*", effect)


    return [
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.ThisCannotRemoveFromPlay(),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            rescued_captive
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(MainScheme),
    ]

