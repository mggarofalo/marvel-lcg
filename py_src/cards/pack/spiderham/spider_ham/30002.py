from . import *

# * Captain Americat: Steve Mouser

def GetAbilities() -> Sequence['Ability']:

    def captain_americat(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        GiveSomeoneAHighFive()
        initiator = effect.GetInitiator()
        Faces.PlaceCountersOn(effect.targets, 1, 'toon', effect)
        Faces.ShuffleAllTo(effect.targets2, initiator.player_deck, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            captain_americat
        )
        .SetTarget("YourIdentity")
        .SetTarget2(card_class="IdentitySpecific", from_where=["YourDiscardPile"])
    ]

