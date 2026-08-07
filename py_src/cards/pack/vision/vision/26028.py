from . import *

# Corrupted Programming

def GetAbilities() -> Sequence['Ability']:

    def corrupted_programming(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)

        Faces.RemoveAllFromGame([this], effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(form="Mass", card_type=Upgrade),
            treat_as_blank=True,
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            corrupted_programming
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity"))
    ]

