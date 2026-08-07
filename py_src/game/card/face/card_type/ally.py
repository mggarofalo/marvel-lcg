from core import *
from game.card.face import *
from game.deck import *
from game.ability import *
from game.message import *
from game.ability.factory import *
from game.player import *
from cards.paper import Paper

@final
class Ally(HasThwart, CanThwart, HasAttack, CanAttack, Friend, CanDefense, HasAccelerationIcon, HasAmplify, HasHazard, HasSetup, HasVictory, ClassCard, CanPlaceCounter, CanAttach, FinalType):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.attack_consequential_damage = 0
        self.thwart_consequential_damage = 0

        super().__init__(paper)

        self.RegisterInfoDict('attack_consequential_damage')
        self.RegisterInfoDict('thwart_consequential_damage')

    @override
    def GetAbilities(self) -> List['Ability']:
        from game.effect.rule import Consequential
        from game.message import Message

        abilities: List['Ability'] = []
        if not self.ability.Find(func_name="Play"):
            abilities.append(AbilityFactory.CanPlayThisAllyCard())
        if not self.ability.Find(func_name="ATK"):
            abilities.append(AbilityFactory.ThisCanAttack())
        if not self.ability.Find(func_name="THW"):
            abilities.append(AbilityFactory.ThisCanThwart())

        def take_consequential_damage(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower'):
            this = effect.this.CastTo(Ally)

            damage = 0
            end_message = message.end_message
            if isinstance(end_message, Message.AfterUnitThwartEnd):
                if this.thwart_consequential_damage > 0 and \
                not any(x.does_not_take_consequential_damage for x in end_message.would_thw_messages):
                    damage = self.thwart_consequential_damage
            if isinstance(end_message, Message.AfterUnitAttackEnd):
                if self.attack_consequential_damage > 0 and \
                not any(x.does_not_take_consequential_damage for x in end_message.would_atk_messages):
                    damage = self.attack_consequential_damage

            if damage:
                would_take_consequential_message = Message.WhenAllyWouldTakeConsequentialDamage(self, damage, message)
                would_take_consequential_message.Send()
                if would_take_consequential_message.is_be_instead:
                    return
                damage = would_take_consequential_message.will_take_damage
                damage_message = self.TakeDamage(self, damage, Consequential(self))
                if damage_message != None:
                    take_damage_message = Message.AfterAllyTakeConsequentialDamage(self, message, would_take_consequential_message)
                    take_damage_message.Send()

        abilities.append(
            AbilityFactory.AfterUnitUseBasicPower(
                AbilityType.Consequential,
                "This",
                take_consequential_damage,
                powers=["ATK", "THW"],
            )
        )

        abilities.append(
            AbilityFactory.ThisCanDefense()
        )
        return abilities + super().GetAbilities()

    def ResetConsequential(self):
        self.attack_consequential_damage = self.printed_attack_consequential_damage
        self.thwart_consequential_damage = self.printed_thwart_consequential_damage

    @override
    def OnReset(self, message: 'Message.WhenCardReset_Text') -> None:
        self.ResetConsequential()
        return super().OnReset(message)

    @override
    def OnFlip(self, by_effect: 'Effect', origin_face: 'CardFace|None'):
        self.ResetConsequential()
        return super().OnFlip(by_effect, origin_face)

    @override
    def OnPutIntoPlay(self, message: 'Message.WhenCardPutIntoPlay') -> 'bool':
        from game.operate.faces import Faces
        player = message.GetToPlayer()
        if Faces.MoveAllTo([self], player.allies, message.by_effect):
            # Fix "38011"
            if not player.limit_ally.CheckLimit([]):
                return False
            return super().OnPutIntoPlay(message)
        else:
            return False

    ################################################################################
    #
    @override
    def OnBeTreatAsIfBlank(self, message: 'Message.WhenCardTreatAsIfBlank') -> None:
        if Player.IsType(self.GetControlBy()):
            player = self.GetControlByPlayer()
            player.limit_ally.CheckLimit([])
        return super().OnBeTreatAsIfBlank(message)

    @override
    def OnWouldEnterPlay(self, into_area: 'Deck') -> bool:
        if super().OnWouldEnterPlay(into_area):
            if into_area.GetOwner().IsPlayer():
                player = into_area.GetOwnerPlayer()
                if not player.limit_ally.CheckLimit([self]):
                    return False
            return True
        return False

    @override
    def OnAfterCardLeavePlay(self, message: 'Message.AfterCardLeavePlay') -> None:
        player = message.from_area.GetOwner()
        if Player.IsType(player):
            player.limit_ally.CheckLimit([])
        return super().OnAfterCardLeavePlay(message)

    ################################################################################
    #
    def GainAttackConsequentialDamage(self, damage: int, by_effect: 'Effect'):
        self.attack_consequential_damage += damage

    def GainThwartConsequentialDamage(self, damage: int, by_effect: 'Effect'):
        self.thwart_consequential_damage += damage

    # Under no player's control
    @override
    def AttachTo2(self, face: 'CardFace', by_effect: 'Effect') -> bool:
        # from game.operate.faces import Faces
        self.PutIntoPlay("FirstPlayer", by_effect)
        self.card.SetOwner(None)
        super().AttachTo2(face, by_effect)
        # Faces.MoveAllTo([self], face.GetInventoryDeck(), by_effect)
        return True

