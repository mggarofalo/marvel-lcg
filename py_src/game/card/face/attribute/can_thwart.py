from . import *

@dataclass
class ThwartProperty(PowerProperty):
    def GetThwart(self, this: 'CardFace') -> int:
        value = 0
        if self.is_basic_power:
            if HasThwart.IsType(this):
                value = this.thwart
        return self.additional_value + value

class HasThwart(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.base_thwart = 0

        self.printed_thwart = 0
        self.printed_thwart_consequential_damage = 0

        super().__init__(paper)

        def parse(value: str):
            if '*' in value:
                consequential_damage = value.count('*')
                self.printed_thwart_consequential_damage = consequential_damage
                value = value.replace('*', '')
            self.printed_thwart = int(value)
        self.RegisterAttribute("THW", parse)
        self.RegisterInfoDict('thwart')

    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.base_thwart = self.printed_thwart
        self.GainThwart(self.base_thwart, by_effect)
        return super().OnResetKeywords(by_effect)

    ################################################################################
    #
    @final
    def SetBaseThwart(self, value: int, by_effect: 'Effect') -> None:
        if self.base_thwart != value:
            diff = value - self.base_thwart
            self.base_thwart = value
            self.GainThwart(diff, by_effect)

    @final
    def GainThwart(self, diff: int, by_effect: 'Effect', *, render_ui: bool=False) -> None:
        self.GainKeyword(diff, 'THW', by_effect, render_ui=render_ui)

    @final
    @property
    def thwart(self) -> int:
        return max(0, self.GetKeyword('THW'))

################################################################################
#
class CanThwart(CardFace):

    @override
    def OnRemoveSchemeThreat(self, schemes: List['Scheme2'], value: int, by_effect: 'Effect') -> int|None:

        if by_effect.ability.is_like_thwart:
            property = ThwartProperty(additional_value=value)

            message = CanThwart.SpecialThwart(self, schemes, by_effect, property=property)
            if not message:
                return None
            else:
                return message.total_remove_threat
        else:
            return self.DefaultRemoveSchemeThreat(self, schemes, value, by_effect)

    @staticmethod
    def ThwartInternal(this: 'Unit2|CanThwart', schemes: List['Scheme2'], by_effect: 'Effect', *, property: 'ThwartProperty') -> 'Message.AfterUnitThwartEnd|None':
        from game.message import Message
        from game.card.face.base import Unit2
        from game.card.face.base import Scheme2
        from game.effect.rule import GameRule

        def gain_value_when_divided_for_this_target(gain_value_message: 'Message.WhenSchemeBeingThwart') -> int:
            if gain_value_message.gain_thw > 0 and property.is_divided:
                return gain_value_message.gain_thw
            return 0

        def gain_value_when_divided(gain_value_message: 'Message.WhenUnitWouldThwart'):
            if gain_value_message.gain_thw > 0 and property.is_divided:
                gain_value = gain_value_message.gain_thw
                player = unit.GetControlByPlayer()
                new_added_targets = player.AskChooseFaces(schemes, (gain_value, gain_value), GameRule(unit), prompt="Assign", repeat_rules="Any")
                for check_face in new_added_targets:
                    if Scheme2.IsType(check_face):
                        gain_value_message.AddTarget(check_face)

        is_basic_thwart = property.is_basic_power

        this = this.card.CastTo(Unit2)

        # TODO: Test
        # if property.additional_value > 0:
        #     value = property.additional_value
        #     property.additional_value = 0
        #     self.GainThwart(value, by_effect)
        would_thw_message = Message.WhenUnitWouldThwart(this, schemes, by_effect, property=property)
        would_thw_message.Send()
        if would_thw_message.is_be_instead:
            return None

        if is_basic_thwart:
            unit = this.card.CastTo(Unit2)
            # assert property.base_value == self.thwart
            use_basic_power_message = Message.WhenUnitUseBasicPower(unit, "THW", would_thw_message)
            use_basic_power_message.Send()
        else:
            use_basic_power_message = None

        gain_value_when_divided(would_thw_message)

        thwart_targets: List['Scheme2'] = []
        total_remove_threat = 0

        from game.operate.faces_helper import FacesHelper
        schemes_dict = FacesHelper.ListToDict(would_thw_message.targets)

        after_thw_messages: List['Message.AfterUnitThwartScheme'] = []

        for target in schemes_dict:

            this = this.card.CastTo(Unit2)
            if not this.IsInPlay():
                break

            if not Scheme2.IsType(target):
                continue

            being_thw_message = Message.WhenSchemeBeingThwart(target, would_thw_message)
            being_thw_message.Send()
            gain_value_when_divided_for_this_target(being_thw_message)

            this = this.card.CastTo(Unit2)

            if property.is_divided:
                thwart = 1
            else:
                thwart = property.GetThwart(this)

            curr_thwart = schemes_dict[target] * thwart

            remove_threat = target.RemoveThreatInternal(this, curr_thwart, by_effect, being_thw_message)
            after_thw_message = Message.AfterUnitThwartScheme(this, target, remove_threat, being_thw_message)
            after_thw_message.Send()

            thwart_targets.append(target)
            total_remove_threat += after_thw_message.remove_threat

            after_thw_messages.append(after_thw_message)


        end_message = Message.AfterUnitThwartEnd(this, thwart_targets, total_remove_threat, by_effect, after_thw_messages)

        if use_basic_power_message or not by_effect.ability.IsLabel('thwart'):
            end_message.Send()

            if use_basic_power_message and not this.IsDefeated():
                unit = this.card.CastTo(Unit2)
                after_use_basic_power_message = Message.AfterUnitUseBasicPower(unit, "THW", end_message, use_basic_power_message)
                after_use_basic_power_message.Send()
            this.card.world.stat.RecordThwart(end_message)

        else:
            by_effect.context.AddThwMessage(end_message)
        return end_message


    @staticmethod
    def GetSchemes(schemes: Sequence['CardFace']) -> List['Scheme2']:
        from game.card.face.base import Scheme2
        targets: List['Scheme2'] = []
        for scheme in schemes:
            if Scheme2.IsType(scheme):
                targets.append(scheme)
        return targets

    @staticmethod
    def SpecialThwart(this: 'Unit2|CanThwart', schemes: List['Scheme2'], by_effect: 'Effect', *, property: 'ThwartProperty') -> 'Message.AfterUnitThwartEnd|None':
        message = CanThwart.ThwartInternal(this, schemes, by_effect, property=property)
        return message

    def BasicThwart(self, schemes: List['CardFace'], by_effect: 'Effect', *, property: 'ThwartProperty|None' = None) -> 'Message.AfterUnitThwartEnd|None':
        if not self.IsCanThwart(by_effect):
            return None
        if property == None:
            property = ThwartProperty(is_basic_power=True)
        else:
            property.is_basic_power = True

        targets = CanThwart.GetSchemes(schemes)

        message = CanThwart.ThwartInternal(self, targets, by_effect, property=property)
        return message

    def IsCanThwart(self, by_effect: 'Effect') -> bool:
        from game.card.face.base import Unit2

        if not self.IsInPlay():
            return False
        if not Unit2.IsType(self):
            return False
        check_message = Message.CheckIfUnitCanThwart(self, by_effect)
        check_message.Send()
        return check_message.can_thwart

