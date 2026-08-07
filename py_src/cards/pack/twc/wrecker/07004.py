from . import *

# Day of Reckoning

def GetAbilities() -> Sequence['Ability']:

    def day_of_reckoning(effect: 'Effect', message: 'Message.AfterSchemePlaceThreat') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)

    return [
        AbilityFactory.SchemeCannotLeavlPlayWhile(
            "This",
            while_card_is_in_play=CardFinder(name=WRECKER),
        ),
        IfThereIsTenThreatThenRemoveAllButThree(day_of_reckoning)
        .SetTarget(Friend, range="All")
    ]

