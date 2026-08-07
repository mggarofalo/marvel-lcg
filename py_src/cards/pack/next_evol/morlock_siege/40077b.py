from . import *

# Knock, Knock

def GetAbilities() -> Sequence['Ability']:

    def knock_knock(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'knock', effect)

        if this.GetCounters('knock') >= 3:
            this.Advance("2A", effect)


    return [
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            knock_knock
        ),
        IfThereAre3VillainsUnderRoutedThePlayersWinTheGame()
    ]

