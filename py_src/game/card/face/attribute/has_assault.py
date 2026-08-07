from . import *

class HasAssault(HasAttribute):
    @override
    def __init__(self, paper: 'Paper') -> None:
        self.printed_assault = 0

        super().__init__(paper)

        self.RegisterAttribute("Assault", "printed_assault")
        self.RegisterInfoDict('assault')

    @override
    def GetAbilities(self) -> List['Ability']:
        def use_atk_instead(effect: 'Effect', message: 'Message.WhenSchemeBeingThwart'):
            unit = message.trigger
            if HasAttack.IsType(unit):
                atk = unit.attack
            else:
                atk = 0
            if HasThwart.IsType(unit):
                thw = unit.thwart
            else:
                thw = 0
            diff = atk - thw
            message.GainThwartForThisThwart(diff, effect)

        return [
            AbilityFactory.WhenUnitThwartScheme(
                AbilityType.Rule,
                None,
                use_atk_instead,
                which_card_be_thwart="This",
                is_basic_thwart=True,
                conditions=[
                    lambda effect, message:
                        message.scheme.IsAssault()
                ]
            )
        ] + super().GetAbilities()

    ################################################################################
    #
    @override
    def OnResetKeywords(self, by_effect: 'Effect'):
        self.GainAssault(self.printed_assault, by_effect)
        return super().OnResetKeywords(by_effect)

    ################################################################################
    #
    @final
    def IsAssault(self) -> bool:
        return self.assault > 0

    @final
    def GainAssault(self, diff: int, by_effect: 'Effect'):
        self.GainKeyword(diff, 'Assault', by_effect)

    @final
    @property
    def assault(self) -> int:
        return self.GetKeyword('Assault')

