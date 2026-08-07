from . import *

# Power Unleashed

def GetAbilities() -> Sequence['Ability']:

    def power_unleashed(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 5, effect)
        this.RemoveThreatFromSchemes(effect.targets2, 5, effect)

        Faces.RemoveAllFromGame([this], effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            power_unleashed
        ).SetPlay().SetLabel()
        .SetTarget(Villain)
        .SetTarget2(MainScheme)
        .SetHasNoTargetEffect()
    ]

