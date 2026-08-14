from . import *

class AbilityFactoryDefeated:

    @staticmethod
    def WhenUnitWouldBeDefeated(ability_type: 'AbilityType',
                                which_unit: CardType|Literal["You"],
                                operation: OperationType[Message.WhenUnitWouldBeDefeated],
                                *,
                                by_consequential_damage: bool|None=None,
                                # by_overkill: bool|None=None,
                                conditions: ConditionsType[Message.WhenUnitWouldBeDefeated]=[],
                                ) -> 'Ability':

        def check_which_unit(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> bool:
            check_unit = Condition.GetYouRule(which_unit, identity=True)
            return Condition.CheckWhichCard(check_unit, message.trigger, effect)

        def check_by_consequential_damage(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> bool:
            if by_consequential_damage == None:
                return True
            return by_consequential_damage == message.IsByConsequentialDamage()

        # def check_by_overkill(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> bool:
        #     if by_overkill == None:
        #         return True
        #     return by_overkill == message

        return Ability(
            ability_type,
            Message.WhenUnitWouldBeDefeated,
            [
                check_which_unit,
                check_by_consequential_damage,
                # check_by_overkill,
                *conditions
            ],
            operation,
            is_local=which_unit == "This"
        )

    @staticmethod
    def WhenUnitBeDefeatedInternal(ability_type: 'AbilityType',
                                    which_unit: CardType,
                                    operation: OperationType[Message.WhenUnitBeDefeated],
                                    *,
                                    has_defeating_player: bool|None=None,
                                    by_attack: Message.WhenUnitWouldAttack|Message.WhenUnitBeingAttack|bool|None=None,
                                    by_source: CardType=None,
                                    by_consequential: bool|None=None,
                                    took_excess_damage: bool|None=None,
                                    conditions: ConditionsType[Message.WhenUnitBeDefeated]=[],
                                    ) -> 'Ability':

        def check_no_ignore_when_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> bool:
            if not effect.ability.type.flags.is_when_defeated:
                return True
            return not message.ignore_when_defeated

        def check_which_unit(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> bool:
            return Condition.CheckWhichCard(which_unit, message.trigger, effect)

        def check_has_defeated_player(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> bool:
            if has_defeating_player == None:
                return True
            return has_defeating_player == (message.defeating_player != None)

        def check_by_attack(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> bool:
            if by_attack == None:
                return True
            if isinstance(by_attack, Message.WhenUnitWouldAttack):
                return message.would_atk_message == by_attack
            if isinstance(by_attack, Message.WhenUnitBeingAttack):
                return message.being_atk_message == by_attack
            if by_attack:
                return message.being_atk_message != None
            else:
                return message.being_atk_message == None

        def check_by_source(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> bool:
            return Condition.CheckWhichCard(by_source, message.killer, effect)

        def check_by_onsequential(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> bool:
            if by_consequential == None:
                return True
            return message.IsByConsequential() == by_consequential

        def check_excess_damage_more_than_zero(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> bool:
            if took_excess_damage == None:
                return True
            return took_excess_damage == bool(message.excess_damage > 0)

        return Ability(
            ability_type,
            Message.WhenUnitBeDefeated,
            [
                check_no_ignore_when_defeated,
                check_which_unit,
                check_has_defeated_player,
                check_by_attack,
                check_by_source,
                check_by_onsequential,
                check_excess_damage_more_than_zero,
                *conditions
            ],
            operation,
            is_local=which_unit == "This" or by_source == "This"
        )

    @staticmethod
    def WhenUnitBeDefeated(ability_type: 'AbilityType',
                           which_unit: CardType,
                           operation: OperationType[Message.WhenUnitBeDefeated],
                           *,
                           has_defeating_player: bool|None=None,
                           by_attack: Message.WhenUnitWouldAttack|bool|None=None,
                           attacker: CardType=None,
                           by_consequential: bool|None=None,
                           took_excess_damage: bool|None=None,
                           without_excess_damage: bool|None=None,
                           conditions: ConditionsType[Message.WhenUnitBeDefeated]=[],
                           ) -> 'Ability':

        if took_excess_damage == None and without_excess_damage != None:
            if not without_excess_damage:
                took_excess_damage = True
            else:
                took_excess_damage = False

        # Hack fix
        if ability_type in [AbilityType.Rule, AbilityType.Temp0, AbilityType.Scenario, AbilityType.Campaign, AbilityType.Challenge, AbilityType.NonKeyword] or \
            which_unit == "This":
            return AbilityFactoryDefeated.WhenUnitBeDefeatedInternal(
                ability_type,
                which_unit,
                operation,
                has_defeating_player=has_defeating_player,
                by_attack=by_attack,
                by_source=attacker,
                by_consequential=by_consequential,
                took_excess_damage=took_excess_damage,
                conditions=conditions,
            )

        def action(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
            this = effect.this

            if Condition.CheckWhichCard(which_unit, message.trigger, effect):
                ability = AbilityFactoryDefeated.WhenUnitBeDefeatedInternal(
                    ability_type,
                    message.trigger,
                    operation,
                    by_attack=by_attack,
                    by_source=attacker,
                    by_consequential=by_consequential,
                    took_excess_damage=took_excess_damage,
                    conditions=conditions,
                )
                ability.CopyFromDelayEffect(effect)
                this.effect.RegisterTemp(
                    ability,
                    unregister_after_exec=True,
                    until_event_end=message
                )

        return AbilityFactoryDefeated.WhenUnitWouldBeDefeated(
            AbilityType.DelayAbility,
            which_unit,
            action,
            by_consequential_damage=by_consequential,
        ).SetSecondType(ability_type)

    @staticmethod
    def WhenUnitAttackAndDefeatUnit(ability_type: 'AbilityType',
                                    attacker: CardType|Literal["You"],
                                    who_be_defeated: CardType,
                                    operation: OperationType[Message.WhenUnitBeDefeated],
                                    *,
                                    conditions: ConditionsType[Message.WhenUnitBeDefeated]=[],
                                    ) -> 'Ability':
        def check_attacker(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> bool:
            rule = Condition.GetYouRule(attacker, identity=True)
            return Condition.CheckWhichCard(rule, message.attacker, effect)

        def check_who_be_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> bool:
            return Condition.CheckWhichCard(who_be_defeated, message.trigger, effect)

        def check_is_from_attack(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> bool:
            return message.IsFromAttack()

        return Ability(
            ability_type,
            Message.WhenUnitBeDefeated,
            [
                check_attacker,
                check_who_be_defeated,
                check_is_from_attack,
                *conditions,
            ],
            operation,
            is_local=attacker == "This" or who_be_defeated == "This"
        )

    @staticmethod
    def AfterUnitAttackAndDefeatUnit(ability_type: 'AbilityType',
                                    attacker: CardType|Literal["You"],
                                    who_be_defeated: CardType,
                                    operation: OperationType[Message.AfterUnitDefeatedUnit],
                                    *,
                                    # this_attach_to: Send.WhenUnitWouldAttack|bool|None=None,
                                    has_excess_damage: bool|None=None,
                                    conditions: ConditionsType[Message.AfterUnitDefeatedUnit]=[],
                                    ) -> 'Ability':
        return AbilityFactoryDefeated.AfterUnitDefeatedUnit(
            ability_type,
            attacker,
            who_be_defeated,
            operation,
            is_from_attack=True,
            has_excess_damage=has_excess_damage,
            conditions=conditions
        )

    @staticmethod
    def AfterUnitDefeatedUnitInternal(ability_type: 'AbilityType',
                                    who_killer: CardType,
                                    who_be_defeated: CardType,
                                    operation: OperationType[Message.AfterUnitDefeatedUnit],
                                    *,
                                    would_message: 'Message.WhenUnitWouldAttackUnit|Message.WhenFaceWouldDealDamage|None'=None,
                                    is_from_attack: bool|None=None,
                                    has_excess_damage: bool|None=None,
                                    conditions: ConditionsType[Message.AfterUnitDefeatedUnit]=[],
                                    ) -> 'Ability':
        # from game.card.face.base import Unit2
        # from game.card.face.card_type import Villain

        def check_who_killer(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> bool:
            return Condition.CheckWhichCard(who_killer, message.killer, effect)

        def check_who_be_defeated(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> bool:
            return Condition.CheckWhichCard(who_be_defeated, message.target, effect)

        def check_would_message(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> bool:
            if would_message == None:
                return True
            return would_message.would_atk_message == message.would_atk_message

        def check_is_from_attack(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> bool:
            if is_from_attack == None:
                return True
            return is_from_attack == message.IsFromAttack()
            # if is_from_attack:
            #     if message.would_atk_message == None:
            #         return False
            #     if who_killer == "This":
            #         return effect.this == message.would_atk_message.attacker
            #     if who_killer == "You" or who_killer == "YourHero":
            #         return Condition.ThisIsYou(effect, message.would_atk_message.attacker)
            #     return True
            # else:
            #     return message.would_atk_message == None

        def check_has_excess_damage(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> bool:
            if has_excess_damage == None:
                return True
            return has_excess_damage == bool(message.excess_damage > 0)

        return Ability(
            ability_type,
            Message.AfterUnitDefeatedUnit,
            [
                check_who_killer,
                check_would_message,
                check_who_be_defeated,
                check_is_from_attack,
                check_has_excess_damage,
                *conditions
            ],
            operation,
            is_local=who_killer == "This" or who_be_defeated == "This"
        )

    @staticmethod
    def AfterUnitDefeatedUnit(ability_type: 'AbilityType',
                            who_killer: CardType|Literal["You"],
                            who_be_defeated: CardType,
                            operation: OperationType[Message.AfterUnitDefeatedUnit],
                            *,
                            is_from_attack: bool|None=None,
                            is_basic_attack: bool|None=None,
                            has_excess_damage: bool|None=None,
                            # card_finder: 'CardFinder|None'=None,
                            conditions: ConditionsType[Message.AfterUnitDefeatedUnit]=[],
                            ) -> 'Ability':
        from game.ability.factory import AbilityFactory
        def action(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit|Message.WhenFaceWouldDealDamage') -> None:
            this = effect.this
            target = message.target

            if Condition.CheckWhichCard(who_be_defeated, target, effect):
                ability = AbilityFactoryDefeated.AfterUnitDefeatedUnitInternal(
                    ability_type,
                    message.attacker,
                    target,
                    operation,
                    would_message=message,
                    has_excess_damage=has_excess_damage,
                    conditions=conditions,
                )
                # Fix ""46014"
                if effect.this in target.GetInventoryDeck().Get():
                    ability.NoOutOfPlayLimit()

                ability.CopyFromDelayEffect(effect)
                this.effect.RegisterTemp(
                    ability,
                    unregister_after_exec=True,
                    until_event_end=message
                )

        if is_from_attack == True or is_basic_attack == True:
            return AbilityFactory.WhenUnitWouldAttackUnit(
                AbilityType.DelayAbility,
                who_killer,
                None,
                action,
                is_basic_attack=is_basic_attack,
            ).SetSecondType(ability_type)
        else:
            return AbilityFactory.WhenFaceWouldDealDamage(
                AbilityType.DelayAbility,
                who_killer,
                action,
                include_overkill=True
            ).SetSecondType(ability_type)

    @staticmethod
    def AfterUnitBeDefeatedInternal(ability_type: 'AbilityType',
                                    who_be_defeated: CardType,
                                    operation: OperationType[Message.AfterUnitBeDefeated],
                                    *,
                                    attacker: CardType=None,
                                    by_attack: bool|None=None,
                                    by_consequential: bool|None=None,
                                    conditions: ConditionsType[Message.AfterUnitBeDefeated]=[],
                                    ) -> 'Ability':
        from game.card.face.card_face import CardFace

        def check_who_be_defeated(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> bool:
            if isinstance(who_be_defeated, CardFace):
                return who_be_defeated == message.trigger
            return Condition.CheckWhichCard(who_be_defeated, message.trigger, effect)

        def check_by_consequential(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> bool:
            if by_consequential == None:
                return True
            return message.IsByConsequential() == by_consequential

        def check_attacker(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> bool:
            if attacker == None:
                return True
            return Condition.CheckWhichCard(attacker, message.attacker, effect)

        def check_by_attack(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> bool:
            if by_attack == None:
                return True
            # return by_attack == message.by_effect.ability.is_like_attack
            return by_attack == (message.would_atk_message != None)

        return Ability(
            ability_type,
            Message.AfterUnitBeDefeated,
            [
                check_who_be_defeated,
                check_by_consequential,
                check_attacker,
                check_by_attack,
                *conditions
            ],
            operation,
            is_local=who_be_defeated == "This" or attacker == "This"
        )

    @staticmethod
    def AfterUnitBeDefeated(ability_type: 'AbilityType',
                            who_be_defeated: CardType,
                            operation: OperationType[Message.AfterUnitBeDefeated],
                            *,
                            attacker: CardType=None,
                            by_attack: bool|None=None,
                            by_consequential: bool|None=None,
                            conditions: ConditionsType[Message.AfterUnitBeDefeated]=[],
                            ) -> 'Ability':
        if not (isinstance(who_be_defeated, tuple) and who_be_defeated[0] == "YouControl") and \
            who_be_defeated != "YouControlAlly":
        # if who_be_defeated != "YourAlly": # Fix "08031"
            return AbilityFactoryDefeated.AfterUnitBeDefeatedInternal(
                ability_type,
                who_be_defeated,
                operation,
                attacker=attacker,
                by_attack=by_attack,
                by_consequential=by_consequential,
                conditions=conditions,
            )

        def action(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
            this = effect.this

            if Condition.CheckWhichCard(who_be_defeated, message.trigger, effect):
                ability = AbilityFactoryDefeated.AfterUnitBeDefeatedInternal(
                    ability_type,
                    message.trigger,
                    operation,
                    attacker=attacker,
                    by_attack=by_attack,
                    by_consequential=by_consequential,
                    conditions=conditions,
                )
                ability.CopyFromDelayEffect(effect)
                this.effect.RegisterTemp(
                    ability,
                    unregister_after_exec=True,
                    until_event_end=message
                )

        return AbilityFactoryDefeated.WhenUnitWouldBeDefeated(
            AbilityType.DelayAbility,
            who_be_defeated,
            action,
            by_consequential_damage=by_consequential,
        ).SetSecondType(ability_type)

    @staticmethod
    def WhenSchemeBeDefeated(ability_type: 'AbilityType',
                             which_scheme: CardType,
                             operation: OperationType[Message.WhenSchemeBeDefeated],
                             *,
                             has_defeating_player: bool|None=None,
                             conditions: ConditionsType[Message.WhenSchemeBeDefeated]=[],
                             ) -> 'Ability':

        def check_no_ignore_when_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> bool:
            if not effect.ability.type.flags.is_when_defeated:
                return True
            return not message.ignore_when_defeated

        def check_which_scheme(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> bool:
            return Condition.CheckWhichCard(which_scheme, message.trigger, effect)

        def check_has_defeated_player(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> bool:
            if has_defeating_player == None:
                return True
            return has_defeating_player == (message.defeating_player != None)

        return Ability(
            # Don't try to use "AbilityType.WhenDefeated", see "11033"
            ability_type,
            Message.WhenSchemeBeDefeated,
            [
                check_no_ignore_when_defeated,
                check_which_scheme,
                check_has_defeated_player,
                *conditions
            ],
            operation,
            is_local=which_scheme == "This"
        )

    ################################################################################
    # Cannot Be Defeated
    @staticmethod
    def CardCannotLeavePlayWhile(ability_type: 'AbilityType',
                                 which_card: CardType,
                                 *,
                                 while_card_is_in_play: CardType=None,
                                 unless_card_in_victory_display: Tuple[int|Literal["1*"], CardType]|None=None,
                                 conditions: ConditionsType[Message.WhenSchemeWouldBeDefeated]|ConditionsType[Message.WhenUnitWouldBeDefeated]=[]
                                 ) -> 'Ability':
        from game.operate.worlds import Worlds

        def cannot_leave_play(effect: 'Effect', message: 'Message.WhenSchemeWouldBeDefeated|Message.WhenUnitWouldBeDefeated') -> None:
            this = effect.this
            Unused(this)
            message.SetBeInstead(effect)

        def check_which_card(effect: 'Effect', message: 'Message.WhenSchemeWouldBeDefeated|Message.WhenUnitWouldBeDefeated') -> bool:
            return Condition.CheckWhichCard(which_card, message.trigger, effect)

        def check_while_card_is_in_play(effect: 'Effect', message: 'Message.WhenSchemeWouldBeDefeated|Message.WhenUnitWouldBeDefeated') -> bool:
            return Condition.CheckWhichCard(while_card_is_in_play, Worlds.GetOnFieldCards(effect), effect)

        def check_unless_card_in_victory_display(effect: 'Effect', message: 'Message.WhenSchemeWouldBeDefeated|Message.WhenUnitWouldBeDefeated') -> bool:
            if not unless_card_in_victory_display:
                return True
            size, rule = unless_card_in_victory_display
            return not Condition.CheckWhichCard(rule, Worlds.VictoryDisplay(effect).GetAll(), effect, size=size)

        def check_condition(effect: 'Effect', message: 'Message.WhenSchemeWouldBeDefeated|Message.WhenUnitWouldBeDefeated') -> bool:
            for condition in conditions:
                if not condition(effect, Cast(Any, message)):
                    return False
            return True

        return Ability(
            ability_type,
            Message.WhenSchemeWouldBeDefeated|Message.WhenUnitWouldBeDefeated,
            [
                check_which_card,
                check_while_card_is_in_play,
                check_unless_card_in_victory_display,
                check_condition
            ],
            cannot_leave_play,
            is_local=which_card == "This"
        )

    @staticmethod
    def UnitCannotBeDefeatedWhile(ability_type: 'AbilityType',
                                  which_card: 'CardFinder|Literal["This"]',
                                  *,
                                  unless_card_in_victory_display: Tuple[int|Literal["1*"], CardType]|None=None,
                                  while_card_is_in_play: CardType=None,
                                  condition: Callable[['Effect'], bool]|None=None,
                                  change_on_event: 'EventType'=OnEvent.NONE) -> List['Ability']:
        from game.ability.factory import AbilityFactory
        from game.card.face.base import Unit2
        from game.operate.worlds import Worlds

        if condition == None:
            condition = lambda effect: True

        def while_this_be_defeated(effect: 'Effect', message: 'Message2') -> None:
            this = effect.this
            Unused(this)

            if which_card == "This":
                target = effect.this.CastTo(Unit2)
            else:
                target = Worlds.FindCardOnField(
                    effect,
                    which_card,
                    card_type=Unit2
                )

            if target and target.health <= 0:
                target.Defeated(None, effect)

        abilities = [
            AbilityFactoryDefeated.CardCannotLeavePlayWhile(
                ability_type,
                which_card,
                while_card_is_in_play=while_card_is_in_play,
                unless_card_in_victory_display=unless_card_in_victory_display,
                conditions=[
                    lambda effect, message:
                        condition(effect)
                ],
            ),
        ]

        if unless_card_in_victory_display and change_on_event == OnEvent.NONE:
            _, rule = unless_card_in_victory_display
            change_on_event = OnEvent.Victory(rule)

        if change_on_event != OnEvent.NONE:
            def new_condition(effect: 'Effect', message: 'Message2') -> bool:
                # found = None
                # for face in Worlds.GetOnFieldCards(effect):
                #     if Condition.CheckWhichCard(which_card, face, effect):
                #         found = face
                #         break
                # if found == None:
                #     return False
                # if found.CastTo(Unit2).health > 0:
                #     return False
                if not condition(effect):
                    return False
                if while_card_is_in_play == None:
                    return True
                return not Condition.CheckWhichCard(while_card_is_in_play, Worlds.GetOnFieldCards(effect), effect)

            abilities += Condition.GetEventAbilities(
                change_on_event,
                AbilityType.NonKeyword,
                new_condition,
                while_this_be_defeated
            )

        if which_card != "This":
            abilities.append(
                AbilityFactory.AfterCardLeavePlay(
                    AbilityType.Temp0,
                    "This",
                    while_this_be_defeated,
                ),
            )

        return abilities

    @staticmethod
    def SchemeCannotLeavlPlayWhile(which_card: CardType,
                                *,
                                while_card_is_in_play: CardType=None,
                                conditions: ConditionsType[Message.WhenSchemeWouldBeDefeated]=[]
                                ) -> 'Ability':
       return AbilityFactoryDefeated.CardCannotLeavePlayWhile(
            AbilityType.NonKeyword,
            which_card,
            while_card_is_in_play=while_card_is_in_play,
            conditions=conditions,
        )

