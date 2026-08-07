from . import *

class AbilityFactoryAttachment:

    ################################################################################
    # Attachment
    @staticmethod
    def PlayerActionToDiscardThis(ability_type: 'AbilityType',
                                  *,
                                  conditions: ConditionsType[Message.WhenPlayerInTurn]=[],
                                  ex_operation: OperationType[Message.WhenPlayerInTurn]=lambda effect, message: None,
                                  ) -> 'Ability':
        from game.ability.factory import AbilityFactory
        from game.operate.faces import Faces

        def action_to_discard(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
            ex_operation(effect, message)

            this = effect.this
            Faces.DiscardAll([this], effect)

        return AbilityFactory.WhenInYourPlayTurn(
            ability_type,
            action_to_discard,
            conditions=conditions,
        )

    @staticmethod
    def PlayerActionToRemoveThisFromGame(ability_type: 'AbilityType',
                                         *,
                                         conditions: ConditionsType[Message.WhenPlayerInTurn]=[]
                                         ) -> 'Ability':
        from game.ability.factory import AbilityFactory
        def remove_this_from_game(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
            this = effect.this
            from game.operate.faces import Faces
            Faces.RemoveAllFromGame([this], effect)

        return AbilityFactory.WhenInYourPlayTurn(
            ability_type,
            remove_this_from_game,
            conditions=conditions,
        )

    @staticmethod
    # TODO: Clean
    def AttachToFaceWhenPutIntoPlay(target: Literal["YourIdentity", "YourHero", "YourAlly", "YourLeader", "EnemyLeader", "Leader|Minion"]|Type['CardFace']|'CardFinder',
                                    *,
                                    highest_printed_hp: bool=False,
                                    fewest_remaining_hp: bool=False,
                                    most_remaining_hp: bool=False,
                                    lowest_atk: bool=False,
                                    highest_atk: bool=False,
                                    highest_sch: bool=False,
                                    lowest_sch: bool=False,
                                    most_traits: bool=False,
                                    highest_printed_atk: bool=False,
                                    lowest_order: bool=False,
                                    highest_order: bool=False,
                                    corresponding: Literal["ActiveVillain"]|None=None,
                                    ##
                                    conditions: ConditionsType[Message.WhenCardPutIntoPlay]=[],
                                    # 
                                    without_another_copy: bool|None=None,
                                    otherwise_attach_to: 'Selector'|Type['TC']|Literal["EnemyLeader", "YourHero"]|None=None,
                                    if_cannot_attach_to: 'Selector'|Type['TC']|Literal["EnemyLeader", "YourHero"]|None=None,
                                    if_cannot_gain_surge: bool=False,
                                    if_cannot_operation: Callable[['Effect', 'Player'], None]|None=None,
                                    #
                                    when_attach_operation: Callable[['CardFace', 'Effect'], Any]|None=None,
                                    when_attach_exhaust_attached: bool|None=None,
                                    ) -> 'Ability':
        from game.operate.faces import Faces
        from game.ability.factory import AbilityFactory
        from game.card.card_finder import CardFinder

        def check_condition(effect: 'Effect', unit: 'CardFace') -> bool:
            from game.card.face.card_type import Attachment
            from game.card.face.base import Asset2
            this = effect.this.CastTo(Attachment)
            def check_without_another_copy():
                if without_another_copy == None:
                    return True
                if unit.GetInventoryDeck().FindCard(name=effect.this.name, card_type=type(effect.this)) == None:
                    return True
                return False

            return check_without_another_copy() and this.CastTo(Asset2).CanAttachTo(unit)

        if if_cannot_attach_to == None and otherwise_attach_to != None:
            if_cannot_attach_to = otherwise_attach_to

        if if_cannot_attach_to == None:
            otherwise_selector = None
        elif isinstance(if_cannot_attach_to, Selector):
            otherwise_selector = if_cannot_attach_to
        elif if_cannot_attach_to == "EnemyLeader":
            otherwise_selector = Select.From("EnemyLeader")
        elif if_cannot_attach_to == "YourHero":
            otherwise_selector = Select.From("YourHero")
        else:
            otherwise_selector = Select.From(CardFinder(card_type=if_cannot_attach_to))

        if target == "YourIdentity":
            selector = Select.From("YourIdentity")
        elif target == "YourHero":
            selector = Select.From("YourHero")
        elif target == "YourAlly":
            selector = Select.From("YourAlly")
        elif target == "Leader|Minion":
            selector = Select.From("Leader|Minion")
        elif target == "YourLeader":
            selector = Select.From("YourLeader")
        elif target == "EnemyLeader":
            selector = Select.From("EnemyLeader")
        else:
            if isinstance(target, type):
                finder = CardFinder(card_type=target)
            else:
                finder = target
            selector = Select.From(finder,
                highest_printed_hp=highest_printed_hp,
                fewest_remaining_hp=fewest_remaining_hp,
                most_remaining_hp=most_remaining_hp,
                lowest_atk=lowest_atk,
                highest_atk=highest_atk,
                highest_sch=highest_sch,
                lowest_sch=lowest_sch,
                most_traits=most_traits,
                highest_printed_atk=highest_printed_atk,
                lowest_order=lowest_order,
                highest_order=highest_order,
                corresponding=corresponding,
            )

        selector.selector_filter.AddParameter(check_effect_fn=check_condition)

        selector.selector_range.SetRange("All")
        if otherwise_selector:
            otherwise_selector.selector_range.SetRange("All")

        def if_cannot_action(effect: 'Effect', player: 'Player') -> None:
            from cards.pack import ThisCardGainSurge
            if if_cannot_operation:
                if_cannot_operation(effect, player)
            if if_cannot_gain_surge:
                ThisCardGainSurge(effect)

        def when_attach_action(face: 'CardFace', effect: 'Effect'):
            if when_attach_operation:
                when_attach_operation(face, effect)
            if when_attach_exhaust_attached:
                Faces.ExhaustAll([face], effect)

        return AbilityFactory.AttachToFaceWhenPutIntoPlayInternal(
            selector,
            conditions=[
                Condition2.ThisIsTrigger,
                # Hasn't attached
                lambda effect, message:
                    not effect.this.card.area.flags.is_upgrades,
                *conditions
            ],
            otherwise_selector=otherwise_selector,
            if_cannot_operation=if_cannot_action,
            when_attach_operation=when_attach_action,
        )

