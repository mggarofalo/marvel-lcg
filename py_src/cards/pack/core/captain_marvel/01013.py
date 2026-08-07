from . import *

# Photonic Blast

def GetAbilities() -> Sequence['Ability']:

    def photonic_blast(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        this.DealDamage(effect.targets, 5, effect)

        if effect.GetPaidResources().HasColor("Y"):
            initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            photonic_blast
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

