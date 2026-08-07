from . import *

# Heroic Strike

def GetAbilities() -> Sequence['Ability']:

    def heroic_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 6, effect)
        if effect.GetPaidResources().HasColor("R"):
            Faces.GiveStatus(effect.targets, "Stunned", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            heroic_strike
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

