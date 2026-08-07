from . import *

# Piercing Strike

def GetAbilities() -> Sequence['Ability']:

    def piercing_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        hero.DealDamage(effect.targets, 3, effect, property=AttackProperty(piercing=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            piercing_strike
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

