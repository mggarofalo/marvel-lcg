from . import *

class AbilityFactoryRecovery:

    @staticmethod
    def WhenUnitMakeRecovery(ability_type: 'AbilityType',
                             which_unit: CardType,
                             operation: OperationType[Message.WhenUnitWouldRecovery],
                             *,
                             conditions: ConditionsType[Message.WhenUnitWouldRecovery]=[],
                             is_basic_recovery: bool|None=None,
                             ) -> 'Ability':

        def check_which_unit(effect: 'Effect', message: 'Message.WhenUnitWouldRecovery') -> bool:
            return Condition.CheckWhichCard(which_unit, message.trigger, effect)

        return Ability(
            ability_type,
            Message.WhenUnitWouldRecovery,
            [
                check_which_unit,
                *conditions
            ],
            operation,
            is_local=which_unit == "This"
        )

    @staticmethod
    def AfterUnitRecovery(ability_type: 'AbilityType',
                          which_unit: CardType|Literal["You"],
                          operation: OperationType[Message.AfterUnitRecovery],
                          *,
                          conditions: ConditionsType[Message.AfterUnitRecovery]=[],
                          is_basic_recovery: bool|None=None,
                          ) -> 'Ability':

        def check_which_unit(effect: 'Effect', message: 'Message.AfterUnitRecovery') -> bool:
            rule = Condition.GetYouRule(which_unit, identity=True)
            return Condition.CheckWhichCard(rule, message.trigger, effect)

        return Ability(
            ability_type,
            Message.AfterUnitRecovery,
            [
                check_which_unit,
                *conditions
            ],
            operation,
            is_local=which_unit == "This"
        )

    @staticmethod
    def AfterUnitMakeRecovery(ability_type: 'AbilityType',
                              which_unit: CardType|Literal["You"],
                              operation: OperationType[Message.AfterUnitRecovery],
                              *,
                              conditions: ConditionsType[Message.AfterUnitRecovery]=[],
                              is_basic_recovery: bool|None=None) -> 'Ability':
        return AbilityFactoryRecovery.AfterUnitRecovery(
            ability_type,
            which_unit,
            operation,
            conditions=conditions,
            is_basic_recovery=is_basic_recovery,
        )

    @staticmethod
    def UnitCannotRecover(which_unit: CardType,
                          *,
                          cannot_trigger_recover_ability: bool|None=None,
                          conditions: ConditionsType[Message.CheckIfUnitCanRecover]=[],
                          ) -> List['Ability']:
        def check_which_unit(effect: 'Effect', message: 'Message.CheckIfUnitCanRecover') -> bool:
            return Condition.CheckWhichCard(which_unit, message.check_identity, effect)

        abilities = [
            Ability(
                AbilityType.NonKeyword,
                Message.CheckIfUnitCanRecover,
                [
                    check_which_unit,
                    *conditions
                ],
                lambda effect, message:
                    message.SetCannotRecover(effect),
                is_local=which_unit == "This"
            )
        ]

        # if cannot_trigger_recover_ability:
        #     abilities.append(
        #         AbilityFactory.PlayersCannotTriggerAbility(
        #             cannot_trigger_recover_ability,
        #             None,
        #             label="Recover",
        #         )
        #     )

        return abilities

