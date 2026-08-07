from . import *

# Quick Strike

def GetAbilities() -> Sequence['Ability']:

    def quick_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        damage = initiator.GetHero().attack
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            quick_strike
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

