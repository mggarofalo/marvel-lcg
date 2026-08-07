from . import *

# Thunderstruck

def GetAbilities() -> Sequence['Ability']:

    def thunderstruck(effect: 'Effect', message: 'Message.AfterSchemePlaceThreat') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)

    return [
        AbilityFactory.SchemeCannotLeavlPlayWhile(
            "This",
            while_card_is_in_play=CardFinder(name=THUNDERBALL),
        ),
        IfThereIsTenThreatThenRemoveAllButThree(thunderstruck)
        .SetTarget(Friend, range="All")
    ]

