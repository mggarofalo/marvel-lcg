from . import *

# Stroke of Genius

def GetAbilities() -> Sequence['Ability']:

    def stroke_of_genius(effect: 'Effect', message: 'Message.AfterCardsBeSpendAsResource') -> 'None':
        this = effect.this.CastTo(Resource)
        Unused(this)

        initiator = effect.GetInitiator()

        Faces.PlaceCountersOn(effect.targets, 1, 'progress', effect)
        initiator.DrawUp(1, effect)

    return [
        AbilityFactory.AfterYouSpendThisCard(
            AbilityType.Response,
            stroke_of_genius
        ).SetTarget("YourIdentity"),
    ]

