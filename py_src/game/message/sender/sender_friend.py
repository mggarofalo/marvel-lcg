from . import *
from typing import Final

class SenderFriend:
    
    ################################################################################
    # Ally
    ################################################################################
    class WhenAllyWouldTakeConsequentialDamage(TriggerUnitMessage, CanBeInstead, DamageMessage, HasEndEventMessage):
        def __init__(self, unit: 'Ally', damage: int, message: 'Message.AfterUnitUseBasicPower'):
            # AfterUnitAttacked|Send.AfterUnitThwarted'):
            from game.message import Message
            end_message = message.end_message
            is_thwart = type(end_message) is Message.AfterUnitThwartEnd

            self.atk_message: Final = None if is_thwart else end_message.CastTo(Message.AfterUnitAttackEnd)
            self.thw_message: Final = end_message.CastTo(Message.AfterUnitThwartEnd) if is_thwart else None
            super().__init__(trigger=unit, damage=damage, end_event=Message.AfterAllyTakeConsequentialDamage)
    
        def ReduceDamage(self, damage: int, by_effect: 'Effect'):
            from game.message import Message
            self.UpdateDamageInternal(-1 * damage)
            Message.WhenDamageUpdated_Text(-1 * damage, by_effect)

        def IncreaseDamage(self, damage: int, by_effect: 'Effect'):
            from game.message import Message
            self.UpdateDamageInternal(damage)
            Message.WhenDamageUpdated_Text(damage, by_effect)

    class AfterAllyTakeConsequentialDamage(TriggerUnitMessage, HasPreEventMessage):
        def __init__(self, unit: 'Ally', message: 'Message.AfterUnitUseBasicPower', would_message: 'Message.WhenAllyWouldTakeConsequentialDamage'):
            # AfterUnitAttacked|Send.AfterUnitThwarted'):
            from game.message import Message
            end_message = message.end_message
            is_thwart = type(end_message) is Message.AfterUnitThwartEnd

            self.atk_message: Final = None if is_thwart else end_message.CastTo(Message.AfterUnitAttackEnd)
            self.thw_message: Final = end_message.CastTo(Message.AfterUnitThwartEnd) if is_thwart else None
            super().__init__(trigger=unit, pre_message=would_message)

    ################################################################################
    # Thwart
    class WhenUnitWouldThwart(TriggerUnitMessage, ThwarterMessage, CanGainValueMessage, CanBeInstead):
        def __init__(self, unit: 'Unit2', schemes: List['Scheme2'], by_effect: 'Effect', *, property: 'ThwartProperty') -> None:
            self.schemes: Final = schemes
            self.property: Final = property
            self.does_not_take_consequential_damage = False
            self.by_effect: Final = by_effect

            super().__init__(trigger=unit, thwarter=unit, thwarted=schemes)
            if schemes == []:
                text = TransText("{unit} will thwart", unit=unit)
            else:
                text = TransText("{unit} will thwart {schemes} ({additional_value:+})", unit=unit, schemes=schemes, additional_value=property.additional_value)
                self.Present(text, "target", unit, *schemes)

        @property
        def get_schemes(self) -> List['Scheme2']:
            return self.schemes

        def DoesNotTakeConsequentialDamage(self, by_effect: 'Effect'):
            self.does_not_take_consequential_damage = True

        def RemoveAdditionalThreat(self, value: int, by_effect: 'Effect'):
            self.property.additional_value += value
            text = TransText("This thwart will remove {value} additional threat ({by_effect})", value=value, by_effect=by_effect.this)
            self.Present_Activate(text, by_effect)

        def GainTHWForThisThwart(self, value: int, by_effect: 'Effect'):
            assert self.IsBasicThwart()
            from game.card.face.base import Unit2
            this = self.trigger.CastTo(Unit2)
            this.GainForThisActive(by_effect, self, thwart=value)

        def UseATKInsteadTHW(self, by_effect: 'Effect'):
            from game.card.face.attribute.can_attack import HasAttack
            from game.card.face.attribute.can_thwart import HasThwart

            unit = self.trigger
            if HasAttack.IsType(unit):
                atk = unit.attack
            else:
                atk = 0
            if HasThwart.IsType(unit):
                thw = unit.thwart
            else:
                thw = 0
            diff = atk - thw
            self.GainTHWForThisThwart(diff, by_effect)

        def AddMatchingPowerToThisPerformance(self, face: 'CardFace', by_effect: 'Effect'):
            from game.card.face.attribute.can_attack import HasAttack
            from game.card.face.attribute.can_thwart import HasThwart
            assert self.IsBasicThwart()
            is_assault = False
            for scheme in self.schemes:
                if scheme.assault:
                    is_assault = True
            if is_assault:
                # Fix "43007" vs assault scheme
                if HasAttack.IsType(face):
                    self.trigger.GainForThisActive(
                        by_effect,
                        self,
                        attack=face.attack
                    )
            else:
                if HasThwart.IsType(face):
                    self.GainTHWForThisThwart(face.thwart, by_effect)

        def AfterThreatRemoved(self, action: Callable[[int], Any]):
            from game.ability.factory import AbilityFactory
            self.by_effect.this.effect.RegisterTemp(
                AbilityFactory.AfterSchemeRemoveThreat(
                    AbilityType.Temp0,
                    None,
                    lambda effect, message:
                        action(message.value),
                    by_thwart=self
                ),
                unregister_after_exec=True,
                until_event_end=self
            )

        def IsBasicThwart(self):
            return self.property.is_basic_power

    class WhenSchemeBeingThwart(TriggerUnitMessage, HasEndEventMessage, CanGainValueMessage):
        def __init__(self, scheme: 'Scheme2', would_thw_message: 'Message.WhenUnitWouldThwart') -> None:
            from game.message import Message
            # self.trigger: Final = unit # attacker
            self.scheme: Final = scheme
            self.property = would_thw_message.property
            self.would_thw_message: Final = would_thw_message
            self.would_message: Final = would_thw_message
            self.who_make_thwart: Final = would_thw_message.trigger
            # Render.Print(f'{unit} thwarted {scheme}, value {value}', unit, scheme)
            super().__init__(trigger=would_thw_message.trigger, end_event=Message.AfterUnitThwartScheme)
            self.AddRelatedFace(scheme)

        def IsBasicThwart(self):
            return self.would_thw_message.IsBasicThwart()

        def GainThwartForThisThwart(self, value: int, by_effect: 'Effect'):
            if self.IsBasicThwart():
                from game.card.face.base import Unit2
                this = self.trigger.CastTo(Unit2)
                this.GainForThisActive(by_effect, self, thwart=value)
            else:
                self.property.additional_value += value
                text = TransText("This thwart gains {value} additional value from {by_effect}", value=value, by_effect=by_effect.this)
                self.Present_Activate(text, by_effect)

    class AfterUnitThwartScheme(TriggerUnitMessage, HasPreEventMessage):
        def __init__(self, unit: 'Unit2', scheme: 'Scheme2', remove_threat: int, would_thw_message: 'Message.WhenSchemeBeingThwart') -> None:
            # self.trigger: Final = unit # attacker
            self.scheme: Final = scheme
            # self.thwart: Final = thwart # value
            self.remove_threat: Final = remove_threat
            # Render.Print(f'{unit} thwarted {scheme}, value {value}', unit, scheme)
            super().__init__(trigger=unit, pre_message=would_thw_message)
            self.AddRelatedFace(unit, scheme)

        def IsBasicThwart(self):
            return self.would_thw_message.IsBasicThwart()

        @property
        def would_thw_scheme_message(self) -> 'Message.WhenSchemeBeingThwart':
            return self.pre_message

        @property
        def would_thw_message(self) -> 'Message.WhenUnitWouldThwart':
            return self.would_thw_scheme_message.would_thw_message

    class AfterUnitThwartEnd(TriggerUnitMessage, ThwarterMessage):
        def __init__(self, unit: 'Unit2', schemes: List['Scheme2'], total_remove_threat: int, by_effect: 'Effect', after_thw_messages: List['Message.AfterUnitThwartScheme']) -> None:
            # self.trigger: Final = unit # attacker
            self.schemes: Final = schemes
            self.by_effect: Final = by_effect
            self.total_remove_threat: Final = total_remove_threat # value
            self.after_thw_messages: Final = after_thw_messages
            self.would_thw_messages: Final = [x.would_thw_message for x in after_thw_messages]
            # Render.Print(f'{unit} thwarted {scheme}, value {value}', unit, scheme)
            remove_all = False
            for message in self.after_thw_messages:
                if message.scheme.threat == 0:
                    remove_all = True
                    break
            self.remove_the_last_threat: Final = remove_all
            super().__init__(trigger=unit, thwarter=unit, thwarted=schemes)

        def IsBasicThwart(self):
            return len(self.would_thw_messages) == 1 and self.would_thw_messages[0].IsBasicThwart()

    ################################################################################
    # Recovery
    class WhenUnitWouldRecovery(TriggerUnitMessage, HasEndEventMessage, CanGainValueMessage, CanBeInstead):
        def __init__(self, unit: 'Unit2'):
            from game.message import Message
            super().__init__(trigger=unit, end_event=Message.AfterUnitRecovery)

        def GainRECForThisRecover(self, value: int, by_effect: 'Effect'):
            self.trigger.GainForThisActive(
                by_effect,
                self,
                recover=value)

    class AfterUnitRecovery(CanBeInstead, TriggerUnitMessage, HasPreEventMessage):
        def __init__(self, unit: 'Unit2', rec_message: 'Message.AfterUnitHealHealth|None', would_message: 'Message.WhenUnitWouldRecovery'):
            self.rec_message = rec_message
            super().__init__(trigger=unit, pre_message=would_message)

