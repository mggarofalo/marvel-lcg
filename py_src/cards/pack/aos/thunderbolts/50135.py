from . import *

# The Coming Storm

def GetAbilities() -> Sequence['Ability']:

    def the_coming_storm_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        EachPlayerEngagesEachMinionEngagedWithThePlayerClockwiseFromThem(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_coming_storm_revealed
        ),
    ]

