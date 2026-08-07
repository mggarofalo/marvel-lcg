from core import *
from game.card.face import *
from game.deck import *
from game.ability import *
from game.message import *
from game.player import *
from cards.paper import Paper
from game.element.damage_property import DamageProperty

@final
class Event(HasTeamUp, HasAlliance, ClassCard, FinalType):
    @override
    def __init__(self, paper: 'Paper') -> None:
        super().__init__(paper)

    @override
    def OnAfterPlay(self, player: 'Player', by_effect: 'Effect', play_message: 'Message.WhenPlayerPlayCard', message: 'Message2', from_area: 'Deck'):
        from game.operate.faces import Faces
        if self.IsInProcessingArea():
            Faces.DiscardAll([self], by_effect)
        super().OnAfterPlay(player, by_effect, play_message, message, from_area)

    @override
    def OnDealDamage(self, units: List['Unit2'], damage: 'int|DamageProperty', by_effect: 'Effect', *, property: 'AttackProperty|None'=None, attack_in_event: bool) -> int|None:
        assert self == by_effect.this, f"{self=} {by_effect.this=}"
        # Fix "48018"
        identity = by_effect.GetInitiator().GetIdentity()
        if identity.IsDefeated():
            return None
        return identity.OnDealDamage(units, damage, by_effect, property=property, attack_in_event=attack_in_event)

    @override
    def OnRemoveSchemeThreat(self, schemes: List['Scheme2'], value: int, by_effect: 'Effect') -> int|None:
        assert by_effect != None, f"{self=}"
        assert self == by_effect.this, f"{self=} {by_effect.this=}"
        identity = by_effect.GetInitiator().GetIdentity()
        return identity.OnRemoveSchemeThreat(schemes, value, by_effect)

