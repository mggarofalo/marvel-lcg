from . import *

# Reinforcements

def GetAbilities() -> Sequence['Ability']:

    def reinforcements(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 1, 'all-purpose', effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            reinforcements
        ).SetPlay().SetLabel()
        .SetTarget(Support, trait="S.H.I.E.L.D", combined_printed_cost=(0, 6)),
    ]

