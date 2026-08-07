from . import *

class RecoverProperty(PowerProperty):
    def GetRecover(self, this: 'CardFace') -> int:
        value = 0
        if self.is_basic_power:
            if HasRecover.IsType(this):
                value = this.recover
        return self.additional_value + value

class HasRecover(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_recovery = 0

        super().__init__(paper)

        self.RegisterAttribute("REC", "printed_recovery")
        self.RegisterInfoDict('recover')

    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainRecover(self.printed_recovery, by_effect)
        return super().OnResetKeywords(by_effect)

    # def SetBaseRecover(self, value: int, reset: bool, by_effect: 'Effect') -> None:
    #     if reset:
    #         self.curr_recover = self.base_recover
    #     if self.IsCanRecover() and self.base_recover != value:
    #         diff = value - self.base_recover
    #         self.base_recover = value
    #         self.GainRecover(diff, by_effect)

    @final
    def GainRecover(self, diff: int, by_effect: 'Effect') -> None:
        self.GainKeyword(diff, 'REC', by_effect)

    # def GetRecover(self) -> int:
    #     return self.GetKeyword('REC')


    @final
    @property
    def recover(self) -> int:
        return self.GetKeyword('REC')

################################################################################
#
class CanRecover(CardFace):

    ################################################################################
    #
    @final
    def RecoverInternal(self, effect: 'Effect', property: 'RecoverProperty') -> 'Message.AfterUnitRecovery|None':
        from game.card.face.attribute.can_health import CanHealth
        from game.card.face.base import Unit2
        this = self.card.CastTo(CanHealth)
        message = Message.WhenUnitWouldRecovery(self.card.CastTo(Unit2))
        message.Send()
        if message.is_be_instead:
            return None

        this = this.card.CastTo(Unit2)

        if property.is_basic_power:
            unit = this.card.CastTo(Unit2)
            use_basic_power_message = Message.WhenUnitUseBasicPower(unit, "REC", message)
            use_basic_power_message.Send()
        else:
            use_basic_power_message = None
        value = property.GetRecover(this)
        rec_message = this.HealHealth(value, effect)

        recovery_message = Message.AfterUnitRecovery(this.card.CastTo(Unit2), rec_message, message)
        recovery_message.Send()

        if use_basic_power_message:
            unit = this.card.CastTo(Unit2)
            after_use_basic_power_message = Message.AfterUnitUseBasicPower(unit, "REC", recovery_message, use_basic_power_message)
            after_use_basic_power_message.Send()

        return recovery_message

    @final
    def BasicRecover(self, effect: 'Effect') -> 'Message.AfterUnitRecovery|None':
        if not self.IsCanRecover(effect):
            return None
        property = RecoverProperty(is_basic_power=True)
        message = self.RecoverInternal(effect, property)
        return message

    @final
    def IsCanRecover(self, by_effect: 'Effect') -> bool:
        from game.card.face.card_type import AlterEgo

        if not self.IsInPlay():
            return False
        if not AlterEgo.IsType(self):
            return False
        check_message = Message.CheckIfUnitCanRecover(self, by_effect)
        check_message.Send()
        return check_message.can_recover

