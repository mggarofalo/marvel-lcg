from . import *

# Super-Soldiers

def GetAbilities() -> Sequence['Ability']:

    def super_soldiers(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets2, 6, effect)
        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            super_soldiers
        ).SetPlay().SetLabel('attack')
        .SetTarget("TeamUp")
        .SetTarget2(Enemy),
    ]

