from . import *

class AbilityFactoryStatus:

    @staticmethod
    def AfterStatusCardPlaceOn(ability_type: 'AbilityType',
                                place_on_who: CardType,
                                status: "CardFace.STATUS|List[CardFace.STATUS]|None",
                                operation: OperationType[Message.AfterStatusCardPlaceOn],
                                *,
                                conditions: ConditionsType[Message.AfterStatusCardPlaceOn]=[],
                                ) -> 'Ability':

        def check_place_on_who(effect: 'Effect', message: 'Message.AfterStatusCardPlaceOn') -> bool:
            return Condition.CheckWhichCard(place_on_who, message.trigger, effect)

        def check_status(effect: 'Effect', message: 'Message.AfterStatusCardPlaceOn') -> bool:
            if status == None:
                return True
            if not isinstance(status, list):
                check_status = [status]
            else:
                check_status = status
            return message.status.name in check_status

        return Ability(
            ability_type,
            Message.AfterStatusCardPlaceOn,
            [
                check_place_on_who,
                check_status,
                *conditions
            ],
            operation,
            is_local=place_on_who == "This"
        )

    @staticmethod
    def AfterStatusDiscardFrom(ability_type: 'AbilityType',
                                from_who: CardType,
                                status: "CardFace.STATUS",
                                operation: OperationType[Message.AfterStatusDiscardFrom],
                                *,
                                conditions: ConditionsType[Message.AfterStatusDiscardFrom]=[],
                                ) -> 'Ability':

        def check_from_who(effect: 'Effect', message: 'Message.AfterStatusDiscardFrom') -> bool:
            # Fix "32001a" flip trigger "32004"
            if message.trigger.card.state.is_flipping:
                return False
            return Condition.CheckWhichCard(from_who, message.trigger, effect, check_status=True)

        def check_status(effect: 'Effect', message: 'Message.AfterStatusDiscardFrom') -> bool:
            if status:
                return message.status.name == status
            return True

        return Ability(
            ability_type,
            Message.AfterStatusDiscardFrom,
            [
                check_from_who,
                check_status,
                *conditions
            ],
            operation,
            is_local=from_who == "This"
        )
    
    @staticmethod
    def ThisCanHaveAdditionalTough(num: int|Literal["Any"]) -> 'Ability':
        from game.ability.factory import AbilityFactory
        from game.operate.effects import Effects
        from game.card.face.base import Unit2
        from game.card.face.card_type import StatusCard

        def action(effect: 'Effect', diff: int, curr: int) -> None:
            this = effect.this.CastTo(Unit2)

            if curr > 0:
                this.effect.RegisterTemp(
                    Ability(
                        AbilityType.NonKeyword,
                        Message.GettingUnitAdditionalTough,
                        [
                            Condition2.ThisIsTrigger
                        ],
                        lambda effect, message:
                            message.SetReturn(9999) if num == "Any" else message.SetReturn(num)
                    ).SetName("ThisCanHaveAdditionalTough"),
                    unregister_after_exec=False
                )
            else:
                effects = this.effect.Find(name="ThisCanHaveAdditionalTough")
                Effects.UnRegister(effects)
                exceed = this.tough - this.tough_max
                if exceed > 0:
                    this.DiscardTough(effect, rule=exceed)

        return AbilityFactory.WhileValid(
            OnEvent.CardInPlay(StatusCard),
            lambda effect, message: True,
            lambda effect, last_value:
                effect.this.IsInPlay(),
            action
        )

    @staticmethod
    def UnitCannotBeConfused(which_unit: CardTypeMin) -> 'Ability':
        from game.ability.factory import AbilityFactory
        from game.buff.buff import BuffCannotBeConfused

        return AbilityFactory.GiveBuffToInPlayWhenApplyThis(
            which_unit,
            buffs=[BuffCannotBeConfused]
        )

    @staticmethod
    def UnitCannotBeStunned(which_unit: CardTypeMin) -> 'Ability':
        from game.ability.factory import AbilityFactory
        from game.buff.buff import BuffCannotBeStunned

        return AbilityFactory.GiveBuffToInPlayWhenApplyThis(
            which_unit,
            buffs=[BuffCannotBeStunned]
        )

    @staticmethod
    def WhenUnitWouldBeStatus(ability_type: 'AbilityType',
                            which_unit: CardType,
                            what_status: List["CardFace.STATUS"],
                            operation: OperationType[Message.WhenStatusWouldCardPlaceOn],
                            *,
                            conditions: ConditionsType[Message.WhenStatusWouldCardPlaceOn]=[],
                            ) -> 'Ability':
        def check_which_unit(effect: 'Effect', message: Message.WhenStatusWouldCardPlaceOn) -> bool:
            return Condition.CheckWhichCard(which_unit, message.trigger, effect)

        def check_what_status(effect: 'Effect', message: Message.WhenStatusWouldCardPlaceOn) -> bool:
            return message.status_name in what_status

        return Ability(
            ability_type,
            Message.WhenStatusWouldCardPlaceOn,
            [
                check_which_unit,
                check_what_status,
                *conditions,
            ],
            operation
        )

