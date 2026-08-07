from . import *

# * Limbo

def GetAbilities() -> Sequence['Ability']:

    def limbo(effect: 'Effect', message: 'Message.WhenPlayerInTurn|Message.AfterPhaseBegin') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        face = initiator.player_deck.GetTop()
        if face:
            initiator.GainCard(face, effect)
            Faces.MoveAllToDeck(effect.targets, initiator.player_deck, "Top", effect)

    return [
        AbilityFactory.AfterPhaseBegin(
            AbilityType.Response,
            "Villain",
            limbo
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(from_where=["YourHandCards"]),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            limbo
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(from_where=["YourHandCards"]),
    ]

