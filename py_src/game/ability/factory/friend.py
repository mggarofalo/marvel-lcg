from . import *

class AbilityFactoryFriend:

    ################################################################################
    # Hero
    @staticmethod
    def ThisCanRecover(*,
                       conditions: ConditionsType[Message.WhenPlayerInTurn]=[]
                       ) -> 'Ability':
        from game.ability.cost_func import CostFunc
        # from game.selector import Select
        from game.ability.factory import AbilityFactory

        def invoke_recovery(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
            from game.card.face.attribute.can_recover import CanRecover
            this = effect.this.CastTo(CanRecover)
            this.BasicRecover(effect)

        def this_can_recover(effect: 'Effect', message: 'Message2') -> bool:
            from game.card.face.attribute.can_recover import CanRecover
            return CanRecover.IsType(effect.this.card.face) and effect.this.card.face.IsCanRecover(effect)

        return AbilityFactory.WhenInYourPlayTurn(
            AbilityType.BasicPower,
            invoke_recovery,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().CanHeath(),
                this_can_recover,
                *conditions,
            ]
        ).SetFuncName("REC").SetCostFunc(CostFunc.Exhaust("This"))
        # .SetTarget2(Select.YourIdentity).TargetOnly(canbe_heal=True)

    ################################################################################
    # Ally and Hero
    @staticmethod
    def ThisCanAttack(*,
                      conditions: ConditionsType[Message.WhenPlayerInTurn]|Literal[False]=[],
                      canbe_divided: bool=False,
                      divided_evenly: bool=False) -> 'Ability':
        from game.card.face.attribute.can_attack import CanAttack, HasAttack
        from game.ability.factory import AbilityFactory
        from game.card.face.attribute.can_attack import AttackProperty
        from game.ability.cost_func import CostFunc
        from game.card.face.base import Unit2
        from game.card.face.base import Enemy

        def make_attack(effect: 'Effect', message: 'Message2') -> Any:
            this = effect.this.CastTo(CanAttack)
            if effect.targets:
                property = AttackProperty(is_basic_power=True, divided_evenly=divided_evenly, is_divided=canbe_divided)
                this.BasicAttack(effect.targets, effect, property=property)
            else:
                # For stunned
                atk_message = Message.WhenUnitWouldAttack(this.CastTo(Unit2), [], effect, property=AttackProperty(is_basic_power=True))
                atk_message.Send()
                assert atk_message.is_be_instead

        def this_can_attack(effect: 'Effect', message: 'Message2') -> bool:
            return CanAttack.IsType(effect.this.card.face) and \
                effect.this.card.face.IsCanAttack(effect)

        def get_min_size_fn(effect: 'Effect') -> int:
            if effect.this.CastTo(Unit2).IsStunned():
                return 0
            if divided_evenly:
                return len(effect.context.all_legal_targets)
            return 1

        def get_max_size_fn(effect: 'Effect') -> int:
            if effect.this.CastTo(Unit2).IsStunned():
                return 0
            if divided_evenly:
                return len(effect.context.all_legal_targets)
            if canbe_divided:
                return effect.this.CastTo(HasAttack).attack
            return 1

        if conditions == False:
            ability = AbilityFactory.WhenInYourPlayTurn(
                AbilityType.BasicPower,
                make_attack,
                conditions=[
                    lambda effect, message:
                        False,
                ],
            ).SetFuncName("ATK")
        else:
            ability = AbilityFactory.WhenInYourPlayTurn(
                AbilityType.BasicPower,
                make_attack,
                conditions=[
                    lambda effect, message:
                        this_can_attack(effect, message),
                    *conditions,
                ],
            ).SetFuncName("ATK").SetCostFunc(CostFunc.Exhaust("This"))
            if canbe_divided:
                ability.SetTarget(Enemy,
                    range=(get_min_size_fn, get_max_size_fn),
                    repeat_rules="Health"
                )
            else:
                ability.SetTarget(Enemy,
                    range=(get_min_size_fn, get_max_size_fn)
                )

            ability.SetTarget2("This", is_stunned=True)

        return ability

    @staticmethod
    def ThisCanThwart(*,
                      conditions: ConditionsType[Message.WhenPlayerInTurn]|Literal[False]=[],
                      canbe_divided: bool=False) -> 'Ability':
        from game.card.face.attribute.can_thwart import HasThwart, CanThwart
        from game.ability.cost_func import CostFunc
        from game.card.face.base import Scheme2
        from game.card.face.base import Unit2
        from game.ability.factory import AbilityFactory
        from game.card.face.attribute.can_thwart import ThwartProperty

        def make_thwart(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> Any:
            this = effect.this.CastTo(CanThwart)
            if effect.targets:
                property = ThwartProperty(is_basic_power=True, is_divided=canbe_divided)
                this.BasicThwart(effect.targets, effect, property=property)
            else:
                # For confused
                thw_message = Message.WhenUnitWouldThwart(this.CastTo(Unit2), [], effect, property=ThwartProperty(is_basic_power=True))
                thw_message.Send()
                assert thw_message.is_be_instead

        def get_min_size_fn(effect: 'Effect') -> int:
            if effect.this.CastTo(Unit2).IsConfused():
                return 0
            return 1

        def get_max_size_fn(effect: 'Effect') -> int:
            if effect.this.CastTo(Unit2).IsConfused():
                return 0
            if canbe_divided:
                return effect.this.CastTo(HasThwart).thwart
            return 1

        if conditions == False:
            ability = AbilityFactory.WhenInYourPlayTurn(
                AbilityType.BasicPower,
                make_thwart,
                conditions=[
                    lambda effect, message:
                        False
                ],
            ).SetFuncName("THW")

        else:
            def this_can_thwart(effect: 'Effect', message: 'Message2') -> bool:
                return CanThwart.IsType(effect.this.card.face) and \
                    effect.this.card.face.IsCanThwart(effect)

            ability = AbilityFactory.WhenInYourPlayTurn(
                AbilityType.BasicPower,
                make_thwart,
                conditions=[
                    this_can_thwart,
                    *conditions
                ],
            ).SetFuncName("THW").SetCostFunc(CostFunc.Exhaust("This"))

            if canbe_divided:
                ability.SetTarget(Scheme2,
                    range=(get_min_size_fn, get_max_size_fn),
                    repeat_rules="Threat"
                )
            else:
                ability.SetTarget(Scheme2,
                    range=(get_min_size_fn, get_max_size_fn)
                )

            ability.SetTarget2("This", is_confused=True)

        return ability

    @staticmethod
    def ThisCanDefense(*,
                        need_exhaust: Callable[['Effect'], bool]|None=None,
                        conditions: ConditionsType[Message.WhenUnitBeingAttack]=[]
                        ) -> 'Ability':
        from game.card.face.card_type import Ally
        from game.card.face.card_type import Hero
        from game.ability.factory import AbilityFactory
        from game.ability.cost_func import CostFunc
        from game.card.face.base import Friend
        def set_defender(effect: 'Effect', message: 'Message.WhenUnitBeingAttack') -> Any:
            this = effect.this.CastTo(Ally|Hero)
            this.BasicDefense(message, effect)

        def check_defender(effect: 'Effect', message: 'Message.WhenUnitBeingAttack') -> Any:
            if message.has_declare_defender:
                return False
            if message.defender == None:
                return True
            return effect.this == message.defender

        def check_must_defend_with_ally(effect: 'Effect', message: 'Message.WhenUnitBeingAttack') -> Any:
            assert effect.context.is_must_choose == False
            if message.would_atk_message.property.other_characters_cannot_defend:
                if effect.this != message.trigger:
                    return False
            if message.would_atk_message.property.must_defend_with_ally:
                if not Ally.IsType(effect.this):
                    effect.context.only_work_when_no_other_options = True
                else:
                    effect.SetMustChoose()
            return True

        return AbilityFactory.WhenUnitBeingAttack(
            AbilityType.BasicPower,
            Friend,
            None,
            set_defender,
            # has_defender=False,
            is_basic_attack=True,
            conditions=[
                lambda effect, message:
                    effect.this != message.attacker and \
                    effect.initiator == message.trigger.card.GetController(),
                check_must_defend_with_ally,
                check_defender,
                *conditions
            ],
        # If the attacked player does not defend, any other player may defend against the attack by exhausting their hero or an ally they control
        ).SetFuncName("DEF") \
        .SetCostFunc(CostFunc.Exhaust("This")) \
        .SetTarget("Attacker")

    @staticmethod
    def AfterAllyTakeConsequentialDamage(ability_type: 'AbilityType',
                                         which_ally: CardType,
                                         operation: OperationType[Message.AfterAllyTakeConsequentialDamage],
                                         *,
                                         from_performing: Literal["Attack", "Thwart"]|None=None,
                                         is_from_thwart: bool|None=None,
                                         defeated_face: CardType=None,
                                         conditions: ConditionsType[Message.AfterAllyTakeConsequentialDamage]=[],
                                         ) -> 'Ability':
        def check_which_ally(effect: 'Effect', message: 'Message.AfterAllyTakeConsequentialDamage') -> bool:
            return Condition.CheckWhichCard(which_ally, message.trigger, effect)

        def check_from_performing(effect: 'Effect', message: 'Message.AfterAllyTakeConsequentialDamage') -> bool:
            if from_performing == None:
                return True
            if from_performing == "Attack":
                return message.atk_message != None
            if from_performing == "Thwart":
                return message.thw_message != None

        def check_defeated_face(effect: 'Effect', message: 'Message.AfterAllyTakeConsequentialDamage') -> bool:
            if defeated_face == None:
                return True
            if message.atk_message:
                targets: List['Unit2'] = []
                for atk_message in message.atk_message.atk_messages:
                    if atk_message.has_defeated_target:
                        targets.append(atk_message.attacked)
                return Condition.CheckWhichCard(defeated_face, targets, effect)
            return False

        def check_is_from_thwart(effect: 'Effect', message: 'Message.AfterAllyTakeConsequentialDamage') -> bool:
            if is_from_thwart == None:
                return True
            return message.thw_message != None

        return Ability(
            ability_type,
            Message.AfterAllyTakeConsequentialDamage,
            [
                check_which_ally,
                check_from_performing,
                check_defeated_face,
                check_is_from_thwart,
                *conditions
            ],
            operation,
            is_local=which_ally == "This"
        )

    @staticmethod
    def ThisNotCountAllyLimit(conditions: ConditionsType[Message.CheckIfAllyCountLimit]=[],
                                change_on_event: EventType=OnEvent.NONE,
                                ) -> List['Ability']:
        from game.card.face.card_type import Ally

        def stinger(effect: 'Effect', message: 'Message.CheckIfAllyCountLimit') -> None:
            this = effect.this.CastTo(Ally)
            Unused(this)

            message.SetNotCountAlly(effect)

        abilities = [Ability(
            AbilityType.NonKeyword,
                Message.CheckIfAllyCountLimit,
                [
                    lambda effect, message:
                        effect.this == message.check_ally,
                    *conditions
                ],
                stinger,
                is_local=True,
            ).NoOutOfPlayLimit().SetFuncName("CheckAllyLimit")
        ]

        if change_on_event != OnEvent.NONE:

            def check_ally_limit(effect: 'Effect', message: 'Message2') -> None:
                this = effect.this.CastTo(Ally)
                Unused(this)

                # Fix -1508578192-
                if not this.GetControlByPlayer().GetIdentity().card.state.is_flipping:
                    this.GetControlByPlayer().limit_ally.CheckLimit([])

            abilities += Condition.GetEventAbilities(
                change_on_event,
                AbilityType.NonKeyword,
                lambda effect, message:
                    True,
                check_ally_limit,
            )

        return abilities


    ################################################################################
    # Use Basic Power
    @staticmethod
    def WhenUnitUseBasicPower(ability_type: 'AbilityType',
                              which_unit: CardType|Literal["You"],
                              operation: OperationType[Message.WhenUnitUseBasicPower],
                              *,
                              conditions: ConditionsType[Message.WhenUnitUseBasicPower]=[],
                              powers: List["CardFace.BASIC_POWER"]|None=None,
                              control_by: Literal["You"]|None=None,
                              ) -> 'Ability':
        def check_which_unit(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> bool:
            rule = Condition.GetYouRule(which_unit, identity=True)
            return Condition.CheckWhichCard(rule, message.trigger, effect)

        def check_control_by(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> bool:
            if control_by == None:
                return True
            return effect.initiator == message.trigger.GetControlBy()

        def check_power(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> bool:
            return Condition.CheckBasicPower(powers, message)

        return Ability(
            ability_type,
            Message.WhenUnitUseBasicPower,
            [
                check_which_unit,
                check_control_by,
                check_power,
                *conditions
            ],
            operation,
            is_local=which_unit == "This"
        )

    @staticmethod
    def AfterUnitUseBasicPower(ability_type: 'AbilityType',
                               which_unit: CardType|Literal["You"],
                               operation: OperationType[Message.AfterUnitUseBasicPower],
                               *,
                               conditions: ConditionsType[Message.AfterUnitUseBasicPower]=[],
                               powers: List["CardFace.BASIC_POWER"]|Literal["Hero"]|None=None,
                               use_message: 'Message.WhenUnitUseBasicPower|None'=None,
                               ) -> 'Ability':
        """
        performs a basic xxx
        """

        if powers == "Hero":
            powers = ["ATK", "THW", "DEF"]

        def check_which_unit(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> bool:
            you_rule = Condition.GetYouRule(which_unit, identity=True)
            return Condition.CheckWhichCard(you_rule, message.trigger, effect)

        def check_power(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> bool:
            return Condition.CheckBasicPower(powers, message)

        def check_use_message(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> bool:
            if use_message == None:
                return True
            return use_message == message.pre_message

        return Ability(
            ability_type,
            Message.AfterUnitUseBasicPower,
            [
                check_which_unit,
                check_power,
                check_use_message,
                *conditions
            ],
            operation,
            is_local=which_unit == "This"
        )

    ################################################################################
    # Ally
    @staticmethod
    def WhenAllyWouldTakeConsequentialDamage(which_unit: CardType,
                                             *,
                                            after_attacking_minion: bool|None=None,
                                            after_thwart_side_scheme: bool|None=None,
                                            update_damage: Callable[['Effect'], int]|int=0) -> 'Ability':
        from game.card.face.card_type import Minion
        from game.card.face.base import SchemeSide2

        def check_who(effect: 'Effect', message: 'Message.WhenAllyWouldTakeConsequentialDamage') -> bool:
            return Condition.CheckWhichCard(which_unit, message.trigger, effect)

        def check_after_after_attacking_minion(effect: 'Effect', message: 'Message.WhenAllyWouldTakeConsequentialDamage') -> bool:
            if after_attacking_minion:
                if message.atk_message == None:
                    return False
                for target in message.atk_message.attacked_targets:
                    if Minion.IsType(target):
                        return True
                return False
            return True

        def check_after_thwart_side_scheme(effect: 'Effect', message: 'Message.WhenAllyWouldTakeConsequentialDamage') -> bool:
            if after_thwart_side_scheme:
                if message.thw_message == None:
                    return False
                from game.operate.filter import Filter
                return not not Filter.ByType(message.thw_message.schemes, SchemeSide2)
            return True

        def update_damage_action(effect: 'Effect', message: 'Message.WhenAllyWouldTakeConsequentialDamage') -> None:
            if isinstance(update_damage, int):
                value = update_damage
            else:
                value = update_damage(effect)
            if value < 0:
                message.ReduceDamage(-1 * value, effect)
            else:
                message.IncreaseDamage(value, effect)

        return Ability(
            AbilityType.NonKeyword,
            Message.WhenAllyWouldTakeConsequentialDamage,
            [
                check_who,
                check_after_after_attacking_minion,
                check_after_thwart_side_scheme
            ],
            update_damage_action,
            is_local=which_unit == "This"
        )


