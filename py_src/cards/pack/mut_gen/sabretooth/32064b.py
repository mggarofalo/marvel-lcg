from . import *

# The Injured Senator 2B

def GetAbilities() -> Sequence['Ability']:

    def the_injured_senator_2b(effect: 'Effect', message: 'Message.WhenMainSchemeStageCompleted') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        ally = Worlds.FindCardOnField(
            effect,
            ROBERT_KELLY_FINDER
        )
        if ally:
            Faces.DefeatUnits([ally], this, effect)


    return [
        AbilityFactory.WhenMainSchemeStageCompleted(
            AbilityType.WhenCompleted,
            "This",
            the_injured_senator_2b,
        ),
        IfRobertKellyLeavesPlayThePlayersLoseTheGame()
    ]

