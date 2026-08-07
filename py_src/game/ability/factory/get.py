from . import *

class AbilityFactoryGet:

    ################################################################################
    # Get
    @staticmethod
    def GetMainScheme(get_main_scheme: Callable[['Effect', 'Message.GettingMainScheme'], 'MainScheme|None'],
                      *,
                      conditions: ConditionsType[Message.GettingMainScheme]=[]
                      ) -> 'Ability':

        def set_mains_cheme(effect: 'Effect', message: 'Message.GettingMainScheme') -> None:
            scheme = get_main_scheme(effect, message)
            if scheme:
                message.SetReturn(scheme)

        return Ability(
            AbilityType.NonKeyword,
            Message.GettingMainScheme,
            conditions,
            set_mains_cheme
        )

    @staticmethod
    def GetScenario(set_scenario: Callable[['Effect', 'Message.GettingScenario'], 'Scenario|None'],
                    *,
                    conditions: ConditionsType[Message.GettingScenario]=[]
                    ) -> 'Ability':
        def get_scenario(effect: 'Effect', message: 'Message.GettingScenario') -> None:
            villain = set_scenario(effect, message)
            if villain:
                message.SetReturn(villain)

        return Ability(
            AbilityType.NonKeyword,
            Message.GettingScenario,
            conditions,
            get_scenario
        )

    @staticmethod
    def GetVillain(set_villain: Callable[['Effect', 'Message.GettingVillain'], 'Villain|None'],
                   *,
                    conditions: ConditionsType[Message.GettingVillain]=[]
                    ) -> 'Ability':
        def get_villain(effect: 'Effect', message: 'Message.GettingVillain') -> None:
            villain = set_villain(effect, message)
            if villain:
                message.SetReturn(villain)

        return Ability(
            AbilityType.NonKeyword,
            Message.GettingVillain,
            conditions,
            get_villain
        )

