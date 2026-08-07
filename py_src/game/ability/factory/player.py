from . import *

class AbilityFactoryPlayer:

    ################################################################################
    # Cannot
    @staticmethod
    def PlayersCannotPlayCardWhile(which_player: 'PlayerType|Player',
                                    which_card: CardType=None,
                                    *,
                                    conditions: ConditionsType[Message.CheckEffectCondition]=[],
                                    # on_while: Literal["AttachThis"]|None=None,
                                   ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: 'Message.CheckEffectCondition') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.check_effect.initiator, effect)

        def check_which_card(effect: 'Effect', message: 'Message.CheckEffectCondition') -> bool:
            return Condition.CheckWhichCard(which_card, message.check_effect.this, effect)

        # def check_on_while(effect: 'Effect', message: 'Send.CheckEffectCondition') -> bool:
        #     if on_while == "AttachThis":
        #         if message.check_effect.initiator:
        #             return Condition.ThisAttachedTo(effect, message.check_effect.initiator.GetRoleCharacter(), True)
        #         else:
        #             return False
        #     if on_while == None:
        #         return True

        return Ability(
            AbilityType.NonKeyword,
            Message.CheckEffectCondition,
            [
                lambda effect, message:
                    # effect.this.CastTo(Obligation).GetGiveToPlayer() == message.for_effect.initiator and \
                    message.check_effect.ability.is_play,
                check_which_player,
                check_which_card,
                # check_on_while,
                *conditions,
            ],
            lambda effect, message:
                message.SetEffectInvalid(effect),
            is_local=which_card == "This"
        )

    @staticmethod
    def PlayersCannotMakeBasicAttack(which_player: PlayerType) -> 'Ability':
        from game.card.face.card_type import Identity

        def check_is_basic_atk(effect: 'Effect', message: 'Message.CheckEffectCondition') -> bool:
            ability = message.check_effect.ability
            return ability.flags.ability_type.flags.is_basic_power and \
                "ATK" in ability.func_names

        def check_which_player(effect: 'Effect', message: 'Message.CheckEffectCondition') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.check_effect.initiator, effect)

        return Ability(
            AbilityType.NonKeyword,
            Message.CheckEffectCondition,
            [
                lambda effect, message:
                    Identity.IsType(message.check_effect.this),
                check_is_basic_atk,
                check_which_player,
            ],
            lambda effect, message:
                message.SetEffectInvalid(effect)
        )

    @staticmethod
    def PlayersCannotChangeAdditionalForms(which_player: PlayerType,
                                           exclude: CardType=None,
                                           ) -> 'Ability':
        def check_which_player(effect: 'Effect', message: 'Message.WhenUnitWouldChangeForm') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.trigger.GetControlBy(), effect)
        
        def check_exclude(effect: 'Effect', message: 'Message.WhenUnitWouldChangeForm') -> bool:
            if exclude == None:
                return True
            return not Condition.CheckWhichCard(exclude, message.by_effect.this, effect)

        return Ability(
            AbilityType.NonKeyword,
            Message.WhenUnitWouldChangeForm,
            [
                check_which_player,
                check_exclude,
                lambda effect, message:
                    message.is_change_additional_form
            ],
            lambda effect, message:
                message.SetBeInstead(effect)
        )

    @staticmethod
    def PlayersCannotChangeForms(ability_type: 'AbilityType', # Don't remove this, AbilityType.NonKeyword
                                 which_player: PlayerType,
                                 *,
                                 from_form: Type['Hero']|Type['AlterEgo']|None=None,
                                 to_form: Type['Hero']|Type['AlterEgo']|None=None,
                                 conditions: ConditionsType[Message.WhenUnitWouldChangeForm]=[],
                                 ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: 'Message.WhenUnitWouldChangeForm') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.trigger.GetControlBy(), effect)

        def check_from_form(effect: 'Effect', message: 'Message.WhenUnitWouldChangeForm') -> bool:
            if from_form == None:
                return True
            return from_form.IsType(message.from_form)

        def check_to_form(effect: 'Effect', message: 'Message.WhenUnitWouldChangeForm') -> bool:
            if to_form == None:
                return True
            return to_form.IsType(message.to_form)

        return Ability(
            ability_type,
            Message.WhenUnitWouldChangeForm,
            [
                lambda effect, message:
                    # Fix "36030"
                    effect.this != message.by_effect.this,
                check_which_player,
                check_from_form,
                check_to_form,
                *conditions
            ],
            lambda effect, message:
                message.SetBeInstead(effect)
        )

    @staticmethod
    def PlayersCannotTriggerAbility(which_player: PlayerType,
                                    which_ability: AbilitiesType,
                                    *,
                                    label: 'Ability.LABEL|None'=None,
                                    on_which_card: CardType=None,
                                    during: Literal["YourTurn"]|None=None,
                                    conditions: ConditionsType[Message.CheckEffectCondition]=[],
                                    ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: 'Message.CheckEffectCondition') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.check_effect.initiator, effect)
            # if which_player == "EngagedPlayer":
            #     return effect.this.CastTo(Minion).GetEngagedPlayer().GetIdentity() == message.by_face
            # if which_player == "Attached":
            #     return Condition.ThisAttachedTo(effect, message.by_face, True)
            # if which_player == "You":
    
        def check_which_ability(effect: 'Effect', message: 'Message.CheckEffectCondition') -> bool:
            ability = message.check_effect.ability
            if ability.flags.is_forced:
                return False
            return Condition.CheckWhichAbility(which_ability, ability)

        def check_label(effect: 'Effect', message: 'Message.CheckEffectCondition') -> bool:
            if label == None:
                return True
            ability = message.check_effect.ability
            return ability.IsLabel(label)

        def check_on_which_card(effect: 'Effect', message: 'Message.CheckEffectCondition') -> bool:
            return Condition.CheckWhichCard(on_which_card, message.check_effect.this, effect)

        def check_during(effect: 'Effect', message: 'Message.CheckEffectCondition') -> bool:
            if during == None:
                return True
            if during == "YourTurn":
                player = effect.world.GetCurrentPlayer()
                return Condition.ThisIsYou(effect, player)

        return Ability(
            AbilityType.NonKeyword,
            Message.CheckEffectCondition,
            [
                check_which_player,
                check_which_ability,
                check_label,
                check_on_which_card,
                check_during,
                *conditions
            ],
            lambda effect, message:
                message.SetEffectInvalid(effect)
        )

    @staticmethod
    def PlayersCannotReadyCardWhile(which_player: PlayerType,
                                    ready_what: CardType,
                                    control_by: PlayerType,
                                    *,
                                    by_effect_face: 'CardFinder|None'=None) -> List['Ability']:
        from game.ability.factory import AbilityFactory

        def check_which_player(effect: 'Effect', message: 'Message.WhenCardWouldReady|Message.CheckIfFaceCanBeReadied') -> bool:
            player= message.by_effect.initiator
            return Condition.CheckWhichPlayer(which_player, player, effect)

        return AbilityFactory.CardCannotReady(
            ready_what,
            control_by=control_by,
            by_effect_face=by_effect_face,
            conditions=[
                check_which_player
            ]
        )

    @staticmethod
    def PlayersCannotThwartWhile(which_player: PlayerType,
                                 which_scheme: CardType,
                                 *,
                                 conditions: ConditionsType[Message.CheckIfSchemeCanBeThwartBy]=[],
                                 ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: 'Message.CheckIfSchemeCanBeThwartBy') -> bool:
            return Condition.CheckWhichPlayerIncludeAllies(which_player, message, effect)

        def check_which_scheme(effect: 'Effect', message: 'Message.CheckIfSchemeCanBeThwartBy') -> bool:
            return Condition.CheckWhichCard(which_scheme, message.scheme, effect)

        return Ability(
            AbilityType.NonKeyword,
            Message.CheckIfSchemeCanBeThwartBy,
            [
                check_which_player,
                check_which_scheme,
                *conditions
            ],
            lambda effect, message:
                message.SetCannotBeThwart(effect)
        )

    @staticmethod
    def PlayersCannotAttackWhile(which_player: PlayerType,
                                 which_target: CardType,
                                 *,
                                 conditions: ConditionsType[Message.CheckIfUnitCanBeAttackBy]=[],
                                 ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: 'Message.CheckIfUnitCanBeAttackBy') -> bool:
            return Condition.CheckWhichPlayerIncludeAllies(which_player, message, effect)

        def check_which_target(effect: 'Effect', message: 'Message.CheckIfUnitCanBeAttackBy') -> bool:
            return Condition.CheckWhichCard(which_target, message.being_attack, effect)

        return Ability(
            AbilityType.NonKeyword,
            Message.CheckIfUnitCanBeAttackBy,
            [
                check_which_player,
                check_which_target,
                *conditions
            ],
            lambda effect, message:
                message.SetCannotBeAttack(effect)
        )

    @staticmethod
    def PlayersCannotRemoveThreatFrom(which_player: PlayerType,
                                      which_scheme: CardType,
                                      *,
                                      conditions: ConditionsType[Message.WhenSchemeWouldRemoveThreat]=[],
                                      ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: 'Message.WhenSchemeWouldRemoveThreat') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.by_effect.initiator, effect)

        def check_which_scheme(effect: 'Effect', message: 'Message.WhenSchemeWouldRemoveThreat') -> bool:
            return Condition.CheckWhichCard(which_scheme, message.trigger, effect)

        def cannot_be_removed(effect: 'Effect', message: 'Message.WhenSchemeWouldRemoveThreat') -> None:
            message.SetCannotBeRemoved(effect)

        return Ability(
            AbilityType.NonKeyword,
            Message.WhenSchemeWouldRemoveThreat,
            [
                check_which_scheme,
                check_which_player,
                *conditions
            ],
            cannot_be_removed
        )

    @staticmethod
    def PlayersCannotDamageUnit(which_player: PlayerType,
                                which_unit: CardType,
                                *,
                                unit_engaged_player: PlayerType="AnyPlayer",
                                conditions: ConditionsType[Message.WhenUnitWouldTakeDamage]=[],
                                ) -> 'Ability':
        from game.card.face.card_type import Minion

        def check_which_player(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> bool:
            return Condition.CheckWhichPlayer(which_player, message.by_effect.initiator, effect)

        def check_which_unit(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> bool:
            return Condition.CheckWhichCard(which_unit, message.trigger, effect)

        def check_unit_engaged_player(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> bool:
            if not Minion.IsType(message.trigger):
                return True
            return Condition.CheckWhichPlayer(unit_engaged_player, message.trigger.GetEngagedPlayer(), effect)

        def prevent_all_damage(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
            message.PreventDamage("All", effect)

        return Ability(
            AbilityType.NonKeyword,
            Message.WhenUnitWouldTakeDamage,
            [
                check_which_player,
                check_which_unit,
                check_unit_engaged_player,
                *conditions
            ],
            prevent_all_damage
        )

    @staticmethod
    def CardsEnterPlayExhausted(which_card: CardType,
                                which_player: "PlayerType|None"=None):
        from game.ability.factory import AbilityFactory
        from game.operate.faces import Faces
        def exhaust(effect: 'Effect', message: 'Message.WhenCardEnterPlay'):
            Faces.ExhaustAll([message.trigger], effect)

        def check_which_player(effect: 'Effect', message: 'Message.WhenCardEnterPlay') -> bool:
            if which_player == None:
                return True
            return Condition.CheckWhichPlayer(which_player, message.trigger.GetControlBy(), effect)

        return AbilityFactory.WhenCardEnterPlay(
            AbilityType.NonKeyword,
            which_card,
            exhaust,
            conditions=[check_which_player]
        )

    ################################################################################
    # Player
    @staticmethod
    def WhenPlayerWouldDrawCard(ability_type: 'AbilityType',
                                which_player: PlayerType,
                                which_card: CardType,
                                operation: OperationType[Message.WhenPlayerWouldDrawCard],
                                *,
                                conditions: ConditionsType[Message.WhenPlayerWouldDrawCard]=[],
                                ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: Message.WhenPlayerWouldDrawCard) -> bool:
            return Condition.CheckWhichPlayer(which_player, message.to_player, effect)

        def check_which_card(effect: 'Effect', message: Message.WhenPlayerWouldDrawCard) -> bool:
            return Condition.CheckWhichCard(which_card, message.trigger, effect)

        return Ability(
            ability_type,
            Message.WhenPlayerWouldDrawCard,
            [
                check_which_player,
                check_which_card,
                *conditions
            ],
            operation
        )

    @staticmethod
    def AfterPlayerDrewCards(ability_type: 'AbilityType',
                            which_player: PlayerType,
                            which_card: CardType,
                            operation: OperationType[Message.AfterPlayerDrewCards],
                            *,
                            conditions: ConditionsType[Message.AfterPlayerDrewCards]=[],
                            ) -> 'Ability':

        def check_which_player(effect: 'Effect', message: Message.AfterPlayerDrewCards) -> bool:
            return Condition.CheckWhichPlayer(which_player, message.to_player, effect)

        def check_which_card(effect: 'Effect', message: Message.AfterPlayerDrewCards) -> bool:
            return Condition.CheckWhichCard(which_card, message.drew_faces, effect)

        return Ability(
            ability_type,
            Message.AfterPlayerDrewCards,
            [
                check_which_player,
                check_which_card,
                *conditions
            ],
            operation
        )

    @staticmethod
    def WhenPlayerWouldBeDealtEncounterCard(ability_type: 'AbilityType',
                                            operation: OperationType[Message.WhenPlayerWouldBeDealtEncounterCard],
                                            *,
                                            conditions: ConditionsType[Message.WhenPlayerWouldBeDealtEncounterCard]=[],
                                            ) -> 'Ability':

        return Ability(
            ability_type,
            Message.WhenPlayerWouldBeDealtEncounterCard,
            [
                *conditions
            ],
            operation
        )

    @staticmethod
    def AfterPlayerDealEncounterCard(ability_type: 'AbilityType',
                                    operation: OperationType[Message.AfterPlayerDealEncounterCard],
                                    *,
                                    conditions: ConditionsType[Message.AfterPlayerDealEncounterCard]=[],
                                    ) -> 'Ability':

        return Ability(
            ability_type,
            Message.AfterPlayerDealEncounterCard,
            [
                *conditions
            ],
            operation
        )

    ################################################################################
    # First Player
    @staticmethod
    def FirstPlayerControlThis() -> 'Ability':
        from game.ability.factory import AbilityFactory

        def when_this_valid(effect: 'Effect', message: 'Message.AfterCardsMoved|Message.AfterPlayersOrderChange') -> None:
            this = effect.this
            first_player = effect.world.GetFirstPlayer()
            this.PutIntoPlay(first_player, effect, under_control=True)

        return AbilityFactory.WhenEvent(
            Message.AfterCardsMoved|Message.AfterPlayersOrderChange,
            lambda effect, last_value:
                not effect.this.card.area.flags.is_place_card_area and \
                not effect.this.card.area.flags.is_upgrades and \
                effect.this.IsInPlay() and \
                effect.this.GetControlBy() != effect.world.GetFirstPlayer(),
            when_this_valid
        )

