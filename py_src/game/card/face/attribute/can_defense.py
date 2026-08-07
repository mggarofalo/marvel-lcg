from . import *

class DefenseProperty(PowerProperty):
    def GetDefense(self, this: 'CardFace') -> int:
        value = 0
        if self.is_basic_power:
            if HasDefense.IsType(this):
                value = this.defense
        return self.additional_value + value

class HasDefense(HasAttribute):

    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_defense = 0

        super().__init__(paper)

        self.RegisterAttribute("DEF", "printed_defense")
        self.RegisterInfoDict('defense')

    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainDefense(self.printed_defense, by_effect)
        return super().OnResetKeywords(by_effect)

    @final
    def GainDefense(self, diff: int, by_effect: 'Effect', *, render_ui: bool=False):
        self.GainKeyword(diff, 'DEF', by_effect, render_ui=render_ui)

    # def GetDefenseValue(self) -> int:
    #     return self.curr_defense

    @final
    @property
    def defense(self) -> int:
        return max(0, self.GetKeyword('DEF'))

class CanDefense(CardFace):

    ################################################################################
    #
    @final
    def DefenseInternal(self, by_effect: 'Effect', being_atk_message: 'Message.WhenUnitBeingAttack', property: 'DefenseProperty', on_event: 'Message2') -> 'Message.AfterUnitDefenseInternal|None':
        from game.message import Message
        from game.card.face.base import Unit2

        unit = self.card.CastTo(Unit2)

        would_defense_message = Message.WhenUnitWouldDefend(unit, by_effect, property, being_atk_message)
        would_defense_message.Send()

        if would_defense_message.property.is_basic_power:
            use_basic_power_message = Message.WhenUnitUseBasicPower(self.card.CastTo(Unit2), "DEF", would_defense_message)
            use_basic_power_message.Send()
        else:
            use_basic_power_message = None

        defense_message = Message.AfterUnitDefenseInternal(unit, would_defense_message, use_basic_power_message)
        defense_message.Send()
        # Fix "13005"
        being_atk_message.SetDefender(unit.card.face.CastTo(Unit2), defense_message, by_effect, on_event)

        return defense_message

    @final
    def BasicDefenseLater(self, atk_message: 'Message.WhenUnitWouldAttack', by_effect: 'Effect') -> 'None':
        from game.ability.factory import AbilityFactory

        def action(effect: 'Effect', message: 'Message.WhenUnitBeingAttack'):
            if atk_message.attacker and not atk_message.attacker.IsDefeated():
                self.BasicDefense(message, by_effect)
        by_effect.this.effect.RegisterTemp(
            AbilityFactory.WhenUnitBeingAttack(
                AbilityType.Temp0,
                None,
                None,
                action,
                would_atk_message=atk_message,
            ),
            unregister_after_exec=True,
        )

    @final
    def BasicDefense(self, atk_message: 'Message.WhenUnitBeingAttack', by_effect: 'Effect') -> 'Message.AfterUnitDefenseInternal|None':
        property = DefenseProperty(is_basic_power=True)
        message = self.DefenseInternal(by_effect, atk_message, property, atk_message)
        return message

    @final
    def SpecialDefense(self, pre_message: 'Message2', by_effect: 'Effect'):
        from game.ability.factory import AbilityFactory
        property = DefenseProperty(is_basic_power=False)
        
        def defense(message: Message.WhenUnitWouldAttack):
            def action(effect: 'Effect', message: 'Message.WhenUnitBeingAttack'):
                self.DefenseInternal(by_effect, message, property, message)
            by_effect.this.effect.RegisterTemp(
                AbilityFactory.WhenUnitBeingAttack(
                    AbilityType.Temp0,
                    None,
                    None,
                    action,
                    would_atk_message=message,
                    has_defender=False, # Fix for "48006"
                ),
                unregister_after_exec=True,
            )

        if isinstance(pre_message, Message.WhenUnitBeingAttack):
            self.DefenseInternal(by_effect, pre_message, property, pre_message)
        elif isinstance(pre_message, Message.WhenUnitWouldAttack):
            defense(pre_message)
        elif isinstance(pre_message, Message.WhenUnitWouldDefend):
            self.DefenseInternal(by_effect, pre_message.being_atk_message, property, pre_message)
        elif isinstance(pre_message, Message.WhenUnitWouldTakeDamage):
            if pre_message.being_atk_message:
                self.DefenseInternal(by_effect, pre_message.being_atk_message, property, pre_message)
        elif isinstance(pre_message, Message.WhenBoostCardTurnedFaceUp):
            if isinstance(pre_message.being_message, Message.WhenUnitBeingAttack):
                self.DefenseInternal(by_effect, pre_message.being_message, property, pre_message)
        elif isinstance(pre_message, Message.WhenBoostCardWouldTurnedFaceUp):
            if isinstance(pre_message.being_message, Message.WhenUnitBeingAttack):
                self.DefenseInternal(by_effect, pre_message.being_message, property, pre_message)
        elif isinstance(pre_message, Message.AfterEnemyGivenBoostCard):
            if pre_message.would_atk_message != None:
                defense(pre_message.would_atk_message)
        elif isinstance(pre_message, Message.AfterUnitBecomeDefender):
            pass
        else:
            assert False

    # def SetBaseDefense(self, value: int, by_effect: 'Effect') -> None:
    #     if self.IsCanDefense() and self.base_defense != value:
    #         diff = value - self.base_defense
    #         self.base_defense = value
    #         self.GainDefense(diff, by_effect)
    # def GainDefenseUntilPhaseEnd(self, value: int, by_effect: 'Effect') -> None:
    #     self.GainUntilPhaseEnd(
    #         by_effect,
    #         defense=value,
    #     )
    # def GainDefenseUntilRoundEnd(self, value: int, by_effect: 'Effect') -> None:
    #     self.GainUntilRoundEnd(
    #         by_effect,
    #         defense=value,
    #     )

    @final
    def IsCanDefense(self, attacker: 'Unit2') -> bool:
        from game.card.face.card_type import Hero
        from game.card.face.card_type import Ally

        if not self.IsInPlay():
            return False
        if not isinstance(self, Ally|Hero):
            return False
        check_message = Message.CheckIfUnitCanDefendAgainstAttack(self, attacker)
        check_message.Send()
        return check_message.can_defend

