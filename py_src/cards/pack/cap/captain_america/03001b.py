from . import *

# * Steve Rogers

def GetAbilities() -> Sequence['Ability']:

    class Buff_03001b(Buff):
        @override
        def __init__(self) -> None:
            super().__init__()
            self.played_ally = False

        @override
        def OnRecordPlayedFace(self, face: 'CardFace'):
            super().OnRecordPlayedFace(face)
            if Ally.IsType(face):
                self.played_ally = True
                self.SetUIText(face.paper.card_id)

        @override
        def OnRoundEnd(self):
            super().OnRoundEnd()
            self.played_ally = False
            self.SetUIText("")

        def __bool__(self) -> bool:
            return not self.played_ally

    def steve_rogers_setup(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        initiator = effect.GetInitiator()

        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name=CAPTAIN_AMERICAS_SHIELD,
            card_type=Upgrade,
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            steve_rogers_setup
        ),
        AbilityFactory.ReduceCostToPlayFaceWhen(
            Ally,
            1,
            "AnyPlayer",
            conditions=[
                lambda effect, message:
                    bool(effect.this.GetBuff(Buff_03001b))
            ],
        ).SetName("Living Legend"),
    ]
