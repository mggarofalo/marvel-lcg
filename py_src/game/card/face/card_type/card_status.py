from core import *
from game.card.face import *
from game.ability import *
from game.ability.factory import *
from game.message import *
from cards.paper import Paper

@final
class StatusCard(FinalType):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.name: CardFace.STATUS
        super().__init__(paper)
        self.symbol_name: str = {
            "Tough": Symbol.tough,
            "Stunned": Symbol.stunned,
            "Confused": Symbol.confused
        }[self.name]

    @override
    def GetAbilities(self) -> List['Ability']:

        def remove_this(effect: 'Effect', message: 'Message.WhenCardWouldDiscard') -> None:
            this = effect.this.CastTo(StatusCard)
            Unused(this)

            message.SetToArea(self.card.world.area_status_cards)

            # this.effect.RegisterTempEffect(
            #     AbilityFactory.AfterCardDiscarded(
            #         AbilityType.Temp0,
            #         "This",
            #         lambda effect, message:
            #             World().area_status_cards.Remove(this.card),
            #         condition=lambda effect, message:
            #             this.card.area == World().area_status_cards
            #     )
            # )

        def send_lost_status(effect: 'Effect', message: 'Message.AfterCardDiscard') -> None:
            from game.message import Message
            from game.card.face.base import Unit2
            this = effect.this.CastTo(StatusCard)
            if message.from_area.bind_card:
                unit = message.from_area.bind_card.face.CastTo(Unit2)
                discard_message = Message.AfterStatusDiscardFrom(unit, this, message.by_effect)
                discard_message.Send()

        return [
            AbilityFactory.WhenCardWouldDiscard(
                AbilityType.Rule,
                "This",
                remove_this
            ).NoOutOfPlayLimit(),
            AbilityFactory.AfterCardDiscard(
                AbilityType.Rule,
                "This",
                send_lost_status
            ).NoOutOfPlayLimit(),
        ] + super().GetAbilities()

    ################################################################################
    #
    @property
    @override
    def bind_face(self) -> 'Unit2|None':
        unit_card = self.card.area.bind_card
        if unit_card and Unit2.IsType(unit_card.face):
            return unit_card.face
        return None

    @override
    def GetBindFace(self) -> 'Unit2':
        face = super().GetBindFace()
        return face.CastTo(Unit2)

    def IsStatusName(self, name: "CardFace.STATUS") -> bool:
        return super().IsName(name, False, False)

