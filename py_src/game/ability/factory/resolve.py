from . import *

class AbilityFactoryResolve:

    ################################################################################
    # Special
    @staticmethod
    def WhenResolveSpecialAbility(which_card: CardType,
                                    operation: OperationType[Message.WhenResolveSpecialAbility],
                                    *,
                                    conditions: ConditionsType[Message.WhenResolveSpecialAbility]=[],
                                    ) -> 'Ability':

        def check_which_card(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> bool:
            return Condition.CheckWhichCard(which_card, message.face, effect)

        def check_ability_name(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> bool:
            if message.ability_name == "":
                return True
            return effect.ability.name == message.ability_name

        # condition, see "01049"
        return Ability(
            AbilityType.Special,
            Message.WhenResolveSpecialAbility,
            [
                check_which_card,
                check_ability_name,
                *conditions
            ],
            operation,
            is_local=which_card == "This"
        )

    @staticmethod
    def WhenCardWouldResolveAbility(ability_type: 'AbilityType',
                                which_abilities: AbilitiesType, # or
                                which_card: CardType,
                                operation: OperationType[Message.WhenEffectWouldResolve],
                                *,
                                label: 'Ability.LABEL|List[Ability.LABEL]|None'=None,
                                ability_name: str|None=None,
                                card_control_by: PlayerType="AnyPlayer",
                                trigger_player: PlayerType="AnyPlayer",
                                conditions: ConditionsType[Message.WhenEffectWouldResolve]=[],
                                ) -> 'Ability':

        def check_which_card(effect: 'Effect', message: Message.WhenEffectWouldResolve) -> bool:
            return Condition.CheckWhichCard(which_card, message.trigger, effect)

        def check_trigger_player(effect: 'Effect', message: Message.WhenEffectWouldResolve) -> bool:
            return Condition.CheckWhichPlayer(trigger_player, message.effect.initiator, effect)

        def check_card_control_by(effect: 'Effect', message: Message.WhenEffectWouldResolve) -> bool:
            return Condition.CheckWhichPlayer(card_control_by, message.control_by, effect)

        def check_ability_name(effect: 'Effect', message: Message.WhenEffectWouldResolve) -> bool:
            if ability_name == None:
                return True
            return ability_name == message.effect.ability.name

        def check_which_ability(effect: 'Effect', message: Message.WhenEffectWouldResolve) -> bool:
            ability = message.effect.ability
            if ability.flags.is_nonkeyword:
                return False
            if which_abilities == None:
                return True
            return Condition.CheckWhichAbility(which_abilities, ability)

        def check_label(effect: 'Effect', message: 'Message.WhenEffectWouldResolve') -> bool:
            if label == None:
                return True
            return message.effect.ability.IsOneOfLabel(label)

        return Ability(
            ability_type,
            Message.WhenEffectWouldResolve,
            [
                check_which_card,
                check_which_ability,
                check_ability_name,
                check_trigger_player,
                check_card_control_by,
                check_label,
                *conditions
            ],
            operation,
            is_local=which_card == "This"
        )

    @staticmethod
    def AfterCardResolveAbility(ability_type: 'AbilityType',
                                which_abilities: AbilitiesType,
                                which_card: 'CardType',
                                operation: OperationType[Message.AfterEffectResolved],
                                *,
                                conditions: ConditionsType[Message.AfterEffectResolved]=[],
                                card_control_by: PlayerType="AnyPlayer",
                                trigger_player: PlayerType="AnyPlayer",
                                which_effect: 'Effect|None'=None,
                                ability_name: str|None=None,
                                ) -> 'Ability':

        def check_which_card(effect: 'Effect', message: Message.AfterEffectResolved) -> bool:
            return Condition.CheckWhichCard(which_card, message.trigger, effect)

        def check_card_control_by(effect: 'Effect', message: Message.AfterEffectResolved) -> bool:
            return Condition.CheckWhichPlayer(card_control_by, message.control_by, effect)

        def check_trigger_player(effect: 'Effect', message: Message.AfterEffectResolved) -> bool:
            return Condition.CheckWhichPlayer(trigger_player, message.to_player, effect)

        def check_which_ability(effect: 'Effect', message: Message.AfterEffectResolved) -> bool:
            ability = message.effect.ability
            if ability.flags.is_nonkeyword:
                return False
            if which_abilities == None:
                return True
            return Condition.CheckWhichAbility(which_abilities, ability)
            # if which_abilities == None:
            #     return True
            # ability = message.effect.ability
            # for ability_type in which_abilities:
            #     if ability_type == "Interrupt" and ability.type.is_interrupt:
            #         return True
            #     if ability_type == "Response" and ability.type.is_response:
            #         return True
            #     if ability_type == "Special" and  ability.type.is_special:
            #         return True
            #     if ability_type == "Action" and  ability.type.is_special:
            #         return True
            #     if ability_type == "WhenRevealed" and ability.type.IsType(AbilityType.WhenRevealed):
            #         return True
            #     if ability_type == "HeroResource" and ability.type.IsType(AbilityType.HeroResource):
            #         return True
            # return False

        def check_ability_name(effect: 'Effect', message: Message.AfterEffectResolved) -> bool:
            if ability_name == None:
                return True
            ability = message.effect.ability
            return ability.name == ability_name

        def check_which_effect(effect: 'Effect', message: Message.AfterEffectResolved) -> bool:
            if which_effect == None:
                return True
            return message.effect == which_effect

        return Ability(
            ability_type,
            Message.AfterEffectResolved,
            [
                check_which_card,
                check_card_control_by,
                check_trigger_player,
                check_which_ability,
                check_ability_name,
                check_which_effect,
                *conditions
            ],
            operation,
            is_local=which_card == "This"
        )

    # @staticmethod
    # def AfterCardResolveAbility(ability_type: 'AbilityType',
    #                             which_abilities: AbilitiesType,
    #                             # List[Literal["Interrupt", "Response", "Special", "Action", "WhenRevealed", "HeroResource"]]|None, # or
    #                             which_card: 'CardType',
    #                             operation: OperationType[Message.AfterEffectResolved],
    #                             *,
    #                             conditions: ConditionsType[Message.AfterEffectResolved]=[],
    #                             card_control_by: PlayerType=None,
    #                             trigger_player: PlayerType=None,
    #                             which_effect: 'Effect|None'=None,
    #                             ability_name: str|None=None,
    #                             label: 'Ability.LABEL|List[Ability.LABEL]|None'=None,
    #                             ) -> 'Ability':
    #     def check_card_control_by(effect: 'Effect', message: Message.WhenEffectWouldResolve) -> bool:
    #         return Condition.CheckWhichPlayer(card_control_by, message.trigger.GetControlByOrOwner(), effect)

    #     def action(effect: 'Effect', message: 'Message.WhenEffectWouldResolve') -> None:
    #         this = effect.this

    #         if Condition.CheckWhichCard(which_card, message.trigger, effect) and \
    #             check_card_control_by(effect, message):
    #             ability = AbilityFactoryResolve.AfterCardResolveAbilityInternal(
    #                 ability_type,
    #                 which_abilities,
    #                 message.trigger,
    #                 operation,
    #                 which_effect=which_effect,
    #                 conditions=conditions,
    #             )
    #             ability.CopyFromDelayEffect(effect)
    #             this.effect.RegisterTemp(
    #                 ability,
    #                 unregister_after_exec=True,
    #                 until_event_end=message
    #             )

    #     return AbilityFactoryResolve.WhenCardWouldResolveAbility(
    #         AbilityType.DelayAbility,
    #         which_abilities,
    #         which_card,
    #         action,
    #         label=label,
    #         ability_name=ability_name,
    #         card_control_by=card_control_by,
    #         trigger_player=trigger_player,
    #     ).SetSecondType(ability_type)

    @staticmethod
    def AfterPlayerTriggerAbility(ability_type: 'AbilityType',
                                trigger_player: PlayerType,
                                which_card: CardType,
                                operation: OperationType[Message.WhenEffectWouldResolve],
                                *,
                                label: 'Ability.LABEL|List[Ability.LABEL]|None'=None,
                                card_control_by: PlayerType="AnyPlayer",
                                conditions: ConditionsType[Message.WhenEffectWouldResolve]=[],
                                ) -> 'Ability':
        def check_is_not_temp(effect: 'Effect', message: Message.WhenEffectWouldResolve) -> bool:
            return not message.effect.ability.flags.is_temp # Must use `ability.type.is_temp`, see "08031" "08001a"
        
        # This is how it design, after trigger = when resolve
        return AbilityFactoryResolve.WhenCardWouldResolveAbility(
            ability_type,
            None,
            which_card,
            operation,
            label=label,
            card_control_by=card_control_by,
            trigger_player=trigger_player,
            conditions=[
                check_is_not_temp,
                *conditions
            ]
        )

    # @staticmethod
    # def AfterPlayerResolveAbilityInternal(ability_type: 'AbilityType',
    #                                     trigger_player: Literal["You"]|None,
    #                                     which_card: CardType,
    #                                     operation: OperationType[Message.AfterEffectResolved],
    #                                     *,
    #                                     conditions: ConditionsType[Message.AfterEffectResolved]=[],
    #                                     which_abilities: AbilitiesType=None, # or
    #                                     ability_name: str|None=None,
    #                                     # control_by_you: bool|None=None,
    #                                     ) -> 'Ability':
    #     return AbilityFactoryResolve.AfterCardResolveAbility(
    #         ability_type,
    #         which_abilities,
    #         which_card,
    #         operation,
    #         trigger_player=trigger_player,
    #         ability_name=ability_name,
    #         conditions=conditions,
    #         # card_control_by="You" if control_by_you else None,
    #     )

    @staticmethod
    def AfterPlayerResolveAbility(ability_type: 'AbilityType',
                                trigger_player: Literal["You", "AnyPlayer"],
                                which_card: CardType,
                                operation: OperationType[Message.AfterEffectResolved],
                                *,
                                conditions: ConditionsType[Message.AfterEffectResolved]=[],
                                which_abilities: AbilitiesType=None, # or
                                ability_name: str|None=None,
                                control_by_you: bool|None=None,
                                ) -> 'Ability':
        return AbilityFactoryResolve.AfterCardResolveAbility(
            ability_type,
            which_abilities,
            which_card,
            operation,
            trigger_player=trigger_player,
            ability_name=ability_name,
            conditions=conditions,
            card_control_by="You" if control_by_you else "AnyPlayer",
        )

    @staticmethod
    def WhenSurgeWouldBeResolved(ability_type: 'AbilityType',
                                 which_card: CardType, # Surge on which card
                                 operation: OperationType[Message.WhenSurgeWouldBeResolved],
                                 *,
                                 conditions: ConditionsType[Message.WhenSurgeWouldBeResolved]=[],
                                 ) -> 'Ability':

        def check_which_card(effect: 'Effect', message: Message.WhenSurgeWouldBeResolved) -> bool:
            return Condition.CheckWhichCard(which_card, message.trigger, effect)

        return Ability(
            ability_type,
            Message.WhenSurgeWouldBeResolved,
            [
                check_which_card,
                *conditions
            ],
            operation,
            is_local=which_card == "This"
        )

