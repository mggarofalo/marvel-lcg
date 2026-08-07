from . import *

class AbilityFactoryCounter:

    @staticmethod
    def ThisEnterPlayWithCounters(counters: int|str,
                                  name: 'CardFace.COUNTER',
                                  ) -> 'Ability':
        from game.card.face.attribute.can_place_counter import CanPlaceCounter
        from game.ability.factory import AbilityFactory
        from game.operate.faces import Faces
        from game.operate.worlds import Worlds

        def with_counters(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
            this = effect.this.CastTo(CanPlaceCounter)
            if isinstance(counters, int):
                new_counters = counters
            else:
                new_counters = int(counters.replace("*", "")) * Worlds.GetPlayerNumIcon(effect)
            Faces.PlaceCountersOn([this], new_counters, name, effect)

        return AbilityFactory.AfterCardEnterPlay(
            AbilityType.NonKeyword,
            "This",
            with_counters
        )

    ################################################################################
    # Counter / Token
    @staticmethod
    def AfterCardRemovedCounter(ability_type: 'AbilityType',
                                which_card: CardType,
                                counter_name: 'CardFace.COUNTER|None',
                                operation: OperationType[Message.AfterCardRemovedCounter],
                                *,
                                is_last_counter: bool|None=None,
                                conditions: ConditionsType[Message.AfterCardRemovedCounter]=[],
                                ) -> 'Ability':
        from game.card.face.attribute.can_place_counter import CanPlaceCounter

        def check_which_card(effect: 'Effect', message: 'Message.AfterCardRemovedCounter') -> bool:
            return Condition.CheckWhichCard(which_card, message.trigger, effect)
    
        def check_is_last_counter(effect: 'Effect', message: 'Message.AfterCardRemovedCounter') -> bool:
            if is_last_counter == None:
                return True
            left_counter = message.trigger.CastTo(CanPlaceCounter).GetCounters(message.counter_name)
            if is_last_counter:
                return left_counter == 0
            else:
                return left_counter != 0

        def check_counter_name(effect: 'Effect', message: 'Message.AfterCardRemovedCounter') -> bool:
            if counter_name == None:
                return True
            return message.counter_name == counter_name

        return Ability(
            ability_type,
            Message.AfterCardRemovedCounter,
            [
                check_which_card,
                check_is_last_counter,
                check_counter_name,
                *conditions
            ],
            operation,
            is_local=which_card == "This"
        )

    @staticmethod
    def IfThereIsNoCounterHere(counter_name: 'CardFace.COUNTER|None',
                               operation: OperationType[Message.AfterCardRemovedCounter],
                               ) -> 'Ability':
        return AbilityFactoryCounter.AfterCardRemovedCounter(
            AbilityType.NonKeyword,
            "This",
            counter_name,
            operation,
            is_last_counter=True,
        )

    @staticmethod
    def WhenTheLastCounterIsRemovedFromHere(ability_type: 'AbilityType',
                                            counter_name: 'CardFace.COUNTER|None',
                                            operation: OperationType[Message.AfterCardRemovedCounter],
                                            ) -> 'Ability':
        return AbilityFactoryCounter.AfterCardRemovedCounter(
            ability_type,
            "This",
            counter_name,
            operation,
            is_last_counter=True,
        )

    @staticmethod
    def AfterCounterPlacedOn(ability_type: 'AbilityType',
                            which_card: CardType,
                            counter_name: 'CardFace.COUNTER|None',
                            operation: OperationType[Message.AfterCardPlacedCounter],
                            *,
                            conditions: ConditionsType[Message.AfterCardPlacedCounter]=[],
                            at_least: Callable[['Effect'], int]|int|Literal["2*", "3*", "5*", "6*", "9*", "10*"]|None=None
                            ) -> 'Ability':
        from game.card.face.attribute.can_place_counter import CanPlaceCounter
        from game.operate.worlds import Worlds

        def check_which_card(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> bool:
            return Condition.CheckWhichCard(which_card, message.trigger, effect)

        def check_token_name(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> bool:
            if counter_name:
                return message.counter_name == counter_name
            return True

        def check_at_least(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> bool:
            if at_least:
                this = message.trigger.CastTo(CanPlaceCounter)

                if isinstance(at_least, str):
                    counter = Worlds.ConvertPerPlayerIconToInt(at_least, effect)
                elif isinstance(at_least, int):
                    counter = at_least
                else:
                    counter = at_least(effect)
                assert counter_name != None
                return this.GetCounters(counter_name) >= counter
            return True

        return Ability(
            ability_type,
            Message.AfterCardPlacedCounter,
            [
                check_which_card,
                check_token_name,
                check_at_least,
                *conditions
            ],
            operation,
            is_local=which_card == "This"
        )

    @staticmethod
    def IfThereAreAtLeastCounterHere(at_least: Callable[['Effect'], int]|int|Literal["2*", "3*", "5*", "10*"],
                                    counter_name: 'CardFace.COUNTER|None',
                                    operation: OperationType[Message.AfterCardPlacedCounter],
                                    *,
                                    conditions: ConditionsType[Message.AfterCardPlacedCounter]=[],
                                    ) -> 'Ability':
        return AbilityFactoryCounter.AfterCounterPlacedOn(
            AbilityType.NonKeyword,
            "This",
            counter_name,
            operation,
            conditions=conditions,
            at_least=at_least,
        )

    @staticmethod
    def IfCardHasOrMoreCounters(which_card: CardType,
                                at_least: Callable[['Effect'], int]|int,
                                counter_name: 'CardFace.COUNTER|None',
                                operation: OperationType[Message.AfterCardPlacedCounter],
                                *,
                                conditions: ConditionsType[Message.AfterCardPlacedCounter]=[],
                                ) -> 'Ability':
        return AbilityFactoryCounter.AfterCounterPlacedOn(
            AbilityType.NonKeyword,
            which_card,
            counter_name,
            operation,
            conditions=conditions,
            at_least=at_least,
        )

    ################################################################################
    @staticmethod
    def WhenCardWouldBePlacedCounter(ability_type: 'AbilityType',
                                     which_face: Literal["Another", "This"]|'CardFinder'|None,
                                     counter_name: 'CardFace.COUNTER|None',
                                     operation: OperationType[Message.WhenCardWouldBePlacedCounter],
                                     *,
                                     conditions: ConditionsType[Message.WhenCardWouldBePlacedCounter]=[],
                                     ) -> 'Ability':

        def check_to_where(effect: 'Effect', message: Message.WhenCardWouldBePlacedCounter) -> bool:
            if which_face == None:
                return True
            elif which_face == "Another":
                return effect.this != message.trigger
            elif which_face == "This":
                return effect.this == message.trigger
            else:
                return which_face.Check(message.trigger)

        def check_counter_name(effect: 'Effect', message: Message.WhenCardWouldBePlacedCounter) -> bool:
            if counter_name:
                return message.counter_name == counter_name
            return True

        return Ability(
            ability_type,
            Message.WhenCardWouldBePlacedCounter,
            [
                check_to_where,
                check_counter_name,
                *conditions
            ],
            operation,
            is_local=which_face == "This"
        )

    @staticmethod
    def MaxCounterHere(size: int,
                        counter_name: 'CardFace.COUNTER|None',
                        *,
                        conditions: ConditionsType[Message.WhenCardWouldBePlacedCounter]=[],
                        ) -> 'Ability':
        from game.ability.factory import AbilityFactory

        def action(effect: 'Effect', message: Message.WhenCardWouldBePlacedCounter) -> None:
            message.maximum = size

        return AbilityFactory.WhenCardWouldBePlacedCounter(
            AbilityType.NonKeyword,
            "This",
            counter_name,
            action,
            conditions=conditions
        )

