from . import *

# Electrical Discharge

def GetAbilities() -> Sequence['Ability']:

    def electrical_discharge(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)

        if effect.GetPaidResources().HasColor("Y"):
            Faces.GiveStatus(effect.targets, "Stunned", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            electrical_discharge
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

