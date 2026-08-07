from . import *

# Take That!

def GetAbilities() -> Sequence['Ability']:

    def take_that(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 7, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            take_that
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy, with_attach=Upgrade),
    ]

