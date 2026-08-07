from . import *

# * Sunfire

def GetAbilities() -> Sequence['Ability']:

    def sunfire(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.DiscardAll(effect.targets, effect)

    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            sunfire,
        ).SetCost(Cost("Y"))
        .SetTarget(Attachment, with_texts=["Hero Action", "Hero Response"])
    ]

