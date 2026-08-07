from . import *

# Ricochet Beam

def GetAbilities() -> Sequence['Ability']:

    def ricochet_beam(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)
        this.DealDamage(effect.targets2, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            ricochet_beam
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
        .SetTarget2(Enemy, with_attach=Upgrade),
    ]

