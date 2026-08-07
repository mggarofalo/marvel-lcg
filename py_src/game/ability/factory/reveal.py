from . import *

class AbilityFactoryReveal:

    @staticmethod
    # Before flip
    def WhenCardWouldReveal(ability_type: 'AbilityType',
                            reveal_to_player: PlayerType,
                            which_card: CardType,
                            operation: OperationType[Message.WhenCardWouldReveal],
                            *,
                            conditions: ConditionsType[Message.WhenCardWouldReveal]=[],
                            ) -> 'Ability':

        def check_who(effect: 'Effect', message: 'Message.WhenCardWouldReveal') -> bool:
            # if reveal_to_player == "You":
            #     return Condition.ThisIsYou(effect, message.GetToPlayer()) and \
            #         effect.GetInitiator() == message.GetToPlayer()
            return Condition.CheckWhichPlayer(reveal_to_player, message.to_player, effect)

        def check_card_type(effect: 'Effect', message: 'Message.WhenCardWouldReveal') -> bool:
            return Condition.CheckWhichCard(which_card, message.trigger, effect)

        return Ability(
            ability_type,
            Message.WhenCardWouldReveal,
            [
                check_who,
                check_card_type,
                *conditions
            ],
            operation,
            is_local=which_card == "This"
        )

    @staticmethod
    # After flip
    def WhenPlayerRevealCard(ability_type: 'AbilityType',
                            which_player: PlayerType,
                            which_card: CardType,
                            can_be_cancel: Literal["All", "WhenRevealed", "Yes"]|None, # Yes means no villain no main scheme and without 'cannot be canceled' keyword
                            operation: OperationType[Message.WhenPlayerRevealCard],
                            *,
                            from_encounter: bool|None=None,
                            conditions: ConditionsType[Message.WhenPlayerRevealCard]=[],
                            ) -> 'Ability':

        def check_card_is_revealing(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> bool:
            face = message.trigger
            return face.CanResolveWhenRevealed()

        def check_which_player(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.to_player, effect)

        def check_from_encounter(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> bool:
            # Only work when reveal from encounter deck, not from discard pile
            if from_encounter != None:
                if from_encounter:
                    return message.IsFromEncounterDeck()
                else:
                    assert False, f"{effect=}"
            return True

        def check_which_card(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> bool:
            return Condition.CheckWhichCard(which_card, message.trigger, effect)

        def check_can_be_cancel(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> bool:
            if can_be_cancel == "Yes":
                # Fix "52008" cannot interact with villain or any card with cannot be canceled
                if message.cannot_be_cancel:
                    return False
            elif can_be_cancel:
                return message.CanBeCancel(can_be_cancel, effect)
            return True

        return Ability(
            ability_type,
            Message.WhenPlayerRevealCard,
            [
                check_card_is_revealing,
                check_which_player,
                check_from_encounter,
                check_which_card,
                check_can_be_cancel,
                *conditions
            ],
            operation,
            is_local=which_card == "This"
        )

    @staticmethod
    def WhenCardRevealed(ability_type: 'AbilityType',
                         which_card: CardType,
                         operation: OperationType[Message.WhenCardRevealed],
                         *,
                         conditions: ConditionsType[Message.WhenCardRevealed]=[],
                         ) -> 'Ability':

        def check_which_card(effect: 'Effect', message: 'Message.WhenCardRevealed') -> bool:
            if which_card == None:
                return True
            if which_card == "This":
                return effect.this == message.trigger
            return Condition.CheckWhichCard(which_card, message.trigger, effect)

        return Ability(
            ability_type,
            Message.WhenCardRevealed,
            [
                check_which_card,
                *conditions
            ],
            operation,
            is_local=which_card == "This"
        )

    @staticmethod
    def WhenThisRevealed(condition: ConditionType[Message.WhenCardRevealed]|Literal["Alter-Ego", "Hero"]|None,
                         operation: OperationType[Message.WhenCardRevealed]) -> 'Ability':
        def check_condition(effect: 'Effect', message: 'Message.WhenCardRevealed') -> bool:
            if condition == "Alter-Ego":
                return message.GetToPlayer().IsAlterEgo()
            if condition == "Hero":
                return message.GetToPlayer().IsHero()
            if condition == None:
                return True
            return condition(effect, message)

        return AbilityFactoryReveal.WhenCardRevealed(
            AbilityType.WhenRevealed,
            "This",
            operation,
            conditions=[
                Condition2.ThisIsTrigger,
                check_condition
            ],
        )

    # @staticmethod
    # def AfterRevealedDiscard() -> 'Action':
    #     return Action(Message.AfterCardRevealed,
    #                   [Condition.TriggerIsThis, Condition.OnField],
    #                   [Filter.IsForce],
    #                   [
    #                     this.Discard(effect))
    # @staticmethod
    # def WhenThisRevealedHero(operation: ActionFuncType[Send.WhenCardRevealed]) -> 'Ability':
    #     return AbilityFactory.WhenThisRevealed(
    #         [
    #             message.GetToPlayer().IsHero() and \
    #             effect.this == message.trigger,
    #         operation
    #     )
    # @staticmethod
    # def WhenThisRevealedAlterEgo(operation: ActionFuncType[Send.WhenCardRevealed]) -> 'Ability':
    #     return AbilityFactory.WhenThisRevealed(
    #         [
    #             message.GetToPlayer().IsAlterEgo() and \
    #             effect.this == message.trigger,
    #         operation
    #     )

    @staticmethod
    def WhenThisObligationRevealed(operation: OperationType[Message.WhenCardRevealed]) -> 'Ability':
        return Ability(
            AbilityType.WhenRevealed,
            Message.WhenCardRevealed,
            [
                Condition2.ThisIsTrigger,
            ],
            operation,
        )

    @staticmethod
    def WhenThisObligationGiveToYou(operation: OperationType[Message.WhenObligationGiveToPlayer]) -> 'Ability':
        return Ability(
            AbilityType.NonKeyword, # This is different from `WhenThisRevealed`
            Message.WhenObligationGiveToPlayer,
            [
                Condition2.ThisIsTrigger,
            ],
            operation,
        )

    @staticmethod
    def AfterYouResolveTreachery(ability_type: 'AbilityType',
                                operation: OperationType[Message.AfterCardRevealed],
                                *,
                                conditions: ConditionsType[Message.AfterCardRevealed]=[],
                                ) -> 'Ability':
        from game.card.face.card_type import Treachery

        return Ability(
            ability_type,
            Message.AfterCardRevealed,
            [
                lambda effect, message:
                    effect.GetInitiator() == message.GetToPlayer() and \
                    Treachery.IsType(message.trigger),
                *conditions
            ],
            operation
        )

    @staticmethod
    # After flip
    def AfterPlayerRevealCard(ability_type: 'AbilityType',
                            which_player: PlayerType,
                            which_card: CardType,
                            operation: OperationType[Message.AfterCardRevealedEnd],
                            *,
                            # from_encounter: bool|None=None,
                            conditions: ConditionsType[Message.AfterCardRevealedEnd]=[],
                            ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: 'Message.AfterCardRevealedEnd') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.to_player, effect)

        # def check_from_encounter(effect: 'Effect', message: 'Send.AfterCardRevealedEnd') -> bool:
        #     # Only work when reveal from encounter deck, not from discard pile
        #     if from_encounter != None:
        #         if from_encounter:
        #             return message.IsFromEncounterDeck()
        #         else:
        #             assert False, f"{effect=}"
        #     return True

        def check_which_card(effect: 'Effect', message: 'Message.AfterCardRevealedEnd') -> bool:
            return Condition.CheckWhichCard(which_card, message.trigger, effect)

        return Ability(
            ability_type,
            Message.AfterCardRevealedEnd,
            [
                check_which_player,
                # check_from_encounter,
                check_which_card,
                *conditions
            ],
            operation,
            is_local=which_card == "This"
        )

