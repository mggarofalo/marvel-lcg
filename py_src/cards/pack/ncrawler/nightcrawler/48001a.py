from . import *

# * Nightcrawler

def GetAbilities() -> Sequence['Ability']:

    def nightcrawler(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReturnToHand(effect.targets, initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            nightcrawler
        ).SetName("Rapid Teleportation")
        .SetCost(Cost("1"))
        .SetTarget(name="Bamf!", from_where=["YourDiscardPile"])
        .LimitOncePerPhase(),
    ]

