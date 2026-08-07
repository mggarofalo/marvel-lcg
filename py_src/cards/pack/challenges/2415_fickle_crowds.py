from . import *

# Fickle Crowds

def GetAbilities() -> Sequence['Ability']:

    def fickle_crowds(effect: 'Effect', message: 'Message.WhenPhaseEnd'):
        this = effect.this.CastTo(Challenge)
        Unused(this)

        from cards.pack.mojo.magog import FindTheChallengers, FindTheChampion

        challengers = FindTheChallengers(effect)
        champion = FindTheChampion(effect)
        if challengers and champion:
            if challengers.GetCounters('ratings') > 0:
                Faces.RemoveCountersOn([challengers], 1, 'ratings', effect)
                Faces.PlaceCountersOn([champion], 1, 'ratings', effect)


    return [
        AbilityFactory.WhenVillainPhaseEnd(
            AbilityType.Challenge,
            fickle_crowds
        )
    ]

