from . import *

# Venom Blast

def GetAbilities() -> Sequence['Ability']:

    def venom_blast(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 5, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            venom_blast
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

