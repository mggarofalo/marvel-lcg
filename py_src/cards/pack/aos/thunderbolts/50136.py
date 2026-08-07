from . import *

# Rumbling Thunder

def GetAbilities() -> Sequence['Ability']:

    def rumbling_thunder_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        EachPlayerEngagesEachMinionEngagedWithThePlayerClockwiseFromThem(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            rumbling_thunder_revealed
        ),
    ]

