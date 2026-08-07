from . import *

# Tactical Genius

def GetAbilities() -> Sequence['Ability']:

    def tactical_genius(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        value = 1
        if effect.this == message.sequence[-1]:
            value = 2

        this.RemoveThreatFromSchemes(effect.targets, value, effect)

    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            tactical_genius
        ).SetLabel('thwart')
        .SetTarget(Scheme2)
    ]

