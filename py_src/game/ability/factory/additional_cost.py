from . import *

from game.message.sender.sender import CanBeInstead

def CostFunction(effect: 'Effect', trigger: 'CardFace', message: 'CanBeInstead', cost_func: 'CostFunc.Base'):
    from game.player import Player
    if not Player.IsType(trigger.GetControlBy()):
        return

    player = trigger.GetControlByPlayer()
    if not cost_func.PayCost(effect, player):
        message.SetBeInstead(effect)

class AbilityFactoryAdditionalCost:

    @staticmethod
    def AdditionalCostToThwartThisScheme(who_thwart: CardType,
                                        cost_func: 'CostFunc.Base') -> 'Ability':
        from game.ability.factory import AbilityFactory

        def cost(effect: 'Effect', message: 'Message.WhenUnitWouldThwart') -> None:
            CostFunction(effect, message.trigger, message, cost_func)

        return AbilityFactory.WhenUnitWouldThwart(
            AbilityType.NonKeyword,
            who_thwart,
            cost,
            thwarted_scheme="This",
        )


    @staticmethod
    def AdditionalCostToChangeForm(which_unit: CardType|Literal["You"],
                                    cost_func: 'CostFunc.Base',
                                    to_form: CardType=None,
                                    during_your_turn: bool|None=None) -> 'Ability':
        from game.ability.factory import AbilityFactory

        def cost(effect: 'Effect', message: 'Message.WhenUnitWouldChangeForm') -> None:
            CostFunction(effect, message.trigger, message, cost_func)

        return AbilityFactory.WhenUnitWouldChangeForm(
            AbilityType.NonKeyword,
            which_unit,
            cost,
            to_form=to_form,
            during_your_turn=during_your_turn
        )

    @staticmethod
    def AdditionalCostToAttack(which_unit: Type['CardFace']|'CardFinder'|Literal["This"],
                                cost_func: 'CostFunc.Base',
                                # attacked: CardType=None,
                                # is_basic_attack: bool|None=None,
                                is_basic_attack: Literal[True]=True,
                                ) -> 'Ability':
        from game.ability.factory import AbilityFactory

        abilities = AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            which_unit,
            additional_cost_for_basic_attack=cost_func,
        )
        assert len(abilities) == 1
        return abilities[0]

    @staticmethod
    def AdditionalCostToThwart(which_unit: Type['CardFace']|'CardFinder'|Literal["This"],
                                cost_func: 'CostFunc.Base',
                                ) -> 'Ability':
        from game.ability.factory import AbilityFactory

        abilities = AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            which_unit,
            additional_cost_for_basic_thwart=cost_func,
        )
        assert len(abilities) == 1
        return abilities[0]

    @staticmethod
    def AdditionalCostToDefense(which_unit: Type['CardFace']|'CardFinder'|Literal["This"],
                                cost_func: 'CostFunc.Base',
                                ) -> 'Ability':
        from game.ability.factory import AbilityFactory

        abilities = AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            which_unit,
            additional_cost_for_basic_defend=cost_func,
        )
        assert len(abilities) == 1
        return abilities[0]

    @staticmethod
    def AdditionalCostToReady(which_card: CardType,
                                cost_func: 'CostFunc.Base',
                                control_by: PlayerType="AnyPlayer",
                                ) -> 'Ability':
        from game.ability.factory import AbilityFactory

        def cost(effect: 'Effect', message: 'Message.WhenCardWouldReady') -> None:
            CostFunction(effect, message.trigger, message, cost_func)

        return AbilityFactory.WhenCardWouldReady(
            AbilityType.NonKeyword,
            which_card,
            cost,
            control_by=control_by
        )

