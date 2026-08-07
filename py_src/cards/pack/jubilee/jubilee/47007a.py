from . import *

# Firecracker

def GetAbilities() -> Sequence['Ability']:

    def firecracker(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)
        if effect.GetPaidResources().GetResourceIconTypes() >= 2:
            Faces.GiveStatus(effect.targets, "Stunned", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            firecracker
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

