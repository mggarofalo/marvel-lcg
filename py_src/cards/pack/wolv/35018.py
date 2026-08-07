from . import *

# Precision Strike

def GetAbilities() -> Sequence['Ability']:

    def precision_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        this.DealDamage(effect.targets, 2, effect)
        if effect.targets[0].IsDefeated():
            this.HealthUnits([initiator.GetHero()], 2, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            precision_strike
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

