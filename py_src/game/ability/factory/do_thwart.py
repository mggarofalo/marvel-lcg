from . import *

class AbilityFactoryDoThwart:

    @staticmethod
    def WhenAttachedPlayerWouldThwart(ability_type: 'AbilityType',
                                    attacker: CardType|Literal["You"],
                                    operation: OperationType[Message.WhenUnitWouldAttack|Message.WhenPlayerWouldPlayCard],
                                    ) -> List['Ability']:
        from game.ability.factory import AbilityFactory
        return [
            AbilityFactory.WhenUnitWouldAttack(
                ability_type,
                attacker,
                operation
            ),
            # Fix: 18012
            AbilityFactory.WhenPlayerWouldPlayCard(
                ability_type,
                "AttachedPlayer",
                None,
                operation,
                conditions=[
                    lambda effect, message:
                        message.play_effect.ability.is_label_thwart
                ],
            )
        ]

