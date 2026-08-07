from . import *

# Crushing Blow

def GetAbilities() -> Sequence['Ability']:

    def crushing_blow(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()

        this.DealDamage(effect.targets, hero.attack, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            crushing_blow
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

