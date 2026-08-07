from . import *

class AbilityFactoryDoScheme:

    @staticmethod
    def WhenUnitWouldScheme(ability_type: 'AbilityType',
                            who_scheme: CardType,
                            operation: OperationType[Message.WhenUnitWouldScheme],
                            *,
                            against_player: PlayerType="AnyPlayer",
                            by_effect: 'Effect|None'=None,
                            conditions: ConditionsType[Message.WhenUnitWouldScheme]=[],
                            ) -> 'Ability':
        def check_who_scheme(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> bool:
            return Condition.CheckWhichCard(who_scheme, message.trigger, effect)

        def check_against_player(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> bool:
            return Condition.CheckAgainstPlayer(against_player, message, effect)

        def check_by_effect(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> bool:
            if by_effect == None:
                return True
            return by_effect == message.by_effect

        return Ability(
            ability_type,
            Message.WhenUnitWouldScheme,
            [
                check_who_scheme,
                check_against_player,
                check_by_effect,
                *conditions
            ],
            operation,
            is_local=who_scheme == "This"
        )

    @staticmethod
    def WhenUnitSchemeAgainst(ability_type: 'AbilityType',
                                who_scheme: CardType,
                                against_player: PlayerType,
                                operation: OperationType[Message.WhenUnitWouldScheme],
                                *,
                                conditions: ConditionsType[Message.WhenUnitWouldScheme]=[],
                                ) -> 'Ability':
        return AbilityFactoryDoScheme.WhenUnitWouldScheme(
            ability_type,
            who_scheme,
            operation,
            against_player=against_player,
            conditions=conditions,
        )

    @staticmethod
    def AfterUnitSchemeAgainst(ability_type: 'AbilityType',
                                who_scheme: CardType,
                                against_player: PlayerType,
                                operation: OperationType[Message.AfterUnitSchemeEnd],
                                conditions: ConditionsType[Message.AfterUnitSchemeEnd]=[],
                                ) -> 'Ability':
        return AbilityFactoryDoScheme.AfterUnitSchemeEnd(
            ability_type,
            who_scheme,
            operation,
            against_player=against_player,
            conditions=conditions,
        )

    @staticmethod
    def AfterUnitSchemeEnd(ability_type: 'AbilityType',
                           which_unit: CardType,
                           operation: OperationType[Message.AfterUnitSchemeEnd],
                           *,
                           against_player: PlayerType="AnyPlayer",
                           conditions: ConditionsType[Message.AfterUnitSchemeEnd]=[],
                           ) -> 'Ability':
        def check_which_unit(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> bool:
            return Condition.CheckWhichCard(which_unit, message.trigger, effect)

        def check_against_player(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> bool:
            return Condition.CheckAgainstPlayer(against_player, message, effect)

        return Ability(
            ability_type,
            Message.AfterUnitSchemeEnd,
            [
                check_which_unit,
                check_against_player,
                *conditions
            ],
            operation,
            is_local=which_unit == "This"
        )

    @staticmethod
    def AfterEnemySchemeAndPlaceThreat(ability_type: 'AbilityType',
                                    which_unit: CardType,
                                    on_which_scheme: CardType,
                                    operation: OperationType[Message.AfterUnitSchemeEnd],
                                    *,
                                    conditions: ConditionsType[Message.AfterUnitSchemeEnd]=[]):
        from game.ability.factory import AbilityFactory

        def check_on_which_scheme(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> bool:
            return Condition.CheckWhichCard(on_which_scheme, message.target_scheme, effect)

        def check_placed_threat(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> bool:
            return message.placed_threat > 0

        return AbilityFactory.AfterUnitSchemeEnd(
            ability_type,
            which_unit,
            operation,
            conditions=[
                check_on_which_scheme,
                check_placed_threat
            ]
        )

    ################################################################################
    #
    @staticmethod
    def WhenRecalculateSchemeValue(ability_type: 'AbilityType',
                                    operation: OperationType[Message.WhenRecalculateSchemeValue],
                                    *,
                                    conditions: ConditionsType[Message.WhenRecalculateSchemeValue]=[],
                                    ) -> 'Ability':
        return Ability(
            ability_type,
            Message.WhenRecalculateSchemeValue,
            [
                *conditions
            ],
            operation
        )

    @staticmethod
    def PlaceThreatOnCardWhenThisScheme(ability_type: 'AbilityType',
                                        which_scheme: 'CardFinder'|Callable[['Effect'], 'Scheme2|None'],
                                        if_able: bool|None=None,
                                        ) -> 'Ability':
        from game.operate.worlds import Worlds
        from game.card.face.base import Scheme2
        from game.card.card_finder import CardFinder

        def check_this(effect: 'Effect', message: 'Message.GettingEnemySchemeTarget') -> bool:
            return Condition.CheckWhichCard("This", message.who_scheme, effect)

        def get_scheme(effect: 'Effect'):
            if isinstance(which_scheme, CardFinder):
                scheme = Worlds.FindCardOnField(
                    effect,
                    which_scheme,
                    card_type=Scheme2
                )
            else:
                scheme = which_scheme(effect)
            return scheme

        def action(effect: 'Effect', message: 'Message.GettingEnemySchemeTarget') -> None:

            scheme = get_scheme(effect)

            if scheme:
                message.SetSchemeTarget(scheme, effect)

        return Ability(
            ability_type,
            Message.GettingEnemySchemeTarget,
            [
                check_this,
                lambda effect, message:
                    get_scheme(effect) != None,
            ],
            action,
            is_local=True
        )

