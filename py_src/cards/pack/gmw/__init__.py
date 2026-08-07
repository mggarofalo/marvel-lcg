from cards.pack import *

def ExhaustTheMilano(condition: Callable[['Effect', Message.WhenPlayerInTurn], bool]|None,
                     operation: Callable[[Effect, Message.WhenPlayerInTurn], Any]):
    if condition == None:
            condition = lambda effect, message: True

    return AbilityFactory.WhenInYourPlayTurn(
        AbilityType.FirstPlayerAction,
        operation,
        conditions=[condition],
    ).SetCostFunc(CostFunc.Exhaust(name="Milano"))

def ExhaustTheMilanoThenForChoiceAbility(name: str,
                                     operation: Callable[[Sequence['CardFace']], Any],
                                     condition: bool=True,
                                     ):
    return AbilityFactory.ForChoiceAbility(
        name,
        operation,
        condition=condition,
    ).SetCostFunc(CostFunc.Exhaust(name="Milano"))

def ExhaustTheMilanoForChoiceAbility(effect: 'Effect'):
    return AbilityFactory.ForChoiceAbility(
        "Exhaust the Milano",
        lambda targets:
            Faces.ExhaustAll(targets, effect),
    ).SetTarget(name="Milano", canbe_exhaust=True)

def ExhaustMilanoRemoveThreatFromThisScheme(number: int):
    def remove_scheme(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Scheme2)
        Unused(this)
        this.RemoveThreatFromSchemes(effect.targets, number, effect)

    return ExhaustTheMilano(
        lambda effect, message: True,
        remove_scheme
    ).SetTarget("This", has_threat=True)

