from . import *

# Cutupper

def GetAbilities() -> Sequence['Ability']:

    def cutupper(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 5, effect)
        Faces.GiveStatus(effect.targets, "Stunned", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            cutupper
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

