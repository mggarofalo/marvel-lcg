from . import *

# * Monica Chang

def GetAbilities() -> Sequence['Ability']:

    def monica_chang(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_discard_pile=True,
            include_player_deck=True,
            include_player_hand_cards=True,
            card_type=Support,
            name="Surveillance Team",
        )
        if face:
            face.PutIntoPlay(initiator, effect)
        faces = initiator.GetControlCards(CardFinder(name="Surveillance Team"))
        Faces.PlaceCountersOn(faces, 1, 'snoop', effect)

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            monica_chang
        ),
    ]

