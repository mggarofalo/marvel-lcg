from . import *

# Blinding Flash

def GetAbilities() -> Sequence['Ability']:

    def blinding_flash(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = effect.GetPaidResources().GetResourceIconTypes()
        targets = effect.targets[0:value]
        Faces.GiveStatus(targets, "Stunned", effect)
        Faces.GiveStatus(targets, "Confused", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            blinding_flash
        ).SetPlay().SetLabel()
        .SetTarget(Enemy, canbe_status=["Stunned", "Confused"], range=(1, 4)),
    ]

