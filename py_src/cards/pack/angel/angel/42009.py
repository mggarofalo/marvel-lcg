from . import *

# * Worthington Industries

def GetAbilities() -> Sequence['Ability']:

    def worthington_industries(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)

        if initiator.IsAlterEgo():
            initiator.DrawUp(1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            worthington_industries,
            conditions=[
                lambda effect, message:
                    not effect.GetInitiator().IsAlterEgo(),
            ]
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(trait="AERIAL", from_where=["YourDiscardPile"]),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            worthington_industries,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().IsAlterEgo(),
            ]
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(trait="AERIAL", from_where=["YourDiscardPile"])
        .SetHasNoTargetEffect(),
    ]

