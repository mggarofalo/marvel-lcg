from . import *

# * Black Widow: Natasha Romanoff

def GetAbilities() -> Sequence['Ability']:

    def black_widow(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReturnToHand(effect.targets, initiator, effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            black_widow,
        ).SetTarget(trait="ATTACK", card_type=Event, from_where=["YourDiscardPile"]),
    ]

