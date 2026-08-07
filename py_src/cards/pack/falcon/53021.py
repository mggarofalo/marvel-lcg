from . import *

# Resource Reserve

def GetAbilities() -> Sequence['Ability']:

    def resource_reserve(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.TuckCardUnderHere(effect.targets, effect, peek=True)


    return [
        *AbilityFactory.YouMayPlayCardLikeInHand(
            AbilityType.NonKeyword,
            None,
            from_where="ThisPlacedCard",
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            resource_reserve,
            conditions=[
                lambda effect, message:
                    effect.this.GetPlacedCardArea().GetSize() == 0
            ]
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("YourHandCards", card_type=Resource),
    ]

