from . import *

class AbilityFactoryMinion:

    @staticmethod
    def WhenMinionWouldEngagePlayer(ability_type: 'AbilityType',
                                which_minion: CardType,
                                engage_to_who: Literal["You", "YourHero"]|None,
                                operation: OperationType[Message.WhenMinionWouldEngagePlayer],
                                *,
                                conditions: ConditionsType[Message.WhenMinionWouldEngagePlayer]=[],
                                ) -> 'Ability':
        def check_which_minion(effect: 'Effect', message: 'Message.WhenMinionWouldEngagePlayer') -> bool:
            return Condition.CheckWhichCard(which_minion, message.trigger, effect)

        def check_engage_to_who(effect: 'Effect', message: 'Message.WhenMinionWouldEngagePlayer') -> bool:
            if engage_to_who == None:
                return True
            player = message.would_engaged_player
            if engage_to_who == "You" or engage_to_who == "YourHero":
                if Player.IsType(effect.this.GetControlBy()):
                    if engage_to_who == "YourHero":
                        return effect.GetInitiator() == player and player.IsHero()
                    else:
                        return effect.GetInitiator() == player
                else:
                    return True

        return Ability(
            ability_type,
            Message.WhenMinionWouldEngagePlayer,
            [
                check_which_minion,
                check_engage_to_who,
                *conditions
            ],
            operation,
            is_local=which_minion == "This"
        )

    @staticmethod
    def WhenMinionEngagePlayer(ability_type: 'AbilityType',
                                which_minion: CardType,
                                engage_to_who: Literal["You", "YourHero"]|None,
                                operation: OperationType[Message.WhenMinionEngagePlayer],
                                *,
                                conditions: ConditionsType[Message.WhenMinionEngagePlayer]=[],
                                ) -> 'Ability':
        from game.player import Player

        def check_which_minion(effect: 'Effect', message: 'Message.WhenMinionEngagePlayer') -> bool:
            return Condition.CheckWhichCard(which_minion, message.trigger, effect)

        def check_engage_to_who(effect: 'Effect', message: 'Message.WhenMinionEngagePlayer') -> bool:
            if engage_to_who == None:
                return True
            # Fix 06001a vs 21143
            player = message.engaged_player
            if engage_to_who == "You" or engage_to_who == "YourHero":
                if Player.IsType(effect.this.GetControlBy()):
                    if engage_to_who == "YourHero":
                        return effect.GetInitiator() == player and player.IsHero()
                    else:
                        return effect.GetInitiator() == player
                else:
                    return True

        return Ability(
            ability_type,
            Message.WhenMinionEngagePlayer,
            [
                check_which_minion,
                check_engage_to_who,
                *conditions
            ],
            operation,
            is_local=which_minion == "This"
        )

    @staticmethod
    def AfterPlayerEngageMinion(ability_type: 'AbilityType', 
                                which_player: Literal["You", "YouHero"]|None,
                                operation: OperationType[Message.AfterMinionEngagePlayer],
                                *,
                                conditions: ConditionsType[Message.AfterMinionEngagePlayer]=[],
                                ) -> 'Ability':
        return AbilityFactoryMinion.AfterMinionEngagePlayer(
            ability_type,
            None,
            which_player,
            operation,
            conditions=conditions
        )

    @staticmethod
    def AfterMinionEngagePlayer(ability_type: 'AbilityType',
                                which_minion: CardType,
                                engage_to_who: Literal["You", "YouHero"]|None,
                                operation: OperationType[Message.AfterMinionEngagePlayer],
                                *,
                                conditions: ConditionsType[Message.AfterMinionEngagePlayer]=[],
                                ) -> 'Ability':
        from game.player import Player

        def check_which_minion(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> bool:
            return Condition.CheckWhichCard(which_minion, message.trigger, effect)

        def check_engage_to_who(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> bool:
            if engage_to_who == None:
                return True
            # Fix 06001a vs 21143
            player = message.engaged_player
            if engage_to_who == "You" or engage_to_who == "YouHero":
                if Player.IsType(effect.this.GetControlBy()):
                    if engage_to_who == "YouHero":
                        return effect.GetInitiator() == player and player.IsHero()
                    else:
                        return effect.GetInitiator() == player
                else:
                    if engage_to_who == "YouHero":
                        return player.IsHero()
                    else:
                        return True

        return Ability(
            ability_type,
            Message.AfterMinionEngagePlayer,
            [
                check_which_minion,
                check_engage_to_who,
                *conditions
            ],
            operation,
            is_local=which_minion == "This"
        )

    @staticmethod
    def ThisMinionEngageFirstPlayer() -> 'Ability':
        from game.card.face.card_type import Minion

        def engage_first_player(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer|Message.AfterPlayersOrderChange') -> None:
            minion = effect.this.CastTo(Minion)
            first_player = effect.world.GetFirstPlayer()
            minion.EngagePlayer(first_player, effect)

        from game.ability.factory import AbilityFactory
        return AbilityFactory.WhenEvent(
            Message.AfterMinionEngagePlayer|Message.AfterPlayersOrderChange,
            lambda effect, last_value:
                effect.this.CastTo(Minion).engaged_player != effect.world.GetFirstPlayer(),
            engage_first_player
        )

    @staticmethod
    def ThisMinionEngagePlayer(which_player: 'PlayerFinder') -> 'Ability':
        from game.card.face.card_type import Minion
        from game.operate.worlds import Worlds

        def get_card_attached_player(effect: 'Effect', last_value: 'Message.AfterMinionEngagePlayer|Message.AfterCardAttachTo') -> bool:
            this = effect.this.CastTo(Minion)
            player = this.engaged_player

            if not player:
                return True
            if not which_player.Check(player):
                return True
            return False

        def engage_escaped_mutant_attached_player(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer|Message.AfterCardAttachTo') -> None:
            minion = effect.this.CastTo(Minion)
            player = Worlds.FindPlayer(which_player, effect)
            if player:
                minion.EngagePlayer(player, effect)

        from game.ability.factory import AbilityFactory
        return AbilityFactory.WhenEvent(
            Message.AfterMinionEngagePlayer|Message.AfterCardAttachTo,
            get_card_attached_player,
            engage_escaped_mutant_attached_player
        )

