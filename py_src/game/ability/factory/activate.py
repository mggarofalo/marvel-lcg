from . import *

class AbilityFactoryActivate:

    @staticmethod
    def WhenEnemyWouldActivate(ability_type: 'AbilityType',
                               which_enemy: CardType,
                               operation: OperationType[Message.WhenEnemyWouldActivate],
                               *,
                               conditions: ConditionsType[Message.WhenEnemyWouldActivate]=[],
                               power: Literal["SCH", "ATK"]|None=None,
                            #    against_you: bool|None=None,
                               during_villain_step: int|None=None,
                               ) -> 'Ability':

        def check_which_enemy(effect: 'Effect', message: 'Message.WhenEnemyWouldActivate') -> bool:
            return Condition.CheckWhichCard(which_enemy, message.trigger, effect)

        def check_power(effect: 'Effect', message: 'Message.WhenEnemyWouldActivate') -> bool:
            if power == None:
                return True
            return power == message.power

        # def check_against_you(effect: 'Effect', message: 'Message.WhenEnemyWouldActivate') -> bool:
        def check_during_villain_step(effect: 'Effect', message: 'Message.WhenEnemyWouldActivate') -> bool:
            if not during_villain_step:
                return True
            return message.world.phase.IsVillainPhaseStep(during_villain_step)

        return Ability(
            ability_type,
            Message.WhenEnemyWouldActivate,
            [
                check_which_enemy,
                check_power,
                check_during_villain_step,
                *conditions
            ],
            operation,
            is_local=which_enemy == "This"
        )

    @staticmethod
    def WhenEnemyActivateAgainstYou(ability_type: 'AbilityType',
                                    which_enemy: CardType,
                                    operation: OperationType[Message.WhenEnemyActivateAgainstYou],
                                    *,
                                    conditions: ConditionsType[Message.WhenEnemyActivateAgainstYou]=[],
                                    ) -> 'Ability':

        def check_which_enemy(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> bool:
            return Condition.CheckWhichCard(which_enemy, message.trigger, effect)

        return Ability(
            ability_type,
            Message.WhenEnemyActivateAgainstYou,
            [
                lambda effect, message:
                    message.to_player != None,
                check_which_enemy,
                *conditions
            ],
            operation,
            is_local=which_enemy == "This"
        )

    @staticmethod
    def AfterEnemyActivatesAgainstYou(ability_type: 'AbilityType',
                                    which_enemy: CardType,
                                    operation: OperationType[Message.AfterEnemyActivationEnd],
                                    *,
                                    conditions: ConditionsType[Message.AfterEnemyActivationEnd]=[],
                                    ) -> 'Ability':
        return AbilityFactoryActivate.AfterEnemyActivationEnd(
            ability_type,
            which_enemy,
            operation,
            conditions=conditions,
            is_against_you=True
        )

    @staticmethod
    def AfterEnemyActivationEnd(ability_type: 'AbilityType',
                                which_enemy: CardType,
                                operation: OperationType[Message.AfterEnemyActivationEnd],
                                *,
                                conditions: ConditionsType[Message.AfterEnemyActivationEnd]=[],
                                is_against_you: bool|None=None,
                                ) -> 'Ability':

        def check_which_enemy(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> bool:
            return Condition.CheckWhichCard(which_enemy, message.trigger, effect)

        def check_against_you(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> bool:
            if is_against_you == None:
                return True
            return is_against_you == Condition.AgainstYou(effect, message.activate_message)

        return Ability(
            ability_type,
            Message.AfterEnemyActivationEnd,
            [
                check_which_enemy,
                check_against_you,
                *conditions,
            ],
            operation,
            is_local=which_enemy == "This"
        )

