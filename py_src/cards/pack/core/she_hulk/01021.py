from . import *

# Gamma Slam

def GetAbilities() -> Sequence['Ability']:

    def gamma_slam(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = initiator.GetIdentity().sustained
        value = min(15, value)
        this.DealDamage(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            gamma_slam
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

