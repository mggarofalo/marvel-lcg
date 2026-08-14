from . import *

class AbilityFactoryForChoice:

    @staticmethod
    def ForChoiceAbilityInternal(name: str,
                                 res: 'Cost|None',
                                 operation: Callable[[Sequence['CardFace'], 'Resources', 'Effect'], Any]|None,
                                 *,
                                 conditions: ConditionsType[Message.WhenPlayerChooseAbility],
                                 targets_is_exhaust_cost: bool=False,
                                 ) -> 'Ability':
        from game.ability.cost_func import CostFunc

        if operation == None:
            operation = lambda targets, res, ability: None

        def action(effect: 'Effect', message: 'Message.WhenPlayerChooseAbility'):
            if targets_is_exhaust_cost:
                targets = effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards
            else:
                targets = effect.targets
            operation(targets, effect.GetPaidResources(), effect)

        ability = Ability(
            AbilityType.ChooseAbility,
            Message.WhenPlayerChooseAbility,
            [
                *conditions
            ],
            action
        ).SetName(name).NoOutOfPlayLimit()

        if res != None:
            ability.SetCost(res, is_choose_ability=True)

        return ability

    @staticmethod
    def ForChoiceAbility(name: str,
                        operation: Callable[[Sequence['CardFace']], Any]|None=None,
                        *,
                        condition: bool=True,
                        targets_is_exhaust_cost: bool=False,
                        ) -> 'Ability':
        def check_condition(effect: 'Effect', message: 'Message.WhenPlayerChooseAbility') -> bool:
            return condition
        if operation == None:
            operation = lambda targets: None
        return AbilityFactoryForChoice.ForChoiceAbilityInternal(
            name,
            None,
            lambda targets, res, effect:
                operation(targets),
            conditions=[check_condition],
            targets_is_exhaust_cost=targets_is_exhaust_cost
        ).SetDefault()

    @staticmethod
    def ForChoiceAbility3(name: str,
                        operation: Callable[[Sequence['CardFace'], 'Effect'], Any]|None=None,
                        *,
                        condition: bool=True,
                        ) -> 'Ability':
        def check_condition(effect: 'Effect', message: 'Message.WhenPlayerChooseAbility') -> bool:
            return condition
        if operation == None:
            operation = lambda targets, effect: None
        return AbilityFactoryForChoice.ForChoiceAbilityInternal(
            name,
            None,
            lambda targets, res, effect:
                operation(targets, effect),
            conditions=[check_condition]
        ).SetDefault()

    @staticmethod
    def ForChoiceAbility2(name: str,
                        operation: Callable[[Sequence['CardFace'], 'Ability'], Any]|None=None,
                        *,
                        condition: bool=True,
                        ) -> 'Ability':
        def check_condition(effect: 'Effect', message: 'Message.WhenPlayerChooseAbility') -> bool:
            return condition
        if operation == None:
            operation = lambda targets, ability: None
        return AbilityFactoryForChoice.ForChoiceAbilityInternal(
            name,
            None,
            lambda targets, res, effect:
                operation(targets, effect.ability),
            conditions=[check_condition]
        ).SetDefault()

    @staticmethod
    def Otherwise(operation: Callable[[Sequence['CardFace']], Any]) -> 'Ability':
        def add_otherwise(effect: 'Effect', message: 'Message.WhenPlayerChooseAbility') -> bool:
            effect.context.only_work_when_no_other_options = True
            return True

        return AbilityFactoryForChoice.ForChoiceAbilityInternal(
            "Otherwise",
            None,
            lambda targets, res, effect:
                operation(targets),
            conditions=[add_otherwise]
        ).SetDefault()

    @staticmethod
    def ForChoiceAbilityWithCost(cost: 'Cost',
                                 name: str|None=None,
                                 operation: Callable[[Sequence['CardFace'], 'Resources'], Any]|None=None,
                                 *,
                                 conditions: ConditionsType[Message.WhenPlayerChooseAbility]=[],
                                 ) -> 'Ability':
        """An option whose resources are a **cost**: "spend X -> do Y".

        A cost is paid in full or the option is not taken, so this option is
        offered only when the player can pay it and is never partially
        resolved. Use `ForChoiceAbilityToSpend` when the printed option is the
        spending itself.
        """
        if operation == None:
            operation = lambda targets, res: None

        if name == None:
            name = f"Spend {cost.GetSpendText()}"

        return AbilityFactoryForChoice.ForChoiceAbilityInternal(
            name,
            cost,
            lambda targets, res, effect:
                operation(targets, res),
            conditions=conditions
        ).SetFuncName("ForChoiceAbilityWithCost")

    @staticmethod
    def ForChoiceAbilityToSpend(cost: 'Cost',
                                name: str|None=None,
                                *,
                                conditions: ConditionsType[Message.WhenPlayerChooseAbility]=[],
                                ) -> 'Ability':
        """An option whose resources are the **effect**: "either spend X or ...".

        Sonic Boom (01123) is the card the distinction is written on: *"you must
        choose an option that you can fulfill. If you cannot fulfill either
        option, then you must do as much as you can, which typically means
        discarding one or two different resource icons from your hand."* An
        effect is resolved as far as it can be; a cost is not. So this option,
        and only this option, can be forced to resolve at a reduced cost when
        the whole choice has nothing anyone can fulfil.

        Which of the two a card prints is the card's to say -- there is no way
        to read it off the resources -- so it is said here, at the call site.
        """
        return AbilityFactoryForChoice.ForChoiceAbilityWithCost(
            cost,
            name,
            conditions=conditions,
        ).SetSpendIsTheEffect()

