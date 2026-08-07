from . import *

class AbilityFactoryEnemy:

    @staticmethod
    def EnemyCannotActivate(ability_type: 'AbilityType',
                            which_unit: CardType,
                            *,
                            conditions: ConditionsType[Message.WhenEnemyWouldActivate|Message.WhenUnitWouldAttack|Message.WhenUnitWouldScheme]=[]
                            ) -> 'Ability':
        def check_which_unit(effect: 'Effect', message: 'Message.WhenEnemyWouldActivate|Message.WhenUnitWouldAttack|Message.WhenUnitWouldScheme') -> bool:
            return Condition.CheckWhichCard(which_unit, message.trigger, effect)

        def action(effect: 'Effect', message: 'Message.WhenEnemyWouldActivate|Message.WhenUnitWouldAttack|Message.WhenUnitWouldScheme') -> None:
            message.SetBeInstead(effect)

        return Ability(
            ability_type,
            Message.WhenEnemyWouldActivate|Message.WhenUnitWouldAttack|Message.WhenUnitWouldScheme,
            [
                check_which_unit,
                *conditions
            ],
            action,
            is_local=which_unit == "This"
        )

