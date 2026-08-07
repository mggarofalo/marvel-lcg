from . import *

# Backflip

def GetAbilities() -> Sequence['Ability']:

    def backflip(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)
        message.PreventDamage("All", effect)

    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            "You",
            backflip,
            is_from_attack=True
        ).SetPlay().SetLabel('defense')
    ]

