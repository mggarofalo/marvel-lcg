from . import *

# Infiltrate A.I.M. Island Embassy

def GetAbilities() -> Sequence['Ability']:

    def infiltrate_aim_island_embassy(effect: 'Effect', message: 'Message.AfterCardRemovedToken') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        this.Advance("2A", effect)


    return [
        AbilityFactory.WhenLastThreatRemoveFromThisScheme(
            AbilityType.ForcedInterrupt,
            infiltrate_aim_island_embassy
        ),
    ]

