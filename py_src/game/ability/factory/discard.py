from . import *

class AbilityFactoryDiscard:

    ################################################################################
    # Discard
    @staticmethod
    def WhenCardWouldDiscard(ability_type: 'AbilityType',
                             which_card: CardType,
                             operation: OperationType[Message.WhenCardWouldDiscard],
                             *,
                             from_player_deck: bool|None=None,
                             conditions: ConditionsType[Message.WhenCardWouldDiscard]=[],
                             ) -> 'Ability':

        def check_which_card(effect: 'Effect', message: Message.WhenCardWouldDiscard) -> bool:
            return Condition.CheckWhichCard(which_card, message.trigger, effect)

        def check_from_player_deck(effect: 'Effect', message: Message.WhenCardWouldDiscard) -> bool:
            if from_player_deck == None:
                return True
            controller = message.trigger.card.GetController()
            return message.trigger.card.area.flags.is_player_deck == from_player_deck and \
                (controller == None or not controller.is_eliminated)

        return Ability(
            ability_type,
            Message.WhenCardWouldDiscard,
            [
                check_which_card,
                check_from_player_deck,
                *conditions
            ],
            operation,
            is_local=which_card == "This"
        )

    @staticmethod
    def AfterCardDiscardInternal(ability_type: 'AbilityType',
                                which_card: CardType,
                                operation: OperationType[Message.AfterCardDiscard],
                                *,
                                from_where: Literal["PlayerDeckTop", "PlayerArea", "UnderIdentity", "EncounterDeckTop"]|None=None,
                                by_effect_face: CardType=None,
                                conditions: ConditionsType[Message.AfterCardDiscard]=[],
                                ) -> 'Ability':
        from game.card.face.card_type import Identity

        def check_which_card(effect: 'Effect', message: Message.AfterCardDiscard) -> bool:
            return Condition.CheckWhichCard(which_card, message.trigger, effect)

        def check_from_where(effect: 'Effect', message: Message.AfterCardDiscard) -> bool:
            if from_where == None:
                return True

            from_area = message.from_area
            if from_where == "PlayerDeckTop":
                # Yes, you can resolve White Fox’s ability if she’s the second card in a deck and discarded via Global Logistics.
                return from_area.flags.is_player_deck
                # return from_area.flags.is_player_deck and \
                #     message.discard_message.is_top
            elif from_where == "PlayerArea":
                return from_area.play_area != None
            elif from_where == "UnderIdentity":
                return from_area.flags.is_place_card_area and \
                    from_area.bind_card != None and \
                    Identity.IsType(from_area.bind_card.face)
            elif from_where == "EncounterDeckTop":
                return from_area.flags.is_encounter_deck and \
                    message.discard_message.is_top
            assert False

        def check_by_effect_face(effect: 'Effect', message: Message.AfterCardDiscard) -> bool:
            if not by_effect_face:
                return True
            return Condition.CheckWhichCard(by_effect_face, message.by_effect.this, effect)

        # def check_is_in_discard_pile(effect: 'Effect', message: Message.AfterCardDiscard) -> bool:
        #     if is_in_discard_pile == None:
        #         return True
        #     return message.trigger.card.area.IsDiscards()

        ability = Ability(
            ability_type,
            Message.AfterCardDiscard,
            [
                check_which_card,
                check_from_where,
                check_by_effect_face,
                # check_is_in_discard_pile,
                *conditions
            ],
            operation,
            is_local=which_card == "This"
        )
        if from_where in ["PlayerDeckTop", "UnderIdentity"]:
            return ability.NoOutOfPlayLimit()
        else:
            return ability


    @staticmethod
    def AfterCardDiscard(ability_type: 'AbilityType',
                         which_card: CardType,
                         operation: OperationType[Message.AfterCardDiscard],
                         *,
                         from_where: Literal["PlayerDeckTop", "PlayerArea", "UnderIdentity", "EncounterDeckTop"]|None=None,
                         control_by_player: PlayerType="AnyPlayer",
                         conditions: ConditionsType[Message.AfterCardDiscard]=[],
                        #  is_in_discard_pile: bool|None=None,
                         ) -> 'Ability':
        def check_control_by_player(effect: 'Effect', message: Message.WhenCardWouldDiscard) -> bool:
            if control_by_player == "AnyPlayer":
                return True
            if not message.trigger.card.area.flags.is_in_play:
                return False
            return Condition.CheckWhichPlayer(control_by_player, message.trigger.GetControlBy(), effect)

        def action(effect: 'Effect', message: 'Message.WhenCardWouldDiscard') -> None:
            this = effect.this

            if Condition.CheckWhichCard(which_card, message.trigger, effect) and \
                check_control_by_player(effect, message):
                ability = AbilityFactoryDiscard.AfterCardDiscardInternal(
                    ability_type,
                    message.trigger,
                    operation,
                    from_where=from_where,
                    conditions=conditions,
                )
                ability.CopyFromDelayEffect(effect)
                this.effect.RegisterTemp(
                    ability,
                    unregister_after_exec=True,
                    until_event_end=message
                )

        ability = AbilityFactoryDiscard.WhenCardWouldDiscard(
            AbilityType.DelayAbility,
            which_card,
            action,
        ).SetSecondType(ability_type)

        if from_where == "PlayerDeckTop":
            return ability.NoOutOfPlayLimit()
        else:
            return ability

