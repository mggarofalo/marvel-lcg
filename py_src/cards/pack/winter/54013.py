from . import *

# * Deathlok: Henry Hayes

def GetAbilities() -> Sequence['Ability']:

    def deathlok(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name="S.H.I.E.L.D. Sidearm"
        )
        if face and CanAttach.IsType(face):
            face.AttachTo2(this, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            deathlok
        ),
    ]

